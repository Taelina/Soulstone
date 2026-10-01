using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Soulstone.SyncServer;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Soulstone.SyncServer.Tests;

public class DiceSystemRegistryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public DiceSystemRegistryTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void Registry_PublishesTenCharacterCodeLinkedToPlayer()
    {
        var registry = new DiceSystemRegistry(TimeProvider.System, NullLogger<DiceSystemRegistry>.Instance);
        var request = new PublishDiceSystemRequest("Player Name", "Moogle", "My System", "{\"systemName\":\"My System\"}");

        Assert.True(registry.TryPublish(request, out var published));
        Assert.Matches("^[A-Z0-9]{10}$", published.Code);
        Assert.Equal("Player Name", published.PlayerName);
        Assert.Equal("Moogle", published.WorldName);
        Assert.True(registry.TryGet(published.Code.ToLowerInvariant(), out var downloaded));
        Assert.Equal(request.Payload, downloaded.Payload);
    }

    [Fact]
    public void Registry_RejectsInvalidOrOversizedPublications()
    {
        var registry = new DiceSystemRegistry(TimeProvider.System, NullLogger<DiceSystemRegistry>.Instance);

        Assert.False(registry.TryPublish(new PublishDiceSystemRequest("", "Moogle", "System", "{}"), out _));
        Assert.False(registry.TryPublish(new PublishDiceSystemRequest("Player", "Moogle", "", "{}"), out _));
        Assert.False(registry.TryPublish(new PublishDiceSystemRequest("Player", "Moogle", "System", new string('x', DiceSystemRegistry.MaxPayloadLength + 1)), out _));
    }

    [Fact]
    public void Registry_RepublishesOwnedSystemUnderTheSameCode()
    {
        var registry = new DiceSystemRegistry(TimeProvider.System, NullLogger<DiceSystemRegistry>.Instance);
        Assert.True(registry.TryPublish(new PublishDiceSystemRequest("Player", "Moogle", "System", "{\"version\":1}"), out var first));

        Assert.True(registry.TryPublish(new PublishDiceSystemRequest("Player", "Moogle", "System", "{\"version\":2}", first.Code), out var updated));
        Assert.Equal(first.Code, updated.Code);
        Assert.True(registry.TryGet(first.Code, out var downloaded));
        Assert.Equal("{\"version\":2}", downloaded.Payload);
        Assert.False(registry.TryPublish(new PublishDiceSystemRequest("Other Player", "Moogle", "System", "{}", first.Code), out _));
    }

    [Fact]
    public async Task HttpEndpoints_PublishDownloadAndCheckVersion()
    {
        using var client = factory.CreateClient();
        var request = new PublishDiceSystemRequest("Player Name", "Moogle", "My System", "{\"systemName\":\"My System\"}");

        var publishResponse = await client.PostAsJsonAsync("/api/dice-systems", request);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        var published = await publishResponse.Content.ReadFromJsonAsync<PublishedDiceSystemResponse>();
        Assert.NotNull(published);

        var downloadResponse = await client.GetAsync($"/api/dice-systems/{published.Code}");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        var downloaded = await downloadResponse.Content.ReadFromJsonAsync<PublishedDiceSystemResponse>();
        Assert.Equal(request.Payload, downloaded!.Payload);
        Assert.Equal(request.PlayerName, downloaded.PlayerName);

        var versionResponse = await client.GetAsync($"/api/dice-systems/{published.Code}/version");
        Assert.Equal(HttpStatusCode.OK, versionResponse.StatusCode);
        var version = await versionResponse.Content.ReadFromJsonAsync<DiceSystemVersionResponse>();
        Assert.Equal(published.UpdatedAtUtc, version!.UpdatedAtUtc);
    }
}
