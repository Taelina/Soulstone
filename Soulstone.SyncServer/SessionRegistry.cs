using System.Net.WebSockets;
using System.Security.Cryptography;

namespace Soulstone.SyncServer;

public sealed record CreatedSession(
    string SessionId,
    string HostToken,
    string MemberToken,
    DateTimeOffset ExpiresAtUtc);

public enum SessionRole
{
    Host,
    Member,
}

public enum JoinResult
{
    Success,
    NotFound,
    Unauthorized,
    Full,
}

public sealed class SessionRegistry(TimeProvider timeProvider, ILogger<SessionRegistry> logger)
{
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    public static readonly TimeSpan EmptySessionLifetime = TimeSpan.FromMinutes(5);

    private readonly object syncRoot = new();
    private readonly Dictionary<string, RelayRoom> rooms = new(StringComparer.Ordinal);

    public CreatedSession Create()
    {
        var now = timeProvider.GetUtcNow();
        var session = new CreatedSession(
            Guid.NewGuid().ToString("N"),
            CreateToken(),
            CreateToken(),
            now.Add(SessionLifetime));

        lock (syncRoot)
        {
            rooms.Add(
                session.SessionId,
                new RelayRoom(session.HostToken, session.MemberToken, now, session.ExpiresAtUtc, timeProvider));
        }

        logger.LogInformation("Created relay session {Session}; expires at {ExpiresAtUtc}", SafeSessionId(session.SessionId), session.ExpiresAtUtc);
        return session;
    }

    public JoinResult TryJoin(string sessionId, string token, out RelayClient? client)
    {
        lock (syncRoot)
        {
            if (!rooms.TryGetValue(sessionId, out var room))
            {
                client = null;
                return JoinResult.NotFound;
            }

            var result = room.TryJoin(token, timeProvider.GetUtcNow(), out client);
            if (result == JoinResult.Success)
                logger.LogInformation("Client joined session {Session} as {Role}", SafeSessionId(sessionId), client!.Role);
            else
                logger.LogWarning("Rejected connection to session {Session}: {Result}", SafeSessionId(sessionId), result);
            return result;
        }
    }

    public async Task RemoveExpiredAsync(CancellationToken cancellationToken)
    {
        List<KeyValuePair<string, RelayRoom>> removedRooms = [];
        var now = timeProvider.GetUtcNow();

        lock (syncRoot)
        {
            foreach (var pair in rooms)
            {
                if (pair.Value.ShouldRemove(now))
                    removedRooms.Add(pair);
            }

            foreach (var pair in removedRooms)
                rooms.Remove(pair.Key);
        }

        foreach (var pair in removedRooms)
            await pair.Value.ExpireAsync(cancellationToken);

        if (removedRooms.Count > 0)
            logger.LogInformation("Removed {Count} expired or inactive relay sessions", removedRooms.Count);
    }

    private static string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string SafeSessionId(string sessionId) => sessionId.Length <= 8 ? sessionId : sessionId[..8];
}

public sealed class RelayRoom
{
    public const int MaximumClients = 16;

    private readonly object syncRoot = new();
    private readonly byte[] hostToken;
    private readonly byte[] memberToken;
    private readonly DateTimeOffset expiresAtUtc;
    private readonly TimeProvider timeProvider;
    private readonly HashSet<RelayClient> clients = [];
    private DateTimeOffset? emptySinceUtc;
    private bool expired;

    public RelayRoom(
        string hostToken,
        string memberToken,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        TimeProvider timeProvider)
    {
        this.hostToken = System.Text.Encoding.UTF8.GetBytes(hostToken);
        this.memberToken = System.Text.Encoding.UTF8.GetBytes(memberToken);
        this.expiresAtUtc = expiresAtUtc;
        this.timeProvider = timeProvider;
        emptySinceUtc = createdAtUtc;
    }

    public JoinResult TryJoin(string token, DateTimeOffset now, out RelayClient? client)
    {
        lock (syncRoot)
        {
            if (expired || now >= expiresAtUtc)
            {
                expired = true;
                client = null;
                return JoinResult.NotFound;
            }

            var role = Authenticate(token);
            if (role is null)
            {
                client = null;
                return JoinResult.Unauthorized;
            }

            if (clients.Count >= MaximumClients)
            {
                client = null;
                return JoinResult.Full;
            }

            client = new RelayClient(this, role.Value);
            clients.Add(client);
            emptySinceUtc = null;
            return JoinResult.Success;
        }
    }

    public bool ShouldRemove(DateTimeOffset now)
    {
        lock (syncRoot)
        {
            return expired || now >= expiresAtUtc ||
                   (clients.Count == 0 && emptySinceUtc is { } emptySince &&
                    now - emptySince >= SessionRegistry.EmptySessionLifetime);
        }
    }

    public async Task RelayAsync(
        RelayClient sender,
        RelayDestination destination,
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken)
    {
        RelayClient[] recipients;
        lock (syncRoot)
        {
            if (expired)
                return;

            recipients = clients
                .Where(client => !ReferenceEquals(client, sender))
                .Where(client => destination == RelayDestination.Group || client.Role == SessionRole.Host)
                .Where(client => client.IsConnected)
                .ToArray();
        }

        var sends = recipients.Select(async recipient =>
        {
            if (!await recipient.SendAsync(message, cancellationToken))
                Remove(recipient);
        });
        await Task.WhenAll(sends);
    }

    public async Task ExpireAsync(CancellationToken cancellationToken)
    {
        RelayClient[] connectedClients;
        lock (syncRoot)
        {
            expired = true;
            connectedClients = clients.ToArray();
        }

        await Task.WhenAll(connectedClients.Select(client => client.CloseForExpirationAsync(cancellationToken)));
    }

    internal void Remove(RelayClient client)
    {
        lock (syncRoot)
        {
            if (clients.Remove(client) && clients.Count == 0)
                emptySinceUtc = timeProvider.GetUtcNow();
        }
    }

    private SessionRole? Authenticate(string token)
    {
        var suppliedToken = System.Text.Encoding.UTF8.GetBytes(token);
        if (suppliedToken.Length == hostToken.Length &&
            CryptographicOperations.FixedTimeEquals(suppliedToken, hostToken))
            return SessionRole.Host;

        if (suppliedToken.Length == memberToken.Length &&
            CryptographicOperations.FixedTimeEquals(suppliedToken, memberToken))
            return SessionRole.Member;

        return null;
    }
}

public sealed class RelayClient : IDisposable
{
    private readonly RelayRoom room;
    private readonly SemaphoreSlim sendLock = new(1, 1);
    private WebSocket? socket;
    private int disposed;

    public RelayClient(RelayRoom room, SessionRole role)
    {
        this.room = room;
        Role = role;
    }

    public SessionRole Role { get; }
    public RelayRoom Room => room;
    public bool IsConnected => socket?.State == WebSocketState.Open;

    public void Attach(WebSocket webSocket)
    {
        socket = webSocket;
    }

    public async Task<bool> SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken)
    {
        var currentSocket = socket;
        if (currentSocket?.State != WebSocketState.Open)
            return false;

        await sendLock.WaitAsync(cancellationToken);
        try
        {
            if (currentSocket.State != WebSocketState.Open)
                return false;

            await currentSocket.SendAsync(message, WebSocketMessageType.Text, true, cancellationToken);
            return true;
        }
        catch (WebSocketException)
        {
            return false;
        }
        finally
        {
            sendLock.Release();
        }
    }

    public async Task CloseForExpirationAsync(CancellationToken cancellationToken)
    {
        var currentSocket = socket;
        if (currentSocket?.State == WebSocketState.Open)
        {
            try
            {
                await currentSocket.CloseAsync(
                    WebSocketCloseStatus.PolicyViolation,
                    "Session expired.",
                    cancellationToken);
            }
            catch (WebSocketException)
            {
                currentSocket.Abort();
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;

        room.Remove(this);
        sendLock.Dispose();
    }
}

public sealed class SessionCleanupService(SessionRegistry sessions, TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await sessions.RemoveExpiredAsync(stoppingToken);
    }
}