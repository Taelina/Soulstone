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
            defDefault.ShowInGroup.Should().BeTrue();

            var def = new ResourceDefinition("Stamina", 150, 150, "#e67e22", "Physical stamina", isRequired: true, showInGroup: false);
            def.Name.Should().Be("Stamina");
            def.DefaultMax.Should().Be(150);
            def.DefaultCurrent.Should().Be(150);
            def.ColorHex.Should().Be("#e67e22");
            def.Description.Should().Be("Physical stamina");
            def.IsRequired.Should().BeTrue();
            def.ShowInGroup.Should().BeFalse();

            var clone = def.Clone();
            clone.Name.Should().Be("Stamina");
            clone.DefaultMax.Should().Be(150);
            clone.ColorHex.Should().Be("#e67e22");
            clone.ShowInGroup.Should().BeFalse();
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
            resDefault.ShowInGroup.Should().BeTrue();

            var res = new CharacterResource("Mana", 80, 100, 20, showInGroup: false);
            res.Name.Should().Be("Mana");
            res.CurrentValue.Should().Be(80);
            res.MaxValue.Should().Be(100);
            res.TempBonus.Should().Be(20);
            res.TotalMaxValue.Should().Be(120);
            res.ShowInGroup.Should().BeFalse();

            var clone = res.Clone();
            clone.Name.Should().Be("Mana");
            clone.CurrentValue.Should().Be(80);
            clone.MaxValue.Should().Be(100);
            clone.TempBonus.Should().Be(20);
            clone.TotalMaxValue.Should().Be(120);
            clone.ShowInGroup.Should().BeFalse();
        }

        [Fact]
        public void DiceSystem_GenericResources_AddRemoveAndDefaults()
        {
            var system = new DiceSystem();

            // Defaults when empty is empty
            var effective = system.GetEffectiveResources();
            effective.Should().BeEmpty();

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
            sheet.CharacterResources["Health"] = new CharacterResource("Health", 75, 120);
            sheet.CharacterResources["Mana"] = new CharacterResource("Mana", 40, 60);

            sheet.SyncResourcesWithLegacyFields();

            sheet.CharacterHealthPoints.Should().Be(75);
            sheet.CharacterMaxHealthPoints.Should().Be(120);
            sheet.CharacterManaPoints.Should().Be(40);
            sheet.CharacterMaxManaPoints.Should().Be(60);

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

            var systemResources = sheet.GetEffectiveResources(system);
            systemResources.Should().Contain(r => r.Name == "Stamina");
            systemResources.Should().NotContain(r => r.Name == "Focus");

            var allResources = sheet.GetEffectiveResources(null);
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
                CharacterFullName = "Mage Hero"
            };
            sheet.CharacterResources["Health"] = new CharacterResource("Health", 80, 100);
            sheet.CharacterResources["Mana"] = new CharacterResource("Mana", 120, 150);
            sheet.SyncResourcesWithLegacyFields();

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

        [Fact]
        public void ResourceType_Counter_InitializesFromZeroUpToCalculatedMax()
        {
            var system = new DiceSystem();
            system.AddResource(new ResourceDefinition(
                name: "ComboPoints",
                defaultMax: 5,
                defaultCurrent: 100, // Even if default is 100, Counter should start at 0
                colorHex: "#e74c3c",
                description: "Combo counter",
                isRequired: false,
                formula: "5",
                resourceType: ResourceType.Counter
            ));

            var sheet = new CharacterSheet { CharacterFullName = "Rogue" };
            var resources = sheet.GetEffectiveResources(system);

            var comboRes = resources.Find(r => r.Name == "ComboPoints");
            comboRes.Should().NotBeNull();
            comboRes!.ResourceType.Should().Be(ResourceType.Counter);
            comboRes.CurrentValue.Should().Be(0);
            comboRes.MaxValue.Should().Be(5);
        }

        [Fact]
        public void ResourceType_FlatNumber_EvaluatesFormulaAndAllowsRolling()
        {
            var system = new DiceSystem();
            system.SystemAttributes["Strength"] = new Soulstone.Datamodels.Attribute("Strength", 16);
            system.AddResource(new ResourceDefinition(
                name: "PassivePerception",
                defaultMax: 10,
                defaultCurrent: 10,
                colorHex: "#9b59b6",
                description: "Passive Perception",
                isRequired: false,
                formula: "10 + Strength / 2",
                resourceType: ResourceType.FlatNumber
            ));

            var sheet = new CharacterSheet { CharacterFullName = "Tracker" };
            sheet.ApplyRulesetTemplate(system);

            var resources = sheet.GetEffectiveResources(system);
            var passiveRes = resources.Find(r => r.Name == "PassivePerception");
            passiveRes.Should().NotBeNull();
            passiveRes!.ResourceType.Should().Be(ResourceType.FlatNumber);

            int effective = sheet.GetEffectiveResourceMax("PassivePerception", system);
            effective.Should().Be(18); // 10 + 16/2 = 18

            // Roll the resource
            var roll = sheet.RollResource("PassivePerception", system);
            roll.Should().NotBeNull();
            roll!.RollResult.Should().BeGreaterThan(0);
        }

        [Fact]
        public void JsonSerialization_PreservesResourceType()
        {
            var sheet = new CharacterSheet { CharacterFullName = "Paladin" };
            sheet.CharacterResources["HolyPower"] = new CharacterResource("HolyPower", 2, 5, 0, "", ResourceType.Counter);
            sheet.CharacterResources["SpellDC"] = new CharacterResource("SpellDC", 15, 15, 0, "", ResourceType.FlatNumber, isRollable: false);
            sheet.CharacterResources["Shield"] = new CharacterResource("Shield", 100, 100, 0, "", ResourceType.Bar);

            var json = JsonSerializer.Serialize(sheet);
            var deserialized = JsonSerializer.Deserialize<CharacterSheet>(json);

            deserialized.Should().NotBeNull();
            deserialized!.CharacterResources["HolyPower"].ResourceType.Should().Be(ResourceType.Counter);
            deserialized.CharacterResources["SpellDC"].ResourceType.Should().Be(ResourceType.FlatNumber);
            deserialized.CharacterResources["SpellDC"].IsRollable.Should().BeFalse();
            deserialized.CharacterResources["Shield"].ResourceType.Should().Be(ResourceType.Bar);
        }

        [Fact]
        public void CharacterSheet_And_DiceSystem_CanDeleteAnyResource_EvenHealthAndMana()
        {
            var sheet = new CharacterSheet();
            sheet.CharacterResources["Health"] = new CharacterResource("Health", 100, 100);
            sheet.CharacterResources["Mana"] = new CharacterResource("Mana", 50, 50);
            sheet.SyncResourcesWithLegacyFields();

            sheet.CharacterResources.Should().ContainKey("Health");
            sheet.CharacterResources.Should().ContainKey("Mana");

            // Remove Health
            sheet.RemoveResource("Health").Should().BeTrue();
            sheet.CharacterResources.Should().NotContainKey("Health");
            sheet.CharacterHealthPoints.Should().Be(0);
            sheet.CharacterMaxHealthPoints.Should().Be(0);

            // Remove Mana
            sheet.RemoveResource("Mana").Should().BeTrue();
            sheet.CharacterResources.Should().NotContainKey("Mana");
            sheet.CharacterManaPoints.Should().Be(0);
            sheet.CharacterMaxManaPoints.Should().Be(0);

            // Subsequent sync should NOT re-add Health or Mana when they have been deleted (max == 0)
            sheet.SyncResourcesWithLegacyFields();
            sheet.CharacterResources.Should().BeEmpty();

            // DiceSystem can also delete any resource
            var system = new DiceSystem();
            system.AddResource(new ResourceDefinition("Health", 100, 100, "#2ecc71", "Health Points", isRequired: true));
            system.AddResource(new ResourceDefinition("Mana", 100, 100, "#3498db", "Mana Points"));

            system.RemoveResource("Health").Should().BeTrue();
            system.RemoveResource("Mana").Should().BeTrue();
            system.GetEffectiveResources().Should().BeEmpty();
        }

        [Fact]
        public void Reordering_Resources_Attributes_Skills_Abilities_Slots_WorksCorrectly()
        {
            // DiceSystem Resource reordering
            var system = new DiceSystem();
            system.AddResource(new ResourceDefinition("ResA", 10, 10, "#111111"));
            system.AddResource(new ResourceDefinition("ResB", 20, 20, "#222222"));
            system.AddResource(new ResourceDefinition("ResC", 30, 30, "#333333"));

            system.MoveResource("ResC", -1).Should().BeTrue(); // Move ResC up: ResA, ResC, ResB
            var resList = system.GetEffectiveResources();
            resList[0].Name.Should().Be("ResA");
            resList[1].Name.Should().Be("ResC");
            resList[2].Name.Should().Be("ResB");

            // CharacterSheet Resource reordering
            var sheet = new CharacterSheet();
            sheet.CharacterResources["ResA"] = new CharacterResource("ResA", 10, 10);
            sheet.CharacterResources["ResB"] = new CharacterResource("ResB", 20, 20);
            sheet.CharacterResources["ResC"] = new CharacterResource("ResC", 30, 30);

            sheet.MoveResource("ResA", 1).Should().BeTrue(); // Move ResA down: ResB, ResA, ResC
            var sheetKeys = sheet.CharacterResources.Keys.ToList();
            sheetKeys[0].Should().Be("ResB");
            sheetKeys[1].Should().Be("ResA");
            sheetKeys[2].Should().Be("ResC");

            // Attribute reordering
            sheet.CharacterAttributes["Attr1"] = new Soulstone.Datamodels.Attribute("Attr1", 10);
            sheet.CharacterAttributes["Attr2"] = new Soulstone.Datamodels.Attribute("Attr2", 12);
            sheet.MoveAttribute("Attr2", -1).Should().BeTrue();
            sheet.CharacterAttributes.Keys.First().Should().Be("Attr2");

            // Skill reordering
            sheet.CharacterSkills["Skill1"] = new Skill("Skill1", 2);
            sheet.CharacterSkills["Skill2"] = new Skill("Skill2", 4);
            sheet.MoveSkill("Skill2", -1).Should().BeTrue();
            sheet.CharacterSkills.Keys.First().Should().Be("Skill2");

            // Ability reordering
            sheet.CharacterAbilities["Ability1"] = new Ability("Ability1", 3);
            sheet.CharacterAbilities["Ability2"] = new Ability("Ability2", 5);
            sheet.MoveAbility("Ability2", -1).Should().BeTrue();
            sheet.CharacterAbilities.Keys.First().Should().Be("Ability2");

            // Slots reordering in DiceSystem
            system.MoveEquipmentSlot("Head", 1).Should().BeTrue();
            system.MoveAugmentationSlot("Optics", -1).Should().BeTrue();
        }
    }
}
