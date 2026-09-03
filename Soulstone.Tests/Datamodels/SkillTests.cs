using System.Text.Json;
using FluentAssertions;
using Xunit;
using Soulstone.Datamodels;

namespace Soulstone.Tests.Datamodels
{
    public class SkillTests
    {
        [Fact]
        public void DefaultConstructor_CreatesInstance()
        {
            // Act
            var skill = new Skill();

            // Assert
            skill.Should().NotBeNull();
        }

        [Fact]
        public void Properties_SetAndGet_ShouldReturnExpectedValues()
        {
            // Arrange
            var skill = new Skill
            {
                Id = 42,
                SkillName = "Stealth",
                SkillDescription = "Ability to move silently and remain undetected",
                LinkedAttribute = "Dexterity",
                SkillModifier = 3
            };

            // Assert
            skill.Id.Should().Be(42);
            skill.SkillName.Should().Be("Stealth");
            skill.SkillDescription.Should().Be("Ability to move silently and remain undetected");
            skill.LinkedAttribute.Should().Be("Dexterity");
            skill.SkillModifier.Should().Be(3);

            // Also verify public backing fields
            skill.id.Should().Be(42);
            skill.skillName.Should().Be("Stealth");
            skill.skillDescription.Should().Be("Ability to move silently and remain undetected");
            skill.linkedAttribute.Should().Be("Dexterity");
            skill.skillModifier.Should().Be(3);
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(0)]
        [InlineData(10)]
        public void SkillModifier_CanBeNegativeZeroOrPositive(int modifier)
        {
            // Arrange
            var skill = new Skill { SkillModifier = modifier };

            // Act & Assert
            skill.SkillModifier.Should().Be(modifier);
            skill.skillModifier.Should().Be(modifier);
        }

        [Fact]
        public void JsonSerialization_PreservesAllFields()
        {
            // Arrange
            var original = new Skill
            {
                Id = 1,
                SkillName = "Arcana",
                SkillDescription = "Knowledge of magical lore",
                LinkedAttribute = "Intelligence",
                SkillModifier = 5
            };

            // Act
            string json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<Skill>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Id.Should().Be(1);
            deserialized.SkillName.Should().Be("Arcana");
            deserialized.SkillDescription.Should().Be("Knowledge of magical lore");
            deserialized.LinkedAttribute.Should().Be("Intelligence");
            deserialized.SkillModifier.Should().Be(5);
        }
    }
}
