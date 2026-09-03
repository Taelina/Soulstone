using System.Text.Json;
using FluentAssertions;
using Xunit;
using Soulstone.Datamodels;

namespace Soulstone.Tests.Datamodels
{
    public class AbilityTests
    {
        [Fact]
        public void DefaultConstructor_CreatesInstance()
        {
            // Act
            var ability = new Ability();

            // Assert
            ability.Should().NotBeNull();
        }

        [Fact]
        public void Properties_SetAndGet_ShouldReturnExpectedValues()
        {
            // Arrange
            var skill = new Skill
            {
                Id = 10,
                SkillName = "Acrobatics",
                LinkedAttribute = "Dexterity",
                SkillModifier = 2
            };

            var ability = new Ability
            {
                Id = 7,
                AbilityName = "Backflip Strike",
                AbilityDescription = "Performs a stylish acrobatic melee attack",
                LinkedAttribute = "Dexterity",
                LinkedSkill = skill,
                AbilityModifier = 4
            };

            // Assert
            ability.Id.Should().Be(7);
            ability.AbilityName.Should().Be("Backflip Strike");
            ability.AbilityDescription.Should().Be("Performs a stylish acrobatic melee attack");
            ability.LinkedAttribute.Should().Be("Dexterity");
            ability.LinkedSkill.Should().BeSameAs(skill);
            ability.AbilityModifier.Should().Be(4);

            // Verify public backing fields
            ability.id.Should().Be(7);
            ability.abilityName.Should().Be("Backflip Strike");
            ability.abilityDescription.Should().Be("Performs a stylish acrobatic melee attack");
            ability.linkedAttribute.Should().Be("Dexterity");
            ability.linkedSkill.Should().BeSameAs(skill);
            ability.abilityModifier.Should().Be(4);
        }

        [Fact]
        public void JsonSerialization_WithNestedLinkedSkill_PreservesAllData()
        {
            // Arrange
            var ability = new Ability
            {
                Id = 12,
                AbilityName = "Fireball",
                AbilityDescription = "Launches a fiery projectile",
                LinkedAttribute = "Intelligence",
                AbilityModifier = 8,
                LinkedSkill = new Skill
                {
                    Id = 2,
                    SkillName = "Evocation",
                    LinkedAttribute = "Intelligence",
                    SkillModifier = 3
                }
            };

            // Act
            string json = JsonSerializer.Serialize(ability);
            var deserialized = JsonSerializer.Deserialize<Ability>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Id.Should().Be(12);
            deserialized.AbilityName.Should().Be("Fireball");
            deserialized.AbilityDescription.Should().Be("Launches a fiery projectile");
            deserialized.LinkedAttribute.Should().Be("Intelligence");
            deserialized.AbilityModifier.Should().Be(8);
            deserialized.LinkedSkill.Should().NotBeNull();
            deserialized.LinkedSkill.Id.Should().Be(2);
            deserialized.LinkedSkill.SkillName.Should().Be("Evocation");
            deserialized.LinkedSkill.LinkedAttribute.Should().Be("Intelligence");
            deserialized.LinkedSkill.SkillModifier.Should().Be(3);
        }

        [Fact]
        public void JsonSerialization_WithNullLinkedSkill_SerializesSuccessfully()
        {
            // Arrange
            var ability = new Ability
            {
                Id = 3,
                AbilityName = "Basic Attack",
                AbilityDescription = "Standard attack",
                LinkedAttribute = "Strength",
                AbilityModifier = 1,
                LinkedSkill = null!
            };

            // Act
            string json = JsonSerializer.Serialize(ability);
            var deserialized = JsonSerializer.Deserialize<Ability>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.AbilityName.Should().Be("Basic Attack");
            deserialized.LinkedSkill.Should().BeNull();
        }
    }
}
