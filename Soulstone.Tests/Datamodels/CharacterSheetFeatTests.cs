using System;
using System.Collections.Generic;
using Soulstone.Datamodels;
using Soulstone.Utils;
using Xunit;

namespace Soulstone.Tests.Datamodels
{
    public class CharacterSheetFeatTests
    {
        [Fact]
        public void AddAndRemoveFeats_WorkCorrectly()
        {
            var sheet = new CharacterSheet();
            var feat1 = new Feat("Alert", "Always on guard", "Passive");
            var feat2 = new Feat("Lucky", "Inexplicable luck", "General");

            sheet.AddFeat(feat1);
            sheet.AddFeat(feat2);

            Assert.Equal(2, sheet.CharacterFeats.Count);
            Assert.Equal(feat1, sheet.GetFeat(feat1.Id));
            Assert.Equal(feat2, sheet.GetFeat(feat2.Id));

            bool removed = sheet.RemoveFeat(feat1.Id);
            Assert.True(removed);
            Assert.Single(sheet.CharacterFeats);
            Assert.Null(sheet.GetFeat(feat1.Id));
        }

        [Fact]
        public void GetFeatStatBonus_AggregatesActiveFeatsOnly()
        {
            var sheet = new CharacterSheet();
            var feat1 = new Feat("Strong Arm", "Bonus strength", "Combat");
            feat1.SetStatModifier("Strength", 2);

            var feat2 = new Feat("Titan Grip", "More strength", "Combat");
            feat2.SetStatModifier("Strength", 3);
            feat2.IsActive = false; // Inactive

            var feat3 = new Feat("Champion", "Universal bonus", "Origin");
            feat3.SetStatModifier("All", 1);

            sheet.AddFeat(feat1);
            sheet.AddFeat(feat2);
            sheet.AddFeat(feat3);

            // feat1 (2) + feat3 All (1) = 3
            Assert.Equal(3, sheet.GetFeatStatBonus("Strength"));

            feat2.IsActive = true;
            // feat1 (2) + feat2 (3) + feat3 All (1) = 6
            Assert.Equal(6, sheet.GetFeatStatBonus("Strength"));
        }

        [Fact]
        public void EffectiveStats_IncludeFeatBonuses()
        {
            var sheet = new CharacterSheet();
            sheet.CharacterAttributes["Strength"] = new Soulstone.Datamodels.Attribute("Strength", 14);
            sheet.CharacterSkills["Athletics"] = new Skill("Athletics", 3, "Strength");
            sheet.CharacterAbilities["Power Attack"] = new Ability("Power Attack", 2, "Strength", sheet.CharacterSkills["Athletics"]);
            sheet.CharacterResources["Health"] = new CharacterResource("Health", 100, 100);

            var feat = new Feat("Brawny", "Physical might", "Combat");
            feat.SetStatModifier("Strength", 2);
            feat.SetStatModifier("Athletics", 1);
            feat.SetStatModifier("Power Attack", 3);
            feat.SetStatModifier("Initiative", 4);
            feat.SetStatModifier("Health", 20);

            sheet.AddFeat(feat);

            // Effective Attribute: 14 base + 2 feat = 16
            Assert.Equal(16, sheet.GetEffectiveAttributeValue("Strength"));

            // Effective Skill Modifier: 3 base + 1 feat = 4
            Assert.Equal(4, sheet.GetEffectiveSkillModifier("Athletics"));

            // Effective Skill Total: 3 (base) + 1 (feat) + 16 (effective Strength) = 20
            Assert.Equal(20, sheet.GetEffectiveSkillTotal("Athletics"));

            // Effective Ability Modifier: 2 (base) + 3 (feat) + 16 (attr) + 4 (skill) = 25
            Assert.Equal(25, sheet.GetEffectiveAbilityModifier("Power Attack"));

            // Initiative Modifier: 4 (feat)
            Assert.Equal(4, sheet.GetInitiativeModifier(null));

            // Effective Resource Max: 100 base + 20 feat = 120
            Assert.Equal(120, sheet.GetEffectiveResourceMax("Health"));
        }

        [Fact]
        public void StatFormulaEvaluator_ResolvesFeatVariables()
        {
            var sheet = new CharacterSheet();
            sheet.CharacterAttributes["Dexterity"] = new Soulstone.Datamodels.Attribute("Dexterity", 16);

            var feat1 = new Feat("Fast Reflexes");
            feat1.SetStatModifier("Dexterity", 2);
            feat1.SetStatModifier("Initiative", 5);
            sheet.AddFeat(feat1);

            var feat2 = new Feat("Observant");
            sheet.AddFeat(feat2);

            // Feat prefix
            int featDex = StatFormulaEvaluator.EvaluateToInt("Feat.Dexterity", sheet);
            Assert.Equal(2, featDex);

            // Feat property on attribute
            int dexFeatProp = StatFormulaEvaluator.EvaluateToInt("Dexterity.Feat", sheet);
            Assert.Equal(2, dexFeatProp);

            // FeatsCount
            int featsCount = StatFormulaEvaluator.EvaluateToInt("FeatsCount", sheet);
            Assert.Equal(2, featsCount);

            // Formula with Feat bonus
            int formulaVal = StatFormulaEvaluator.EvaluateToInt("10 + Feat.Initiative + FeatsCount", sheet);
            Assert.Equal(17, formulaVal); // 10 + 5 + 2 = 17
        }
    }
}
