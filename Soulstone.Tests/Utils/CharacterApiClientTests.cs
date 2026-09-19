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
    }
}
