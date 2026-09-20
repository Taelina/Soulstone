using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Soulstone.Datamodels;
using Soulstone.Utils;
using Xunit;
using Attribute = Soulstone.Datamodels.Attribute;

namespace Soulstone.Tests.Utils
{
    public class ComplexDiceFormulaTests
    {
        [Fact]
        public void ParseDiceRollString_MultipleDiceTerms_EvaluatesSumAndCollectsRolls()
        {
            var roll = DiceRoll.ParseDiceRollString("2d6 + 1d8 + 3");
            roll.Should().NotBeNull();
            roll!.IndividualRolls.Should().HaveCount(3); // 2 from d6 + 1 from d8
            roll.RollResult.Should().BeInRange(2 + 1 + 3, 12 + 8 + 3);
            roll.RollResultString.TextValue.Should().Contain("2d6 + 1d8 + 3");
            roll.RollDetailedResultString.TextValue.Should().Contain("2d6");
            roll.RollDetailedResultString.TextValue.Should().Contain("1d8");
        }

        [Fact]
        public void ParseDiceRollString_StandaloneDiceNotation_WorksCorrectly()
        {
            var roll = DiceRoll.ParseDiceRollString("d20 + d4");
            roll.Should().NotBeNull();
            roll!.IndividualRolls.Should().HaveCount(2);
            roll.RollResult.Should().BeInRange(2, 24);
        }

        [Fact]
        public void ParseDiceRollString_KeepHighestAndKeepLowest_AppliesKeepDropModifiers()
        {
            // 4d6kh3 (standard D&D stat roll: roll 4d6, keep highest 3)
            var rollKh = DiceRoll.ParseDiceRollString("4d6kh3");
            rollKh.Should().NotBeNull();
            rollKh!.IndividualRolls.Should().HaveCount(4);
            rollKh.RollResult.Should().BeInRange(3, 18);

            // 2d20kl1 (disadvantage: roll 2d20, keep lowest 1)
            var rollKl = DiceRoll.ParseDiceRollString("2d20kl1");
            rollKl.Should().NotBeNull();
            rollKl!.IndividualRolls.Should().HaveCount(2);
            rollKl.RollResult.Should().BeInRange(1, 20);

            // 4d6dl1 (drop lowest 1)
            var rollDl = DiceRoll.ParseDiceRollString("4d6dl1");
            rollDl.Should().NotBeNull();
            rollDl!.IndividualRolls.Should().HaveCount(4);
            rollDl.RollResult.Should().BeInRange(3, 18);

            // 4d6dh1 (drop highest 1)
            var rollDh = DiceRoll.ParseDiceRollString("4d6dh1");
            rollDh.Should().NotBeNull();
            rollDh!.IndividualRolls.Should().HaveCount(4);
            rollDh.RollResult.Should().BeInRange(3, 18);
        }

        [Fact]
        public void ParseDiceRollString_RerollModifiers_ExecutesRerolls()
        {
            // 1d6r1 (reroll 1s)
            var rollR = DiceRoll.ParseDiceRollString("1d6r1");
            rollR.Should().NotBeNull();
            rollR!.RollResult.Should().BeInRange(2, 6);

            // 2d6ro1 (reroll 1s once)
            var rollRo = DiceRoll.ParseDiceRollString("2d6ro1");
            rollRo.Should().NotBeNull();
            rollRo!.RollResult.Should().BeInRange(2, 12);
        }

        [Fact]
        public void ParseDiceRollString_ExplodingDice_HandlesExplosions()
        {
            // 3d6!
            var rollBang = DiceRoll.ParseDiceRollString("3d6!");
            rollBang.Should().NotBeNull();
            rollBang!.IndividualRolls.Count.Should().BeGreaterThanOrEqualTo(3);
            rollBang.RollResult.Should().BeGreaterThanOrEqualTo(3);

            // 1d10!>=9 (explodes on 9 or 10)
            var rollBangCond = DiceRoll.ParseDiceRollString("1d10!>=9");
            rollBangCond.Should().NotBeNull();
            rollBangCond!.RollResult.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public void ParseDiceRollString_MinMaxClamps_AppliesClampsPerDie()
        {
            // 1d20min10 (Reliable Talent: floor at 10)
            for (int i = 0; i < 20; i++)
            {
                var rollMin = DiceRoll.ParseDiceRollString("1d20min10");
                rollMin.Should().NotBeNull();
                rollMin!.RollResult.Should().BeInRange(10, 20);
            }

            // 1d20max15 (ceiling at 15)
            for (int i = 0; i < 20; i++)
            {
                var rollMax = DiceRoll.ParseDiceRollString("1d20max15");
                rollMax.Should().NotBeNull();
                rollMax!.RollResult.Should().BeInRange(1, 15);
            }
        }

        [Fact]
        public void ParseDiceRollString_TargetSuccesses_CountsSuccesses()
        {
            // 5d10>=8
            var rollSuccess = DiceRoll.ParseDiceRollString("5d10>=8");
            rollSuccess.Should().NotBeNull();
            rollSuccess!.IndividualRolls.Should().HaveCount(5);
            rollSuccess.RollResult.Should().BeInRange(0, 5);

            // 5d10cs>=8cf<=1 (count success >=8 and failure <=1)
            var rollSuccessFail = DiceRoll.ParseDiceRollString("5d10cs>=8cf<=1");
            rollSuccessFail.Should().NotBeNull();
            rollSuccessFail!.IndividualRolls.Should().HaveCount(5);
            rollSuccessFail.RollResult.Should().BeInRange(-5, 5);
        }

        [Fact]
        public void ParseDiceRollString_ArithmeticExpressionsAndParentheses_CalculatesProperly()
        {
            // (1d8 + 2) * 2
            var roll = DiceRoll.ParseDiceRollString("(1d8 + 2) * 2");
            roll.Should().NotBeNull();
            roll!.RollResult.Should().BeInRange((1 + 2) * 2, (8 + 2) * 2);
            (roll.RollResult % 2).Should().Be(0);

            // 2d6 * 2 + 1d4
            var roll2 = DiceRoll.ParseDiceRollString("2d6 * 2 + 1d4");
            roll2.Should().NotBeNull();
            roll2!.RollResult.Should().BeInRange(2 * 2 + 1, 12 * 2 + 4);
        }

        [Fact]
        public void StatFormulaEvaluator_WithDiceAndCharacterSheetStats_EvaluatesCorrectly()
        {
            var sheet = new CharacterSheet
            {
                CharacterAttributes = new Dictionary<string, Attribute>
                {
                    { "Strength", new Attribute("Strength", 16) },
                    { "Constitution", new Attribute("Constitution", 14) }
                }
            };

            // 1d20 + Strength (16)
            double result = StatFormulaEvaluator.Evaluate("1d20 + Strength", sheet);
            result.Should().BeInRange(1 + 16, 20 + 16);

            // Multiple dice and stats: 2d6 + 1d4 + Constitution (14)
            double resultMulti = StatFormulaEvaluator.Evaluate("2d6 + 1d4 + Constitution", sheet);
            resultMulti.Should().BeInRange(2 + 1 + 14, 12 + 4 + 14);

            // Mathematical functions with dice: min(10, 2d6)
            double resultFunc = StatFormulaEvaluator.Evaluate("min(10, 2d6)", sheet);
            resultFunc.Should().BeInRange(2, 10);
        }

        [Fact]
        public void StatFormulaEvaluator_RollFormula_ReturnsRichBreakdown()
        {
            var formulaResult = StatFormulaEvaluator.RollFormula("2d6 + 1d8 + 5");
            formulaResult.Success.Should().BeTrue();
            formulaResult.IndividualRolls.Should().HaveCount(3);
            formulaResult.Total.Should().BeInRange(2 + 1 + 5, 12 + 8 + 5);
            formulaResult.DetailedBreakdown.Should().Contain("2d6");
            formulaResult.DetailedBreakdown.Should().Contain("1d8");
        }

        [Fact]
        public void StatFormulaEvaluator_AdvantageAndDisadvantage_ModifiesRolls()
        {
            var advResult = StatFormulaEvaluator.RollFormula("1d20 + 5", advantage: true);
            advResult.Success.Should().BeTrue();
            advResult.IndividualRolls.Should().HaveCount(2); // Rolled 2 dice for advantage
            advResult.Total.Should().BeInRange(6, 25);

            var disadvResult = StatFormulaEvaluator.RollFormula("1d20 + 5", disadvantage: true);
            disadvResult.Success.Should().BeTrue();
            disadvResult.IndividualRolls.Should().HaveCount(2);
            disadvResult.Total.Should().BeInRange(6, 25);
        }

        [Fact]
        public void ParseDiceRollString_FateFudgeDice_EvaluatesCorrectly()
        {
            var roll = DiceRoll.ParseDiceRollString("4dF + 2");
            roll.Should().NotBeNull();
            roll!.IndividualRolls.Should().HaveCount(4);
            roll.RollResult.Should().BeInRange(-4 + 2, 4 + 2);
        }

        [Fact]
        public void ParseDiceRollString_PercentileDice_EvaluatesCorrectly()
        {
            var roll = DiceRoll.ParseDiceRollString("1d% + 10");
            roll.Should().NotBeNull();
            roll!.IndividualRolls.Should().HaveCount(1);
            roll.RollResult.Should().BeInRange(11, 110);
        }
    }
}
