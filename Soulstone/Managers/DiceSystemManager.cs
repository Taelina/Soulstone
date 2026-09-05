using Soulstone.Datamodels;
using Soulstone.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Attribute = Soulstone.Datamodels.Attribute;

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

            try
            {
                SaveCurrentStateBeforeSwitch(hostRuleset.systemName);

                if (!isSessionRulesetActive && currentDiceSystem != null)
                {
                    localBackupDiceSystem = currentDiceSystem;
                }

                currentDiceSystem = hostRuleset;
                isSessionRulesetActive = true;
                Plugin.Log?.Information($"Adopted host ruleset: {hostRuleset.systemName}");

                var sheet = CharacterManager.Instance.CharacterSheet;
                if (sheet != null)
                {
                    sheet.ApplyRulesetTemplate(hostRuleset);
                    CharacterSheet.SaveSheet(sheet);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to adopt host ruleset '{hostRuleset.systemName}'");
            }
        }

        public void SwitchDiceSystem(DiceSystem newSystem)
        {
            if (newSystem == null) return;

            try
            {
                SaveCurrentStateBeforeSwitch(newSystem.systemName);

                currentDiceSystem = newSystem;
                isSessionRulesetActive = false;
                localBackupDiceSystem = null;

                var sheet = CharacterManager.Instance.CharacterSheet;
                if (sheet != null)
                {
                    sheet.ApplyRulesetTemplate(newSystem);
                    CharacterSheet.SaveSheet(sheet);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to switch dice system to '{newSystem.systemName}'");
            }
        }

        private void SaveCurrentStateBeforeSwitch(string targetSystemName)
        {
            var sheet = CharacterManager.Instance.CharacterSheet;
            string charName = sheet != null && !string.IsNullOrWhiteSpace(sheet.CharacterFullName) ? sheet.CharacterFullName : "Character";
            string oldSysName = currentDiceSystem != null && !string.IsNullOrWhiteSpace(currentDiceSystem.systemName) ? currentDiceSystem.systemName : "Local System";
            string newSysName = !string.IsNullOrWhiteSpace(targetSystemName) ? targetSystemName : "New System";

            if (sheet != null)
            {
                if (currentDiceSystem != null && string.IsNullOrWhiteSpace(sheet.linkedDiceSystem))
                {
                    sheet.linkedDiceSystem = currentDiceSystem.systemName;
                }
                CharacterSheet.SaveSheet(sheet);
            }

            if (currentDiceSystem != null)
            {
                DiceSystem.SaveDiceSystem(currentDiceSystem);
            }

            string warnMsg = $"[Soulstone] Saved '{charName}' and ruleset '{oldSysName}' before switching to '{newSysName}'.";
            Messages.PrintEcho(warnMsg);

            try
            {
                if (Plugin.ToastGui != null)
                {
                    var toastOptions = new Dalamud.Game.Gui.Toast.QuestToastOptions
                    {
                        PlaySound = true,
                        DisplayCheckmark = true,
                        IconId = 0
                    };
                    Plugin.ToastGui.ShowQuest(warnMsg, toastOptions);
                }
            }
            catch { }
        }

        public void RevertToLocalRuleset()
        {
            try
            {
                if (localBackupDiceSystem != null)
                {
                    var target = localBackupDiceSystem;
                    SaveCurrentStateBeforeSwitch(target.systemName);
                    currentDiceSystem = target;
                    localBackupDiceSystem = null;

                    var sheet = CharacterManager.Instance.CharacterSheet;
                    if (sheet != null)
                    {
                        sheet.ApplyRulesetTemplate(target);
                        CharacterSheet.SaveSheet(sheet);
                    }
                }
                isSessionRulesetActive = false;
                Plugin.Log?.Information("Reverted to local ruleset.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to revert to local ruleset");
            }
        }

        public void Init()
        {
            try
            {
                currentDiceSystem = DiceSystem.LoadDiceSystem("Standard_Dice_System");
                PartySyncManager.Instance.OnRulesetOffered += OnRulesetOfferedFromParty;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to initialize DiceSystemManager in Init()");
            }
        }

        public void OnRulesetOfferedFromParty(RulesetBroadcastPayload payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.RulesetJson)) return;
            try
            {
                if (currentDiceSystem != null &&
                    !string.IsNullOrWhiteSpace(payload.SystemName) &&
                    string.Equals(currentDiceSystem.systemName, payload.SystemName, StringComparison.OrdinalIgnoreCase))
                {
                    Plugin.Log?.Information($"Ruleset '{payload.SystemName}' already matches active ruleset name. Skipping ruleset adoption.");
                    return;
                }

                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sharedSystem = System.Text.Json.JsonSerializer.Deserialize<DiceSystem>(payload.RulesetJson, options);
                if (sharedSystem != null)
                {
                    if (payload.Attributes != null && payload.Attributes.Count > 0)
                    {
                        sharedSystem.SystemAttributes = new Dictionary<string, Attribute>(payload.Attributes, StringComparer.OrdinalIgnoreCase);
                    }
                    if (payload.Skills != null && payload.Skills.Count > 0)
                    {
                        sharedSystem.SystemSkills = new Dictionary<string, Skill>(payload.Skills, StringComparer.OrdinalIgnoreCase);
                    }
                    if (payload.Abilities != null && payload.Abilities.Count > 0)
                    {
                        sharedSystem.SystemAbilities = new Dictionary<string, Ability>(payload.Abilities, StringComparer.OrdinalIgnoreCase);
                    }

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
