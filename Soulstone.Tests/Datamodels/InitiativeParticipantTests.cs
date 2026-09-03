using System;
using System.Text.Json;
using FluentAssertions;
using Soulstone.Datamodels;
using Xunit;

namespace Soulstone.Tests.Datamodels
{
    public class InitiativeParticipantTests
    {
        [Fact]
        public void DefaultConstructor_InitializesDefaults()
        {
            var p = new InitiativeParticipant();

            p.Id.Should().NotBeNullOrWhiteSpace();
            p.Name.Should().BeEmpty();
            p.InitiativeValue.Should().Be(0);
            p.BonusModifier.Should().Be(0);
            p.IsCurrentCharacter.Should().BeFalse();
            p.Notes.Should().BeEmpty();
        }

        [Fact]
        public void ParameterizedConstructor_InitializesCorrectly()
        {
            var p = new InitiativeParticipant("Hero", 18, 3, true, "Leader");

            p.Id.Should().NotBeNullOrWhiteSpace();
            p.Name.Should().Be("Hero");
            p.InitiativeValue.Should().Be(18);
            p.BonusModifier.Should().Be(3);
            p.IsCurrentCharacter.Should().BeTrue();
            p.Notes.Should().Be("Leader");
        }

        [Fact]
        public void JsonSerialization_PreservesAllFields()
        {
            var original = new InitiativeParticipant("Goblin", 12, 1, false, "Minion #1");

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<InitiativeParticipant>(json);

            deserialized.Should().NotBeNull();
            deserialized!.Id.Should().Be(original.Id);
            deserialized.Name.Should().Be("Goblin");
            deserialized.InitiativeValue.Should().Be(12);
            deserialized.BonusModifier.Should().Be(1);
            deserialized.IsCurrentCharacter.Should().BeFalse();
            deserialized.Notes.Should().Be("Minion #1");
        }
    }
}
