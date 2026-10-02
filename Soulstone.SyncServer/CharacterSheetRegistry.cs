using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Soulstone.SyncServer;

public sealed record StoredCharacterSheet(string CharacterName, string WorldName, string Payload, DateTimeOffset UpdatedAtUtc);

public sealed class CharacterSheetRegistry(TimeProvider timeProvider, ILogger<CharacterSheetRegistry> logger)
{
    public static readonly TimeSpan SheetLifetime = TimeSpan.FromDays(7);
    public const int MaxPayloadLength = PublicationSecurity.MaximumPayloadBytes;
    private readonly object syncRoot = new();
    private readonly Dictionary<(string Name, string World), Entry> sheets = new();
    private long storageBytes;

    public bool TryStore(string characterName, string? worldName, string payload, string? ownerToken = null) =>
        Store(characterName, worldName, payload, ownerToken) == RegistryWriteResult.Success;

    public RegistryWriteResult Store(string characterName, string? worldName, string payload, string? ownerToken)
    {
        if (!PublicationSecurity.IsValidToken(ownerToken))
            return RegistryWriteResult.Unauthorized;
        if (string.IsNullOrWhiteSpace(characterName) || characterName.Length > 128 ||
            (worldName?.Length ?? 0) > 128 || !PublicationSecurity.IsValidPayload(payload))
            return RegistryWriteResult.Invalid;

        var cleanName = characterName.Trim();
        var cleanWorld = (worldName ?? string.Empty).Trim();
        var key = MakeKey(cleanName, cleanWorld);
        int bytes = Encoding.UTF8.GetByteCount(payload);
        lock (syncRoot)
        {
            CleanupExpiredCore();
            sheets.TryGetValue(key, out var existing);
            if (existing != null && !PublicationSecurity.Matches(existing.OwnerHash, ownerToken!))
                return RegistryWriteResult.Unauthorized;
            if ((existing == null && sheets.Count >= PublicationSecurity.MaximumEntries) ||
                storageBytes - (existing?.Bytes ?? 0) + bytes > PublicationSecurity.MaximumStorageBytes)
                return RegistryWriteResult.Full;
            sheets[key] = new Entry(new StoredCharacterSheet(cleanName, cleanWorld, payload, timeProvider.GetUtcNow()),
                existing?.OwnerHash ?? PublicationSecurity.HashToken(ownerToken!), bytes);
            storageBytes += bytes - (existing?.Bytes ?? 0);
        }
        logger.LogInformation("Stored public character profile");
        return RegistryWriteResult.Success;
    }

    public bool TryGet(string characterName, string? worldName, [NotNullWhen(true)] out string? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(characterName))
            return false;
        var key = MakeKey(characterName, worldName ?? string.Empty);
        lock (syncRoot)
        {
            CleanupExpiredCore();
            if (sheets.TryGetValue(key, out var entry))
            {
                payload = entry.Sheet.Payload;
                return true;
            }
            // A world-qualified lookup must never return another character's profile.
            if (!string.IsNullOrWhiteSpace(worldName))
                return false;
            var match = sheets.Values.Where(s => string.Equals(s.Sheet.CharacterName, characterName.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.Sheet.UpdatedAtUtc).FirstOrDefault();
            payload = match?.Sheet.Payload;
            return payload != null;
        }
    }

    public bool TryDelete(string characterName, string? worldName, string? ownerToken = null) =>
        Delete(characterName, worldName, ownerToken) == RegistryWriteResult.Success;

    public RegistryWriteResult Delete(string characterName, string? worldName, string? ownerToken)
    {
        if (!PublicationSecurity.IsValidToken(ownerToken))
            return RegistryWriteResult.Unauthorized;
        if (string.IsNullOrWhiteSpace(characterName))
            return RegistryWriteResult.Invalid;
        lock (syncRoot)
        {
            CleanupExpiredCore();
            var key = MakeKey(characterName, worldName ?? string.Empty);
            if (!sheets.TryGetValue(key, out var entry))
                return RegistryWriteResult.NotFound;
            if (!PublicationSecurity.Matches(entry.OwnerHash, ownerToken!))
                return RegistryWriteResult.Unauthorized;
            sheets.Remove(key);
            storageBytes -= entry.Bytes;
            return RegistryWriteResult.Success;
        }
    }

    public int CleanupExpired()
    {
        lock (syncRoot)
            return CleanupExpiredCore();
    }

    private int CleanupExpiredCore()
    {
        var now = timeProvider.GetUtcNow();
        var expired = sheets.Where(pair => now - pair.Value.Sheet.UpdatedAtUtc > SheetLifetime).Select(pair => pair.Key).ToArray();
        foreach (var key in expired)
        {
            storageBytes -= sheets[key].Bytes;
            sheets.Remove(key);
        }
        return expired.Length;
    }

    private static (string Name, string World) MakeKey(string name, string world) =>
        (name.Trim().ToUpperInvariant(), world.Trim().ToUpperInvariant());

    private sealed record Entry(StoredCharacterSheet Sheet, byte[] OwnerHash, int Bytes);
}
