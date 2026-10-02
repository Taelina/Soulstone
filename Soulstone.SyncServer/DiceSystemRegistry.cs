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

public sealed class DiceSystemRegistry(TimeProvider timeProvider, ILogger<DiceSystemRegistry> logger)
{
    public const int CodeLength = 10;
    public const int MaxPayloadLength = PublicationSecurity.MaximumPayloadBytes;
    public static readonly TimeSpan PublicationLifetime = TimeSpan.FromDays(30);
    private const string CodeCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly object syncRoot = new();
    private readonly Dictionary<string, Entry> systems = new(StringComparer.OrdinalIgnoreCase);
    private long storageBytes;

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
        lock (syncRoot)
        {
            CleanupExpiredCore();
            string code;
            Entry? existing = null;
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                code = request.Code.Trim().ToUpperInvariant();
                if (code.Length != CodeLength || !code.All(char.IsAsciiLetterOrDigit))
                    return RegistryWriteResult.Invalid;
                if (!systems.TryGetValue(code, out existing))
                    return RegistryWriteResult.NotFound;
                if (!PublicationSecurity.Matches(existing.OwnerHash, ownerToken!))
                    return RegistryWriteResult.Unauthorized;
            }
            else
            {
                do { code = RandomNumberGenerator.GetString(CodeCharacters, CodeLength); }
                while (systems.ContainsKey(code));
            }
            if ((existing == null && systems.Count >= PublicationSecurity.MaximumEntries) ||
                storageBytes - (existing?.Bytes ?? 0) + bytes > PublicationSecurity.MaximumStorageBytes)
                return RegistryWriteResult.Full;
            published = new PublishedDiceSystemResponse(code, request.PlayerName.Trim(), request.WorldName.Trim(),
                request.SystemName.Trim(), request.Payload, timeProvider.GetUtcNow());
            systems[code] = new Entry(published, existing?.OwnerHash ?? PublicationSecurity.HashToken(ownerToken!), bytes);
            storageBytes += bytes - (existing?.Bytes ?? 0);
        }
        logger.LogInformation("Published public dice system");
        return RegistryWriteResult.Success;
    }

    public bool TryGet(string code, [NotNullWhen(true)] out PublishedDiceSystemResponse? published)
    {
        lock (syncRoot)
        {
            CleanupExpiredCore();
            published = systems.TryGetValue(code, out var entry) ? entry.Publication : null;
            return published != null;
        }
    }

    public bool TryGetVersion(string code, [NotNullWhen(true)] out DiceSystemVersionResponse? version)
    {
        version = TryGet(code, out var published) ? new DiceSystemVersionResponse(published.Code, published.SystemName, published.UpdatedAtUtc) : null;
        return version != null;
    }

    public int CleanupExpired()
    {
        lock (syncRoot)
            return CleanupExpiredCore();
    }

    private int CleanupExpiredCore()
    {
        var now = timeProvider.GetUtcNow();
        var expired = systems.Where(pair => now - pair.Value.Publication.UpdatedAtUtc > PublicationLifetime).Select(pair => pair.Key).ToArray();
        foreach (var key in expired)
        {
            storageBytes -= systems[key].Bytes;
            systems.Remove(key);
        }
        return expired.Length;
    }

    private sealed record Entry(PublishedDiceSystemResponse Publication, byte[] OwnerHash, int Bytes);
}
