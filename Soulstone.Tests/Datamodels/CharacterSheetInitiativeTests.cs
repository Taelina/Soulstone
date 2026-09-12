using System;
using System.Collections.Generic;
using FluentAssertions;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Xunit;
using Attribute = Soulstone.Datamodels.Attribute;

namespace Soulstone.Tests.Datamodels
{
    [Collection("NonParallel")]
    public class CharacterSheetInitiativeTests
    {
        public CharacterSheetInitiativeTests()
        {
            TestHelper.EnsureMockServices();
        }

        [Fact]
        public void GetInitiativeModifier_WithNone_ReturnsZero()
        {
            var sheet = new CharacterSheet();
            var diceSys = new DiceSystem
            {
                InitiativeStatType = InitiativeStatType.None,
                InitiativeStatName = ""
            };

            sheet.GetInitiativeModifier(diceSys).Should().Be(0);
        }

        [Fact]
        public void GetInitiativeModifier_WithAttribute_CalculatesCorrectly()
        {
            var sheet = new CharacterSheet();
            sheet.characterAttributes["Dexterity"] = new Attribute("Dexterity", 14)
            {
                TempBonus = 2,
                PermBonus = 1
            };

            var diceSys = new DiceSystem
            {
                InitiativeStatType = InitiativeStatType.Attribute,
                InitiativeStatName = "Dexterity"
            };

            // Base 14 + Temp 2 + Perm 1 = 17
            sheet.GetInitiativeModifier(diceSys).Should().Be(17);
        }

        [Fact]
        public void GetInitiativeModifier_WithSkill_IncludesLinkedAttributeAndGear()
        {
            var sheet = new CharacterSheet();
            sheet.characterAttributes["Agility"] = new Attribute("Agility", 5);
            sheet.characterSkills["Reflexes"] = new Skill
            {
                SkillName = "Reflexes",
                SkillModifier = 3,
                LinkedAttribute = "Agility"
            };

            var diceSys = new DiceSystem
            {
                InitiativeStatType = InitiativeStatType.Skill,
                InitiativeStatName = "Reflexes",
                SkillLinkedToOneAttribute = true
            };

            // Skill 3 + Linked Agility 5 = 8
            sheet.GetInitiativeModifier(diceSys).Should().Be(8);
        }

        [Fact]
        public void GetInitiativeModifier_WithFormula_CalculatesFromAttributes()
        {
            var sheet = new CharacterSheet();
            sheet.characterAttributes["Dexterity"] = new Attribute("Dexterity", 14);
            sheet.characterAttributes["Wits"] = new Attribute("Wits", 10);

            var diceSys = new DiceSystem
            {
                InitiativeStatType = InitiativeStatType.Formula,
                InitiativeFormula = "10 + [Dexterity] / 2 + [Wits]"
            };

            // 10 + 7 + 10 = 27
            sheet.GetInitiativeModifier(diceSys).Should().Be(27);
        }

        [Fact]
        public void RollInitiative_FollowsDiceSystemTypeAndSides()
        {
            var sheet = new CharacterSheet();
            sheet.characterAttributes["Speed"] = new Attribute("Speed", 3);

            var diceSys = new DiceSystem
            {
                InitiativeStatType = InitiativeStatType.Attribute,
                InitiativeStatName = "Speed",
                DiceType = DiceType.d10,
                SystemType = SystemType.DicePoolSystem,
                SuccessThreshold = 6
            };

            var roll = sheet.RollInitiative(diceSys);

            roll.Should().NotBeNull();
            // In dice pool system, it rolls dice pool based on modifier
            roll.IndividualRolls.Should().HaveCount(3);
            foreach (var die in roll.IndividualRolls)
            {
                die.Should().BeInRange(1, 10);
            }
        }

        [Fact]
        public void RollInitiative_ProducesValidRollResult()
        {
            var sheet = new CharacterSheet();
            sheet.characterAttributes["Dexterity"] = new Attribute("Dexterity", 4);

            var diceSys = new DiceSystem
            {
                InitiativeStatType = InitiativeStatType.Attribute,
                InitiativeStatName = "Dexterity",
                DiceType = DiceType.d20
            };

            var roll = sheet.RollInitiative(diceSys);

            roll.Should().NotBeNull();
            roll.RollResult.Should().BeInRange(1 + 4, 20 + 4);
            roll.IndividualRolls.Should().HaveCount(1);
            roll.IndividualRolls[0].Should().BeInRange(1, 20);
            roll.RollResultString.TextValue.Should().Contain("Initiative");
        }
    }
}
