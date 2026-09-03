using System;
using System.Collections.Generic;
using System.Text.Json;
using Soulstone.Datamodels;
using Xunit;
using Attribute = Soulstone.Datamodels.Attribute;

namespace Soulstone.Tests.Datamodels
{
    public class CharacterSheetBuffTests
    {
        [Fact]
        public void AddAndRemoveBuff_ModifiesActiveBuffsList()
        {
            var sheet = new CharacterSheet();
            var buff = new Buff("Bull's Strength", 3, "Strength", 4);

            sheet.AddBuff(buff);
            Assert.Single(sheet.ActiveBuffs);
            Assert.Equal(4, sheet.GetBuffStatBonus("Strength"));

            bool removed = sheet.RemoveBuff(buff.Id);
            Assert.True(removed);
            Assert.Empty(sheet.ActiveBuffs);
            Assert.Equal(0, sheet.GetBuffStatBonus("Strength"));
        }

        [Fact]
        public void GetEffectiveAttributeValue_IncludesBuffAndDebuff()
        {
            var sheet = new CharacterSheet();
            sheet.characterAttributes["Strength"] = new Attribute("Strength", 14);

            Assert.Equal(14, sheet.GetEffectiveAttributeValue("Strength"));

            var buff = new Buff("Might", 3, "Strength", 3);
            sheet.AddBuff(buff);
            Assert.Equal(17, sheet.GetEffectiveAttributeValue("Strength"));

            var debuff = new Buff("Fatigue", 2, "Strength", -2, isDebuff: true);
            sheet.AddBuff(debuff);
            Assert.Equal(15, sheet.GetEffectiveAttributeValue("Strength"));
        }

        [Fact]
        public void GetEffectiveSkillModifierAndTotal_IncludesBuffs()
        {
            var sheet = new CharacterSheet();
            sheet.characterAttributes["Agility"] = new Attribute("Agility", 3);
            sheet.characterSkills["Stealth"] = new Skill
            {
                skillName = "Stealth",
                skillModifier = 2,
                linkedAttribute = "Agility"
            };

            var diceSys = new DiceSystem { skillLinkedToOneAttribute = true };

            Assert.Equal(2, sheet.GetEffectiveSkillModifier("Stealth"));
            Assert.Equal(5, sheet.GetEffectiveSkillTotal("Stealth", diceSys));

            var stealthBuff = new Buff("Shadow Cloak", 3, "Stealth", 2);
            sheet.AddBuff(stealthBuff);

            Assert.Equal(4, sheet.GetEffectiveSkillModifier("Stealth"));
            Assert.Equal(7, sheet.GetEffectiveSkillTotal("Stealth", diceSys));

            // Also buff linked attribute
            var agiBuff = new Buff("Cat's Grace", 3, "Agility", 2);
            sheet.AddBuff(agiBuff);

            Assert.Equal(9, sheet.GetEffectiveSkillTotal("Stealth", diceSys));
        }

        [Fact]
        public void GetEffectiveAbilityModifier_IncludesBuffs()
        {
            var sheet = new CharacterSheet();
            sheet.characterAttributes["Intelligence"] = new Attribute("Intelligence", 4);
            sheet.characterAbilities["Fireball"] = new Ability
            {
                abilityName = "Fireball",
                abilityModifier = 5,
                linkedAttribute = "Intelligence"
            };

            Assert.Equal(9, sheet.GetEffectiveAbilityModifier("Fireball"));

            var abilityBuff = new Buff("Empowered Spell", 1, "Fireball", 4);
            sheet.AddBuff(abilityBuff);

            Assert.Equal(13, sheet.GetEffectiveAbilityModifier("Fireball"));
        }

        [Fact]
        public void GetEffectiveResourceMax_IncludesResourceBuffs()
        {
            var sheet = new CharacterSheet();
            sheet.characterHealthPoints = 20;
            sheet.characterMaxHealthPoints = 20;
            sheet.SyncResourcesWithLegacyFields();

            Assert.Equal(20, sheet.GetEffectiveResourceMax("Health"));

            var hpBuff = new Buff("Aid", 5, "Health", 10);
            sheet.AddBuff(hpBuff);

            Assert.Equal(30, sheet.GetEffectiveResourceMax("Health"));
        }

        [Fact]
        public void GetInitiativeModifier_IncludesInitiativeBuffs()
        {
            var sheet = new CharacterSheet();
            sheet.characterAttributes["Dexterity"] = new Attribute("Dexterity", 2);
            var diceSys = new DiceSystem
            {
                InitiativeStatType = InitiativeStatType.Attribute,
                InitiativeStatName = "Dexterity"
            };

            Assert.Equal(2, sheet.GetInitiativeModifier(diceSys));

            var initBuff = new Buff("Alertness", 3, "Initiative", 5);
            sheet.AddBuff(initBuff);

            Assert.Equal(7, sheet.GetInitiativeModifier(diceSys));
        }

        [Fact]
        public void GlobalAndAllModifier_AppliesToAnyStat()
        {
            var sheet = new CharacterSheet();
            sheet.characterAttributes["Strength"] = new Attribute("Strength", 10);

            var globalBuff = new Buff("Heroism", 3, "All", 2);
            sheet.AddBuff(globalBuff);

            Assert.Equal(2, sheet.GetBuffStatBonus("Strength"));
            Assert.Equal(12, sheet.GetEffectiveAttributeValue("Strength"));
        }

        [Fact]
        public void TickBuffs_DecrementsAndRemovesExpired()
        {
            var sheet = new CharacterSheet();
            var b1 = new Buff("Short Buff", 1, "Strength", 2);
            var b2 = new Buff("Long Buff", 3, "Strength", 3);

            sheet.AddBuff(b1);
            sheet.AddBuff(b2);

            var expired = sheet.TickBuffs();
            Assert.Single(expired);
            Assert.Equal("Short Buff", expired[0].Name);
            Assert.Single(sheet.ActiveBuffs);
            Assert.Equal(2, sheet.ActiveBuffs[0].Duration);
        }

        [Fact]
        public void JsonSerialization_PreservesActiveBuffs()
        {
            var sheet = new CharacterSheet();
            sheet.CharacterFullName = "Hero";
            sheet.AddBuff(new Buff("Haste", 3, "Agility", 2));

            string json = JsonSerializer.Serialize(sheet);
            var deserialized = JsonSerializer.Deserialize<CharacterSheet>(json);

            Assert.NotNull(deserialized);
            Assert.Single(deserialized.ActiveBuffs);
            Assert.Equal("Haste", deserialized.ActiveBuffs[0].Name);
            Assert.Equal(2, deserialized.GetBuffStatBonus("Agility"));
        }
    }
}
