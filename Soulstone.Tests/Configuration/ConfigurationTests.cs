using System.Collections.Generic;
using System.Text.Json;
using Dalamud.Plugin;
using FluentAssertions;
using Moq;
using Xunit;
using Soulstone.Localizations;

namespace Soulstone.Tests.Configuration
{
    [Collection("NonParallel")]
    public class ConfigurationTests
    {
        public ConfigurationTests()
        {
            TestHelper.EnsureMockServices();
        }

        [Fact]
        public void DefaultConstructor_InitializesDefaultValues()
        {
            // Act
            var config = new Soulstone.Configuration();

            // Assert
            config.Version.Should().Be(0);
            config.IsConfigWindowMovable.Should().BeTrue();
            config.SomePropertyToBeSavedAndWithADefault.Should().BeTrue();
            config.detailedRolls.Should().BeFalse();
            config.showEpicBonus.Should().BeFalse();
            config.Language.Should().Be(Language.Français);
            config.PinnedFileBrowserPaths.Should().NotBeNull().And.BeEmpty();
            config.LastBrowserDirectory.Should().BeNull();
        }

        [Fact]
        public void Properties_SetAndGet_UpdatesCorrectly()
        {
            // Arrange
            var config = new Soulstone.Configuration();

            // Act
            config.Version = 2;
            config.IsConfigWindowMovable = false;
            config.SomePropertyToBeSavedAndWithADefault = false;
            config.detailedRolls = true;
            config.showEpicBonus = true;
            config.Language = Language.English;
            config.LastBrowserDirectory = @"C:\Soulstone\Sheets";
            config.PinnedFileBrowserPaths.Add(@"C:\Soulstone\Sheets\MainChar.json");

            // Assert
            config.Version.Should().Be(2);
            config.IsConfigWindowMovable.Should().BeFalse();
            config.SomePropertyToBeSavedAndWithADefault.Should().BeFalse();
            config.detailedRolls.Should().BeTrue();
            config.showEpicBonus.Should().BeTrue();
            config.Language.Should().Be(Language.English);
            config.LastBrowserDirectory.Should().Be(@"C:\Soulstone\Sheets");
            config.PinnedFileBrowserPaths.Should().ContainSingle().Which.Should().Be(@"C:\Soulstone\Sheets\MainChar.json");
        }

        [Fact]
        public void JsonSerialization_PreservesAllProperties()
        {
            // Arrange
            var original = new Soulstone.Configuration
            {
                Version = 3,
                IsConfigWindowMovable = true,
                detailedRolls = true,
                showEpicBonus = true,
                Language = Language.English,
                LastBrowserDirectory = @"C:\Games\FFXIV",
                PinnedFileBrowserPaths = new List<string> { @"C:\Path1", @"C:\Path2" }
            };

            // Act
            string json = JsonSerializer.Serialize(original, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
            var deserialized = JsonSerializer.Deserialize<Soulstone.Configuration>(json, new JsonSerializerOptions { IncludeFields = true });

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Version.Should().Be(3);
            deserialized.IsConfigWindowMovable.Should().BeTrue();
            deserialized.detailedRolls.Should().BeTrue();
            deserialized.showEpicBonus.Should().BeTrue();
            deserialized.Language.Should().Be(Language.English);
            deserialized.LastBrowserDirectory.Should().Be(@"C:\Games\FFXIV");
            deserialized.PinnedFileBrowserPaths.Should().BeEquivalentTo(new[] { @"C:\Path1", @"C:\Path2" });
        }

        [Fact]
        public void Save_CallsSavePluginConfigOnPluginInterface()
        {
            // Arrange
            var mockPluginInterface = new Mock<IDalamudPluginInterface>();
            Plugin.PluginInterface = mockPluginInterface.Object;

            var config = new Soulstone.Configuration
            {
                Language = Language.English,
                detailedRolls = true
            };

            // Act
            config.Save();

            // Assert
            mockPluginInterface.Verify(x => x.SavePluginConfig(config), Times.Once);
        }
    }
}
