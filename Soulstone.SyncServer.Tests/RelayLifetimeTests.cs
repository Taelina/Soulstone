using FluentAssertions;
using Soulstone.SyncServer;
using System.Net.WebSockets;
using Xunit;

namespace Soulstone.SyncServer.Tests;

public class RelayLifetimeTests
{
    [Fact]
    public async Task DisposingARecipient_DoesNotBreakAnInFlightSend()
    {
        var socket = new ControlledSocket();
        var room = CreateRoom();
        var client = new RelayClient(room, SessionRole.Member);
        client.Attach(socket);
        var send = client.SendAsync(new byte[] { 1 }, CancellationToken.None);
        await socket.SendStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        client.Dispose();
        socket.CompleteSend.TrySetResult();
        Func<Task> completion = async () => await send;
        await completion.Should().NotThrowAsync();
        (await client.SendAsync(new byte[] { 1 }, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Expiration_SendsCloseOutputWithoutWaitingForThePeerHandshake()
    {
        using var socket = new ControlledSocket();
        using var client = new RelayClient(CreateRoom(), SessionRole.Member);
        client.Attach(socket);
        await client.CloseForExpirationAsync(CancellationToken.None);
        socket.CloseOutputCalls.Should().Be(1);
    }

    private static RelayRoom CreateRoom() => new("host", "member", DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow.AddHours(1), TimeProvider.System);

    private sealed class ControlledSocket : WebSocket
    {
        public TaskCompletionSource SendStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource CompleteSend { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int CloseOutputCalls { get; private set; }
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override string? SubProtocol => null;
        public override WebSocketState State => WebSocketState.Open;
        public override void Abort() { }
        public override void Dispose() { }
        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Do not wait for the peer handshake during expiration.");
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            CloseOutputCalls++;
            return Task.CompletedTask;
        }
        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            SendStarted.TrySetResult();
            await CompleteSend.Task.WaitAsync(cancellationToken);
        }
    }
}
