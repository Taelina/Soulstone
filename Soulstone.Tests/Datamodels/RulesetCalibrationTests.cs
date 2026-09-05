using Soulstone.Datamodels;
using Soulstone.Managers;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Soulstone.Tests.Datamodels
{
    [Collection("NonParallel")]
    public class RulesetCalibrationTests
    {
        public RulesetCalibrationTests()
        {
            TestHelper.EnsureMockServices();
            DiceSystemManager.Instance.RevertToLocalRuleset();
        }

        [Fact]
        public void AdoptSessionRuleset_BacksUpLocalAndActivatesSession()
        {
            var manager = DiceSystemManager.Instance;
            var localSystem = new DiceSystem
            {
                systemName = "Local D20 Rules",
                systemType = SystemType.DnDSystem,
                diceType = DiceType.d20
            };
            manager.CurrentDiceSystem = localSystem;
            manager.LocalBackupDiceSystem = null;

            var dmSystem = new DiceSystem
            {
                systemName = "DM Shadowrun D6",
                systemType = SystemType.DicePoolSystem,
                diceType = DiceType.d6,
                SuccessThreshold = 5
            };

            manager.AdoptSessionRuleset(dmSystem);

            Assert.True(manager.IsSessionRulesetActive);
            Assert.Equal("DM Shadowrun D6", manager.CurrentDiceSystem?.systemName);
            Assert.Equal("Local D20 Rules", manager.LocalBackupDiceSystem?.systemName);

            // Revert back
            manager.RevertToLocalRuleset();

            Assert.False(manager.IsSessionRulesetActive);
            Assert.Equal("Local D20 Rules", manager.CurrentDiceSystem?.systemName);
            Assert.Null(manager.LocalBackupDiceSystem);
        }

        [Fact]
        public void AdoptSessionRuleset_MultipleAdoptions_PreservesOriginalBackup()
        {
            var manager = DiceSystemManager.Instance;
            var localSystem = new DiceSystem { systemName = "Original Local" };
            manager.CurrentDiceSystem = localSystem;
            manager.LocalBackupDiceSystem = null;

            var dmSystem1 = new DiceSystem { systemName = "DM Ruleset v1" };
            var dmSystem2 = new DiceSystem { systemName = "DM Ruleset v2" };

            manager.AdoptSessionRuleset(dmSystem1);
            manager.AdoptSessionRuleset(dmSystem2);

            Assert.True(manager.IsSessionRulesetActive);
            Assert.Equal("DM Ruleset v2", manager.CurrentDiceSystem?.systemName);
            Assert.Equal("Original Local", manager.LocalBackupDiceSystem?.systemName);

            manager.RevertToLocalRuleset();
            Assert.Equal("Original Local", manager.CurrentDiceSystem?.systemName);
        }

        [Fact]
        public void OnRulesetOfferedFromParty_DeserializesAndAdoptsPayload()
        {
            var manager = DiceSystemManager.Instance;
            manager.CurrentDiceSystem = new DiceSystem { systemName = "My System" };
            manager.LocalBackupDiceSystem = null;

            var sharedSystem = new DiceSystem
            {
                systemName = "Cyberpunk 2077 Redux",
                systemType = SystemType.DnDSystem,
                systemHasAugmentations = true
            };

            var payload = new RulesetBroadcastPayload
            {
                SenderName = "Party Leader",
                SystemName = "Cyberpunk 2077 Redux",
                RulesetJson = JsonSerializer.Serialize(sharedSystem)
            };

            manager.OnRulesetOfferedFromParty(payload);

            Assert.True(manager.IsSessionRulesetActive);
            Assert.Equal("Cyberpunk 2077 Redux", manager.CurrentDiceSystem?.systemName);
            Assert.True(manager.CurrentDiceSystem?.systemHasAugmentations);
            Assert.Equal("My System", manager.LocalBackupDiceSystem?.systemName);
        }

        [Fact]
        public void CharacterSheet_ResourceModifications_UpdatesLocalSyncData()
        {
            var sheet = new CharacterSheet
            {
                characterFullName = "Krile Baldesion"
            };
            CharacterManager.Instance.CharacterSheet = sheet;

            sheet.SetResourceCurrent("Health", 85);
            sheet.SetResourceMax("Health", 120);

            var localMember = PartySyncManager.Instance.ConnectedPartyMembers.GetOrAdd("Krile Baldesion", n => new PartyMemberSyncData { CharacterName = n });
            PartySyncManager.Instance.PopulateLocalPlayerVitals(localMember);

            Assert.Equal(85, localMember.CurrentHp);
            Assert.Equal(120, localMember.MaxHp);
        }
    }
}
