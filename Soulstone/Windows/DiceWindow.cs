using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
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
        private string testroll = "";
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
            ImGui.SetNextItemWidth(200.0f);
            ImGui.InputText("Manual Roll Input", ref rollInputText);
            detailedRoll = configuration.detailedRolls;

            ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
            ImGui.Checkbox("Advantage", ref advantage);
            ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
            ImGui.Checkbox("Disadvantage", ref disadvantage);

            if (ImGui.Button("Roll dice"))
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
