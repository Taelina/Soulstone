using Soulstone.Datamodels;
using Soulstone.Managers;
using System;
using System.Collections.Generic;
using Xunit;

namespace Soulstone.Tests.Windows
{
    [Collection("NonParallelCollection")]
    public class GroupWindowTests
    {
        [Fact]
        public void PartyMemberSyncData_VitalsAndFraction_CalculatesCorrectly()
        {
            var member = new PartyMemberSyncData
            {
                CharacterName = "G'raha Tia",
                CurrentHp = 75,
                MaxHp = 150,
                CurrentMana = 200,
                MaxMana = 400
            };

            float hpFraction = (float)member.CurrentHp / member.MaxHp;
            float manaFraction = (float)member.CurrentMana / member.MaxMana;

            Assert.Equal(0.5f, hpFraction);
            Assert.Equal(0.5f, manaFraction);
        }

        [Fact]
        public void PartyMemberSyncData_RulesetSyncEvaluation_WorksCorrectly()
        {
            var member = new PartyMemberSyncData
            {
                CharacterName = "Estinien Varlineau"
            };

            var presence = new PresencePayload
            {
                CharacterName = "Estinien Varlineau",
                RulesetName = "Dragoon D20"
            };

            member.ApplyPresence(presence, "Dragoon D20");
            Assert.True(member.IsRulesetInSync);

            member.ApplyPresence(presence, "Black Mage D20");
            Assert.False(member.IsRulesetInSync);
        }

        [Fact]
        public void InitiativeTracker_ImportPartyMembers_AddsAllMembersWithoutDuplicates()
        {
            var manager = InitiativeTrackerManager.Instance;
            manager.ClearParticipants();

            var members = new List<PartyMemberSyncData>
            {
                new PartyMemberSyncData { CharacterName = "Y'shtola", JobName = "BLM", CurrentHp = 100, MaxHp = 100 },
                new PartyMemberSyncData { CharacterName = "Thancred", JobName = "GNB", CurrentHp = 200, MaxHp = 200 },
                new PartyMemberSyncData { CharacterName = "Urianger", JobName = "AST", CurrentHp = 90, MaxHp = 90 }
            };

            manager.ImportPartyMembers(members);

            Assert.Equal(3, manager.Participants.Count);
            Assert.Contains(manager.Participants, p => p.Name == "Y'shtola");
            Assert.Contains(manager.Participants, p => p.Name == "Thancred");
            Assert.Contains(manager.Participants, p => p.Name == "Urianger");

            // Calling import again should not duplicate participants
            manager.ImportPartyMembers(members);
            Assert.Equal(3, manager.Participants.Count);
        }
    }
}
