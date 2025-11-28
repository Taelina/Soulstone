using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using Soulstone.Datamodels;
using Soulstone.Managers;
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
        DiceSystem currentDiceSystem = null;

        private bool editingStats = false;

        private string newAttributeName = "";
        private int newAttributeValue = 0;

        private string newSkillName = "";
        private int newSkillValue = 0;
        private int selectedAttributeIndex = 0;
        private string selectedAttribute = "";
        private Skill newSkill = null;

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
                if (DiceSystemManager.Instance.CurrentDiceSystem != null)
                {
                    currentDiceSystem = DiceSystemManager.Instance.CurrentDiceSystem;
                    diceType = Enum.GetName(typeof(DiceType),DiceSystemManager.Instance.CurrentDiceSystem.DiceType);
                }
                if (currentCharacter != null)
                {
                    ImGui.SetNextItemWidth(50.0f);
                    ImGui.Text("Type de dé du système :");
                    ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                    ImGui.Text(diceType);
                    if (ImGui.Checkbox("Éditer les stats du personnage", ref editingStats))
                    { }
                    if (ImGui.Button("Sauvegarder la fiche de personnage"))
                    {
                        CharacterSheet.SaveSheet(currentCharacter);
                    }
                    ImGui.Text("Attributs :");
                    using (var family = ImRaii.Child("##Attributes", new Vector2(200.0f, 200.0f), true))
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
                                    if (currentDiceSystem != null)
                                    {
                                        if (currentDiceSystem.DicePoolSystemEnabled)
                                        {
                                            string[] parsedType = diceType.Split('d');
                                            int parsedSides = Convert.ToInt32(parsedType[1]);
                                            Plugin.Log.Information($"Rolling {attribute.Value}d{parsedSides} against success threshold {currentDiceSystem.SuccessThreshold}");
                                            DiceRoll roll = DiceRoll.RollDicePool(attribute.Value, parsedSides, currentDiceSystem.SuccessThreshold, attribute.Key);
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
                                        if (currentDiceSystem.RegularDiceSystemEnabled)
                                        {
                                            string[] parsedType = diceType.Split('d');
                                            int parsedSides = Convert.ToInt32(parsedType[1]);
                                            Plugin.Log.Information($"Rolling {attribute.Value}d{parsedSides}");
                                            DiceRoll roll = DiceRoll.RollDiceRegular(1, parsedSides, attribute.Value, attribute.Key);
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
                    ImGui.Text("Compétences :");
                    using (var family = ImRaii.Child("##Skills", new Vector2(200.0f, 200.0f), true))
                    {
                        if (ImGui.Button("Ajouter"))
                        {
                            showSkillPopup = true;
                        }
                        if (showSkillPopup)
                        {
                            string[] attributeKeys = currentCharacter.characterAttributes.Keys.ToArray<string>();
                            ImGui.BeginPopupModal("Nouvel Compétence", ref showSkillPopup, ImGuiWindowFlags.AlwaysAutoResize);
                            ImGui.InputText("Nom de la compétence", ref newSkillName, 100);
                            ImGui.InputInt("Valeur", ref newSkillValue, 1);
                            ImGui.SetNextItemWidth(75.0f);
                            if (ImGui.Combo("Attribut lié##Combo", ref selectedAttributeIndex, attributeKeys))
                            {
                                selectedAttribute = attributeKeys[selectedAttributeIndex];
                            }
                            if (ImGui.Button("Ajouter"))
                            {
                                newSkill = new Skill
                                {
                                    skillName = newSkillName,
                                    skillModifier = newSkillValue,
                                    linkedAttribute = selectedAttribute
                                };
                                if (currentCharacter.characterSkills == null)
                                    currentCharacter.characterSkills = new Dictionary<string, Skill>();
                                if (!currentCharacter.characterSkills.ContainsKey(newAttributeName))
                                    currentCharacter.characterSkills.Add(newAttributeName, newSkill);
                                showSkillPopup = false;
                            }
                            ImGui.OpenPopup("Nouvel attribut");
                            ImGui.EndPopup();
                        }
                    }
                }                
            }
        }
    }
}
