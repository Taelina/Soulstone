using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
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
using static Dalamud.Interface.Windowing.Window;
using static System.Net.Mime.MediaTypeNames;

namespace Soulstone.Windows
{
    internal class CharacterWindow
    {


        private float defaultContentHeight = 150.0f;

        private bool showFamilyPopup = false;
        private bool showFriendsPopup = false;
        private bool showEnemiesPopup = false;

        private bool editingCharsheet = false;

        string newMemberName = "Nouveau membre";
        string newMemberDescription = "Description";

        CharacterSheet currentCharacter = null;

        private readonly Plugin plugin;

        private readonly Configuration configuration;
        public CharacterWindow(Plugin _plugin)
        {
            plugin = _plugin;
            configuration = plugin.Configuration;
        }

        public void Dispose() { }

        

        public void DrawCharTab()
        {
            using (var parent = ImRaii.Child("##CharSheet", Vector2.Zero))
            {
                if (CharacterManager.Instance.CharacterSheet != null)
                {
                    currentCharacter = CharacterManager.Instance.CharacterSheet;
                }

                if (currentCharacter != null)
                {
                    if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("EditCharsheetCheck")}##EditCheck", ref editingCharsheet))
                    { }
                    if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("SaveCharsheetButton")}##SaveButton"))
                    {
                        CharacterSheet.SaveSheet(currentCharacter);
                    }
                    using (var child = ImRaii.Child("##Identity", new Vector2(0.0f, defaultContentHeight), true))
                    {
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharFullnameField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                        UiUtils.ManageInputField(ref currentCharacter.characterFullName, "FullName", editingCharsheet);

                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharNicknameField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterNickName, "NickName", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharSpecieField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterRace, "CharacterRace", editingCharsheet);
                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharSubSpecieField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterSubRace, "CharacterSubRace", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharClassField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterJob, "CharacterJob", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharSexField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterSex, "CharacterSex", editingCharsheet);

                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharGenderField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterGender, "CharacterGender", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharPronounsField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterPronouns, "CharacterPronouns", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharAgeField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterAge, "CharacterAge", editingCharsheet);
                    }

                    using (var child = ImRaii.Child("##HRP", new Vector2(0.0f, defaultContentHeight), true))
                    {
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("PlayerOOCInfo")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterInfo, "CharacterHrpInfo", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("PlayerTimezone")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.playerTimezone, "PlayerTimezone", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("PlayerAvailability")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.playerAvailability, "PlayerAvailability", editingCharsheet);
                    }

                    using (var child = ImRaii.Child("##Appearance", new Vector2(0.0f, defaultContentHeight), true))
                    {
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharHeightField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterHeight, "CharacterHeight", editingCharsheet);

                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharWeightField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterWeight, "CharacterWeight", editingCharsheet);

                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharBuildField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterBuild, "CharacterBuild", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharEyeColorField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterEyeColor, "CharacterEyeColor", editingCharsheet);

                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharHairColorField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterHairColor, "CharacterHairColor", editingCharsheet);

                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharSkinColorField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterSkinTone, "CharacterSkinTone", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharScarsField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterScars, "CharacterScars", editingCharsheet);

                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharTatooField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterTattoos, "CharacterTattoos", editingCharsheet);

                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharOtherQuirkField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterDistinctiveFeatures, "CharacterDistinctiveFeatures", editingCharsheet);
                    }

                    using (var child = ImRaii.Child("##QuickLook", new Vector2(0.0f, defaultContentHeight), true))
                    {
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("QuickLookField1")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterQuickLook1, "CharacterQuickLook", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("QuickLookField2")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterQuickLook2, "CharacterQuickLook2", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("QuickLookField3")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterQuickLook3, "CharacterQuickLook3", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("QuickLookField4")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterQuickLook4, "CharacterQuickLook4", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("QuickLookField5")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterQuickLook5, "CharacterQuickLook5", editingCharsheet);
                    }

                    using (var child = ImRaii.Child("##Background", new Vector2(0.0f, 300.0f), true))
                    {
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharBirthplaceField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterHomeland, "CharacterHomeland", editingCharsheet);

                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharOriginField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterOrigin, "CharacterOrigin", editingCharsheet);

                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharAffiliationField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterAffiliation, "CharacterAffiliation", editingCharsheet);

                        ImGui.SameLine(0.0f, UiUtils.defaultFieldSpacing);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharWorkField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageInputField(ref currentCharacter.characterOccupation, "CharacterOccupation", editingCharsheet);

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharReputationField")}");
                        ImGuiEx.Tooltip("La réputation de votre personnage dans son environnement social et professionnel.");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageBigInputField(ref currentCharacter.characterReputation, "CharacterReputation", editingCharsheet);


                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharFamilyRelationTab")}");
                        ImGui.SameLine(0.0f, 90.0f);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharFriendsTab")}");
                        ImGui.SameLine(0.0f, 90.0f);
                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharEnemiesTab")}");
                        using (var family = ImRaii.Child("##Family", new Vector2(200.0f, 150.0f), true))
                        {
                            if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddButton")}##AddFMButton"))
                            {
                                showFamilyPopup = true;
                            }
                            if (showFamilyPopup)
                            {
                                ImGui.BeginPopupModal("NouveauMembre", ref showFamilyPopup, ImGuiWindowFlags.AlwaysAutoResize);
                                ImGui.InputText($"{LocalizationManager.Instance.GetLocalizedString("MemberNameField")}##MemberName", ref newMemberName, 100);
                                ImGui.InputText($"{LocalizationManager.Instance.GetLocalizedString("MemberDescriptionField")}##MemberDesc", ref newMemberDescription, 500);
                                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddButton")}##SaveFMButton"))
                                {
                                    if (currentCharacter.characterFamily == null)
                                        currentCharacter.characterFamily = new Dictionary<string, string>();
                                    if (!currentCharacter.characterFamily.ContainsKey(newMemberName))
                                        currentCharacter.characterFamily.Add(newMemberName, newMemberDescription);
                                    showFriendsPopup = false;
                                }
                                ImGui.OpenPopup("NouveauMembre");
                                ImGui.EndPopup();
                            }

                            if (currentCharacter.characterFamily != null)
                            {
                                foreach (KeyValuePair<string, string> relation in currentCharacter.characterFamily)
                                {
                                    ImGui.Text($"{relation.Key} : ");
                                    ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                                    UiUtils.ManageInputField(ref CollectionsMarshal.GetValueRefOrNullRef(currentCharacter.characterFamily, relation.Key), $"FamilyRelation_{relation.Key}", editingCharsheet);
                                }
                            }
                        }
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        using (var friends = ImRaii.Child("##Friends", new Vector2(200.0f, 150.0f), true))
                        {
                            if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddButton")}##AddFriendButton"))
                            {
                                showFriendsPopup = true;
                            }
                            if (showFriendsPopup)
                            {
                                ImGui.BeginPopupModal("NouvelAmi", ref showFriendsPopup, ImGuiWindowFlags.AlwaysAutoResize);
                                ImGui.InputText($"{LocalizationManager.Instance.GetLocalizedString("FriendNameField")}##FriendName", ref newMemberName, 100);
                                ImGui.InputText($"{LocalizationManager.Instance.GetLocalizedString("FriendDescriptionField")}##FriendDesc", ref newMemberDescription, 500);
                                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddButton")}##SaveFriendButton"))
                                {
                                    if (currentCharacter.characterFriends == null)
                                        currentCharacter.characterFriends = new Dictionary<string, string>();
                                    if (!currentCharacter.characterFriends.ContainsKey(newMemberName))
                                        currentCharacter.characterFriends.Add(newMemberName, newMemberDescription);
                                    showFriendsPopup = false;
                                }
                                ImGui.OpenPopup("NouvelAmi");
                                ImGui.EndPopup();
                            }

                            if (currentCharacter.characterFriends != null)
                            {
                                foreach (KeyValuePair<string, string> relation in currentCharacter.characterFriends)
                                {
                                    ImGui.Text($"{relation.Key} : ");
                                    ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                                    UiUtils.ManageInputField(ref CollectionsMarshal.GetValueRefOrNullRef(currentCharacter.characterFriends, relation.Key), $"FamilyRelation_{relation.Key}", editingCharsheet);
                                }
                            }
                        }
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        using (var enemies = ImRaii.Child("##Enemies", new Vector2(200.0f, 150.0f), true))
                        {
                            if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddButton")}##AddEnemyButton"))
                            {
                                showEnemiesPopup = true;
                            }
                            if (showEnemiesPopup)
                            {
                                ImGui.BeginPopupModal("NouvelEnnemi", ref showEnemiesPopup, ImGuiWindowFlags.AlwaysAutoResize);
                                ImGui.InputText($"{LocalizationManager.Instance.GetLocalizedString("EnemyNameField")}##EnemyName", ref newMemberName, 100);
                                ImGui.InputText($"{LocalizationManager.Instance.GetLocalizedString("EnemyDescriptionField")}##EnemyDesc", ref newMemberDescription, 500);
                                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddButton")}##SaveEnemyButton"))
                                {
                                    if (currentCharacter.characterEnnemies == null)
                                        currentCharacter.characterEnnemies = new Dictionary<string, string>();
                                    if (!currentCharacter.characterEnnemies.ContainsKey(newMemberName))
                                        currentCharacter.characterEnnemies.Add(newMemberName, newMemberDescription);
                                    showEnemiesPopup = false;
                                }
                                ImGui.OpenPopup("NouvelEnnemi");
                                ImGui.EndPopup();
                            }

                            if (currentCharacter.characterEnnemies != null)
                            {
                                foreach (KeyValuePair<string, string> relation in currentCharacter.characterEnnemies)
                                {
                                    ImGui.Text($"{relation.Key} : ");
                                    ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                                    UiUtils.ManageInputField(ref CollectionsMarshal.GetValueRefOrNullRef(currentCharacter.characterEnnemies, relation.Key), $"FamilyRelation_{relation.Key}", editingCharsheet);
                                }
                            }
                        }

                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("CharBackgroundField")}");
                        ImGui.SameLine(0.0f, UiUtils.defaultNextToSpace);
                        UiUtils.ManageBigInputField(ref currentCharacter.characterBackground, "CharacterBackground", editingCharsheet);

                    }
                }
            }
        }
    }
}
