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

        public DiceWindow()
        {
        }

        public void Dispose() { }

        public void DrawDiceTab(Plugin plugin)
        {
            ImGui.InputText("Manual Roll Input", ref rollInputText);
            if (ImGui.Checkbox("Detailed Roll", ref detailedRoll))
            {
                //DO THING ?
            }

            if (ImGui.Button("Roll dice"))
            {

                Plugin.Log.Info($"Rolling dice with input: {rollInputText}");
                DiceRoll DR = DiceRoll.ParseDiceRollString(rollInputText);
                if (DR != null)
                {
                    if (!detailedRoll)
                    {
                        XivChatEntry testeuh = new XivChatEntry
                        {
                            Message = DR.RollResultString,
                            Type = XivChatType.Say
                        };
                        Messages.SendMessage(testeuh);
                    }
                    else
                    {
                        XivChatEntry testeuh = new XivChatEntry
                        {
                            Message = DR.RollDetailedResultString,
                            Type = XivChatType.Say
                        };
                        Messages.SendMessage(testeuh);
                    }

                }
            }
        }
    }
}
