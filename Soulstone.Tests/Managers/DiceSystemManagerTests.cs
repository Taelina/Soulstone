using System;
using System.IO;
using FluentAssertions;
using Xunit;
using Soulstone.Datamodels;
using Soulstone.Managers;

namespace Soulstone.Tests.Managers
{
    [Collection("NonParallel")]
    public class DiceSystemManagerTests : IDisposable
    {
        private readonly string tempDirectory;

        public DiceSystemManagerTests()
        {
            TestHelper.EnsureMockServices();
            tempDirectory = Path.Combine(Path.GetTempPath(), "SoulstoneDiceMgrTests_" + Guid.NewGuid().ToString("N"));
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
            var instance1 = DiceSystemManager.Instance;
            var instance2 = DiceSystemManager.Instance;

            // Assert
            instance1.Should().NotBeNull();
            instance1.Should().BeSameAs(instance2);
        }

        [Fact]
        public void CurrentDiceSystem_SetAndGet_StoresReference()
        {
            // Arrange
            var customSystem = new DiceSystem
            {
                SystemName = "Manager Test System",
                DiceType = DiceType.d12
            };

            // Act
            DiceSystemManager.Instance.CurrentDiceSystem = customSystem;

            // Assert
            DiceSystemManager.Instance.CurrentDiceSystem.Should().BeSameAs(customSystem);
            DiceSystemManager.Instance.CurrentDiceSystem.SystemName.Should().Be("Manager Test System");
        }

        [Fact]
        public void Init_ShouldLoadOrCreateStandardDiceSystem()
        {
            // Act
            DiceSystemManager.Instance.Init();

            // Assert
            DiceSystemManager.Instance.CurrentDiceSystem.Should().NotBeNull();
            DiceSystemManager.Instance.CurrentDiceSystem.SystemName.Should().Be("Standard Dice System");
        }

        [Fact]
        public void Init_WithLastActiveDiceSystemInConfig_LoadsSavedDiceSystem()
        {
            // Arrange
            var customSystem = new DiceSystem
            {
                SystemName = "Custom SciFi System",
                DiceType = DiceType.d100
            };
            DiceSystem.SaveDiceSystem(customSystem);

            var config = new Soulstone.Configuration
            {
                LastActiveDiceSystem = "Custom SciFi System"
            };

            // Act
            DiceSystemManager.Instance.Init(config);

            // Assert
            DiceSystemManager.Instance.CurrentDiceSystem.Should().NotBeNull();
            DiceSystemManager.Instance.CurrentDiceSystem!.SystemName.Should().Be("Custom SciFi System");
        }

        [Fact]
        public void SwitchDiceSystem_UpdatesConfigurationLastActiveDiceSystem()
        {
            // Arrange
            var customSystem = new DiceSystem
            {
                SystemName = "Cyberpunk 2020",
                DiceType = DiceType.d10
            };
            DiceSystem.SaveDiceSystem(customSystem);

            var config = new Soulstone.Configuration();
            DiceSystemManager.Instance.Init(config);

            // Act
            DiceSystemManager.Instance.SwitchDiceSystem(customSystem);

            // Assert
            config.LastActiveDiceSystem.Should().Be("Cyberpunk 2020");
        }

        [Fact]
        public void LoadDiceSystem_WhenFileIsCorrupted_ReturnsNullAndDoesNotThrow()
        {
            // Arrange
            var diceSysDir = Path.Combine(tempDirectory, "diceSystem");
            Directory.CreateDirectory(diceSysDir);
            var corruptedPath = Path.Combine(diceSysDir, "corrupted_sys.json");
            File.WriteAllText(corruptedPath, "{ invalid json content !!!");

            // Act
            var loaded = DiceSystem.LoadDiceSystem(corruptedPath, isFullPath: true);

            // Assert
            loaded.Should().BeNull();
        }

        [Fact]
        public void SaveDiceSystem_WhenSystemIsNull_DoesNotThrow()
        {
            // Act & Assert
            var action = () => DiceSystem.SaveDiceSystem(null!);
            action.Should().NotThrow();
        }
    }
}
