using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Soulstone.SyncServer;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace Soulstone.SyncServer.Tests;

public class CharacterSheetRegistryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Token = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private readonly WebApplicationFactory<Program> factory;

    public CharacterSheetRegistryTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void Registry_StoresAndRetrievesCharacterSheets()
    {
        var registry = new CharacterSheetRegistry(TimeProvider.System, NullLogger<CharacterSheetRegistry>.Instance);

        Assert.True(registry.TryStore("Test Character", "Ragnarok", "{\"name\":\"Test Character\"}", Token));
        Assert.True(registry.TryGet("Test Character", "Ragnarok", out var payload));
        Assert.Equal("{\"name\":\"Test Character\"}", payload);

        // Case insensitivity
        Assert.True(registry.TryGet("test character", "ragnarok", out var payloadCase));
        Assert.Equal("{\"name\":\"Test Character\"}", payloadCase);

        // Fallback without world
        Assert.True(registry.TryGet("test character", null, out var payloadNoWorld));
        Assert.Equal("{\"name\":\"Test Character\"}", payloadNoWorld);

        // Delete
        Assert.True(registry.TryDelete("test character", "ragnarok", Token));
        Assert.False(registry.TryGet("test character", "ragnarok", out _));
    }

    [Fact]
    public void Registry_RejectsEmptyOrOversizedPayloads()
    {
        var registry = new CharacterSheetRegistry(TimeProvider.System, NullLogger<CharacterSheetRegistry>.Instance);

        Assert.False(registry.TryStore("", "Ragnarok", "{}", Token));
        Assert.False(registry.TryStore("Test", "Ragnarok", "", Token));
        Assert.False(registry.TryStore("Test", "Ragnarok", new string('x', CharacterSheetRegistry.MaxPayloadLength + 1), Token));
    }

    [Fact]
    public async Task HttpEndpoints_PutAndGetCharacterSheet()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);

        const string charName = "Taelina Vael";
        const string world = "Moogle";
        const string sheetJson = "{\"characterFullName\":\"Taelina Vael\",\"characterRace\":\"Elezen\"}";

        // Put with world
        var putResponse = await client.PutAsync($"/api/characters/{Uri.EscapeDataString(charName)}/{Uri.EscapeDataString(world)}",
            new StringContent(sheetJson, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        // Get with world
        var getResponse = await client.GetAsync($"/api/characters/{Uri.EscapeDataString(charName)}/{Uri.EscapeDataString(world)}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var content = await getResponse.Content.ReadAsStringAsync();
        Assert.Equal(sheetJson, content);

        // Get fallback without world
        var getNoWorldResponse = await client.GetAsync($"/api/characters/{Uri.EscapeDataString(charName)}");
        Assert.Equal(HttpStatusCode.OK, getNoWorldResponse.StatusCode);
        var noWorldContent = await getNoWorldResponse.Content.ReadAsStringAsync();
        Assert.Equal(sheetJson, noWorldContent);

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/characters/{Uri.EscapeDataString(charName)}/{Uri.EscapeDataString(world)}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Get after delete
        var getAfterDelete = await client.GetAsync($"/api/characters/{Uri.EscapeDataString(charName)}/{Uri.EscapeDataString(world)}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }
}
