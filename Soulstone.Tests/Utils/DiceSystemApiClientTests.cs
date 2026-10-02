using FluentAssertions;
using Soulstone.Datamodels;
using Soulstone.Utils;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Soulstone.Tests.Utils;

public class DiceSystemApiClientTests
{
    [Fact]
    public async Task InvalidInputs_DoNotSendRequests()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("A request should not be sent"));
        var client = new DiceSystemApiClient(new HttpClient(handler));

        (await client.DownloadAsync("http://localhost", "short")).Should().BeNull();
        (await client.GetVersionAsync("", "ABCDEFGHIJ")).Should().BeNull();
        (await client.PublishAsync("http://localhost", new DiceSystem(), "", "Moogle")).Should().BeNull();
    }

    [Fact]
    public async Task DownloadAsync_DeserializesPublicationAndPayloadMetadata()
    {
        const string response = "{\"code\":\"ABCDEF1234\",\"playerName\":\"Player Name\",\"worldName\":\"Moogle\",\"systemName\":\"My System\",\"payload\":\"{\\\"systemName\\\":\\\"My System\\\"}\",\"updatedAtUtc\":\"2026-10-01T15:00:00Z\"}";
        var client = new DiceSystemApiClient(new HttpClient(new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "application/json") })));

        var published = await client.DownloadAsync("http://localhost:5077", "ABCDEF1234");
        var system = DiceSystemApiClient.DeserializeSystem(published!);

        system.Should().NotBeNull();
        system!.systemName.Should().Be("My System");
        system.publishedCode.Should().Be("ABCDEF1234");
        system.publisherPlayerName.Should().Be("Player Name");
        system.publishedAtUtc.Should().Be(DateTimeOffset.Parse("2026-10-01T15:00:00Z"));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
