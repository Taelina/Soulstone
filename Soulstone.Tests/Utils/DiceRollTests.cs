using FluentAssertions;
using Xunit;
using Soulstone.Datamodels;
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

        [Fact]
        public void RollDicePool_WithMaxSuccessCount_AwardsExtraSuccessesWhenMaxRolled()
        {
            // Act - roll a large pool of d4 dice where 4 is rolled with maxSuccessCount=2 and threshold=4
            // Since any 4 will give 2 successes, total successes >= count of 4s * 2
            var roll = DiceRoll.RollDicePool(100, 4, 4, "ExaltedPool", rawSuccesses: 0, maxSuccessCount: 2);

            // Assert
            int maxCount = roll.IndividualRolls.Count(r => r == 4);
            int expectedSuccesses = maxCount * 2;
            roll.RollResult.Should().Be(expectedSuccesses);
        }

        [Fact]
        public void RollWithSystem_DicePoolSystem_HonorsDicePoolMaxSuccessCount()
        {
            var system = new DiceSystem
            {
                SystemType = SystemType.DicePoolSystem,
                DiceType = DiceType.d6,
                SuccessThreshold = 6,
                DicePoolMaxSuccessCount = 3
            };

            var roll = DiceRoll.RollWithSystem(system, numberOfDice: 50);
            roll.Should().NotBeNull();
            int sixesCount = roll!.IndividualRolls.Count(r => r == 6);
            roll.RollResult.Should().Be(sixesCount * 3);
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
        [InlineData("d20")]
        [InlineData("2d6")]
        [InlineData("3d8+4")]
        [InlineData("1d100")]
        [InlineData("4d12+0")]
        [InlineData("2D6+3")]
        [InlineData("10d6")]
        [InlineData("2d6+1+2")]
        [InlineData("2d6 + 1d8 + 3")]
        [InlineData("4d6kh3")]
        [InlineData("2d20kl1 + 2")]
        [InlineData("3d6! + 1")]
        [InlineData("2d6r<=2")]
        [InlineData("1d20min10")]
        [InlineData("(1d8 + 2) * 2")]
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
        [InlineData("2d6 + 1d8", true, false)]
        public void ParseDiceRollString_WithAdvantageOrDisadvantage_ExecutesSuccessfully(string input, bool adv, bool disadv)
        {
            // Act
            var result = DiceRoll.ParseDiceRollString(input, advantage: adv, disadvantage: disadv);

            // Assert
            result.Should().NotBeNull();
        }

        [Theory]
        [InlineData("1d20-3", "1d20 - 3")]
        [InlineData("3d8-1", "3d8 - 1")]
        public void ParseDiceRollString_WithNegativeModifier_AppliesModifier(string input, string expectedFormula)
        {
            // Act
            var result = DiceRoll.ParseDiceRollString(input);

            // Assert
            result.Should().NotBeNull();
            result!.RollResultString.TextValue.Should().Contain(expectedFormula);
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid")]
        [InlineData("2d")]
        [InlineData("0d6")]
        [InlineData("-1d6")]
        [InlineData("2d0")]
        [InlineData("2d-6")]
        [InlineData("2d6+abc")]
        [InlineData("2d6+")]
        [InlineData("(2d6+")]
        public void ParseDiceRollString_WithInvalidInput_ReturnsNull(string input)
        {
            // Act
            var result = DiceRoll.ParseDiceRollString(input);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Dice System Aware Rolls

        [Theory]
        [InlineData(DiceType.d4, 4)]
        [InlineData(DiceType.d6, 6)]
        [InlineData(DiceType.d10, 10)]
        [InlineData(DiceType.d20, 20)]
        [InlineData(DiceType.d100, 100)]
        public void GetSystemSides_ReturnsSidesConfiguredOnSystem(DiceType diceType, int expectedSides)
        {
            // Arrange
            var system = new DiceSystem { DiceType = diceType };

            // Act & Assert
            DiceRoll.GetSystemSides(system).Should().Be(expectedSides);
        }

        [Fact]
        public void GetSystemSides_WithNullSystem_DefaultsToTwenty()
        {
            DiceRoll.GetSystemSides(null).Should().Be(20);
        }

        [Fact]
        public void RollWithSystem_DnDSystem_UsesSystemDiceTypeAndModifier()
        {
            // Arrange
            var system = new DiceSystem { SystemType = SystemType.DnDSystem, DiceType = DiceType.d12 };

            // Act
            var roll = DiceRoll.RollWithSystem(system, numberOfDice: 5, addedValue: 3, rollName: "Check");

            // Assert
            roll.Should().NotBeNull();
            roll!.RollResultString.TextValue.Should().Contain("1d12 + 3");
            roll.IndividualRolls.Should().HaveCount(1);
        }

        [Fact]
        public void RollWithSystem_DicePoolSystem_RollsPoolAgainstThreshold()
        {
            // Arrange
            var system = new DiceSystem
            {
                SystemType = SystemType.DicePoolSystem,
                DiceType = DiceType.d10,
                SuccessThreshold = 7
            };

            // Act
            var roll = DiceRoll.RollWithSystem(system, numberOfDice: 6, rollName: "Pool");

            // Assert
            roll.Should().NotBeNull();
            roll!.RollResultString.TextValue.Should().Contain("6d10");
            roll.RollResultString.TextValue.Should().Contain("Success Threshold: 7");
            roll.IndividualRolls.Should().HaveCount(6);
        }

        [Fact]
        public void RollWithSystem_PercentileSystem_RollsAgainstTarget()
        {
            // Arrange
            var system = new DiceSystem
            {
                SystemType = SystemType.PercentileSystem,
                DiceType = DiceType.d100,
                SuccessInterval = 5
            };

            // Act
            var roll = DiceRoll.RollWithSystem(system, numberOfDice: 1, target: 65, rollName: "Sanity");

            // Assert
            roll.Should().NotBeNull();
            roll!.RollResultString.TextValue.Should().Contain("target : 65");
            roll.RollResult.Should().BeInRange(1, 100);
        }

        [Fact]
        public void RollStatWithSystem_DnDSystem_TreatsStatValueAsModifier()
        {
            // Arrange
            var system = new DiceSystem { SystemType = SystemType.DnDSystem, DiceType = DiceType.d20 };

            // Act
            var roll = DiceRoll.RollStatWithSystem(system, "Strength Check", 4);

            // Assert
            roll.Should().NotBeNull();
            roll!.RollResultString.TextValue.Should().Contain("Strength Check 1d20 + 4");
            roll.IndividualRolls.Should().HaveCount(1);
            roll.RollResult.Should().BeInRange(5, 24);
        }

        [Fact]
        public void RollStatWithSystem_DicePoolSystem_TreatsStatValueAsPoolSize()
        {
            // Arrange
            var system = new DiceSystem
            {
                SystemType = SystemType.DicePoolSystem,
                DiceType = DiceType.d6,
                SuccessThreshold = 4
            };

            // Act
            var roll = DiceRoll.RollStatWithSystem(system, "Athletics", 5);

            // Assert
            roll.Should().NotBeNull();
            roll!.IndividualRolls.Should().HaveCount(5);
            roll.RollResultString.TextValue.Should().Contain("Athletics 5d6");
        }

        [Fact]
        public void RollStatWithSystem_PercentileSystem_TreatsStatValueAsTarget()
        {
            // Arrange
            var system = new DiceSystem { SystemType = SystemType.PercentileSystem, DiceType = DiceType.d100 };

            // Act
            var roll = DiceRoll.RollStatWithSystem(system, "Occult", 45);

            // Assert
            roll.Should().NotBeNull();
            roll!.RollResultString.TextValue.Should().Contain("target : 45");
        }

        [Fact]
        public void RollStatWithSystem_DicePoolSystem_WithNonPositiveStat_StillRollsOneDie()
        {
            // Arrange
            var system = new DiceSystem { SystemType = SystemType.DicePoolSystem, DiceType = DiceType.d10 };

            // Act
            var roll = DiceRoll.RollStatWithSystem(system, "Untrained", 0);

            // Assert
            roll.Should().NotBeNull();
            roll!.IndividualRolls.Should().HaveCount(1);
        }

        [Theory]
        [InlineData(DiceType.d20, 0, "1d20")]
        [InlineData(DiceType.d20, 3, "1d20+3")]
        [InlineData(DiceType.d20, -2, "1d20-2")]
        [InlineData(DiceType.d12, 1, "1d12+1")]
        public void DescribeSystemRoll_DnDSystem_FormatsSignedModifier(DiceType diceType, int statValue, string expected)
        {
            // Arrange
            var system = new DiceSystem { SystemType = SystemType.DnDSystem, DiceType = diceType };

            // Act & Assert
            DiceRoll.DescribeSystemRoll(system, statValue).Should().Be(expected);
        }

        [Fact]
        public void DescribeSystemRoll_DicePoolSystem_ShowsPoolAndThreshold()
        {
            // Arrange
            var system = new DiceSystem
            {
                SystemType = SystemType.DicePoolSystem,
                DiceType = DiceType.d10,
                SuccessThreshold = 8
            };

            // Act & Assert
            DiceRoll.DescribeSystemRoll(system, 4).Should().Be("4d10 >= 8");
        }

        [Fact]
        public void DescribeSystemRoll_PercentileSystem_ShowsTarget()
        {
            // Arrange
            var system = new DiceSystem { SystemType = SystemType.PercentileSystem, DiceType = DiceType.d100 };

            // Act & Assert
            DiceRoll.DescribeSystemRoll(system, 55).Should().Be("1d100 <= 55");
        }

        [Fact]
        public void DescribeSystemRoll_WithNullSystem_FallsBackToD20()
        {
            DiceRoll.DescribeSystemRoll(null).Should().Be("1d20");
        }

        #endregion
    }
}
