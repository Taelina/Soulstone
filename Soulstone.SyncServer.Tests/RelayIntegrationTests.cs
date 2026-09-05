using Microsoft.AspNetCore.Mvc.Testing;
using Soulstone.SyncServer;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using Xunit;

namespace Soulstone.SyncServer.Tests;

public class RelayIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public RelayIntegrationTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task HealthAndSessionCreationWorkWithoutInteraction()
    {
        using var client = factory.CreateClient();

        using var health = await client.GetAsync("/health");
        using var response = await client.PostAsync("/api/sessions", null);
        var session = await response.Content.ReadFromJsonAsync<CreatedSession>();

        health.EnsureSuccessStatusCode();
        response.EnsureSuccessStatusCode();
        Assert.NotNull(session);
        Assert.NotEmpty(session!.SessionId);
        Assert.NotEmpty(session.HostToken);
        Assert.NotEmpty(session.MemberToken);
    }

    [Fact]
    public async Task HostCanRegisterAndMemberCanResolveOpaqueInvite()
    {
        using var client = factory.CreateClient();
        using var createResponse = await client.PostAsync("/api/sessions", null);
        var session = (await createResponse.Content.ReadFromJsonAsync<CreatedSession>())!;
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/sessions/{session.SessionId}/invite")
        {
            Content = JsonContent.Create(new InviteRegistrationRequest("invite-id", "opaque-payload"))
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.HostToken);

        using var registerResponse = await client.SendAsync(request);
        using var resolveResponse = await client.GetAsync("/api/invites/invite-id");
        var resolved = await resolveResponse.Content.ReadFromJsonAsync<InviteResolutionResponse>();

        registerResponse.EnsureSuccessStatusCode();
        resolveResponse.EnsureSuccessStatusCode();
        Assert.Equal("opaque-payload", resolved!.Payload);
    }

    [Fact]
    public async Task GroupBroadcastsToPeersButHostDestinationOnlyReachesHost()
    {
        using var http = factory.CreateClient();
        using var response = await http.PostAsync("/api/sessions", null);
        var session = (await response.Content.ReadFromJsonAsync<CreatedSession>())!;

        using var host = await ConnectAsync(session.SessionId, session.HostToken);
        using var sender = await ConnectAsync(session.SessionId, session.MemberToken);
        using var otherMember = await ConnectAsync(session.SessionId, session.MemberToken);

        const string groupMessage = "{\"destination\":\"group\",\"ciphertext\":\"opaque-group\"}";
        await SendAsync(sender, groupMessage);
        Assert.Equal(groupMessage, await ReceiveAsync(host));
        Assert.Equal(groupMessage, await ReceiveAsync(otherMember));

        const string privateMessage = "{\"destination\":\"host\",\"ciphertext\":\"opaque-private\"}";
        await SendAsync(sender, privateMessage);
        Assert.Equal(privateMessage, await ReceiveAsync(host));

        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReceiveAsync(otherMember, timeout.Token));
    }

    [Fact]
    public async Task ConnectWithInvalidTokenOrUnknownSessionFails()
    {
        var client = factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers["Authorization"] = "Bearer wrong-token";
        
        await Assert.ThrowsAnyAsync<Exception>(() =>
            client.ConnectAsync(new Uri("ws://localhost/api/sessions/unknown-session/connect"), CancellationToken.None));
    }

    private async Task<WebSocket> ConnectAsync(string sessionId, string token)
    {
        var client = factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers["Authorization"] = $"Bearer {token}";
        return await client.ConnectAsync(new Uri($"ws://localhost/api/sessions/{sessionId}/connect"), CancellationToken.None);
    }

    private static Task SendAsync(WebSocket socket, string message)
    {
        return socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task<string> ReceiveAsync(WebSocket socket, CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[4096];
        var result = await socket.ReceiveAsync(buffer, cancellationToken);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }
}