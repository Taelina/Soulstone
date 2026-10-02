using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Soulstone.SyncServer;

public enum RegistryWriteResult
{
    Success,
    Invalid,
    Unauthorized,
    Full,
    NotFound,
}

internal static class PublicationSecurity
{
    public const int MaximumPayloadBytes = 2 * 1024 * 1024;
    public const int MaximumEntries = 1024;
    public const long MaximumStorageBytes = 64 * 1024 * 1024;

    public static bool IsValidToken(string? token) => token is { Length: 64 } && token.All(char.IsAsciiHexDigit);

    public static byte[] HashToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    public static bool Matches(byte[] hash, string token) =>
        CryptographicOperations.FixedTimeEquals(hash, HashToken(token));

    public static bool IsValidPayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload) || Encoding.UTF8.GetByteCount(payload) > MaximumPayloadBytes)
            return false;
        try
        {
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string? ReadToken(HttpRequest request)
    {
        const string prefix = "Bearer ";
        var authorization = request.Headers.Authorization.ToString();
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;
        var token = authorization[prefix.Length..].Trim();
        return IsValidToken(token) ? token : null;
    }
}
