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
    }
}
