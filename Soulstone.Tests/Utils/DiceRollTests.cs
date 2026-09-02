using FluentAssertions;
using Xunit;
using Soulstone.Utils;

namespace Soulstone.Tests.Utils
{
    public class DiceRollTests
    {
        public DiceRollTests()
        {
            TestHelper.EnsureMockServices();
        }

        #region Regular Dice Rolls

        [Theory]
        [InlineData(1, 20, 0)]
        [InlineData(2, 6, 3)]
        [InlineData(3, 8, -2)]
        [InlineData(4, 4, 10)]
        [InlineData(1, 100, 0)]
        public void RollDiceRegular_ProducesResultsWithinValidRange(int numDice, int sides, int addedValue)
        {
            // Act
            var roll = DiceRoll.RollDiceRegular(numDice, sides, addedValue, "TestRoll");

            // Assert
            int minPossible = numDice * 1 + addedValue;
            int maxPossible = numDice * sides + addedValue;

            roll.Should().NotBeNull();
            roll.RollResultString.TextValue.Should().Contain("TestRoll");
            roll.RollDetailedResultString.TextValue.Should().Contain("TestRoll");
            roll.RollDetailedResultString.TextValue.Should().Contain("[");
            roll.RollDetailedResultString.TextValue.Should().Contain("]");
        }

        [Fact]
        public void RollDiceRegular_WithAddedValueZero_FormatsResultStringWithoutPlus()
        {
            // Act
            var roll = DiceRoll.RollDiceRegular(1, 20, 0, "Initiative");

            // Assert
            roll.RollResultString.TextValue.Should().StartWith("Rolled Initiative 1d20: Total: ");
            roll.RollDetailedResultString.TextValue.Should().StartWith("Rolled Initiative 1d20: [");
        }

        [Fact]
        public void RollDiceRegular_WithAddedValueNonZero_FormatsResultStringWithPlus()
        {
            // Act
            var roll = DiceRoll.RollDiceRegular(2, 6, 4, "Damage");

            // Assert
            roll.RollResultString.TextValue.Should().StartWith("Rolled Damage 2d6 + 4:  Total: ");
            roll.RollDetailedResultString.TextValue.Should().StartWith("Rolled Damage 2d6 + 4: [");
        }

        [Fact]
        public void RollDiceRegular_WithAdvantage_ProducesValidRolls()
        {
            // Act
            var roll = DiceRoll.RollDiceRegular(1, 20, 0, "AttackAdv", advantage: true, disadvantage: false);

            // Assert
            roll.Should().NotBeNull();
            roll.RollResultString.TextValue.Should().Contain("Total: ");
        }

        [Fact]
        public void RollDiceRegular_WithDisadvantage_ProducesValidRolls()
        {
            // Act
            var roll = DiceRoll.RollDiceRegular(1, 20, 0, "AttackDisadv", advantage: false, disadvantage: true);

            // Assert
            roll.Should().NotBeNull();
            roll.RollResultString.TextValue.Should().Contain("Total: ");
        }

        #endregion

        #region Dice Pool Rolls

        [Theory]
        [InlineData(5, 10, 6, 0)]
        [InlineData(6, 10, 7, 2)]
        [InlineData(3, 6, 4, 1)]
        public void RollDicePool_CalculatesSuccessesCorrectly(int numDice, int sides, int threshold, int rawSuccesses)
        {
            // Act
            var roll = DiceRoll.RollDicePool(numDice, sides, threshold, "PoolRoll", rawSuccesses);

            // Assert
            roll.Should().NotBeNull();
            roll.RollResultString.TextValue.Should().Contain($"Rolled PoolRoll {numDice}d{sides}");
            roll.RollDetailedResultString.TextValue.Should().Contain($"Rolled PoolRoll {numDice}d{sides}");
            roll.RollDetailedResultString.TextValue.Should().Contain("[");
        }

        [Fact]
        public void RollDicePool_WithRawSuccesses_IncludesEpicBonusInResultString()
        {
            // Act
            var roll = DiceRoll.RollDicePool(4, 10, 6, "EpicSkill", rawSuccesses: 3);

            // Assert
            roll.RollResultString.TextValue.Should().Contain("+ 3 epic bonus: Successes:");
            roll.RollDetailedResultString.TextValue.Should().Contain("+ 3 epic bonus: [");
        }

        [Fact]
        public void RollDicePool_WithoutRawSuccesses_DoesNotIncludeEpicBonusInResultString()
        {
            // Act
            var roll = DiceRoll.RollDicePool(4, 10, 6, "RegularPool", rawSuccesses: 0);

            // Assert
            roll.RollResultString.TextValue.Should().NotContain("epic bonus");
            roll.RollResultString.TextValue.Should().Contain("(Success Threshold: 6): Successes:");
        }

        #endregion

        #region Percentile Dice Rolls

        [Theory]
        [InlineData(50, 10)]
        [InlineData(80, 5)]
        [InlineData(25, 1)]
        public void RollDicePercentile_ReturnsValidResultAndFormattedString(int targetValue, int successInterval)
        {
            // Act
            var roll = DiceRoll.RollDicePercentile(targetValue, "SanityCheck", successInterval);

            // Assert
            roll.Should().NotBeNull();
            roll.RollResultString.TextValue.Should().Contain("Rolled SanityCheck target : " + targetValue);
            roll.RollResultString.TextValue.Should().Contain("Roll : ");
            (roll.RollResultString.TextValue.Contains("Sucess by :") || roll.RollResultString.TextValue.Contains("Failure by :")).Should().BeTrue();
            roll.RollDetailedResultString.TextValue.Should().Be(roll.RollResultString.TextValue);
        }

        #endregion

        #region ParseDiceRollString

        [Theory]
        [InlineData("1d20")]
        [InlineData("2d6")]
        [InlineData("3d8+4")]
        [InlineData("1d100")]
        [InlineData("4d12+0")]
        [InlineData("2D6+3")]
        [InlineData("10d6")]
        public void ParseDiceRollString_WithValidInput_ReturnsDiceRoll(string input)
        {
            // Act
            var result = DiceRoll.ParseDiceRollString(input);

            // Assert
            result.Should().NotBeNull();
            result!.RollResultString.TextValue.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [InlineData("1d20", true, false)]
        [InlineData("2d6+2", false, true)]
        public void ParseDiceRollString_WithAdvantageOrDisadvantage_ExecutesSuccessfully(string input, bool adv, bool disadv)
        {
            // Act
            var result = DiceRoll.ParseDiceRollString(input, advantage: adv, disadvantage: disadv);

            // Assert
            result.Should().NotBeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid")]
        [InlineData("d20")]
        [InlineData("2d")]
        [InlineData("0d6")]
        [InlineData("-1d6")]
        [InlineData("2d0")]
        [InlineData("2d-6")]
        [InlineData("2d6+abc")]
        [InlineData("2d6+1+2")]
        public void ParseDiceRollString_WithInvalidInput_ReturnsNull(string input)
        {
            // Act
            var result = DiceRoll.ParseDiceRollString(input);

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}
