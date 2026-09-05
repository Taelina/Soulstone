using System.Buffers;
using System.Net.WebSockets;

namespace Soulstone.SyncServer;

public static class WebSocketRelay
{
    public static async Task HandleAsync(HttpContext context)
    {
        var sessions = context.RequestServices.GetRequiredService<SessionRegistry>();
        var timeProvider = context.RequestServices.GetRequiredService<TimeProvider>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Soulstone.SyncServer.WebSocketRelay");
        var sessionId = context.Request.RouteValues["sessionId"]?.ToString();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (!TryReadBearerToken(context.Request, out var token))
        {
            logger.LogWarning("Rejected WebSocket connection without bearer authentication");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var joinResult = sessions.TryJoin(sessionId, token, out var client);
        if (joinResult != JoinResult.Success)
        {
            context.Response.StatusCode = joinResult switch
            {
                JoinResult.NotFound => StatusCodes.Status404NotFound,
                JoinResult.Unauthorized => StatusCodes.Status401Unauthorized,
                JoinResult.Full => StatusCodes.Status429TooManyRequests,
                _ => StatusCodes.Status400BadRequest,
            };
            return;
        }

        using (var joinedClient = client!)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            joinedClient.Attach(socket);
            logger.LogInformation("Accepted {Role} WebSocket connection", joinedClient.Role);
            await ReceiveMessagesAsync(socket, joinedClient, timeProvider, logger, context.RequestAborted);
            logger.LogInformation("Closed {Role} WebSocket connection", joinedClient.Role);
        }
    }

    private static async Task ReceiveMessagesAsync(
        WebSocket socket,
        RelayClient client,
        TimeProvider timeProvider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var rateLimiter = new SlidingWindowRateLimiter(timeProvider);
        var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await socket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await CloseNormallyAsync(socket, cancellationToken);
                        return;
                    }

                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        logger.LogWarning("Closed client for sending a non-text WebSocket message");
                        await CloseAsync(socket, WebSocketCloseStatus.InvalidMessageType,
                            "Only text messages are accepted.", cancellationToken);
                        return;
                    }

                    if (message.Length + result.Count > RelayProtocol.MaximumMessageBytes)
                    {
                        logger.LogWarning("Closed client for exceeding the 64 KiB message limit");
                        await CloseAsync(socket, WebSocketCloseStatus.MessageTooBig,
                            "Message exceeds 64 KiB.", cancellationToken);
                        return;
                    }

                    message.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (!rateLimiter.TryAcquire())
                {
                    logger.LogWarning("Closed client for exceeding the message rate limit");
                    await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation,
                        "Rate limit exceeded.", cancellationToken);
                    return;
                }

                var payload = message.ToArray();
                if (!RelayProtocol.TryReadDestination(payload, out var destination))
                {
                    logger.LogWarning("Closed client for sending an invalid relay envelope");
                    await CloseAsync(socket, WebSocketCloseStatus.InvalidPayloadData,
                        "A valid destination is required.", cancellationToken);
                    return;
                }

                await client.Room.RelayAsync(client, destination, payload, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (WebSocketException ex)
        {
            logger.LogDebug(ex, "WebSocket transport ended unexpectedly");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static bool TryReadBearerToken(HttpRequest request, out string token)
    {
        const string prefix = "Bearer ";
        var authorization = request.Headers.Authorization.ToString();
        if (authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            authorization.Length > prefix.Length)
        {
            token = authorization[prefix.Length..].Trim();
            return token.Length > 0;
        }

        token = string.Empty;
        return false;
    }

    private static async Task CloseNormallyAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        if (socket.State == WebSocketState.CloseReceived)
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
    }

    private static async Task CloseAsync(
        WebSocket socket,
        WebSocketCloseStatus status,
        string description,
        CancellationToken cancellationToken)
    {
        if (socket.State == WebSocketState.Open)
            await socket.CloseAsync(status, description, cancellationToken);
    }
}