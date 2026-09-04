using Microsoft.Extensions.Logging.Abstractions;
using Soulstone.SyncServer;
using System.Text;
using Xunit;

namespace Soulstone.SyncServer.Tests;

public class RelayServerTests
{
    [Theory]
    [InlineData("{\"destination\":\"group\"}", RelayDestination.Group)]
    [InlineData("{\"destination\":\"host\"}", RelayDestination.Host)]
    public void ProtocolAcceptsKnownDestinations(string json, RelayDestination expected)
    {
        Assert.True(RelayProtocol.TryReadDestination(Encoding.UTF8.GetBytes(json), out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"destination\":\"everyone\"}")]
    [InlineData("not-json")]
    public void ProtocolRejectsInvalidEnvelopes(string json)
    {
        Assert.False(RelayProtocol.TryReadDestination(Encoding.UTF8.GetBytes(json), out _));
    }

    [Fact]
    public void CreatedSessionAcceptsHostAndMemberTokensOnly()
    {
        var registry = CreateRegistry();
        var session = registry.Create();

        Assert.Equal(JoinResult.Success, registry.TryJoin(session.SessionId, session.HostToken, out var host));
        Assert.Equal(SessionRole.Host, host!.Role);
        Assert.Equal(JoinResult.Success, registry.TryJoin(session.SessionId, session.MemberToken, out var member));
        Assert.Equal(SessionRole.Member, member!.Role);
        Assert.Equal(JoinResult.Unauthorized, registry.TryJoin(session.SessionId, "wrong-token", out _));

        host.Dispose();
        member.Dispose();
    }

    [Fact]
    public void RoomRejectsMoreThanSixteenClients()
    {
        var registry = CreateRegistry();
        var session = registry.Create();
        var clients = new List<RelayClient>();

        for (int i = 0; i < RelayRoom.MaximumClients; i++)
        {
            Assert.Equal(JoinResult.Success, registry.TryJoin(session.SessionId, session.MemberToken, out var client));
            clients.Add(client!);
        }

        Assert.Equal(JoinResult.Full, registry.TryJoin(session.SessionId, session.MemberToken, out _));
        foreach (var client in clients) client.Dispose();
    }

    [Fact]
    public void RateLimiterRejectsTwentyFirstImmediateMessage()
    {
        var limiter = new SlidingWindowRateLimiter(TimeProvider.System);

        for (int i = 0; i < 20; i++) Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());
    }

    private static SessionRegistry CreateRegistry() =>
        new(TimeProvider.System, NullLogger<SessionRegistry>.Instance);
}