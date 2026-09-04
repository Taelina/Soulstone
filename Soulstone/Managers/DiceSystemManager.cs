using Soulstone.Datamodels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Managers
{
    internal class DiceSystemManager
    {
        private static DiceSystemManager? instance = null;

        private DiceSystem? currentDiceSystem;
        private DiceSystem? localBackupDiceSystem;
        private bool isSessionRulesetActive = false;

        private DiceSystemManager()
        {
            // Private constructor to prevent instantiation
        }

        public static DiceSystemManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DiceSystemManager();
                }
                return instance;
            }
        }

        internal DiceSystem? CurrentDiceSystem { get => currentDiceSystem; set => currentDiceSystem = value; }
        internal DiceSystem? LocalBackupDiceSystem { get => localBackupDiceSystem; set => localBackupDiceSystem = value; }
        public bool IsSessionRulesetActive => isSessionRulesetActive;

        public void AdoptSessionRuleset(DiceSystem hostRuleset)
        {
            if (hostRuleset == null) return;

            if (!isSessionRulesetActive && currentDiceSystem != null)
            {
                localBackupDiceSystem = currentDiceSystem;
            }

            currentDiceSystem = hostRuleset;
            isSessionRulesetActive = true;
            Plugin.Log?.Information($"Adopted host ruleset: {hostRuleset.systemName}");
        }

        public void RevertToLocalRuleset()
        {
            if (localBackupDiceSystem != null)
            {
                currentDiceSystem = localBackupDiceSystem;
                localBackupDiceSystem = null;
            }
            isSessionRulesetActive = false;
            Plugin.Log?.Information("Reverted to local ruleset.");
        }

        public void Init()
        {
            currentDiceSystem = DiceSystem.LoadDiceSystem("Standard_Dice_System");
            PartySyncManager.Instance.OnRulesetOffered += OnRulesetOfferedFromParty;
        }

        public void OnRulesetOfferedFromParty(RulesetBroadcastPayload payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.RulesetJson)) return;
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sharedSystem = System.Text.Json.JsonSerializer.Deserialize<DiceSystem>(payload.RulesetJson, options);
                if (sharedSystem != null)
                {
                    AdoptSessionRuleset(sharedSystem);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to adopt broadcast ruleset from party");
            }
        }
    }
}
