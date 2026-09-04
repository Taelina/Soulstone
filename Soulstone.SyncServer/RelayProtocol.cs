using System.Text.Json;

namespace Soulstone.SyncServer;

public enum RelayDestination
{
    Group,
    Host,
}

public static class RelayProtocol
{
    public const int MaximumMessageBytes = 64 * 1024;

    public static bool TryReadDestination(ReadOnlyMemory<byte> message, out RelayDestination destination)
    {
        try
        {
            using var document = JsonDocument.Parse(message);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("destination", out var property) ||
                property.ValueKind != JsonValueKind.String)
            {
                destination = default;
                return false;
            }

            destination = property.GetString() switch
            {
                "group" => RelayDestination.Group,
                "host" => RelayDestination.Host,
                _ => default,
            };
            return property.ValueEquals("group") || property.ValueEquals("host");
        }
        catch (JsonException)
        {
            destination = default;
            return false;
        }
    }
}

public sealed class SlidingWindowRateLimiter(
    TimeProvider timeProvider,
    int maximumMessages = 20,
    TimeSpan? window = null)
{
    private readonly Queue<DateTimeOffset> messages = new();
    private readonly TimeSpan window = window ?? TimeSpan.FromSeconds(10);

    public bool TryAcquire()
    {
        var now = timeProvider.GetUtcNow();
        while (messages.TryPeek(out var timestamp) && now - timestamp >= window)
            messages.Dequeue();

        if (messages.Count >= maximumMessages)
            return false;

        messages.Enqueue(now);
        return true;
    }
}