using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.ImGuiMethods;
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

        private bool setupCombo = false;

        private string newAbilityName = "";
        private int newAbilityValue = 0;
        private int selectedSkillIndex = 0;
        private string selectedSkill = "";
        private Ability newAbility = null;

        private bool advantageRoll = false;
        private bool disadvantageRoll = false;

        private string[] attributeKeys = new string[] { };
        private string[] skillKeys = new string[] { };

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
                    diceType = Enum.GetName(typeof(DiceType), DiceSystemManager.Instance.CurrentDiceSystem.DiceType);
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
                    if (ImGui.Checkbox("Jet avec avantage", ref advantageRoll))
                    {
                        disadvantageRoll = false;
                    }
                    ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                    if (ImGui.Checkbox("Jet avec désavantage", ref disadvantageRoll))
                    {
                        advantageRoll = false;
                    }
                    ImGui.Text("Attributs :");
                    ImGui.SameLine(0.0f, 145.0f);
                    ImGui.Text("Compétences :");
                    ImGui.SameLine(0.0f, 120.0f);
                    ImGui.Text("Capacités :");
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
                                {
                                    currentCharacter.characterAttributes = new Dictionary<string, int>();
                                    currentCharacter.characterAttributes.Add(newAttributeName, newAttributeValue);
                                }
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
                    ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                    using (var family = ImRaii.Child("##Skills", new Vector2(200.0f, 200.0f), true))
                    {
                        if (ImGui.Button("Ajouter"))
                        {
                            showSkillPopup = true;
                            if (currentCharacter.characterAttributes != null)
                                attributeKeys = currentCharacter.characterAttributes.Keys.ToArray<string>();
                        }
                        if (showSkillPopup)
                        {
                            if (ImGui.BeginPopupModal("Nouvelle Compétence", ref showSkillPopup, ImGuiWindowFlags.AlwaysAutoResize))
                            {
                                selectedAttributeIndex = 0;
                                ImGui.InputText("Nom de la compétence", ref newSkillName, 100);
                                ImGui.InputInt("Valeur", ref newSkillValue, 1);
                                ImGui.SetNextItemWidth(75.0f);
                                ImGui.InputText("Attribut lié##InputSkill", ref selectedAttribute, 100);
                                if (ImGui.Button("Ajouter"))
                                {
                                    if (currentCharacter.characterAttributes != null && !currentCharacter.characterAttributes.ContainsKey(selectedAttribute))
                                    {
                                        Plugin.Log.Information("L'attribut lié n'existe pas.");
                                    }
                                    else
                                    {
                                        newSkill = new Skill
                                        {
                                            skillName = newSkillName,
                                            skillModifier = newSkillValue,
                                            linkedAttribute = selectedAttribute
                                        };
                                        if (currentCharacter.characterSkills == null)
                                        {
                                            currentCharacter.characterSkills = new Dictionary<string, Skill>();
                                            currentCharacter.characterSkills.Add(newSkillName, newSkill);
                                        }
                                        if (!currentCharacter.characterSkills.ContainsKey(newSkillName))
                                            currentCharacter.characterSkills.Add(newSkillName, newSkill);
                                        showSkillPopup = false;
                                    }
                                }
                            }
                            ImGui.OpenPopup("Nouvelle Compétence");
                            ImGui.EndPopup();
                        }

                        if (currentCharacter.characterSkills != null)
                        {
                            foreach (KeyValuePair<string, Skill> skill in currentCharacter.characterSkills)
                            {
                                ImGui.Text($"{skill.Value.skillName} (lié à {skill.Value.linkedAttribute}) : ");
                                ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                                UiUtils.ManageInputField(ref CollectionsMarshal.GetValueRefOrNullRef(currentCharacter.characterSkills, skill.Key).skillModifier, $"Skill_{skill.Value.skillName}", editingStats);
                                ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                                if (ImGui.Button("Lancer"))
                                {
                                    if (currentDiceSystem != null)
                                    {
                                        if (currentDiceSystem.DicePoolSystemEnabled)
                                        {
                                            string[] parsedType = diceType.Split('d');
                                            int parsedSides = Convert.ToInt32(parsedType[1]);
                                            int attributeValue = 0;
                                            currentCharacter.characterAttributes.TryGetValue(skill.Value.linkedAttribute, out attributeValue);
                                            int totalDice = skill.Value.skillModifier + attributeValue;
                                            Plugin.Log.Information($"Rolling {totalDice}d{parsedSides} against success threshold {currentDiceSystem.SuccessThreshold}");
                                            DiceRoll roll = DiceRoll.RollDicePool(totalDice, parsedSides, currentDiceSystem.SuccessThreshold, skill.Value.skillName);
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
                                            int attributeValue = currentCharacter.characterAttributes[skill.Value.linkedAttribute];
                                            int totalModifier = skill.Value.skillModifier + attributeValue;
                                            Plugin.Log.Information($"Rolling 1d{parsedSides} + {totalModifier}");
                                            DiceRoll roll = DiceRoll.RollDiceRegular(1, parsedSides, totalModifier, skill.Value.skillName, advantageRoll, disadvantageRoll);
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
                    ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                    using (var family = ImRaii.Child("##Abilities", new Vector2(300.0f, 200.0f), true))
                    {
                        if (ImGui.Button("Ajouter"))
                        {
                            showAbilitiesPopup = true;
                            if (currentCharacter.characterAttributes != null)
                                attributeKeys = currentCharacter.characterAttributes.Keys.ToArray<string>();

                            if (currentCharacter.characterSkills != null)
                                skillKeys = currentCharacter.characterSkills.Keys.ToArray<string>();
                        }
                        if (showAbilitiesPopup)
                        {
                            ImGui.BeginPopupModal("Nouvelle Capacité", ref showAbilitiesPopup, ImGuiWindowFlags.AlwaysAutoResize);
                            selectedAttributeIndex = 0;
                            selectedSkillIndex = 0;
                            ImGui.InputText("Nom de la capacité", ref newAbilityName, 100);
                            ImGui.InputInt("Valeur", ref newAbilityValue, 1);
                            ImGui.SetNextItemWidth(100.0f);
                            ImGui.InputText("Attribut lié##InputCap", ref selectedAttribute, 100);
                            ImGui.SetNextItemWidth(100.0f);
                            ImGui.InputText("Compétence lié##InputCap", ref selectedSkill,100);
                            if (ImGui.Button("Ajouter"))
                            {
                                if (currentCharacter.characterAttributes != null && !currentCharacter.characterAttributes.ContainsKey(selectedAttribute)
                                    && currentCharacter.characterSkills != null && !currentCharacter.characterSkills.ContainsKey(selectedSkill))
                                {
                                    Plugin.Log.Information("L'attribut ou compenténce lié n'existe pas.");
                                    return;
                                }
                                else
                                {
                                    newAbility = new Ability
                                    {
                                        abilityName = newAbilityName,
                                        abilityModifier = newAbilityValue,
                                        linkedAttribute = selectedAttribute
                                    };
                                    currentCharacter.characterSkills.TryGetValue(selectedSkill, out newAbility.linkedSkill);
                                    if (currentCharacter.characterAbilities == null)

                                    {
                                        currentCharacter.characterAbilities = new Dictionary<string, Ability>();
                                        currentCharacter.characterAbilities.Add(newAttributeName, newAbility);
                                    }
                                    if (!currentCharacter.characterAbilities.ContainsKey(newAttributeName))
                                        currentCharacter.characterAbilities.Add(newAttributeName, newAbility);
                                    showAbilitiesPopup = false;
                                }
                            }
                            ImGui.OpenPopup("Nouvelle Capacité");
                            ImGui.EndPopup();
                        }
                        if (currentCharacter.characterAbilities != null)
                        {
                            foreach (KeyValuePair<string, Ability> ability in currentCharacter.characterAbilities)
                            {
                                ImGui.Text($"{ability.Value.abilityName} (lié à {ability.Value.linkedAttribute} et {ability.Value.linkedSkill.skillName}) : ");
                                ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                                UiUtils.ManageInputField(ref CollectionsMarshal.GetValueRefOrNullRef(currentCharacter.characterAbilities, ability.Key).abilityModifier, $"Ability_{ability.Value.abilityName}", editingStats);
                                ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                                if (ImGui.Button("Lancer"))
                                {
                                    if (currentDiceSystem != null)
                                    {
                                        if (currentDiceSystem.DicePoolSystemEnabled)
                                        {
                                            string[] parsedType = diceType.Split('d');
                                            int parsedSides = Convert.ToInt32(parsedType[1]);
                                            int attributeValue = currentCharacter.characterAttributes[ability.Value.linkedAttribute];
                                            int skillValue = ability.Value.linkedSkill.skillModifier;
                                            int totalDice = ability.Value.abilityModifier + attributeValue + skillValue;
                                            Plugin.Log.Information($"Rolling {totalDice}d{parsedSides} against success threshold {currentDiceSystem.SuccessThreshold}");
                                            DiceRoll roll = DiceRoll.RollDicePool(totalDice, parsedSides, currentDiceSystem.SuccessThreshold, ability.Value.abilityName);
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
                                            int attributeValue = currentCharacter.characterAttributes[ability.Value.linkedAttribute];
                                            int skillValue = ability.Value.linkedSkill.skillModifier;
                                            int totalModifier = ability.Value.abilityModifier + attributeValue + skillValue;
                                            Plugin.Log.Information($"Rolling 1d{parsedSides} + {totalModifier}");
                                            DiceRoll roll = DiceRoll.RollDiceRegular(1, parsedSides, totalModifier, ability.Value.abilityName, advantageRoll, disadvantageRoll);
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
}
