using System;
using System.IO;
using FluentAssertions;
using Xunit;
using Soulstone.Datamodels;
using Soulstone.Managers;

namespace Soulstone.Tests.Managers
{
    [Collection("NonParallel")]
    public class CharacterManagerTests : IDisposable
    {
        private readonly string tempDirectory;

        public CharacterManagerTests()
        {
            TestHelper.EnsureMockServices();
            tempDirectory = Path.Combine(Path.GetTempPath(), "SoulstoneCharMgrTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            Plugin.dataLocation = tempDirectory;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Fact]
        public void Instance_ShouldReturnSingleton()
        {
            // Act
            var instance1 = CharacterManager.Instance;
            var instance2 = CharacterManager.Instance;

            // Assert
            instance1.Should().NotBeNull();
            instance1.Should().BeSameAs(instance2);
        }

        [Fact]
        public void CharacterSheet_SetAndGet_StoresReference()
        {
            // Arrange
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Estinien Varlineau"
            };

            // Act
            CharacterManager.Instance.CharacterSheet = sheet;

            // Assert
            CharacterManager.Instance.CharacterSheet.Should().BeSameAs(sheet);
            CharacterManager.Instance.CharacterSheet.CharacterFullName.Should().Be("Estinien Varlineau");
        }

        [Fact]
        public void ForceLoadCharData_LoadsCharacterSheetIntoManager()
        {
            // Arrange
            var sheet = new CharacterSheet
            {
                CharacterFullName = "G'raha Tia",
                CharacterClass = "All-Rounder",
                CharacterLevel = 90
            };
            CharacterSheet.SaveSheet(sheet);

            // Act
            CharacterManager.Instance.ForceLoadCharData("G'raha Tia");

            // Assert
            CharacterManager.Instance.CharacterSheet.Should().NotBeNull();
            CharacterManager.Instance.CharacterSheet!.CharacterFullName.Should().Be("G'raha Tia");
            CharacterManager.Instance.CharacterSheet.CharacterClass.Should().Be("All-Rounder");
            CharacterManager.Instance.CharacterSheet.CharacterLevel.Should().Be(90);
        }

        [Fact]
        public void Init_WhenObjectTableOrLocalPlayerIsNull_DoesNotThrow()
        {
            // Act & Assert
            var action = () => CharacterManager.Instance.Init();
            action.Should().NotThrow();
        }

        [Fact]
        public void Reset_ClearsLoadedSheetAndState()
        {
            // Arrange
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Alphinaud Leveilleur"
            };
            CharacterManager.Instance.CharacterSheet = sheet;

            // Act
            CharacterManager.Instance.Reset();

            // Assert
            CharacterManager.Instance.CharacterSheet.Should().BeNull();
        }

        [Fact]
        public void ForceLoadCharData_WhenCharacterChanges_UpdatesSheet()
        {
            // Arrange
            var sheet1 = new CharacterSheet
            {
                CharacterFullName = "Alisaie Leveilleur",
                CharacterClass = "Red Mage"
            };
            var sheet2 = new CharacterSheet
            {
                CharacterFullName = "Y'shtola Rhul",
                CharacterClass = "Sorceress"
            };
            CharacterSheet.SaveSheet(sheet1);
            CharacterSheet.SaveSheet(sheet2);

            // Act
            CharacterManager.Instance.ForceLoadCharData("Alisaie Leveilleur");
            CharacterManager.Instance.CharacterSheet!.CharacterFullName.Should().Be("Alisaie Leveilleur");

            CharacterManager.Instance.ForceLoadCharData("Y'shtola Rhul");

            // Assert
            CharacterManager.Instance.CharacterSheet.Should().NotBeNull();
            CharacterManager.Instance.CharacterSheet!.CharacterFullName.Should().Be("Y'shtola Rhul");
            CharacterManager.Instance.CharacterSheet.CharacterClass.Should().Be("Sorceress");
        }

        [Fact]
        public void LoadSheet_WhenFileIsCorrupted_ReturnsNullAndDoesNotThrow()
        {
            // Arrange
            var sheetsDir = Path.Combine(tempDirectory, "sheets");
            Directory.CreateDirectory(sheetsDir);
            var corruptedPath = Path.Combine(sheetsDir, "corrupted_char.json");
            File.WriteAllText(corruptedPath, "{ invalid json content !!!");

            // Act
            var loaded = CharacterSheet.LoadSheet(corruptedPath, isFullPath: true);

            // Assert
            loaded.Should().BeNull();
        }

        [Fact]
        public void SaveSheet_WhenSheetIsNull_DoesNotThrow()
        {
            // Act & Assert
            var action = () => CharacterSheet.SaveSheet(null!);
            action.Should().NotThrow();
        }
    }
}
