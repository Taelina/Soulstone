using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Windows
{
    internal class DiceWindow
    {
        private bool detailedRoll = false;
        private string rollInputText = "";

        private bool advantage = false;
        private bool disadvantage = false;

        private readonly Plugin plugin;

        private readonly Configuration configuration;

        public DiceWindow(Plugin _plugin)
        {
            plugin = _plugin;
            configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public void DrawDiceTab()
        {
            DiceSystem currentSystem = DiceSystemManager.Instance.CurrentDiceSystem;
            ImGui.SetNextItemWidth(200.0f);
            ImGui.InputText($"{LocalizationManager.Instance.GetLocalizedString("RollInputLabel")}", ref rollInputText);
            detailedRoll = configuration.detailedRolls;

            if(currentSystem.systemHasAdvantageDisadvantage)
            {
                ImGui.SameLine(0.0f, UiUtils.DefaultNextToSpace);
                if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("AdvantageCheckbox")}", ref advantage))
                {
                    disadvantage = false;
                }
                ImGui.SameLine(0.0f, UiUtils.DefaultNextToSpace);
                if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("DisadvantageCheckbox")}", ref disadvantage))
                {
                    advantage = false;
                }
            }            

            if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")}"))
            {

                Plugin.Log.Info($"Rolling dice with input: {rollInputText}");
                DiceRoll DR = DiceRoll.ParseDiceRollString(rollInputText);
                if (DR != null)
                {
                    if (!detailedRoll)
                    {
                        XivChatEntry rollMessage = new XivChatEntry
                        {
                            Message = DR.RollResultString,
                            Type = XivChatType.Say
                        };
                        Messages.SendMessage(rollMessage);
                    }
                    else
                    {
                        XivChatEntry rollMessage = new XivChatEntry
                        {
                            Message = DR.RollDetailedResultString,
                            Type = XivChatType.Say
                        };
                        Messages.SendMessage(rollMessage);
                    }

                }
            }
        }
    }
}
