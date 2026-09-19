using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Soulstone.Datamodels;
using Soulstone.Utils;
using Xunit;

namespace Soulstone.Tests.Utils
{
    [Collection("NonParallel")]
    public class CharacterApiClientTests
    {
        public CharacterApiClientTests()
        {
            TestHelper.EnsureMockServices();
        }

        [Fact]
        public async Task UploadCharacterSheetAsync_WhenInputInvalid_ReturnsFalse()
        {
            var sheet = new CharacterSheet { CharacterFullName = "Test Character" };

            (await CharacterApiClient.UploadCharacterSheetAsync("", sheet)).Should().BeFalse();
            (await CharacterApiClient.UploadCharacterSheetAsync("http://localhost:5077", null!)).Should().BeFalse();
            (await CharacterApiClient.UploadCharacterSheetAsync("http://localhost:5077", new CharacterSheet { CharacterFullName = "" })).Should().BeFalse();
        }

        [Fact]
        public async Task FetchCharacterSheetAsync_WhenInputInvalid_ReturnsNull()
        {
            (await CharacterApiClient.FetchCharacterSheetAsync("", "Test Character")).Should().BeNull();
            (await CharacterApiClient.FetchCharacterSheetAsync("http://localhost:5077", "")).Should().BeNull();
        }

        [Fact]
        public async Task DeleteCharacterSheetAsync_WhenInputInvalid_ReturnsFalse()
        {
            (await CharacterApiClient.DeleteCharacterSheetAsync("", "Test Character")).Should().BeFalse();
            (await CharacterApiClient.DeleteCharacterSheetAsync("http://localhost:5077", "")).Should().BeFalse();
        }

        [Theory]
        [InlineData(null, "http://82.65.2.251:5077")]
        [InlineData("", "http://82.65.2.251:5077")]
        [InlineData("   ", "http://82.65.2.251:5077")]
        [InlineData("82.65.2.251:5077", "http://82.65.2.251:5077")]
        [InlineData("82.65.2.251:5077/", "http://82.65.2.251:5077")]
        [InlineData("http://82.65.2.251:5077", "http://82.65.2.251:5077")]
        [InlineData("http://82.65.2.251:5077/", "http://82.65.2.251:5077")]
        [InlineData("https://custom.relay.net:5077", "https://custom.relay.net:5077")]
        [InlineData("192.168.1.100:5077", "http://192.168.1.100:5077")]
        public void NormalizeServerUrl_FormatsIpAndUrlCorrectly(string? input, string expected)
        {
            Soulstone.Sync.RelayCrypto.NormalizeServerUrl(input).Should().Be(expected);
        }

        [Fact]
        public void Configuration_SyncServerUrl_DefaultsToRemoteIpAndUpgradesLegacyLocalhost()
        {
            var config = new Soulstone.Configuration();
            config.SyncServerUrl.Should().Be("http://82.65.2.251:5077");

            config.SyncServerUrl = "http://127.0.0.1:5077";
            config.SyncServerUrl.Should().Be("http://82.65.2.251:5077");

            config.SyncServerUrl = "http://192.168.1.42:5077";
            config.SyncServerUrl.Should().Be("http://192.168.1.42:5077");
        }

        [Fact]
        public void CharacterSheet_Deserialization_DoesNotThrowPropertyCollision()
        {
            var original = new CharacterSheet
            {
                CharacterFullName = "Hitomi Umezawa",
                CharacterNickName = "Hito",
                CharacterRace = "Au Ra",
                CharacterSubRace = "Raen",
                CharacterJob = "Samurai",
                CharacterQuickLook1 = "Silent blade",
                CharacterQuickLook2 = "Wears cherry blossoms",
                CharacterDistinctiveFeatures = "Intricate floral hairpins",
                CharacterReputation = "Known duelist across Hingashi"
            };

            var json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = false });
            json.Should().NotBeNullOrWhiteSpace();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var act = () => JsonSerializer.Deserialize<CharacterSheet>(json, options);
            act.Should().NotThrow();

            var deserialized = act();
            deserialized.Should().NotBeNull();
            deserialized!.CharacterFullName.Should().Be("Hitomi Umezawa");
            deserialized.CharacterQuickLook1.Should().Be("Silent blade");
            deserialized.CharacterQuickLook2.Should().Be("Wears cherry blossoms");
            deserialized.CharacterDistinctiveFeatures.Should().Be("Intricate floral hairpins");
            deserialized.CharacterReputation.Should().Be("Known duelist across Hingashi");
        }

        [Fact]
        public void CharacterSheet_Deserialization_HandlesCamelCasePayloadWithoutCollision()
        {
            const string camelCaseJson = """
            {
                "characterFullName": "Hitomi Umezawa",
                "characterNickName": "Hito",
                "characterRace": "Au Ra",
                "characterJob": "Samurai",
                "characterQuickLook1": "A calm presence",
                "characterDistinctiveFeatures": "Katana scar",
                "characterReputation": "Master Swordsman"
            }
            """;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var act = () => JsonSerializer.Deserialize<CharacterSheet>(camelCaseJson, options);
            act.Should().NotThrow();

            var sheet = act();
            sheet.Should().NotBeNull();
            sheet!.CharacterFullName.Should().Be("Hitomi Umezawa");
            sheet.CharacterNickName.Should().Be("Hito");
            sheet.CharacterRace.Should().Be("Au Ra");
            sheet.CharacterJob.Should().Be("Samurai");
            sheet.CharacterQuickLook1.Should().Be("A calm presence");
            sheet.CharacterDistinctiveFeatures.Should().Be("Katana scar");
            sheet.CharacterReputation.Should().Be("Master Swordsman");
        }
    }
}
