using System;
using System.Collections.Generic;
using System.Text.Json;
using Soulstone.Datamodels;
using Xunit;

namespace Soulstone.Tests.Datamodels
{
    public class FeatTests
    {
        [Fact]
        public void DefaultConstructor_InitializesDefaults()
        {
            var feat = new Feat();

            Assert.False(string.IsNullOrEmpty(feat.Id));
            Assert.Equal(string.Empty, feat.Name);
            Assert.Equal(string.Empty, feat.Description);
            Assert.Equal("General", feat.Category);
            Assert.Equal(string.Empty, feat.RollFormula);
            Assert.True(feat.IsActive);
            Assert.NotNull(feat.StatModifiers);
            Assert.Empty(feat.StatModifiers);
        }

        [Fact]
        public void ParameterizedConstructor_InitializesCorrectly()
        {
            var dict = new Dictionary<string, int>
            {
                { "Strength", 2 },
                { "Athletics", 1 }
            };

            var feat = new Feat("Athlete", "Peak physical conditioning", "Combat", "1d20 + Athletics", dict, true);

            Assert.Equal("Athlete", feat.Name);
            Assert.Equal("Peak physical conditioning", feat.Description);
            Assert.Equal("Combat", feat.Category);
            Assert.Equal("1d20 + Athletics", feat.RollFormula);
            Assert.True(feat.IsActive);
            Assert.Equal(2, feat.GetStatModifier("Strength"));
            Assert.Equal(2, feat.GetStatModifier("strength")); // Case-insensitive
            Assert.Equal(1, feat.GetStatModifier("Athletics"));
            Assert.Equal(0, feat.GetStatModifier("Intelligence"));
        }

        [Fact]
        public void StatModifier_SetAndRemove()
        {
            var feat = new Feat("Tough", "Extra durability", "General");
            feat.SetStatModifier("Health", 10);
            feat.SetStatModifier("Defense", 1);

            Assert.Equal(10, feat.GetStatModifier("Health"));
            Assert.Equal(1, feat.GetStatModifier("Defense"));

            // Setting to 0 should remove the modifier
            feat.SetStatModifier("Defense", 0);
            Assert.Equal(0, feat.GetStatModifier("Defense"));

            bool removed = feat.RemoveStatModifier("Health");
            Assert.True(removed);
            Assert.Equal(0, feat.GetStatModifier("Health"));

            bool removedAgain = feat.RemoveStatModifier("NonExistent");
            Assert.False(removedAgain);
        }

        [Fact]
        public void InactiveFeat_ReturnsZeroModifier()
        {
            var feat = new Feat("Sharpshooter", "Ranged prowess", "Combat", "1d20 + DEX_Mod");
            feat.SetStatModifier("Attack", 3);
            feat.IsActive = false;

            Assert.Equal(0, feat.GetStatModifier("Attack"));

            feat.IsActive = true;
            Assert.Equal(3, feat.GetStatModifier("Attack"));
        }

        [Fact]
        public void GetFormattedModifiers_FormatsCorrectly()
        {
            var feat = new Feat("Dual Wielder");
            feat.SetStatModifier("Attack", 2);
            feat.SetStatModifier("Penalty", -1);

            string formatted = feat.GetFormattedModifiers();
            Assert.Contains("+2 Attack", formatted);
            Assert.Contains("-1 Penalty", formatted);
        }

        [Fact]
        public void Clone_CreatesIndependentCopy()
        {
            var original = new Feat("War Caster", "Spellcasting in battle", "Magic", "1d20 + CON_Mod");
            original.SetStatModifier("Constitution", 1);

            var clone = original.Clone();

            Assert.NotEqual(original.Id, clone.Id);
            Assert.Equal(original.Name, clone.Name);
            Assert.Equal(original.Description, clone.Description);
            Assert.Equal(original.Category, clone.Category);
            Assert.Equal(original.RollFormula, clone.RollFormula);
            Assert.Equal(1, clone.GetStatModifier("Constitution"));

            clone.Name = "War Caster Master";
            clone.SetStatModifier("Constitution", 2);

            Assert.Equal("War Caster", original.Name);
            Assert.Equal(1, original.GetStatModifier("Constitution"));
        }

        [Fact]
        public void JsonSerialization_PreservesAllFields()
        {
            var dict = new Dictionary<string, int>
            {
                { "Stealth", 3 },
                { "Dexterity", 1 }
            };
            var feat = new Feat("Skulker", "Master of shadows", "Passive", "1d20 + Stealth", dict, true);

            string json = JsonSerializer.Serialize(feat);
            var deserialized = JsonSerializer.Deserialize<Feat>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(feat.Id, deserialized.Id);
            Assert.Equal(feat.Name, deserialized.Name);
            Assert.Equal(feat.Description, deserialized.Description);
            Assert.Equal(feat.Category, deserialized.Category);
            Assert.Equal(feat.RollFormula, deserialized.RollFormula);
            Assert.True(deserialized.IsActive);
            Assert.Equal(3, deserialized.GetStatModifier("Stealth"));
            Assert.Equal(1, deserialized.GetStatModifier("Dexterity"));
        }

        [Fact]
        public void ResolveFormulaPreview_ReplacesStatTokens()
        {
            var sheet = new CharacterSheet();
            sheet.CharacterAttributes["Strength"] = new Soulstone.Datamodels.Attribute("Strength", 16);
            sheet.CharacterSkills["Athletics"] = new Skill("Athletics", 5, "Strength");

            var feat = new Feat("Mighty Leap", "Leap with power", "Active", "1d20 + Athletics");

            string preview = feat.ResolveFormulaPreview(sheet);
            Assert.Contains("1d20 + 21", preview); // Athletics total = 5 + 16 = 21
        }

        [Fact]
        public void RollFeat_CalculatesRollResult()
        {
            var sheet = new CharacterSheet();
            sheet.CharacterAttributes["Dexterity"] = new Soulstone.Datamodels.Attribute("Dexterity", 14);

            var feat = new Feat("Sneak Attack", "Extra precision damage", "Combat", "2d6 + 3");

            var roll = feat.RollFeat(sheet);
            Assert.NotNull(roll);
            Assert.InRange(roll.RollResult, 5, 15); // 2d6 (2-12) + 3
            Assert.Equal(2, roll.IndividualRolls.Count);
        }
    }
}
