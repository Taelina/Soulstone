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
        private int selectedSystemTypeIndex = 0;

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
            selectedSystemTypeIndex = (int)currentSystem.systemType;
            if (currentSystem != null)
            {
                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("DiceSystemSaveButton")}"))
                {
                    DiceSystem.SaveDiceSystem(currentSystem);
                }
                using (var parent = ImRaii.Child("##DiceSystem", Vector2.Zero))
                {
                    if(parent.Success)
                    {
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("DiceSystemNameLabel")}");
                        ImGui.SameLine(0.0f, UiUtils.DefaultFieldSpacing);
                        ImGui.SetNextItemWidth(200.0f);
                        ImGui.InputText("##DiceSystemName", ref currentSystem.systemName, 100);
                        ImGui.Separator();
                        ImGui.SetNextItemWidth(150.0f);
                        if (ImGui.Combo($"{LocalizationManager.Instance.GetLocalizedString("SystemTypeCombo")}##DiceSystemCombo", ref selectedSystemTypeIndex, Enum.GetNames<SystemType>()))
                        {
                            currentSystem.systemType = (SystemType)selectedSystemTypeIndex;
                        }
                        ImGui.Separator();

                        //Should have d20, d6, d10, d12, d100 like inputs
                        ImGui.SetNextItemWidth(75.0f);
                        if (ImGui.Combo($"{LocalizationManager.Instance.GetLocalizedString("DiceTypeCombo")}##DiceTypeCombo", ref selectedDiceTypeIndex, Enum.GetNames<DiceType>()))
                        {
                            currentSystem.diceType = (DiceType)selectedDiceTypeIndex;
                        }

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("SuccessThresholdLabel")}");
                        ImGui.SameLine(0.0f, UiUtils.DefaultFieldSpacing);
                        ImGui.SetNextItemWidth(50.0f);
                        ImGui.InputInt("##SuccessThreshold", ref currentSystem.successThreshold);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("SuccessIntervalLabel")}");
                        ImGui.SameLine(0.0f, UiUtils.DefaultFieldSpacing);
                        ImGui.SetNextItemWidth(50.0f);
                        ImGui.InputInt("##SuccessInterval", ref currentSystem.successInterval);

                        //ImGui.Checkbox("Attributs de style DnD", ref currentSystem.dndStyleAttributes); TODO Implement real D&D Style modifiers for attributes for this.
                        //ImGui.Checkbox("Compétence liée à un seul attribut", ref currentSystem.skillLinkedToOneAttribute);
                        //ImGui.Checkbox("Capacité liée à un seul attribut", ref currentSystem.abilityLinkedToOneAttribute);
                        //ImGui.Checkbox("Capacité liée à une seule compétence", ref currentSystem.abilityLinkedToOneSkill); TODO : Determine if this is relevant anymore.
                        //ImGui.Checkbox("Le système gère les jets de sauvegarde", ref currentSystem.systemHasSaves); TODO : Implement real D&D Style saves.
                        ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("DnDStyleAdvDisadvCheckbox")}", ref currentSystem.systemHasAdvantageDisadvantage);
                    }
                }
            }            
        }
    }
}
