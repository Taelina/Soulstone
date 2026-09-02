using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Soulstone.Datamodels;
using Xunit;

namespace Soulstone.Tests.Datamodels
{
    public class GenericResourceTests
    {
        [Fact]
        public void ResourceDefinition_ConstructorsAndProperties_WorkCorrectly()
        {
            var defDefault = new ResourceDefinition();
            defDefault.Name.Should().BeEmpty();
            defDefault.DefaultMax.Should().Be(100);
            defDefault.DefaultCurrent.Should().Be(100);
            defDefault.ColorHex.Should().Be("#2ecc71");
            defDefault.IsRequired.Should().BeFalse();

            var def = new ResourceDefinition("Stamina", 150, 150, "#e67e22", "Physical stamina", isRequired: true);
            def.Name.Should().Be("Stamina");
            def.DefaultMax.Should().Be(150);
            def.DefaultCurrent.Should().Be(150);
            def.ColorHex.Should().Be("#e67e22");
            def.Description.Should().Be("Physical stamina");
            def.IsRequired.Should().BeTrue();

            var clone = def.Clone();
            clone.Name.Should().Be("Stamina");
            clone.DefaultMax.Should().Be(150);
            clone.ColorHex.Should().Be("#e67e22");
        }

        [Fact]
        public void CharacterResource_ConstructorsAndTotalMaxValue_WorkCorrectly()
        {
            var resDefault = new CharacterResource();
            resDefault.Name.Should().BeEmpty();
            resDefault.CurrentValue.Should().Be(0);
            resDefault.MaxValue.Should().Be(0);
            resDefault.TempBonus.Should().Be(0);
            resDefault.TotalMaxValue.Should().Be(0);

            var res = new CharacterResource("Mana", 80, 100, 20);
            res.Name.Should().Be("Mana");
            res.CurrentValue.Should().Be(80);
            res.MaxValue.Should().Be(100);
            res.TempBonus.Should().Be(20);
            res.TotalMaxValue.Should().Be(120);

            var clone = res.Clone();
            clone.Name.Should().Be("Mana");
            clone.CurrentValue.Should().Be(80);
            clone.MaxValue.Should().Be(100);
            clone.TempBonus.Should().Be(20);
            clone.TotalMaxValue.Should().Be(120);
        }

        [Fact]
        public void DiceSystem_GenericResources_AddRemoveAndDefaults()
        {
            var system = new DiceSystem
            {
                systemHasManaOrResourcePoints = true
            };

            // Defaults when empty
            var effective = system.GetEffectiveResources();
            effective.Should().HaveCount(2);
            effective[0].Name.Should().Be("Health");
            effective[1].Name.Should().Be("Mana");

            // Add custom resource
            system.AddResource(new ResourceDefinition("Rage", 100, 0, "#e74c3c", "Combat rage"));
            system.SystemResources.Should().HaveCount(1);
            system.SystemResources[0].Name.Should().Be("Rage");

            // Replace existing by name
            system.AddResource(new ResourceDefinition("Rage", 120, 0, "#e74c3c", "Updated rage"));
            system.SystemResources.Should().HaveCount(1);
            system.SystemResources[0].DefaultMax.Should().Be(120);

            // Remove
            system.RemoveResource("Rage").Should().BeTrue();
            system.SystemResources.Should().BeEmpty();
            system.RemoveResource("Rage").Should().BeFalse();
        }

        [Fact]
        public void CharacterSheet_GenericResourcesAndLegacyFieldsSync()
        {
            var sheet = new CharacterSheet();
            sheet.characterHealthPoints = 75;
            sheet.characterMaxHealthPoints = 120;
            sheet.characterManaPoints = 40;
            sheet.characterMaxManaPoints = 60;

            sheet.SyncResourcesWithLegacyFields();

            sheet.CharacterResources.Should().ContainKey("Health");
            sheet.CharacterResources["Health"].CurrentValue.Should().Be(75);
            sheet.CharacterResources["Health"].MaxValue.Should().Be(120);

            sheet.CharacterResources.Should().ContainKey("Mana");
            sheet.CharacterResources["Mana"].CurrentValue.Should().Be(40);
            sheet.CharacterResources["Mana"].MaxValue.Should().Be(60);

            // Updating via SetResourceCurrent / SetResourceMax
            sheet.SetResourceCurrent("Health", 90);
            sheet.CharacterHealthPoints.Should().Be(90);
            sheet.CharacterResources["Health"].CurrentValue.Should().Be(90);

            sheet.SetResourceMax("Health", 150);
            sheet.CharacterMaxHealthPoints.Should().Be(150);
            sheet.CharacterResources["Health"].MaxValue.Should().Be(150);

            // Adding a custom resource like Focus
            sheet.SetResourceCurrent("Focus", 50);
            sheet.SetResourceMax("Focus", 100);
            sheet.CharacterResources["Focus"].CurrentValue.Should().Be(50);
            sheet.CharacterResources["Focus"].MaxValue.Should().Be(100);

            // Effective Resources with DiceSystem
            var system = new DiceSystem();
            system.AddResource(new ResourceDefinition("Stamina", 200, 200, "#f39c12"));

            var allResources = sheet.GetEffectiveResources(system);
            allResources.Should().Contain(r => r.Name == "Health");
            allResources.Should().Contain(r => r.Name == "Mana");
            allResources.Should().Contain(r => r.Name == "Focus");
            allResources.Should().Contain(r => r.Name == "Stamina");
        }

        [Fact]
        public void JsonSerialization_PreservesResourcesAndEquippedGear()
        {
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Mage Hero",
                CharacterHealthPoints = 80,
                CharacterMaxHealthPoints = 100,
                CharacterManaPoints = 120,
                CharacterMaxManaPoints = 150
            };

            sheet.SetResourceCurrent("Shield", 50);
            sheet.SetResourceMax("Shield", 50);

            var staff = new GearItem("Archmage Staff", "MainHand", "Ancient staff", "Legendary");
            staff.SetStatModifier("Intelligence", 8);
            staff.SetStatModifier("Mana", 30);
            sheet.AddItem(staff);
            sheet.EquipGear(staff);

            var json = JsonSerializer.Serialize(sheet);
            var deserialized = JsonSerializer.Deserialize<CharacterSheet>(json);

            deserialized.Should().NotBeNull();
            deserialized!.CharacterFullName.Should().Be("Mage Hero");
            deserialized.CharacterHealthPoints.Should().Be(80);
            deserialized.CharacterMaxHealthPoints.Should().Be(100);
            deserialized.CharacterManaPoints.Should().Be(120);
            deserialized.CharacterMaxManaPoints.Should().Be(150);
            deserialized.CharacterResources.Should().ContainKey("Shield");
            deserialized.CharacterResources["Shield"].CurrentValue.Should().Be(50);

            deserialized.EquippedGear.Should().ContainKey("MainHand");
            deserialized.IsItemEquipped(staff.Id).Should().BeTrue();
            deserialized.GetEquippedGear("MainHand").Should().NotBeNull();
            deserialized.GetEquippedGear("MainHand")!.Name.Should().Be("Archmage Staff");
            deserialized.GetGearStatBonus("Intelligence").Should().Be(8);
            deserialized.GetGearStatBonus("Mana").Should().Be(30);
            deserialized.GetEffectiveResourceMax("Mana").Should().Be(180); // 150 + 30
        }
    }
}
