using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Soulstone.SyncServer;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace Soulstone.SyncServer.Tests;

public class PublicationSecurityTests : IClassFixture<RelayWebApplicationFactory>
{
    private readonly RelayWebApplicationFactory factory;
    private static readonly string Owner = new('A', 64);
    private static readonly string Other = new('B', 64);

    public PublicationSecurityTests(RelayWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task CharacterWrites_RequireTheOriginalCredential_AndReadsRemainPublic()
    {
        using var client = factory.CreateClient();
        string path = $"/api/characters/{Guid.NewGuid():N}/Moogle";
        using var anonymousPut = await client.PutAsync(path, Json("{}"));
        anonymousPut.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Owner);
        using var created = await client.PutAsync(path, Json("{\"name\":\"original\"}"));
        created.StatusCode.Should().Be(HttpStatusCode.NoContent);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Other);
        using var overwrite = await client.PutAsync(path, Json("{\"name\":\"forged\"}"));
        overwrite.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var deletion = await client.DeleteAsync(path);
        deletion.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        client.DefaultRequestHeaders.Authorization = null;
        (await client.GetStringAsync(path)).Should().Be("{\"name\":\"original\"}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Owner);
        using var deleted = await client.DeleteAsync(path);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DiceSystem_PublicMetadataCannotAuthorizeAnOverwrite()
    {
        using var client = factory.CreateClient();
        var request = new PublishDiceSystemRequest("Player", "Moogle", "Rules", "{}");
        using var anonymous = await client.PostAsJsonAsync("/api/dice-systems", request);
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Owner);
        using var created = await client.PostAsJsonAsync("/api/dice-systems", request);
        var published = await created.Content.ReadFromJsonAsync<PublishedDiceSystemResponse>();
        published.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = null;
        var publicJson = await client.GetStringAsync($"/api/dice-systems/{published!.Code}");
        publicJson.Should().NotContain(Owner).And.NotContain("ownerToken");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Other);
        using var forged = await client.PostAsJsonAsync("/api/dice-systems", request with { Code = published.Code, Payload = "{\"forged\":true}" });
        forged.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Owner);
        using var updated = await client.PostAsJsonAsync("/api/dice-systems", request with { Code = published.Code, Payload = "{\"updated\":true}" });
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MalformedNullAndOversizedPayloads_AreRejected()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Owner);
        using var nullWorld = await client.PostAsJsonAsync("/api/dice-systems", new PublishDiceSystemRequest("Player", null!, "Rules", "{}"));
        nullWorld.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var invalidJson = await client.PutAsync("/api/characters/Invalid/Moogle", Json("not json"));
        invalidJson.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var oversized = await client.PutAsync("/api/characters/Large/Moogle", Json(new string('x', CharacterSheetRegistry.MaxPayloadLength + 1)));
        oversized.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public void WorldQualifiedLookup_DoesNotReturnAnotherWorld()
    {
        using var storage = new TestStorage();
        using var database = storage.Open();
        var registry = new CharacterSheetRegistry(database, TimeProvider.System, NullLogger<CharacterSheetRegistry>.Instance);
        registry.TryStore("Player", "Moogle", "{}", Owner).Should().BeTrue();
        registry.TryGet("Player", "Ragnarok", out _).Should().BeFalse();
    }

    [Fact]
    public void RegistryKeys_DoNotCollideWhenNamesContainSeparators()
    {
        using var storage = new TestStorage();
        using var database = storage.Open();
        var registry = new CharacterSheetRegistry(database, TimeProvider.System, NullLogger<CharacterSheetRegistry>.Instance);
        registry.TryStore("A@B", "C", "{\"value\":1}", Owner).Should().BeTrue();
        registry.TryStore("A", "B@C", "{\"value\":2}", Other).Should().BeTrue();
        registry.TryGet("A@B", "C", out var first).Should().BeTrue();
        first.Should().Be("{\"value\":1}");
    }

    [Fact]
    public void Registries_ExpirePublications_AndReleaseCapacity()
    {
        var clock = new MutableTimeProvider();
        using var storage = new TestStorage();
        using var database = storage.Open();
        var sheets = new CharacterSheetRegistry(database, clock, NullLogger<CharacterSheetRegistry>.Instance);
        var systems = new DiceSystemRegistry(database, clock, NullLogger<DiceSystemRegistry>.Instance);
        sheets.TryStore("Player", "Moogle", "{}", Owner).Should().BeTrue();
        systems.TryPublish(new PublishDiceSystemRequest("Player", "Moogle", "Rules", "{}"), out var published, Owner).Should().BeTrue();
        clock.Now += TimeSpan.FromDays(31);
        sheets.CleanupExpired().Should().Be(1);
        systems.CleanupExpired().Should().Be(1);
        systems.TryGet(published!.Code, out _).Should().BeFalse();
        sheets.TryStore("Player", "Moogle", "{}", Other).Should().BeTrue();
    }

    [Fact]
    public void CharacterRegistry_EnforcesTotalCapacity_ButAllowsOwnedUpdates()
    {
        using var storage = new TestStorage();
        using var database = storage.Open();
        var registry = new CharacterSheetRegistry(database, TimeProvider.System, NullLogger<CharacterSheetRegistry>.Instance);
        string payload = "{\"data\":\"" + new string('x', CharacterSheetRegistry.MaxPayloadLength - 11) + "\"}";
        for (int i = 0; i < 32; i++)
            registry.TryStore($"Player{i}", "Moogle", payload, Owner).Should().BeTrue();
        database.Dispose();
        using var reopened = storage.Open();
        registry = new CharacterSheetRegistry(reopened, TimeProvider.System, NullLogger<CharacterSheetRegistry>.Instance);
        registry.Store("Overflow", "Moogle", payload, Owner).Should().Be(RegistryWriteResult.Full);
        registry.TryStore("Player0", "Moogle", "{}", Owner).Should().BeTrue();
        registry.TryStore("Overflow", "Moogle", "{}", Owner).Should().BeTrue();
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private sealed class MutableTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
