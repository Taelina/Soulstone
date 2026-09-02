using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Soulstone.Datamodels;
using Xunit;
using Attribute = Soulstone.Datamodels.Attribute;

namespace Soulstone.Tests.Datamodels
{
    public class GearItemTests
    {
        [Fact]
        public void DefaultConstructor_InitializesDefaultValues()
        {
            var gear = new GearItem();

            gear.Id.Should().NotBeNullOrWhiteSpace();
            gear.ItemType.Should().Be("Equipment");
            gear.MaxStack.Should().Be(1);
            gear.Quantity.Should().Be(1);
            gear.Slot.Should().Be("Head");
            gear.Durability.Should().Be(100);
            gear.MaxDurability.Should().Be(100);
            gear.StatModifiers.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void ParameterizedConstructor_SetsExpectedProperties()
        {
            var modifiers = new Dictionary<string, int>
            {
                { "Strength", 5 },
                { "Dexterity", 2 }
            };

            var gear = new GearItem(
                name: "Iron Helm",
                slot: "Head",
                description: "Sturdy iron helmet",
                rarity: "Uncommon",
                modifiers: modifiers,
                effect: "+5 Armor",
                weight: 2.5f
            );

            gear.Name.Should().Be("Iron Helm");
            gear.Slot.Should().Be("Head");
            gear.Description.Should().Be("Sturdy iron helmet");
            gear.Rarity.Should().Be("Uncommon");
            gear.Effect.Should().Be("+5 Armor");
            gear.Weight.Should().Be(2.5f);
            gear.GetStatModifier("Strength").Should().Be(5);
            gear.GetStatModifier("Dexterity").Should().Be(2);
            gear.GetStatModifier("Intelligence").Should().Be(0);
        }

        [Fact]
        public void StatModifierHelpers_AddUpdateAndRemove()
        {
            var gear = new GearItem();

            gear.SetStatModifier("Strength", 10);
            gear.GetStatModifier("Strength").Should().Be(10);

            // Update
            gear.SetStatModifier("Strength", 15);
            gear.GetStatModifier("Strength").Should().Be(15);

            // Case-insensitivity check
            gear.GetStatModifier("strength").Should().Be(15);

            // Remove
            gear.RemoveStatModifier("Strength").Should().BeTrue();
            gear.GetStatModifier("Strength").Should().Be(0);
            gear.RemoveStatModifier("NonExistent").Should().BeFalse();
        }

        [Fact]
        public void GetFormattedModifiers_FormatsCorrectly()
        {
            var gear = new GearItem();
            gear.GetFormattedModifiers().Should().BeEmpty();

            gear.SetStatModifier("Strength", 5);
            gear.SetStatModifier("Agility", -2);

            var formatted = gear.GetFormattedModifiers();
            formatted.Should().Contain("+5 Strength");
            formatted.Should().Contain("-2 Agility");
        }

        [Fact]
        public void Clone_CreatesIndependentDeepCopy()
        {
            var gear = new GearItem("Dragon Shield", "OffHand", "Shield of dragons", "Epic")
            {
                Durability = 85,
                MaxDurability = 120
            };
            gear.SetStatModifier("Defense", 20);
            gear.CustomProperties["CraftedBy"] = "Blacksmith";

            var cloned = gear.Clone() as GearItem;

            cloned.Should().NotBeNull();
            cloned!.Id.Should().NotBe(gear.Id);
            cloned.Name.Should().Be("Dragon Shield");
            cloned.Slot.Should().Be("OffHand");
            cloned.Rarity.Should().Be("Epic");
            cloned.Durability.Should().Be(85);
            cloned.MaxDurability.Should().Be(120);
            cloned.GetStatModifier("Defense").Should().Be(20);
            cloned.CustomProperties["CraftedBy"].Should().Be("Blacksmith");

            // Mutating original should not affect clone
            gear.SetStatModifier("Defense", 30);
            cloned.GetStatModifier("Defense").Should().Be(20);

            gear.CustomProperties["CraftedBy"] = "Alchemist";
            cloned.CustomProperties["CraftedBy"].Should().Be("Blacksmith");
        }

        [Fact]
        public void PolymorphicJsonSerialization_SerializesAndDeserializesCorrectly()
        {
            Item item = new GearItem("Shadow Boots", "Feet", "Quiet boots", "Rare")
            {
                Durability = 90
            };
            ((GearItem)item).SetStatModifier("Stealth", 7);
            ((GearItem)item).SetStatModifier("Speed", 3);

            var json = item.ToJson();
            json.Should().Contain("GearItem");
            json.Should().Contain("Shadow Boots");
            json.Should().Contain("Stealth");

            var deserialized = Item.FromJson(json);
            deserialized.Should().BeOfType<GearItem>();

            var deserializedGear = (GearItem)deserialized!;
            deserializedGear.Name.Should().Be("Shadow Boots");
            deserializedGear.Slot.Should().Be("Feet");
            deserializedGear.Rarity.Should().Be("Rare");
            deserializedGear.Durability.Should().Be(90);
            deserializedGear.GetStatModifier("Stealth").Should().Be(7);
            deserializedGear.GetStatModifier("Speed").Should().Be(3);
        }

        [Fact]
        public void CharacterSheet_EquippingAndStatAlterations_WorkSeamlessly()
        {
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Warrior",
                characterAttributes = new Dictionary<string, Attribute>
                {
                    { "Strength", new Attribute("Strength", 14) },
                    { "Dexterity", new Attribute("Dexterity", 10) }
                },
                characterSkills = new Dictionary<string, Skill>
                {
                    { "Athletics", new Skill { SkillName = "Athletics", SkillModifier = 3, LinkedAttribute = "Strength" } }
                }
            };

            var weapon = new GearItem("Greatsword", "MainHand", "Heavy sword", "Rare");
            weapon.SetStatModifier("Strength", 4);
            weapon.SetStatModifier("Athletics", 2);

            var helmet = new GearItem("Iron Helm", "Head", "Iron helm", "Common");
            helmet.SetStatModifier("Strength", 1);
            helmet.SetStatModifier("Health", 25);

            sheet.AddItem(weapon);
            sheet.AddItem(helmet);

            // Initially not equipped
            sheet.IsItemEquipped(weapon.Id).Should().BeFalse();
            sheet.GetGearStatBonus("Strength").Should().Be(0);
            sheet.GetEffectiveAttributeValue("Strength").Should().Be(14);

            // Equip weapon
            sheet.EquipGear(weapon).Should().BeTrue();
            sheet.IsItemEquipped(weapon.Id).Should().BeTrue();
            sheet.GetEquippedSlot(weapon.Id).Should().Be("MainHand");
            sheet.GetEquippedGear("MainHand")?.Name.Should().Be("Greatsword");

            // Check stat alterations
            sheet.GetGearStatBonus("Strength").Should().Be(4);
            sheet.GetEffectiveAttributeValue("Strength").Should().Be(18); // 14 + 4
            sheet.GetEffectiveSkillModifier("Athletics").Should().Be(5); // 3 + 2

            // Equip helmet
            sheet.EquipGear(helmet).Should().BeTrue();
            sheet.GetGearStatBonus("Strength").Should().Be(5); // 4 + 1
            sheet.GetEffectiveAttributeValue("Strength").Should().Be(19); // 14 + 5
            sheet.GetEffectiveResourceMax("Health").Should().Be(125); // 100 + 25

            // GetAllGearStatBonuses
            var allBonuses = sheet.GetAllGearStatBonuses();
            allBonuses["Strength"].Should().Be(5);
            allBonuses["Athletics"].Should().Be(2);
            allBonuses["Health"].Should().Be(25);

            // Unequip weapon
            sheet.UnequipGear("MainHand").Should().BeTrue();
            sheet.IsItemEquipped(weapon.Id).Should().BeFalse();
            sheet.GetEquippedGear("MainHand").Should().BeNull();
            sheet.GetGearStatBonus("Strength").Should().Be(1);

            // Remove helmet from inventory -> automatically unequips
            sheet.RemoveItem(helmet.Id).Should().BeTrue();
            sheet.IsItemEquipped(helmet.Id).Should().BeFalse();
            sheet.GetGearStatBonus("Strength").Should().Be(0);
        }

        [Fact]
        public void StandardSlots_ContainsExpectedSlots()
        {
            GearItem.StandardSlots.Should().Contain(new[]
            {
                "MainHand", "OffHand", "Head", "Body", "Hands", "Legs", "Feet", "Neck", "Earrings", "Wrists", "Ring1", "Ring2"
            });
        }
    }
}
