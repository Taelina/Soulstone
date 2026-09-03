using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Soulstone.Datamodels;

namespace Soulstone.Tests.Datamodels
{
    [Collection("NonParallel")]
    public class DiceSystemTests : IDisposable
    {
        private readonly string tempDirectory;

        public DiceSystemTests()
        {
            TestHelper.EnsureMockServices();
            tempDirectory = Path.Combine(Path.GetTempPath(), "SoulstoneTests_" + Guid.NewGuid().ToString("N"));
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
        public void DefaultConstructor_InitializesWithDefaultValues()
        {
            // Arrange & Act
            var system = new DiceSystem();

            // Assert
            system.SystemName.Should().Be("Standard Dice System");
            system.DicePoolSystemEnabled.Should().BeFalse();
            system.RegularDiceSystemEnabled.Should().BeTrue();
            system.DndStyleAttributes.Should().BeTrue();
            system.SkillLinkedToOneAttribute.Should().BeTrue();
            system.AbilityLinkedToOneAttribute.Should().BeTrue();
            system.AbilityLinkedToOneSkill.Should().BeTrue();
            system.SystemHasSaves.Should().BeTrue();
            system.SystemHasAdvantageDisadvantage.Should().BeTrue();
            system.SystemHasManaOrResourcePoints.Should().BeFalse();
            system.SystemHasClasses.Should().BeFalse();
            system.SystemHasBonusTemp.Should().BeFalse();
            system.SystemHasBonusPerm.Should().BeFalse();
            system.SystemHasEpicAttributes.Should().BeFalse();
            system.SystemHasInventoryLimit.Should().BeFalse();
            system.InventoryMaxSlots.Should().Be(30);
            system.DiceType.Should().Be(DiceType.d20);
            system.SystemType.Should().Be(SystemType.DnDSystem);
            system.SuccessThreshold.Should().Be(0);
            system.SuccessInterval.Should().Be(0);
        }

        [Fact]
        public void Properties_SetAndGet_UpdatesBackingFields()
        {
            // Arrange
            var system = new DiceSystem
            {
                SystemName = "Custom Pool System",
                DicePoolSystemEnabled = true,
                RegularDiceSystemEnabled = false,
                DndStyleAttributes = false,
                SkillLinkedToOneAttribute = false,
                AbilityLinkedToOneAttribute = false,
                AbilityLinkedToOneSkill = false,
                SystemHasSaves = false,
                SystemHasAdvantageDisadvantage = false,
                SystemHasManaOrResourcePoints = true,
                SystemHasClasses = true,
                SystemHasBonusTemp = true,
                SystemHasBonusPerm = true,
                SystemHasEpicAttributes = true,
                SystemHasInventoryLimit = true,
                InventoryMaxSlots = 45,
                DiceType = DiceType.d10,
                SystemType = SystemType.DicePoolSystem,
                SuccessThreshold = 6,
                SuccessInterval = 10
            };

            // Assert
            system.SystemName.Should().Be("Custom Pool System");
            system.systemName.Should().Be("Custom Pool System");
            system.DicePoolSystemEnabled.Should().BeTrue();
            system.dicePoolSystemEnabled.Should().BeTrue();
            system.RegularDiceSystemEnabled.Should().BeFalse();
            system.regularDiceSystemEnabled.Should().BeFalse();
            system.DndStyleAttributes.Should().BeFalse();
            system.dndStyleAttributes.Should().BeFalse();
            system.DiceType.Should().Be(DiceType.d10);
            system.diceType.Should().Be(DiceType.d10);
            system.SystemType.Should().Be(SystemType.DicePoolSystem);
            system.systemType.Should().Be(SystemType.DicePoolSystem);
            system.SuccessThreshold.Should().Be(6);
            system.successThreshold.Should().Be(6);
            system.SuccessInterval.Should().Be(10);
            system.successInterval.Should().Be(10);
            system.SystemHasBonusTemp.Should().BeTrue();
            system.SystemHasBonusPerm.Should().BeTrue();
            system.SystemHasEpicAttributes.Should().BeTrue();
            system.SystemHasInventoryLimit.Should().BeTrue();
            system.systemHasInventoryLimit.Should().BeTrue();
            system.InventoryMaxSlots.Should().Be(45);
            system.inventoryMaxSlots.Should().Be(45);
        }

        [Theory]
        [InlineData(DiceType.d4, 0)]
        [InlineData(DiceType.d6, 1)]
        [InlineData(DiceType.d8, 2)]
        [InlineData(DiceType.d10, 3)]
        [InlineData(DiceType.d12, 4)]
        [InlineData(DiceType.d20, 5)]
        [InlineData(DiceType.d100, 6)]
        public void DiceTypeEnum_MatchesExpectedIntegerValues(DiceType diceType, int expectedValue)
        {
            ((int)diceType).Should().Be(expectedValue);
        }

        [Theory]
        [InlineData(SystemType.DnDSystem, 0)]
        [InlineData(SystemType.DicePoolSystem, 1)]
        [InlineData(SystemType.PercentileSystem, 2)]
        public void SystemTypeEnum_MatchesExpectedIntegerValues(SystemType systemType, int expectedValue)
        {
            ((int)systemType).Should().Be(expectedValue);
        }

        [Fact]
        public void JsonSerialization_PreservesAllProperties()
        {
            // Arrange
            var original = new DiceSystem
            {
                SystemName = "Call of Cthulhu Style",
                SystemType = SystemType.PercentileSystem,
                DiceType = DiceType.d100,
                SuccessInterval = 5,
                SystemHasBonusPerm = true,
                SystemHasBonusTemp = true,
                SystemHasEpicAttributes = true
            };

            // Act
            string json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<DiceSystem>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.SystemName.Should().Be("Call of Cthulhu Style");
            deserialized.SystemType.Should().Be(SystemType.PercentileSystem);
            deserialized.DiceType.Should().Be(DiceType.d100);
            deserialized.SuccessInterval.Should().Be(5);
            deserialized.SystemHasBonusPerm.Should().BeTrue();
            deserialized.SystemHasBonusTemp.Should().BeTrue();
            deserialized.SystemHasEpicAttributes.Should().BeTrue();
        }

        [Fact]
        public void SaveDiceSystem_And_LoadDiceSystem_WorksWithFullFilePath()
        {
            // Arrange
            var system = new DiceSystem
            {
                SystemName = "Test Custom System",
                DiceType = DiceType.d8,
                SystemType = SystemType.DicePoolSystem,
                SuccessThreshold = 5
            };

            string filePath = Path.Combine(tempDirectory, "test_custom_system.json");
            File.WriteAllText(filePath, JsonSerializer.Serialize(system));

            // Act
            var loaded = DiceSystem.LoadDiceSystem(filePath, isFullPath: true);

            // Assert
            loaded.Should().NotBeNull();
            loaded.SystemName.Should().Be("Test Custom System");
            loaded.DiceType.Should().Be(DiceType.d8);
            loaded.SystemType.Should().Be(SystemType.DicePoolSystem);
            loaded.SuccessThreshold.Should().Be(5);
        }

        [Fact]
        public void SaveDiceSystem_CreatesDirectoryAndSavesFormattedJson()
        {
            // Arrange
            var system = new DiceSystem
            {
                SystemName = "Shadowrun System",
                DiceType = DiceType.d6,
                SystemType = SystemType.DicePoolSystem,
                SuccessThreshold = 5
            };

            // Act
            DiceSystem.SaveDiceSystem(system);

            // Assert
            string expectedPath = Path.Combine(tempDirectory, "diceSystem", "shadowrun_system.json");
            File.Exists(expectedPath).Should().BeTrue();

            var loaded = DiceSystem.LoadDiceSystem("shadowrun_system", isFullPath: false);
            loaded.Should().NotBeNull();
            loaded.SystemName.Should().Be("Shadowrun System");
            loaded.DiceType.Should().Be(DiceType.d6);
            loaded.SuccessThreshold.Should().Be(5);
        }

        [Fact]
        public void LoadDiceSystem_WhenFileNotFound_CreatesAndSavesNewSystem()
        {
            // Act
            var system = DiceSystem.LoadDiceSystem("non_existent_system", isFullPath: false);

            // Assert
            system.Should().NotBeNull();
            system.SystemName.Should().Be("Standard Dice System");
            string expectedPath = Path.Combine(tempDirectory, "diceSystem", "standard_dice_system.json");
            File.Exists(expectedPath).Should().BeTrue();
        }

        [Fact]
        public void AddAndRemoveResource_UpdatesSystemResourcesList()
        {
            var system = new DiceSystem();
            var res1 = new ResourceDefinition("Stamina", 150, 150, "#e67e22", "Stamina Points");
            var res2 = new ResourceDefinition("Fury", 100, 0, "#e74c3c", "Combat Fury");

            system.AddResource(res1);
            system.AddResource(res2);

            system.SystemResources.Should().HaveCount(2);
            system.SystemResources.Should().Contain(r => r.Name == "Stamina");
            system.SystemResources.Should().Contain(r => r.Name == "Fury");

            // Overwrite existing resource by name
            var res1Updated = new ResourceDefinition("Stamina", 200, 200, "#f39c12", "Updated Stamina");
            system.AddResource(res1Updated);
            system.SystemResources.Should().HaveCount(2);
            system.SystemResources.First(r => r.Name == "Stamina").DefaultMax.Should().Be(200);

            // Remove resource
            bool removed = system.RemoveResource("Fury");
            removed.Should().BeTrue();
            system.SystemResources.Should().HaveCount(1);
            system.SystemResources.Should().NotContain(r => r.Name == "Fury");

            bool removeNonExistent = system.RemoveResource("Unknown");
            removeNonExistent.Should().BeFalse();
        }

        [Fact]
        public void GetEffectiveResources_WhenEmpty_ReturnsDefaultsIncludingManaIfEnabled()
        {
            var system = new DiceSystem { SystemHasManaOrResourcePoints = false };
            var eff1 = system.GetEffectiveResources();
            eff1.Should().ContainSingle(r => r.Name == "Health");

            system.SystemHasManaOrResourcePoints = true;
            var eff2 = system.GetEffectiveResources();
            eff2.Should().HaveCount(2);
            eff2.Should().Contain(r => r.Name == "Health");
            eff2.Should().Contain(r => r.Name == "Mana");
        }
    }
}
