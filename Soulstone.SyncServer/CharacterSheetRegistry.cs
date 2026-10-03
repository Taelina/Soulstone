using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Soulstone.SyncServer;

public sealed record StoredCharacterSheet(string CharacterName, string WorldName, string Payload, DateTimeOffset UpdatedAtUtc);

public sealed class CharacterSheetRegistry(PublicationDatabase database, TimeProvider timeProvider, ILogger<CharacterSheetRegistry> logger)
{
    public static readonly TimeSpan SheetLifetime = TimeSpan.FromDays(7);
    public const int MaxPayloadLength = PublicationSecurity.MaximumPayloadBytes;

    public bool TryStore(string characterName, string? worldName, string payload, string? ownerToken = null) =>
        Store(characterName, worldName, payload, ownerToken) == RegistryWriteResult.Success;

    public RegistryWriteResult Store(string characterName, string? worldName, string payload, string? ownerToken)
    {
        if (!PublicationSecurity.IsValidToken(ownerToken))
            return RegistryWriteResult.Unauthorized;
        if (string.IsNullOrWhiteSpace(characterName) || characterName.Length > 128 ||
            (worldName?.Length ?? 0) > 128 || !PublicationSecurity.IsValidPayload(payload))
            return RegistryWriteResult.Invalid;

        var key = MakeKey(characterName, worldName ?? string.Empty);
        int bytes = Encoding.UTF8.GetByteCount(payload);
        var result = database.Write((db, transaction) =>
        {
            var now = timeProvider.GetUtcNow();
            PublicationQueries.Cleanup(db, transaction, "CharacterProfiles", (now - SheetLifetime).UtcTicks);
            byte[]? ownerHash = null;
            int? previousBytes = null;
            using (var lookup = PublicationQueries.Command(db, transaction,
                       "SELECT OwnerHash, PayloadBytes FROM CharacterProfiles WHERE NameKey = $name AND WorldKey = $world",
                       ("$name", key.Name), ("$world", key.World)))
            using (var reader = lookup.ExecuteReader())
            {
                if (reader.Read())
                {
                    ownerHash = (byte[])reader[0];
                    previousBytes = reader.GetInt32(1);
                }
            }
            if (ownerHash != null && !PublicationSecurity.Matches(ownerHash, ownerToken!))
                return RegistryWriteResult.Unauthorized;
            if (!PublicationQueries.HasCapacity(db, transaction, "CharacterProfiles", previousBytes, bytes))
                return RegistryWriteResult.Full;

            using var write = PublicationQueries.Command(db, transaction, """
                INSERT INTO CharacterProfiles (NameKey, WorldKey, Payload, OwnerHash, PayloadBytes, UpdatedTicks)
                VALUES ($name, $world, $payload, $owner, $bytes, $updated)
                ON CONFLICT (NameKey, WorldKey) DO UPDATE SET
                    Payload = excluded.Payload, PayloadBytes = excluded.PayloadBytes, UpdatedTicks = excluded.UpdatedTicks
                """, ("$name", key.Name), ("$world", key.World), ("$payload", payload),
                ("$owner", ownerHash ?? PublicationSecurity.HashToken(ownerToken!)), ("$bytes", bytes), ("$updated", now.UtcTicks));
            write.ExecuteNonQuery();
            return RegistryWriteResult.Success;
        });
        if (result == RegistryWriteResult.Success)
            logger.LogInformation("Stored public character profile");
        return result;
    }

    public bool TryGet(string characterName, string? worldName, [NotNullWhen(true)] out string? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(characterName))
            return false;
        var key = MakeKey(characterName, worldName ?? string.Empty);
        payload = database.Read(db =>
        {
            long oldest = (timeProvider.GetUtcNow() - SheetLifetime).UtcTicks;
            using var exact = PublicationQueries.Command(db, null,
                "SELECT Payload FROM CharacterProfiles WHERE NameKey = $name AND WorldKey = $world AND UpdatedTicks >= $oldest",
                ("$name", key.Name), ("$world", key.World), ("$oldest", oldest));
            if (exact.ExecuteScalar() is string found)
                return found;
            // A world-qualified lookup must never return another character's profile.
            if (!string.IsNullOrWhiteSpace(worldName))
                return null;
            using var fallback = PublicationQueries.Command(db, null,
                "SELECT Payload FROM CharacterProfiles WHERE NameKey = $name AND UpdatedTicks >= $oldest ORDER BY UpdatedTicks DESC LIMIT 1",
                ("$name", key.Name), ("$oldest", oldest));
            return fallback.ExecuteScalar() as string;
        });
        return payload != null;
    }

    public bool TryDelete(string characterName, string? worldName, string? ownerToken = null) =>
        Delete(characterName, worldName, ownerToken) == RegistryWriteResult.Success;

    public RegistryWriteResult Delete(string characterName, string? worldName, string? ownerToken)
    {
        if (!PublicationSecurity.IsValidToken(ownerToken))
            return RegistryWriteResult.Unauthorized;
        if (string.IsNullOrWhiteSpace(characterName))
            return RegistryWriteResult.Invalid;
        var key = MakeKey(characterName, worldName ?? string.Empty);
        return database.Write((db, transaction) =>
        {
            PublicationQueries.Cleanup(db, transaction, "CharacterProfiles", (timeProvider.GetUtcNow() - SheetLifetime).UtcTicks);
            using var lookup = PublicationQueries.Command(db, transaction,
                "SELECT OwnerHash FROM CharacterProfiles WHERE NameKey = $name AND WorldKey = $world",
                ("$name", key.Name), ("$world", key.World));
            if (lookup.ExecuteScalar() is not byte[] ownerHash)
                return RegistryWriteResult.NotFound;
            if (!PublicationSecurity.Matches(ownerHash, ownerToken!))
                return RegistryWriteResult.Unauthorized;
            using var delete = PublicationQueries.Command(db, transaction,
                "DELETE FROM CharacterProfiles WHERE NameKey = $name AND WorldKey = $world",
                ("$name", key.Name), ("$world", key.World));
            delete.ExecuteNonQuery();
            return RegistryWriteResult.Success;
        });
    }

    public int CleanupExpired() => database.Write((db, transaction) =>
        PublicationQueries.Cleanup(db, transaction, "CharacterProfiles", (timeProvider.GetUtcNow() - SheetLifetime).UtcTicks));

    private static (string Name, string World) MakeKey(string name, string world) =>
        (name.Trim().ToUpperInvariant(), world.Trim().ToUpperInvariant());
}
