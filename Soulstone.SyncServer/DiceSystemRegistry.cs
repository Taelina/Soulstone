using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Soulstone.SyncServer;

public sealed record PublishDiceSystemRequest(
    string PlayerName,
    string WorldName,
    string SystemName,
    string Payload,
    string? Code = null);

public sealed record PublishedDiceSystemResponse(
    string Code,
    string PlayerName,
    string WorldName,
    string SystemName,
    string Payload,
    DateTimeOffset UpdatedAtUtc);

public sealed record DiceSystemVersionResponse(
    string Code,
    string SystemName,
    DateTimeOffset UpdatedAtUtc);

public sealed class DiceSystemRegistry(TimeProvider timeProvider, ILogger<DiceSystemRegistry> logger)
{
    public const int CodeLength = 10;
    public const int MaxPayloadLength = 2 * 1024 * 1024;
    private const string CodeCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly object syncRoot = new();
    private readonly Dictionary<string, PublishedDiceSystemResponse> systems = new(StringComparer.OrdinalIgnoreCase);

    public bool TryPublish(PublishDiceSystemRequest request, [NotNullWhen(true)] out PublishedDiceSystemResponse? published)
    {
        published = null;
        if (string.IsNullOrWhiteSpace(request.PlayerName) || request.PlayerName.Length > 128 ||
            request.WorldName.Length > 128 ||
            string.IsNullOrWhiteSpace(request.SystemName) || request.SystemName.Length > 128 ||
            string.IsNullOrWhiteSpace(request.Payload) || request.Payload.Length > MaxPayloadLength)
            return false;

        lock (syncRoot)
        {
            string code;
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                code = request.Code.Trim().ToUpperInvariant();
                if (code.Length != CodeLength || !code.All(char.IsLetterOrDigit) ||
                    !systems.TryGetValue(code, out var existing) ||
                    !string.Equals(existing.PlayerName, request.PlayerName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(existing.WorldName, request.WorldName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(existing.SystemName, request.SystemName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else
            {
                do
                {
                    code = RandomNumberGenerator.GetString(CodeCharacters, CodeLength);
                } while (systems.ContainsKey(code));
            }

            published = new PublishedDiceSystemResponse(
                code,
                request.PlayerName.Trim(),
                request.WorldName.Trim(),
                request.SystemName.Trim(),
                request.Payload,
                timeProvider.GetUtcNow());
            systems[code] = published;
        }

        logger.LogInformation("Published dice system {SystemName} for player {PlayerName} ({WorldName})",
            published.SystemName, published.PlayerName, published.WorldName);
        return true;
    }

    public bool TryGet(string code, [NotNullWhen(true)] out PublishedDiceSystemResponse? published)
    {
        lock (syncRoot)
        {
            return systems.TryGetValue(code, out published);
        }
    }

    public bool TryGetVersion(string code, [NotNullWhen(true)] out DiceSystemVersionResponse? version)
    {
        if (TryGet(code, out var published))
        {
            version = new DiceSystemVersionResponse(published.Code, published.SystemName, published.UpdatedAtUtc);
            return true;
        }

        version = null;
        return false;
    }
}
