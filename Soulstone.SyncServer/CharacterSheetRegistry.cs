using System.Diagnostics.CodeAnalysis;

namespace Soulstone.SyncServer;

public sealed record StoredCharacterSheet(
    string CharacterName,
    string WorldName,
    string Payload,
    DateTimeOffset UpdatedAtUtc);

public sealed class CharacterSheetRegistry(TimeProvider timeProvider, ILogger<CharacterSheetRegistry> logger)
{
    public static readonly TimeSpan SheetLifetime = TimeSpan.FromDays(7);
    public const int MaxPayloadLength = 2 * 1024 * 1024; // 2 MB

    private readonly object syncRoot = new();
    private readonly Dictionary<string, StoredCharacterSheet> sheets = new(StringComparer.OrdinalIgnoreCase);

    public bool TryStore(string characterName, string? worldName, string payload)
    {
        if (string.IsNullOrWhiteSpace(characterName) || characterName.Length > 128)
            return false;

        if (string.IsNullOrWhiteSpace(payload) || payload.Length > MaxPayloadLength)
            return false;

        var cleanName = characterName.Trim();
        var cleanWorld = (worldName ?? string.Empty).Trim();
        var key = MakeKey(cleanName, cleanWorld);
        var now = timeProvider.GetUtcNow();

        lock (syncRoot)
        {
            sheets[key] = new StoredCharacterSheet(cleanName, cleanWorld, payload, now);
        }

        logger.LogInformation("Stored character sheet for {CharacterName} ({WorldName})", cleanName, cleanWorld);
        return true;
    }

    public bool TryGet(string characterName, string? worldName, [NotNullWhen(true)] out string? payload)
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            payload = null;
            return false;
        }

        var cleanName = characterName.Trim();
        var cleanWorld = (worldName ?? string.Empty).Trim();
        var key = MakeKey(cleanName, cleanWorld);
        var now = timeProvider.GetUtcNow();

        lock (syncRoot)
        {
            if (sheets.TryGetValue(key, out var sheet) && now - sheet.UpdatedAtUtc <= SheetLifetime)
            {
                payload = sheet.Payload;
                return true;
            }

            // Fallback: if worldName was provided or empty, try searching matching characterName regardless of world
            if (string.IsNullOrEmpty(cleanWorld))
            {
                var match = sheets.Values
                    .Where(s => string.Equals(s.CharacterName, cleanName, StringComparison.OrdinalIgnoreCase) && now - s.UpdatedAtUtc <= SheetLifetime)
                    .OrderByDescending(s => s.UpdatedAtUtc)
                    .FirstOrDefault();

                if (match != null)
                {
                    payload = match.Payload;
                    return true;
                }
            }
            else
            {
                // Also check if stored without world
                var keyNoWorld = MakeKey(cleanName, string.Empty);
                if (sheets.TryGetValue(keyNoWorld, out var sheetNoWorld) && now - sheetNoWorld.UpdatedAtUtc <= SheetLifetime)
                {
                    payload = sheetNoWorld.Payload;
                    return true;
                }
            }
        }

        payload = null;
        return false;
    }

    public bool TryDelete(string characterName, string? worldName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return false;

        var key = MakeKey(characterName.Trim(), (worldName ?? string.Empty).Trim());
        lock (syncRoot)
        {
            return sheets.Remove(key);
        }
    }

    public int CleanupExpired()
    {
        var now = timeProvider.GetUtcNow();
        lock (syncRoot)
        {
            var expiredKeys = sheets
                .Where(kvp => now - kvp.Value.UpdatedAtUtc > SheetLifetime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                sheets.Remove(key);
            }

            return expiredKeys.Count;
        }
    }

    private static string MakeKey(string characterName, string worldName) =>
        $"{characterName.ToLowerInvariant()}@{worldName.ToLowerInvariant()}";
}
