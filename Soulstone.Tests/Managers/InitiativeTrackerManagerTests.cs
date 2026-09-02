using System;
using System.Linq;
using FluentAssertions;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Xunit;

namespace Soulstone.Tests.Managers
{
    [Collection("NonParallel")]
    public class InitiativeTrackerManagerTests : IDisposable
    {
        private readonly InitiativeTrackerManager manager;

        public InitiativeTrackerManagerTests()
        {
            TestHelper.EnsureMockServices();
            manager = InitiativeTrackerManager.Instance;
            manager.FullReset();
        }

        public void Dispose()
        {
            manager.FullReset();
        }

        [Fact]
        public void InitialState_IsCorrect()
        {
            manager.Participants.Should().BeEmpty();
            manager.CurrentRound.Should().Be(1);
            manager.CurrentTurnNumber.Should().Be(1);
            manager.ActiveParticipantIndex.Should().Be(0);
            manager.IsAscendingOrder.Should().BeFalse();
            manager.ActiveParticipant.Should().BeNull();
        }

        [Fact]
        public void AddParticipant_AddsAndSortsDescendingByDefault()
        {
            manager.AddParticipant("Player 1", 15, 2);
            manager.AddParticipant("Monster", 22, 0);
            manager.AddParticipant("Player 2", 8, 1);

            manager.Participants.Should().HaveCount(3);
            manager.Participants[0].Name.Should().Be("Monster");
            manager.Participants[0].InitiativeValue.Should().Be(22);
            manager.Participants[1].Name.Should().Be("Player 1");
            manager.Participants[1].InitiativeValue.Should().Be(15);
            manager.Participants[2].Name.Should().Be("Player 2");
            manager.Participants[2].InitiativeValue.Should().Be(8);
        }

        [Fact]
        public void SortParticipants_Ascending_SortsLowestFirst()
        {
            manager.AddParticipant("Player 1", 15, 2);
            manager.AddParticipant("Monster", 22, 0);
            manager.AddParticipant("Player 2", 8, 1);

            manager.SortParticipants(true);

            manager.IsAscendingOrder.Should().BeTrue();
            manager.Participants[0].Name.Should().Be("Player 2");
            manager.Participants[0].InitiativeValue.Should().Be(8);
            manager.Participants[1].Name.Should().Be("Player 1");
            manager.Participants[1].InitiativeValue.Should().Be(15);
            manager.Participants[2].Name.Should().Be("Monster");
            manager.Participants[2].InitiativeValue.Should().Be(22);
        }

        [Fact]
        public void SortParticipants_PreservesActiveParticipant()
        {
            var p1 = manager.AddParticipant("Player 1", 15);
            var p2 = manager.AddParticipant("Monster", 22);
            var p3 = manager.AddParticipant("Player 2", 8);

            // In descending order: Monster (0), Player 1 (1), Player 2 (2)
            manager.SetActiveIndex(1); // Player 1 is active
            manager.ActiveParticipant!.Name.Should().Be("Player 1");

            // Sort ascending: Player 2 (0), Player 1 (1), Monster (2)
            manager.SortParticipants(true);
            manager.ActiveParticipant!.Name.Should().Be("Player 1");
            manager.ActiveParticipantIndex.Should().Be(1);

            // Set active to Monster (index 2 in asc)
            manager.SetActiveIndex(2);
            manager.ActiveParticipant!.Name.Should().Be("Monster");

            // Sort descending: Monster (0), Player 1 (1), Player 2 (2)
            manager.SortParticipants(false);
            manager.ActiveParticipant!.Name.Should().Be("Monster");
            manager.ActiveParticipantIndex.Should().Be(0);
        }

        [Fact]
        public void NextTurn_CyclesThroughParticipantsAndIncrementsRound()
        {
            manager.AddParticipant("P1", 20);
            manager.AddParticipant("P2", 15);
            manager.AddParticipant("P3", 10);

            manager.ActiveParticipantIndex.Should().Be(0);
            manager.CurrentRound.Should().Be(1);
            manager.CurrentTurnNumber.Should().Be(1);
            manager.ActiveParticipant!.Name.Should().Be("P1");

            // Move to P2
            manager.NextTurn();
            manager.ActiveParticipantIndex.Should().Be(1);
            manager.CurrentRound.Should().Be(1);
            manager.CurrentTurnNumber.Should().Be(2);
            manager.ActiveParticipant!.Name.Should().Be("P2");

            // Move to P3
            manager.NextTurn();
            manager.ActiveParticipantIndex.Should().Be(2);
            manager.CurrentRound.Should().Be(1);
            manager.CurrentTurnNumber.Should().Be(3);
            manager.ActiveParticipant!.Name.Should().Be("P3");

            // Wrap around to P1 in Round 2
            manager.NextTurn();
            manager.ActiveParticipantIndex.Should().Be(0);
            manager.CurrentRound.Should().Be(2);
            manager.CurrentTurnNumber.Should().Be(4);
            manager.ActiveParticipant!.Name.Should().Be("P1");
        }

        [Fact]
        public void PreviousTurn_DecrementsTurnAndRoundCorrectly()
        {
            manager.AddParticipant("P1", 20);
            manager.AddParticipant("P2", 15);

            manager.NextTurn(); // P2, round 1, turn 2
            manager.NextTurn(); // P1, round 2, turn 3

            manager.CurrentRound.Should().Be(2);
            manager.ActiveParticipantIndex.Should().Be(0);
            manager.CurrentTurnNumber.Should().Be(3);

            // Step back to P2, round 1
            manager.PreviousTurn();
            manager.CurrentRound.Should().Be(1);
            manager.ActiveParticipantIndex.Should().Be(1);
            manager.CurrentTurnNumber.Should().Be(2);

            // Step back to P1, round 1
            manager.PreviousTurn();
            manager.CurrentRound.Should().Be(1);
            manager.ActiveParticipantIndex.Should().Be(0);
            manager.CurrentTurnNumber.Should().Be(1);

            // Step back again should clamp at round 1, turn 1, index 0
            manager.PreviousTurn();
            manager.CurrentRound.Should().Be(1);
            manager.ActiveParticipantIndex.Should().Be(0);
            manager.CurrentTurnNumber.Should().Be(1);
        }

        [Fact]
        public void ResetTurns_ResetsRoundAndTurnWithoutClearingParticipants()
        {
            manager.AddParticipant("P1", 20);
            manager.AddParticipant("P2", 15);
            manager.NextTurn();
            manager.NextTurn();

            manager.CurrentRound.Should().Be(2);

            manager.ResetTurns();

            manager.CurrentRound.Should().Be(1);
            manager.CurrentTurnNumber.Should().Be(1);
            manager.ActiveParticipantIndex.Should().Be(0);
            manager.Participants.Should().HaveCount(2);
        }

        [Fact]
        public void FullReset_ClearsParticipantsAndResetsTurns()
        {
            manager.AddParticipant("P1", 20);
            manager.AddParticipant("P2", 15);
            manager.NextTurn();

            manager.FullReset();

            manager.Participants.Should().BeEmpty();
            manager.CurrentRound.Should().Be(1);
            manager.CurrentTurnNumber.Should().Be(1);
            manager.ActiveParticipantIndex.Should().Be(0);
        }

        [Fact]
        public void RemoveParticipant_RemovesAndAdjustsActiveIndex()
        {
            var p1 = manager.AddParticipant("P1", 20);
            var p2 = manager.AddParticipant("P2", 15);
            var p3 = manager.AddParticipant("P3", 10);

            manager.SetActiveIndex(2); // P3
            manager.ActiveParticipant!.Name.Should().Be("P3");

            manager.RemoveParticipant(p3.Id).Should().BeTrue();
            manager.Participants.Should().HaveCount(2);
            manager.ActiveParticipantIndex.Should().Be(1); // Clamped to P2
            manager.ActiveParticipant!.Name.Should().Be("P2");

            manager.RemoveParticipant("non-existent-id").Should().BeFalse();
        }

        [Fact]
        public void AddOrUpdateCurrentCharacter_UpdatesExistingIfAlreadyPresent()
        {
            var sheet = new CharacterSheet { CharacterFullName = "Warrior of Light" };
            var diceSys = new DiceSystem();

            manager.AddOrUpdateCurrentCharacter(sheet, diceSys, 17, 3);
            manager.Participants.Should().HaveCount(1);
            manager.Participants[0].Name.Should().Be("Warrior of Light");
            manager.Participants[0].InitiativeValue.Should().Be(17);
            manager.Participants[0].BonusModifier.Should().Be(3);
            manager.Participants[0].IsCurrentCharacter.Should().BeTrue();

            // Re-roll with new value
            manager.AddOrUpdateCurrentCharacter(sheet, diceSys, 24, 3);
            manager.Participants.Should().HaveCount(1);
            manager.Participants[0].InitiativeValue.Should().Be(24);
        }
    }
}
