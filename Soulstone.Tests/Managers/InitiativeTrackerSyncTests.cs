using Soulstone.Datamodels;
using Soulstone.Managers;
using System;
using System.Collections.Generic;
using Xunit;

namespace Soulstone.Tests.Managers
{
    [Collection("NonParallelCollection")]
    public class InitiativeTrackerSyncTests
    {
        [Fact]
        public void ApplyRemoteTurnAdvance_UpdatesRoundTurnAndActiveParticipant()
        {
            var manager = InitiativeTrackerManager.Instance;
            manager.ClearParticipants();

            var p1 = new InitiativeParticipant("Alphinaud", 18, 2);
            var p2 = new InitiativeParticipant("Alisaie", 15, 3);
            var p3 = new InitiativeParticipant("Estinien", 12, 1);

            manager.AddParticipant(p1, false);
            manager.AddParticipant(p2, false);
            manager.AddParticipant(p3, false);

            var payload = new InitiativeTurnPayload
            {
                Round = 3,
                TurnNumber = 7,
                ActiveParticipantId = p2.Id
            };

            manager.ApplyRemoteTurnAdvance(payload);

            Assert.Equal(3, manager.CurrentRound);
            Assert.Equal(7, manager.CurrentTurnNumber);
            Assert.Equal(p2.Id, manager.ActiveParticipant?.Id);
            Assert.Equal("Alisaie", manager.ActiveParticipant?.Name);
        }

        [Fact]
        public void ApplyRemoteParticipantUpsert_AddsAndUpdatesParticipants()
        {
            var manager = InitiativeTrackerManager.Instance;
            manager.ClearParticipants();

            var p1 = new InitiativeParticipant("Graha", 16, 2, false, "GNB");
            manager.ApplyRemoteParticipantUpsert(p1);

            Assert.Single(manager.Participants);
            Assert.Equal("Graha", manager.Participants[0].Name);
            Assert.Equal(16, manager.Participants[0].InitiativeValue);

            // Update existing
            var updatedP1 = new InitiativeParticipant("Graha", 22, 2, false, "GNB")
            {
                Id = p1.Id
            };
            manager.ApplyRemoteParticipantUpsert(updatedP1);

            Assert.Single(manager.Participants);
            Assert.Equal(22, manager.Participants[0].InitiativeValue);
        }

        [Fact]
        public void ApplyRemoteParticipantRemove_RemovesCorrectParticipant()
        {
            var manager = InitiativeTrackerManager.Instance;
            manager.ClearParticipants();

            var p1 = new InitiativeParticipant("Krile", 14, 1);
            var p2 = new InitiativeParticipant("Tataru", 8, 0);

            manager.AddParticipant(p1, false);
            manager.AddParticipant(p2, false);

            Assert.Equal(2, manager.Participants.Count);

            manager.ApplyRemoteParticipantRemove(p1.Id);

            Assert.Single(manager.Participants);
            Assert.Equal("Tataru", manager.Participants[0].Name);
        }

        [Fact]
        public void ApplyRemoteReset_ResetsEncounterState()
        {
            var manager = InitiativeTrackerManager.Instance;
            manager.CurrentRound = 5;
            manager.CurrentTurnNumber = 14;
            manager.ActiveParticipantIndex = 2;

            manager.ApplyRemoteReset();

            Assert.Equal(1, manager.CurrentRound);
            Assert.Equal(1, manager.CurrentTurnNumber);
            Assert.Equal(0, manager.ActiveParticipantIndex);
        }

        [Fact]
        public void ApplyRemoteFullSync_ReplacesParticipantsAndTurnState()
        {
            var manager = InitiativeTrackerManager.Instance;
            manager.ClearParticipants();

            var p1 = new InitiativeParticipant("Lyse", 20, 3);
            var p2 = new InitiativeParticipant("Hien", 17, 2);

            var payload = new InitiativeSyncPayload
            {
                Round = 2,
                TurnNumber = 4,
                ActiveParticipantId = p2.Id,
                Participants = new List<InitiativeParticipant> { p1, p2 }
            };

            manager.ApplyRemoteFullSync(payload);

            Assert.Equal(2, manager.Participants.Count);
            Assert.Equal(2, manager.CurrentRound);
            Assert.Equal(4, manager.CurrentTurnNumber);
            Assert.Equal(p2.Id, manager.ActiveParticipant?.Id);
        }
    }
}
