using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Soulstone.SyncServer;

/// <summary>A public ruleset publication; the bearer credential is supplied separately.</summary>
public sealed record PublishDiceSystemRequest(string PlayerName, string WorldName, string SystemName, string Payload, string? Code = null);

/// <summary>Public ruleset content and publisher metadata. Never includes ownership credentials.</summary>
public sealed record PublishedDiceSystemResponse(string Code, string PlayerName, string WorldName, string SystemName, string Payload, DateTimeOffset UpdatedAtUtc);

/// <summary>The current version of a public ruleset.</summary>
public sealed record DiceSystemVersionResponse(string Code, string SystemName, DateTimeOffset UpdatedAtUtc);

public sealed class DiceSystemRegistry(PublicationDatabase database, TimeProvider timeProvider, ILogger<DiceSystemRegistry> logger)
{
    public const int CodeLength = 10;
    public const int MaxPayloadLength = PublicationSecurity.MaximumPayloadBytes;
    public static readonly TimeSpan PublicationLifetime = TimeSpan.FromDays(30);
    private const string CodeCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public bool TryPublish(PublishDiceSystemRequest request, [NotNullWhen(true)] out PublishedDiceSystemResponse? published, string? ownerToken = null) =>
        Publish(request, ownerToken, out published) == RegistryWriteResult.Success;

    public RegistryWriteResult Publish(PublishDiceSystemRequest request, string? ownerToken, out PublishedDiceSystemResponse? published)
    {
        published = null;
        if (!PublicationSecurity.IsValidToken(ownerToken))
            return RegistryWriteResult.Unauthorized;
        if (string.IsNullOrWhiteSpace(request.PlayerName) || request.PlayerName.Length > 128 ||
            request.WorldName == null || request.WorldName.Length > 128 ||
            string.IsNullOrWhiteSpace(request.SystemName) || request.SystemName.Length > 128 ||
            !PublicationSecurity.IsValidPayload(request.Payload))
            return RegistryWriteResult.Invalid;
        int bytes = Encoding.UTF8.GetByteCount(request.Payload);
        PublishedDiceSystemResponse? publication = null;
        var result = database.Write((db, transaction) =>
        {
            var now = timeProvider.GetUtcNow();
            PublicationQueries.Cleanup(db, transaction, "DiceSystems", (now - PublicationLifetime).UtcTicks);
            string code;
            byte[]? ownerHash = null;
            int? previousBytes = null;
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                code = request.Code.Trim().ToUpperInvariant();
                if (code.Length != CodeLength || !code.All(char.IsAsciiLetterOrDigit))
                    return RegistryWriteResult.Invalid;
                using var lookup = PublicationQueries.Command(db, transaction,
                    "SELECT OwnerHash, PayloadBytes FROM DiceSystems WHERE Code = $code", ("$code", code));
                using var reader = lookup.ExecuteReader();
                if (!reader.Read())
                    return RegistryWriteResult.NotFound;
                ownerHash = (byte[])reader[0];
                previousBytes = reader.GetInt32(1);
                if (!PublicationSecurity.Matches(ownerHash, ownerToken!))
                    return RegistryWriteResult.Unauthorized;
            }
            else
            {
                using var lookup = PublicationQueries.Command(db, transaction,
                    "SELECT 1 FROM DiceSystems WHERE Code = $code");
                var parameter = lookup.Parameters.Add("$code", Microsoft.Data.Sqlite.SqliteType.Text);
                do
                {
                    code = RandomNumberGenerator.GetString(CodeCharacters, CodeLength);
                    parameter.Value = code;
                } while (lookup.ExecuteScalar() != null);
            }
            if (!PublicationQueries.HasCapacity(db, transaction, "DiceSystems", previousBytes, bytes))
                return RegistryWriteResult.Full;
            var value = new PublishedDiceSystemResponse(code, request.PlayerName.Trim(), request.WorldName.Trim(),
                request.SystemName.Trim(), request.Payload, now);
            using var write = PublicationQueries.Command(db, transaction, """
                INSERT INTO DiceSystems (Code, PlayerName, WorldName, SystemName, Payload, OwnerHash, PayloadBytes, UpdatedTicks)
                VALUES ($code, $player, $world, $system, $payload, $owner, $bytes, $updated)
                ON CONFLICT (Code) DO UPDATE SET PlayerName = excluded.PlayerName, WorldName = excluded.WorldName,
                    SystemName = excluded.SystemName, Payload = excluded.Payload,
                    PayloadBytes = excluded.PayloadBytes, UpdatedTicks = excluded.UpdatedTicks
                """, ("$code", code), ("$player", value.PlayerName), ("$world", value.WorldName),
                ("$system", value.SystemName), ("$payload", value.Payload),
                ("$owner", ownerHash ?? PublicationSecurity.HashToken(ownerToken!)), ("$bytes", bytes), ("$updated", now.UtcTicks));
            write.ExecuteNonQuery();
            publication = value;
            return RegistryWriteResult.Success;
        });
        // Do not expose a publication until its transaction has committed successfully.
        published = publication;
        if (result == RegistryWriteResult.Success)
            logger.LogInformation("Published public dice system");
        return result;
    }

    public bool TryGet(string code, [NotNullWhen(true)] out PublishedDiceSystemResponse? published)
    {
        published = database.Read(db =>
        {
            using var lookup = PublicationQueries.Command(db, null, """
                SELECT Code, PlayerName, WorldName, SystemName, Payload, UpdatedTicks
                FROM DiceSystems WHERE Code = $code AND UpdatedTicks >= $oldest
                """, ("$code", code.ToUpperInvariant()), ("$oldest", (timeProvider.GetUtcNow() - PublicationLifetime).UtcTicks));
            using var reader = lookup.ExecuteReader();
            return reader.Read() ? new PublishedDiceSystemResponse(reader.GetString(0), reader.GetString(1),
                reader.GetString(2), reader.GetString(3), reader.GetString(4), new DateTimeOffset(reader.GetInt64(5), TimeSpan.Zero)) : null;
        });
        return published != null;
    }

    public bool TryGetVersion(string code, [NotNullWhen(true)] out DiceSystemVersionResponse? version)
    {
        version = TryGet(code, out var published) ? new DiceSystemVersionResponse(published.Code, published.SystemName, published.UpdatedAtUtc) : null;
        return version != null;
    }

    public int CleanupExpired() => database.Write((db, transaction) =>
        PublicationQueries.Cleanup(db, transaction, "DiceSystems", (timeProvider.GetUtcNow() - PublicationLifetime).UtcTicks));
}
