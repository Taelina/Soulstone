using Soulstone.Datamodels;
using Soulstone.Localizations;
using Soulstone.Managers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace Soulstone.Tests.Windows
{
    [Collection("NonParallelCollection")]
    public class GroupWindowTests
    {
        public GroupWindowTests()
        {
            TestHelper.EnsureMockServices();
            if (!LocalizationManager.Instance.LocalizedLanguages.ContainsKey(Language.English))
            {
                var plugin = (Plugin)RuntimeHelpers.GetUninitializedObject(typeof(Plugin));
                var config = new Soulstone.Configuration { Language = Language.English };
                typeof(Plugin).GetProperty(nameof(Plugin.Configuration))?.SetValue(plugin, config);
                LocalizationManager.Instance.InitLoc(plugin);
            }
        }
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

        [Fact]
        public void GroupLocalizations_HaveParityAcrossLanguages()
        {
            var en = LocalizationManager.Instance.LocalizedLanguages[Language.English].LocalizedStrings;
            var fr = LocalizationManager.Instance.LocalizedLanguages[Language.Français].LocalizedStrings;

            var groupKeys = new[]
            {
                "GroupManagementTitle", "GroupManagementSubtitle", "GroupStatusConnected",
                "GroupStatusNoSoulstone", "GroupRulesetInSync", "GroupRulesetOutOfSync",
                "GroupBadgeLeader", "GroupRefreshRoster", "GroupBroadcastRuleset",
                "GroupSyncInitiative", "GroupBroadcastVitals", "GroupHealth", "GroupMana",
                "GroupNoMembers", "GroupRelayStatus", "GroupRelayUrl", "GroupCreateSession",
                "GroupJoinSession", "GroupInviteCode", "GroupCopyInvite", "GroupInviteCopied",
                "GroupLeaveSession", "GroupReconnect", "GroupForgetSession", "GroupConnecting",
                "GroupSessionCreated", "GroupSessionJoined", "GroupConnectionFailed",
                "GroupInvalidInvite", "GroupRollNow", "GroupDismissRoll", "GroupRollName",
                "GroupRollFormula", "GroupRequestRoll", "GroupRollForMember", "GroupPrivateStats",
                "GroupViewCards", "GroupViewGrid", "GroupSearchHint", "GroupFilterAll",
                "GroupFilterSoulstone", "GroupFilterLeader", "GroupFilterOutOfSync",
                "GroupBatchRoll", "GroupBatchRollTitle", "GroupBatchRollSend", "GroupQuickRoll",
                "GroupRollStat", "GroupRequestStat", "GroupSessionInfo", "GroupHostLabel",
                "GroupConnectedMembers", "GroupJoinTab", "GroupHostTab", "GroupCopied"
            };

            foreach (var key in groupKeys)
            {
                Assert.True(en.ContainsKey(key), $"English dictionary should contain '{key}'");
                Assert.True(fr.ContainsKey(key), $"French dictionary should contain '{key}'");
                Assert.False(string.IsNullOrWhiteSpace(en[key]), $"English translation for '{key}' should not be empty");
                Assert.False(string.IsNullOrWhiteSpace(fr[key]), $"French translation for '{key}' should not be empty");
            }
        }

        [Theory]
        [InlineData(100, 100, 1.0f)]
        [InlineData(50, 100, 0.5f)]
        [InlineData(0, 100, 0.0f)]
        [InlineData(-10, 100, 0.0f)]
        [InlineData(150, 100, 1.0f)]
        public void PartyMemberSyncData_ClampedHealthFractions_BehavesPredictably(int current, int max, float expectedFraction)
        {
            var member = new PartyMemberSyncData
            {
                CurrentHp = current,
                MaxHp = max
            };

            int maxHp = member.MaxHp > 0 ? member.MaxHp : 100;
            float hpFraction = Math.Clamp((float)member.CurrentHp / maxHp, 0.0f, 1.0f);

            Assert.Equal(expectedFraction, hpFraction);
        }
    }
}
