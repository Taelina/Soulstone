using System.Text.Json;
using FluentAssertions;
using Xunit;
using Soulstone.Datamodels;
using Attribute = Soulstone.Datamodels.Attribute;

namespace Soulstone.Tests.Datamodels
{
    public class AttributeTests
    {
        [Fact]
        public void Constructor_WithValidNameAndValue_InitializesPropertiesCorrectly()
        {
            // Arrange & Act
            var attribute = new Attribute("Strength", 14);

            // Assert
            attribute.Name.Should().Be("Strength");
            attribute.Value.Should().Be(14);
            attribute.TempBonus.Should().Be(0);
            attribute.PermBonus.Should().Be(0);
            attribute.EpicBonus.Should().Be(0);
            attribute.TotalValue.Should().Be(14);
        }

        [Theory]
        [InlineData(10, 0, 0, 10)]
        [InlineData(10, 2, 3, 15)]
        [InlineData(10, -2, 0, 8)]
        [InlineData(10, 0, -4, 6)]
        [InlineData(10, -3, -2, 5)]
        [InlineData(0, 5, 5, 10)]
        [InlineData(-5, 2, 1, -2)]
        public void TotalValue_WithVariousBonuses_CalculatesSumOfValueTempAndPerm(
            int baseValue, int tempBonus, int permBonus, int expectedTotal)
        {
            // Arrange
            var attribute = new Attribute("Dexterity", baseValue)
            {
                TempBonus = tempBonus,
                PermBonus = permBonus
            };

            // Act & Assert
            attribute.TotalValue.Should().Be(expectedTotal);
        }

        [Fact]
        public void EpicBonus_DoesNotAffectTotalValue()
        {
            // Arrange
            var attribute = new Attribute("Intelligence", 16)
            {
                TempBonus = 2,
                PermBonus = 1,
                EpicBonus = 5
            };

            // Act & Assert
            attribute.TotalValue.Should().Be(19);
            attribute.EpicBonus.Should().Be(5);
        }

        [Fact]
        public void JsonSerialization_PreservesAllFields()
        {
            // Arrange
            var original = new Attribute("Charisma", 18)
            {
                TempBonus = 1,
                PermBonus = 2,
                EpicBonus = 3
            };

            // Act
            string json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<Attribute>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Name.Should().Be("Charisma");
            deserialized.Value.Should().Be(18);
            deserialized.TempBonus.Should().Be(1);
            deserialized.PermBonus.Should().Be(2);
            deserialized.EpicBonus.Should().Be(3);
            deserialized.TotalValue.Should().Be(21);
        }

        [Fact]
        public void Constructor_WithEmptyNameAndZeroValue_InitializesDefaults()
        {
            // Arrange & Act
            var attribute = new Attribute("", 0);

            // Assert
            attribute.Name.Should().BeEmpty();
            attribute.Value.Should().Be(0);
            attribute.TotalValue.Should().Be(0);
        }
    }
}
