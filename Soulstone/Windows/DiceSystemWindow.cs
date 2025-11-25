using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Windows
{
    internal class DiceSystemWindow
    {
        private readonly Plugin plugin;

        private readonly Configuration configuration;

        private int selectedDiceTypeIndex = 0;

        public DiceSystemWindow(Plugin _plugin)
        {
            plugin = _plugin;
            configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public void DrawDiceSystemTab()
        {
            DiceSystem currentSystem = DiceSystemManager.Instance.CurrentDiceSystem;
            selectedDiceTypeIndex = (int)currentSystem.diceType;
            if (currentSystem != null)
            {
                if (ImGui.Button("Sauvegarder le système de dés"))
                {
                    DiceSystem.SaveDiceSystem(currentSystem);
                }
                using (var parent = ImRaii.Child("##DiceSystem", Vector2.Zero))
                {
                    ImGui.Text("Nom du système de dés :");
                    ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                    ImGui.SetNextItemWidth(200.0f);
                    ImGui.InputText("##DiceSystemName", ref currentSystem.systemName, 100);
                    ImGui.Separator();
                    ImGui.Checkbox("Système à pool de dés", ref currentSystem.dicePoolSystemEnabled);
                    ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                    ImGui.Checkbox("Système de dés standard", ref currentSystem.regularDiceSystemEnabled);
                    ImGui.Separator();

                    //Should have d20, d6, d10, d12, d100 like inputs
                    ImGui.SetNextItemWidth(75.0f);
                    if(ImGui.Combo("Type de dé ##Combo", ref selectedDiceTypeIndex, Enum.GetNames(typeof(DiceType))))
                    {
                        currentSystem.diceType = (DiceType)selectedDiceTypeIndex;
                    }

                    ImGui.Text("Seuil de réussite (pour les systèmes à pool de dés) :");
                    ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                    ImGui.SetNextItemWidth(50.0f);
                    ImGui.InputInt("##SuccessThreshold", ref currentSystem.successThreshold);
                }
            }            
        }
    }
}
