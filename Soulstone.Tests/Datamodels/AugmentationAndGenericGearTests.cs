using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Soulstone.Datamodels;
using Xunit;
using Attribute = Soulstone.Datamodels.Attribute;

namespace Soulstone.Tests.Datamodels
{
    public class AugmentationAndGenericGearTests
    {
        [Fact]
        public void AugmentationItem_InitializationAndProperties()
        {
            var aug = new GearItem("Cybernetic Arm", "Arms", "Titanium reinforced limb", "Rare", isAugmentation: true);

            aug.IsAugmentation.Should().BeTrue();
            aug.ItemType.Should().Be("Augmentation");
            aug.Slot.Should().Be("Arms");
            aug.Rarity.Should().Be("Rare");
            aug.MaxStack.Should().Be(1);

            GearItem.StandardAugmentationSlots.Should().Contain(new[]
            {
                "Neural", "Optics", "Cranial", "Torso", "Arms", "Legs", "Subdermal", "Internal"
            });
        }

        [Fact]
        public void AugmentationItem_Clone_PreservesIsAugmentation()
        {
            var aug = new GearItem("Subdermal Plating", "Subdermal", "Armor under skin", "Epic", isAugmentation: true);
            aug.SetStatModifier("Health", 50);
            aug.SetStatModifier("Body", 3);

            var clone = aug.Clone() as GearItem;
            clone.Should().NotBeNull();
            clone!.Id.Should().NotBe(aug.Id);
            clone.IsAugmentation.Should().BeTrue();
            clone.Slot.Should().Be("Subdermal");
            clone.GetStatModifier("Health").Should().Be(50);
            clone.GetStatModifier("Body").Should().Be(3);
        }

        [Fact]
        public void DiceSystem_AugmentationsConfiguration()
        {
            var system = new DiceSystem
            {
                SystemHasAugmentations = true,
                AugmentationTitle = "Cyberware & Bioware"
            };

            system.SystemHasAugmentations.Should().BeTrue();
            system.AugmentationTitle.Should().Be("Cyberware & Bioware");

            // Default slots when none configured
            var defaultSlots = system.GetEffectiveAugmentationSlots();
            defaultSlots.Should().BeEquivalentTo(GearItem.StandardAugmentationSlots);

            // Custom slots
            system.CustomAugmentationSlots = new List<string> { "Head", "Spine", "Heart" };
            system.GetEffectiveAugmentationSlots().Should().BeEquivalentTo(new[] { "Head", "Spine", "Heart" });

            // Dynamic additions, removal, and moving
            system.AddAugmentationSlot("Eyes");
            system.GetEffectiveAugmentationSlots().Should().Contain("Eyes");
            system.MoveAugmentationSlot("Eyes", -1).Should().BeTrue();
            system.RemoveAugmentationSlot("Spine").Should().BeTrue();
            system.GetEffectiveAugmentationSlots().Should().NotContain("Spine");
        }

        [Fact]
        public void DiceSystem_EquipmentSlotsConfiguration()
        {
            var system = new DiceSystem();

            // Default slots when none configured
            var defaultSlots = system.GetEffectiveEquipmentSlots();
            defaultSlots.Should().BeEquivalentTo(GearItem.StandardSlots);

            // Custom slots list assignment
            system.CustomEquipmentSlots = new List<string> { "Head", "Chest", "Weapon", "Trinket" };
            system.GetEffectiveEquipmentSlots().Should().BeEquivalentTo(new[] { "Head", "Chest", "Weapon", "Trinket" });

            // Dynamic add, move, and remove
            system.AddEquipmentSlot("Belt");
            system.GetEffectiveEquipmentSlots().Should().Contain("Belt");
            system.MoveEquipmentSlot("Belt", -1).Should().BeTrue();
            system.RemoveEquipmentSlot("Weapon").Should().BeTrue();
            system.GetEffectiveEquipmentSlots().Should().NotContain("Weapon");
            system.GetEffectiveEquipmentSlots().Should().Equal(new[] { "Head", "Chest", "Belt", "Trinket" });
        }

        [Fact]
        public void GenericStatModifiers_TargetingAttributesSkillsAbilitiesAndResources()
        {
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Netrunner",
                characterAttributes = new Dictionary<string, Attribute>
                {
                    { "Intelligence", new Attribute("Intelligence", 15) },
                    { "Reflexes", new Attribute("Reflexes", 12) }
                },
                characterSkills = new Dictionary<string, Skill>
                {
                    { "Hacking", new Skill { SkillName = "Hacking", SkillModifier = 4, LinkedAttribute = "Intelligence" } }
                },
                characterAbilities = new Dictionary<string, Ability>
                {
                    {
                        "Overclock",
                        new Ability
                        {
                            AbilityName = "Overclock",
                            AbilityModifier = 2,
                            LinkedAttribute = "Intelligence",
                            LinkedSkill = new Skill { SkillName = "Hacking", SkillModifier = 4 }
                        }
                    }
                },
                characterResources = new Dictionary<string, CharacterResource>
                {
                    { "Health", new CharacterResource("Health", 80, 80) },
                    { "RAM", new CharacterResource("RAM", 16, 16) }
                }
            };

            // Standard Gear
            var neuralDeck = new GearItem("Cyberdeck Mk.V", "MainHand", "Deck for netrunning", "Epic");
            neuralDeck.SetStatModifier("Intelligence", 2);
            neuralDeck.SetStatModifier("Hacking", 3);
            neuralDeck.SetStatModifier("Overclock", 2);
            neuralDeck.SetStatModifier("RAM", 8);

            // Cyberware / Augmentation
            var neuralProcessor = new GearItem("Neural Processor", "Neural", "Boosts brain functions", "Legendary", isAugmentation: true);
            neuralProcessor.SetStatModifier("Intelligence", 3);
            neuralProcessor.SetStatModifier("Reflexes", 2);
            neuralProcessor.SetStatModifier("Overclock", 3);
            neuralProcessor.SetStatModifier("Health", 20);

            sheet.AddItem(neuralDeck);
            sheet.AddItem(neuralProcessor);

            sheet.EquipGear(neuralDeck).Should().BeTrue();
            sheet.EquipAugmentation(neuralProcessor).Should().BeTrue();

            // Verify simultaneous bonus aggregation
            // Total Intelligence bonus = 2 (deck) + 3 (processor) = 5
            sheet.GetGearStatBonus("Intelligence").Should().Be(5);
            sheet.GetEffectiveAttributeValue("Intelligence").Should().Be(20); // 15 + 5

            // Total Reflexes bonus = 2 (processor)
            sheet.GetGearStatBonus("Reflexes").Should().Be(2);
            sheet.GetEffectiveAttributeValue("Reflexes").Should().Be(14); // 12 + 2

            // Total Hacking bonus = 3 (deck)
            sheet.GetGearStatBonus("Hacking").Should().Be(3);
            sheet.GetEffectiveSkillModifier("Hacking").Should().Be(7); // 4 + 3

            // Total Overclock bonus = 2 (deck) + 3 (processor) = 5
            sheet.GetGearStatBonus("Overclock").Should().Be(5);
            // Effective Ability = base (2) + ability bonus (5) + effective attribute (20) + effective skill (7) = 34
            sheet.GetEffectiveAbilityModifier("Overclock").Should().Be(34);

            // Resources bonuses
            // Health = 80 + 20 = 100
            sheet.GetEffectiveResourceMax("Health").Should().Be(100);
            // RAM = 16 + 8 = 24
            sheet.GetEffectiveResourceMax("RAM").Should().Be(24);

            // GetAllGearStatBonuses includes both gear and augmentations
            var allBonuses = sheet.GetAllGearStatBonuses();
            allBonuses["Intelligence"].Should().Be(5);
            allBonuses["Reflexes"].Should().Be(2);
            allBonuses["Hacking"].Should().Be(3);
            allBonuses["Overclock"].Should().Be(5);
            allBonuses["Health"].Should().Be(20);
            allBonuses["RAM"].Should().Be(8);
        }

        [Fact]
        public void Augmentations_EquipUnequipAndInspectionWorkflows()
        {
            var sheet = new CharacterSheet { CharacterFullName = "AugmentedSoldier" };
            var optics = new GearItem("Kiroshi Optics", "Optics", "Enhanced eye implants", "Rare", isAugmentation: true);
            optics.SetStatModifier("Perception", 5);

            sheet.AddItem(optics);

            sheet.IsAugmentationEquipped(optics.Id).Should().BeFalse();
            sheet.IsItemEquipped(optics.Id).Should().BeFalse();

            // Equip into Optics slot
            sheet.EquipAugmentation("Optics", optics.Id).Should().BeTrue();
            sheet.IsAugmentationEquipped(optics.Id).Should().BeTrue();
            sheet.IsItemEquipped(optics.Id).Should().BeTrue();
            sheet.GetEquippedAugmentationSlot(optics.Id).Should().Be("Optics");
            sheet.GetEquippedSlot(optics.Id).Should().Be("Optics");
            sheet.GetEquippedAugmentation("Optics")?.Name.Should().Be("Kiroshi Optics");

            var equippedMap = sheet.GetEquippedAugmentationItems();
            equippedMap.Should().ContainKey("Optics");
            equippedMap["Optics"].Name.Should().Be("Kiroshi Optics");

            // Unequip
            sheet.UnequipAugmentation("Optics").Should().BeTrue();
            sheet.IsAugmentationEquipped(optics.Id).Should().BeFalse();
            sheet.GetEquippedAugmentation("Optics").Should().BeNull();
        }

        [Fact]
        public void CharacterSheet_SerializationWithEquippedAugmentations_PreservesState()
        {
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Cyborg01"
            };

            var implant = new GearItem("Dermal Armor", "Subdermal", "Subdermal plates", "Epic", isAugmentation: true);
            implant.SetStatModifier("Armor", 15);
            implant.SetStatModifier("Health", 40);

            sheet.AddItem(implant);
            sheet.EquipAugmentation(implant);

            var json = JsonSerializer.Serialize(sheet, new JsonSerializerOptions { WriteIndented = true });
            var deserialized = JsonSerializer.Deserialize<CharacterSheet>(json);

            deserialized.Should().NotBeNull();
            deserialized!.CharacterFullName.Should().Be("Cyborg01");
            deserialized.EquippedAugmentations.Should().ContainKey("Subdermal");
            deserialized.EquippedAugmentations["Subdermal"].Should().Be(implant.Id);

            var equippedAug = deserialized.GetEquippedAugmentation("Subdermal");
            equippedAug.Should().NotBeNull();
            equippedAug!.Name.Should().Be("Dermal Armor");
            equippedAug.IsAugmentation.Should().BeTrue();
            deserialized.GetGearStatBonus("Armor").Should().Be(15);
            deserialized.GetGearStatBonus("Health").Should().Be(40);
        }

        [Fact]
        public void AugmentationsAndGear_MutualExclusivityOnEquip()
        {
            var sheet = new CharacterSheet { CharacterFullName = "TestRunner" };

            var standardGear = new GearItem("Iron Helm", "Head", "A sturdy helm", "Common", isAugmentation: false);
            var augmentation = new GearItem("Neural Chip", "Neural", "Brain processor", "Rare", isAugmentation: true);

            sheet.AddItem(standardGear);
            sheet.AddItem(augmentation);

            // Augmentation MUST NOT be equippable as standard gear
            sheet.EquipGear(augmentation).Should().BeFalse();
            sheet.EquipGear("Head", augmentation.Id).Should().BeFalse();
            sheet.EquipGear("Neural", augmentation.Id).Should().BeFalse();
            sheet.GetEquippedGear("Head").Should().BeNull();
            sheet.GetEquippedGear("Neural").Should().BeNull();
            sheet.EquippedGear.Should().BeEmpty();

            // Standard gear MUST NOT be equippable as an augmentation
            sheet.EquipAugmentation(standardGear).Should().BeFalse();
            sheet.EquipAugmentation("Neural", standardGear.Id).Should().BeFalse();
            sheet.EquipAugmentation("Head", standardGear.Id).Should().BeFalse();
            sheet.GetEquippedAugmentation("Neural").Should().BeNull();
            sheet.GetEquippedAugmentation("Head").Should().BeNull();
            sheet.EquippedAugmentations.Should().BeEmpty();

            // Valid equips should succeed
            sheet.EquipGear(standardGear).Should().BeTrue();
            sheet.EquipAugmentation(augmentation).Should().BeTrue();

            sheet.GetEquippedGear("Head")?.Name.Should().Be("Iron Helm");
            sheet.GetEquippedAugmentation("Neural")?.Name.Should().Be("Neural Chip");
        }

        [Fact]
        public void DeleteItem_EquippedGearAndAugmentations_UnequipsAndRemovesFromInventory()
        {
            var sheet = new CharacterSheet { CharacterFullName = "Deleter" };

            var gear = new GearItem("Steel Boots", "Feet", "Heavy boots", "Common", isAugmentation: false);
            var aug = new GearItem("Cyber Arm", "Arms", "Augmented arm", "Epic", isAugmentation: true);

            sheet.AddItem(gear);
            sheet.AddItem(aug);

            sheet.EquipGear(gear).Should().BeTrue();
            sheet.EquipAugmentation(aug).Should().BeTrue();

            sheet.IsItemEquipped(gear.Id).Should().BeTrue();
            sheet.IsItemEquipped(aug.Id).Should().BeTrue();

            // Delete gear
            sheet.RemoveItem(gear.Id).Should().BeTrue();
            sheet.CharacterInventory.Should().NotContain(gear);
            sheet.IsItemEquipped(gear.Id).Should().BeFalse();
            sheet.GetEquippedGear("Feet").Should().BeNull();

            // Delete augmentation
            sheet.RemoveItem(aug.Id).Should().BeTrue();
            sheet.CharacterInventory.Should().NotContain(aug);
            sheet.IsItemEquipped(aug.Id).Should().BeFalse();
            sheet.GetEquippedAugmentation("Arms").Should().BeNull();
        }
    }
}
