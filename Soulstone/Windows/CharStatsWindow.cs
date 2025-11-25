using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using Soulstone.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Windows
{
    internal class CharStatsWindow
    {
        public string diceType = "";

        private bool showAbilitiesPopup = false;
        private bool showSkillPopup = false;
        private bool showAttributesPopup = false;

        CharacterSheet currentCharacter = null;

        private bool editingStats = false;

        private string newAttributeName = "";
        private int newAttributeValue = 0;

        private readonly Plugin plugin;

        public  bool detailedRoll = false;

        private readonly Configuration configuration;

        public CharStatsWindow(Plugin _plugin)
        {
            plugin = _plugin;
            configuration = plugin.Configuration;
        }

        public void Dispose()
        { }

        public void CharStatsDraw()
        {
            detailedRoll = configuration.detailedRolls;
            using (var parent = ImRaii.Child("##CharStats", Vector2.Zero))
            {
                if (CharacterManager.Instance.CharacterSheet != null)
                {
                    currentCharacter = CharacterManager.Instance.CharacterSheet;
                }
                if (currentCharacter != null)
                {
                    ImGui.SetNextItemWidth(50.0f);
                    ImGui.InputText("Type de dé du système", ref diceType, 10);
                    if (ImGui.Checkbox("Éditer les stats du personnage", ref editingStats))
                    { }
                    if (ImGui.Button("Sauvegarder la fiche de personnage"))
                    {
                        CharacterSheet.SaveSheet(currentCharacter);
                    }
                    ImGui.Text("Attributs :");
                    using (var family = ImRaii.Child("##Attributes", new Vector2(200.0f, 150.0f), true))
                    {
                        if (ImGui.Button("Ajouter"))
                        {
                            showAttributesPopup = true;
                        }
                        if (showAttributesPopup)
                        {
                            ImGui.BeginPopupModal("Nouvel attribut", ref showAttributesPopup, ImGuiWindowFlags.AlwaysAutoResize);
                            ImGui.InputText("Nom de l'attribut", ref newAttributeName, 100);
                            ImGui.InputInt("Valeur", ref newAttributeValue, 1);
                            if (ImGui.Button("Ajouter"))
                            {
                                if (currentCharacter.characterAttributes == null)
                                    currentCharacter.characterAttributes = new Dictionary<string, int>();
                                if (!currentCharacter.characterAttributes.ContainsKey(newAttributeName))
                                    currentCharacter.characterAttributes.Add(newAttributeName, newAttributeValue);
                                showAttributesPopup = false;
                            }
                            ImGui.OpenPopup("Nouvel attribut");
                            ImGui.EndPopup();
                        }

                        if (currentCharacter.characterAttributes != null)
                        {
                            foreach (KeyValuePair<string, int> attribute in currentCharacter.characterAttributes)
                            {
                                ImGui.Text($"{attribute.Key} : ");
                                ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                                UiUtils.ManageInputField(ref CollectionsMarshal.GetValueRefOrNullRef(currentCharacter.characterAttributes, attribute.Key), $"FamilyRelation_{attribute.Key}", editingStats);
                                ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                                if (ImGui.Button("Lancer"))
                                {
                                    DiceRoll roll = DiceRoll.RollDice(1, Convert.ToInt32(diceType), attribute.Value, attribute.Key);
                                    if (roll != null)
                                    {
                                        if (!detailedRoll)
                                        {
                                            XivChatEntry rollMessage = new XivChatEntry
                                            {
                                                Message = roll.RollResultString,
                                                Type = XivChatType.Say
                                            };
                                            Messages.SendMessage(rollMessage);
                                        }
                                        else
                                        {
                                            XivChatEntry rollMessage = new XivChatEntry
                                            {
                                                Message = roll.RollDetailedResultString,
                                                Type = XivChatType.Say
                                            };
                                            Messages.SendMessage(rollMessage);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }                
            }
        }
    }
}
