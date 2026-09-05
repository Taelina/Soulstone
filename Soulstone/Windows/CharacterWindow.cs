using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
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

namespace Soulstone.Windows
{
    internal class CharacterWindow
    {
        private string newCharname = "Nouveau personnage";

        private bool showFamilyPopup = false;
        private bool showFriendsPopup = false;
        private bool showEnemiesPopup = false;
        private bool showCreateCharPopup = false;

        private bool editingCharsheet = false;

        private string newMemberName = "";
        private string newMemberDescription = "";

        private CharacterSheet? currentCharacter = null;

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
            if (CharacterManager.Instance.CharacterSheet != null)
            {
                currentCharacter = CharacterManager.Instance.CharacterSheet;
            }

            DrawTopActionBar();
            ImGui.Spacing();

            if (currentCharacter == null)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("NoCharLoadedMessage"));
                return;
            }

            DrawHeroCard();
            ImGui.Spacing();
            DrawIdentitySection();
            ImGui.Spacing();
            DrawOocSection();
            ImGui.Spacing();
            DrawAppearanceSection();
            ImGui.Spacing();
            DrawQuickLookSection();
            ImGui.Spacing();
            DrawBackgroundSection();

            DrawModals();
        }

        private void DrawHeroCard()
        {
            if (currentCharacter == null) return;

            var portraitWidth = 130.0f * ImGuiHelpers.GlobalScale;
            var portraitHeight = 160.0f * ImGuiHelpers.GlobalScale;

            using (var card = ImRaii.Child("##HeroCard", new Vector2(0, 185.0f * ImGuiHelpers.GlobalScale), true, ImGuiWindowFlags.NoScrollbar))
            {
                if (!card.Success) return;

                // Draw portrait column on the left
                ImGui.BeginGroup();
                {
                    var placeholder = !string.IsNullOrWhiteSpace(currentCharacter.characterFullName) 
                        ? (currentCharacter.characterFullName.Length > 2 ? currentCharacter.characterFullName[..2].ToUpper() : currentCharacter.characterFullName.ToUpper()) 
                        : "RP";
                    ImageHelper.DrawThumbnailOrPlaceholder(currentCharacter.characterPictureUrl, new Vector2(portraitWidth, portraitHeight), placeholder, ImGuiColors.ParsedGold, 6.0f);
                }
                ImGui.EndGroup();

                ImGui.SameLine(0, 16.0f * ImGuiHelpers.GlobalScale);

                // Character Identity Summary on the right
                ImGui.BeginGroup();
                {
                    // Name header
                    var displayName = !string.IsNullOrWhiteSpace(currentCharacter.characterFullName) ? currentCharacter.characterFullName : LocalizationManager.Instance.GetLocalizedString("UnnamedCharacter");
                    ImGui.TextColored(ImGuiColors.ParsedGold, displayName);

                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterNickName))
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, $"\"{currentCharacter.characterNickName}\"");
                    }

                    ImGui.Spacing();

                    // Badges row
                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterJob))
                    {
                        UiUtils.Badge(currentCharacter.characterJob, new Vector4(0.2f, 0.4f, 0.7f, 0.4f), ImGuiColors.ParsedBlue);
                        ImGui.SameLine();
                    }
                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterRace))
                    {
                        var raceText = !string.IsNullOrWhiteSpace(currentCharacter.characterSubRace) ? $"{currentCharacter.characterRace} ({currentCharacter.characterSubRace})" : currentCharacter.characterRace;
                        UiUtils.Badge(raceText, new Vector4(0.4f, 0.3f, 0.6f, 0.4f), ImGuiColors.DalamudViolet);
                        ImGui.SameLine();
                    }
                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterGender) || !string.IsNullOrWhiteSpace(currentCharacter.characterPronouns))
                    {
                        var genderText = !string.IsNullOrWhiteSpace(currentCharacter.characterPronouns) ? $"{currentCharacter.characterGender} ({currentCharacter.characterPronouns})" : currentCharacter.characterGender;
                        UiUtils.Badge(genderText, new Vector4(0.25f, 0.45f, 0.35f, 0.4f), ImGuiColors.ParsedGreen);
                        ImGui.SameLine();
                    }
                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterAge))
                    {
                        UiUtils.Badge(string.Format(LocalizationManager.Instance.GetLocalizedString("AgeYearsFormat"), currentCharacter.characterAge), new Vector4(0.4f, 0.4f, 0.4f, 0.4f), ImGuiColors.DalamudWhite);
                    }

                    ImGui.NewLine();
                    ImGui.Spacing();

                    if (editingCharsheet)
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharPictureField"));
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 180.0f * ImGuiHelpers.GlobalScale);
                        ImGui.InputText("##HeroPicUrlInput", ref currentCharacter.characterPictureUrl, 500);

                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                        if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("CharPictureBrowse")}###BrowseHeroPic"))
                        {
                            plugin.OpenFilePicker(LocalizationManager.Instance.GetLocalizedString("ChooseCharPicPickerTitle"), ".png;.jpg;.jpeg;.bmp;.webp;.gif", (path) =>
                            {
                                var localCopy = ImageHelper.CopyImageToLocalFolder(path, "portraits");
                                currentCharacter.characterPictureUrl = localCopy;
                            });
                        }

                        ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                        if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("CharPictureClear")}###ClearHeroPic"))
                        {
                            currentCharacter.characterPictureUrl = string.Empty;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(currentCharacter.characterOccupation))
                        {
                            ImGui.TextColored(ImGuiColors.DalamudGrey, $"{LocalizationManager.Instance.GetLocalizedString("CharWorkField")} ");
                            ImGui.SameLine();
                            ImGui.TextUnformatted(currentCharacter.characterOccupation);
                        }
                        if (!string.IsNullOrWhiteSpace(currentCharacter.characterAffiliation))
                        {
                            ImGui.TextColored(ImGuiColors.DalamudGrey, $"{LocalizationManager.Instance.GetLocalizedString("CharAffiliationField")} ");
                            ImGui.SameLine();
                            ImGui.TextUnformatted(currentCharacter.characterAffiliation);
                        }
                        if (!string.IsNullOrWhiteSpace(currentCharacter.characterHomeland))
                        {
                            ImGui.TextColored(ImGuiColors.DalamudGrey, $"{LocalizationManager.Instance.GetLocalizedString("CharBirthplaceField")} ");
                            ImGui.SameLine();
                            ImGui.TextUnformatted(currentCharacter.characterHomeland);
                        }
                    }
                }
                ImGui.EndGroup();
            }
        }

        private void DrawTopActionBar()
        {
            // Left side: Edit mode toggle
            ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("EditCharsheetCheck")}###EditCheck", ref editingCharsheet);
            ImGui.SameLine();
            if (editingCharsheet)
            {
                UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("BadgeEditing"), new Vector4(0.8f, 0.4f, 0.1f, 0.4f), ImGuiColors.DalamudOrange);
            }
            else
            {
                UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("BadgeViewing"), new Vector4(0.2f, 0.5f, 0.8f, 0.3f), ImGuiColors.ParsedBlue);
            }

            ImGui.SameLine(0, 16.0f * ImGuiHelpers.GlobalScale);
            if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("NewCharButton")}###NewCharBtn"))
            {
                showCreateCharPopup = true;
                newCharname = LocalizationManager.Instance.GetLocalizedString("NewCharnameDefault");
            }

            if (currentCharacter != null)
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconButton("SaveCharBtn", FontAwesomeIcon.Save, LocalizationManager.Instance.GetLocalizedString("SaveCharsheetButton")))
                {
                    CharacterSheet.SaveSheet(currentCharacter);
                }
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconButton("ChooseSheetBtn", FontAwesomeIcon.FolderOpen, LocalizationManager.Instance.GetLocalizedString("CharsheetChoose")))
            {
                plugin.OpenFilePicker(LocalizationManager.Instance.GetLocalizedString("ChooseCharSheetPickerTitle"), ".json", (path) =>
                {
                    try
                    {
                        Plugin.Log?.Information($"Selected file: {path}");
                        CharacterSheet? loadedSheet = CharacterSheet.LoadSheet(path, true);
                        if (loadedSheet != null)
                        {
                            CharacterManager.Instance.CharacterSheet = loadedSheet;
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Error(ex, $"Failed to load character sheet from '{path}' in file picker callback");
                    }
                });
            }
        }

        private void DrawIdentitySection()
        {
            if (currentCharacter == null) return;

            var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("CharFullnameField").Replace(":", "").Trim()}###IdentitySection", flags))
            {
                using var table = ImRaii.Table("##IdentityTable", 4, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
                if (table.Success)
                {
                    ImGui.TableSetupColumn("Label1", ImGuiTableColumnFlags.WidthFixed, 100.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Value1", ImGuiTableColumnFlags.WidthStretch, 1.0f);
                    ImGui.TableSetupColumn("Label2", ImGuiTableColumnFlags.WidthFixed, 100.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Value2", ImGuiTableColumnFlags.WidthStretch, 1.0f);

                    // Row 1: Full name & Nickname
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharFullnameField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterFullName, "FullName", editingCharsheet, -1f);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharNicknameField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterNickName, "NickName", editingCharsheet, -1f);

                    // Row 2: Specie & Sub-specie
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharSpecieField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterRace, "CharacterRace", editingCharsheet, -1f);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharSubSpecieField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterSubRace, "CharacterSubRace", editingCharsheet, -1f);

                    // Row 3: Class & Age
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharClassField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterJob, "CharacterJob", editingCharsheet, -1f);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharAgeField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterAge, "CharacterAge", editingCharsheet, -1f);

                    // Row 4: Sex & Gender
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharSexField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterSex, "CharacterSex", editingCharsheet, -1f);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharGenderField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterGender, "CharacterGender", editingCharsheet, -1f);

                    // Row 5: Pronouns
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharPronounsField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterPronouns, "CharacterPronouns", editingCharsheet, -1f);
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                }
            }
        }

        private void DrawOocSection()
        {
            if (currentCharacter == null) return;

            var flags = ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("PlayerOOCInfo").Replace(":", "").Trim()}###OOCSection", flags))
            {
                using var table = ImRaii.Table("##OOCTable", 4, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
                if (table.Success)
                {
                    ImGui.TableSetupColumn("Label1", ImGuiTableColumnFlags.WidthFixed, 100.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Value1", ImGuiTableColumnFlags.WidthStretch, 1.0f);
                    ImGui.TableSetupColumn("Label2", ImGuiTableColumnFlags.WidthFixed, 100.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Value2", ImGuiTableColumnFlags.WidthStretch, 1.0f);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("PlayerTimezone"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.playerTimezone, "PlayerTimezone", editingCharsheet, -1f);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("PlayerAvailability"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.playerAvailability, "PlayerAvailability", editingCharsheet, -1f);
                }

                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("PlayerOOCInfo"));
                UiUtils.ManageBigInputField(ref currentCharacter.characterInfo, "CharacterHrpInfo", editingCharsheet, 60.0f);
            }
        }

        private void DrawAppearanceSection()
        {
            if (currentCharacter == null) return;

            var flags = ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("CharEyeColorField").Replace(":", "").Trim()} / {LocalizationManager.Instance.GetLocalizedString("CharBuildField").Replace(":", "").Trim()}###AppearanceSection", flags))
            {
                using var table = ImRaii.Table("##AppearanceTable", 4, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
                if (table.Success)
                {
                    ImGui.TableSetupColumn("Label1", ImGuiTableColumnFlags.WidthFixed, 120.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Value1", ImGuiTableColumnFlags.WidthStretch, 1.0f);
                    ImGui.TableSetupColumn("Label2", ImGuiTableColumnFlags.WidthFixed, 120.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Value2", ImGuiTableColumnFlags.WidthStretch, 1.0f);

                    // Row 1: Height & Weight
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharHeightField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterHeight, "CharacterHeight", editingCharsheet, -1f);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharWeightField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterWeight, "CharacterWeight", editingCharsheet, -1f);

                    // Row 2: Build & Skin
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharBuildField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterBuild, "CharacterBuild", editingCharsheet, -1f);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharSkinColorField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterSkinTone, "CharacterSkinTone", editingCharsheet, -1f);

                    // Row 3: Eyes & Hair
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharEyeColorField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterEyeColor, "CharacterEyeColor", editingCharsheet, -1f);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharHairColorField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterHairColor, "CharacterHairColor", editingCharsheet, -1f);

                    // Row 4: Scars & Tattoos
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharScarsField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterScars, "CharacterScars", editingCharsheet, -1f);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharTatooField"));
                    ImGui.TableNextColumn();
                    UiUtils.ManageInputField(ref currentCharacter.characterTattoos, "CharacterTattoos", editingCharsheet, -1f);
                }

                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharOtherQuirkField"));
                UiUtils.ManageBigInputField(ref currentCharacter.characterDistinctiveFeatures, "CharacterDistinctiveFeatures", editingCharsheet, 50.0f);
            }
        }

        private void DrawQuickLookSection()
        {
            if (currentCharacter == null) return;

            var flags = ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("QuickLookField1").Replace(":", "").Trim()}###QuickLookSection", flags))
            {
                using var table = ImRaii.Table("##QuickLookTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
                if (table.Success)
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 120.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch, 1.0f);

                    string[] qlLabels = {
                        LocalizationManager.Instance.GetLocalizedString("QuickLookField1"),
                        LocalizationManager.Instance.GetLocalizedString("QuickLookField2"),
                        LocalizationManager.Instance.GetLocalizedString("QuickLookField3"),
                        LocalizationManager.Instance.GetLocalizedString("QuickLookField4"),
                        LocalizationManager.Instance.GetLocalizedString("QuickLookField5"),
                    };

                    DrawQuickLookRow(qlLabels[0], ref currentCharacter.characterQuickLook1, "CharacterQuickLook1");
                    DrawQuickLookRow(qlLabels[1], ref currentCharacter.characterQuickLook2, "CharacterQuickLook2");
                    DrawQuickLookRow(qlLabels[2], ref currentCharacter.characterQuickLook3, "CharacterQuickLook3");
                    DrawQuickLookRow(qlLabels[3], ref currentCharacter.characterQuickLook4, "CharacterQuickLook4");
                    DrawQuickLookRow(qlLabels[4], ref currentCharacter.characterQuickLook5, "CharacterQuickLook5");
                }
            }
        }

        private void DrawQuickLookRow(string label, ref string field, string fieldName)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextColored(ImGuiColors.DalamudGrey, label);
            ImGui.TableNextColumn();
            UiUtils.ManageInputField(ref field, fieldName, editingCharsheet, -1f);
        }

        private void DrawBackgroundSection()
        {
            if (currentCharacter == null) return;

            var flags = ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("CharBackgroundField").Replace(":", "").Trim()} & {LocalizationManager.Instance.GetLocalizedString("CharFamilyRelationTab").Replace(":", "").Trim()}###BackgroundSection", flags))
            {
                using (var table = ImRaii.Table("##BgMetaTable", 4, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
                {
                    if (table.Success)
                    {
                        ImGui.TableSetupColumn("Label1", ImGuiTableColumnFlags.WidthFixed, 120.0f * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("Value1", ImGuiTableColumnFlags.WidthStretch, 1.0f);
                        ImGui.TableSetupColumn("Label2", ImGuiTableColumnFlags.WidthFixed, 120.0f * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("Value2", ImGuiTableColumnFlags.WidthStretch, 1.0f);

                        // Row 1: Birthplace & Origin
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharBirthplaceField"));
                        ImGui.TableNextColumn();
                        UiUtils.ManageInputField(ref currentCharacter.characterHomeland, "CharacterHomeland", editingCharsheet, -1f);
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharOriginField"));
                        ImGui.TableNextColumn();
                        UiUtils.ManageInputField(ref currentCharacter.characterOrigin, "CharacterOrigin", editingCharsheet, -1f);

                        // Row 2: Affiliation & Occupation
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharAffiliationField"));
                        ImGui.TableNextColumn();
                        UiUtils.ManageInputField(ref currentCharacter.characterAffiliation, "CharacterAffiliation", editingCharsheet, -1f);
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharWorkField"));
                        ImGui.TableNextColumn();
                        UiUtils.ManageInputField(ref currentCharacter.characterOccupation, "CharacterOccupation", editingCharsheet, -1f);
                    }
                }

                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharReputationField"));
                UiUtils.ManageBigInputField(ref currentCharacter.characterReputation, "CharacterReputation", editingCharsheet, 50.0f);

                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharBackgroundField"));
                UiUtils.ManageBigInputField(ref currentCharacter.characterBackground, "CharacterBackground", editingCharsheet, 90.0f);

                ImGui.Spacing();
                DrawRelationsColumns();
            }
        }

        private void DrawRelationsColumns()
        {
            if (currentCharacter == null) return;

            var boxHeight = 160.0f * ImGuiHelpers.GlobalScale;

            using (var table = ImRaii.Table("##RelationsColumnsTable", 3, ImGuiTableFlags.SizingStretchSame))
            {
                if (table.Success)
                {
                    ImGui.TableNextColumn();
                    DrawRelationCard("Family", ImGuiColors.ParsedGold,
                        LocalizationManager.Instance.GetLocalizedString("CharFamilyRelationTab"),
                        currentCharacter.characterFamily ??= new Dictionary<string, string>(),
                        () => showFamilyPopup = true, boxHeight);

                    ImGui.TableNextColumn();
                    DrawRelationCard("Friends", ImGuiColors.ParsedGreen,
                        LocalizationManager.Instance.GetLocalizedString("CharFriendsTab"),
                        currentCharacter.characterFriends ??= new Dictionary<string, string>(),
                        () => showFriendsPopup = true, boxHeight);

                    ImGui.TableNextColumn();
                    DrawRelationCard("Enemies", ImGuiColors.DPSRed,
                        LocalizationManager.Instance.GetLocalizedString("CharEnemiesTab"),
                        currentCharacter.characterEnnemies ??= new Dictionary<string, string>(),
                        () => showEnemiesPopup = true, boxHeight);
                }
            }
        }

        private void DrawRelationCard(string id, Vector4 color, string title, Dictionary<string, string> relations, Action onAddClick, float height)
        {
            using (var child = ImRaii.Child($"##{id}Card", new Vector2(0, height), true))
            {
                if (child.Success)
                {
                    ImGui.TextColored(color, title.Replace(":", "").Trim());
                    ImGui.SameLine();
                    UiUtils.Badge(relations.Count.ToString(), new Vector4(0.2f, 0.2f, 0.2f, 0.5f), ImGuiColors.DalamudGrey);

                    var addBtnWidth = 24.0f * ImGuiHelpers.GlobalScale;
                    var rightX = ImGui.GetWindowContentRegionMax().X - addBtnWidth;
                    if (ImGui.GetCursorPosX() < rightX)
                        ImGui.SameLine(rightX);
                    else
                        ImGui.SameLine();

                    if (ImGui.Button($"+##Add_{id}", new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                    {
                        newMemberName = "";
                        newMemberDescription = "";
                        onAddClick();
                    }
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("AddButton"));

                    ImGui.Separator();

                    if (relations.Count == 0)
                    {
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoneText"));
                    }
                    else
                    {
                        string? keyToRemove = null;
                        using (var relTable = ImRaii.Table($"##{id}RelTable", editingCharsheet ? 3 : 2, ImGuiTableFlags.SizingStretchProp))
                        {
                            if (relTable.Success)
                            {
                                if (editingCharsheet)
                                {
                                    ImGui.TableSetupColumn("Del", ImGuiTableColumnFlags.WidthFixed, 22.0f * ImGuiHelpers.GlobalScale);
                                }
                                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 75.0f * ImGuiHelpers.GlobalScale);
                                ImGui.TableSetupColumn("Desc", ImGuiTableColumnFlags.WidthStretch, 1.0f);

                                foreach (var kvp in relations.ToList())
                                {
                                    ImGui.TableNextRow();
                                    ImGui.PushID($"{id}_{kvp.Key}");
                                    if (editingCharsheet)
                                    {
                                        ImGui.TableNextColumn();
                                        if (ImGui.Button($"x##Del_{kvp.Key}", new Vector2(18, 18) * ImGuiHelpers.GlobalScale))
                                        {
                                            keyToRemove = kvp.Key;
                                        }
                                        if (ImGui.IsItemHovered()) ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"));
                                    }

                                    ImGui.TableNextColumn();
                                    ImGui.TextColored(color, kvp.Key);

                                    ImGui.TableNextColumn();
                                    var desc = kvp.Value;
                                    if (editingCharsheet)
                                    {
                                        ImGui.SetNextItemWidth(-1f);
                                        if (ImGui.InputText("##desc", ref desc, 300))
                                        {
                                            relations[kvp.Key] = desc;
                                        }
                                    }
                                    else
                                    {
                                        ImGui.TextWrapped(desc);
                                    }
                                    ImGui.PopID();
                                }
                            }
                        }

                        if (keyToRemove != null)
                        {
                            relations.Remove(keyToRemove);
                        }
                    }
                }
            }
        }

        private void DrawModals()
        {
            if (currentCharacter == null) return;

            // Create character popup
            if (showCreateCharPopup)
            {
                ImGui.OpenPopup("CreateCharacterModal");
            }
            if (ImGui.BeginPopupModal("CreateCharacterModal", ref showCreateCharPopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("NewCharButton"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewCharnameField"));
                ImGui.InputText("##NewCharNameInput", ref newCharname, 100);

                ImGui.Spacing();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), new Vector2(100, 0) * ImGuiHelpers.GlobalScale))
                {
                    if (!string.IsNullOrWhiteSpace(newCharname))
                    {
                        CharacterSheet.CreateNewSheet(newCharname);
                        showCreateCharPopup = false;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("CancelButton"), new Vector2(80, 0) * ImGuiHelpers.GlobalScale))
                {
                    showCreateCharPopup = false;
                }

                ImGui.EndPopup();
            }

            // Family popup
            if (showFamilyPopup)
            {
                ImGui.OpenPopup("NewFamilyMemberModal");
            }
            if (ImGui.BeginPopupModal("NewFamilyMemberModal", ref showFamilyPopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("CharFamilyRelationTab"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("MemberNameField"));
                ImGui.InputText("##FMName", ref newMemberName, 100);
                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("MemberDescriptionField"));
                ImGui.InputText("##FMDesc", ref newMemberDescription, 500);

                ImGui.Spacing();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), new Vector2(100, 0) * ImGuiHelpers.GlobalScale))
                {
                    if (!string.IsNullOrWhiteSpace(newMemberName))
                    {
                        currentCharacter.characterFamily ??= new Dictionary<string, string>();
                        currentCharacter.characterFamily[newMemberName] = newMemberDescription;
                        showFamilyPopup = false;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("CancelButton"), new Vector2(80, 0) * ImGuiHelpers.GlobalScale))
                {
                    showFamilyPopup = false;
                }

                ImGui.EndPopup();
            }

            // Friends popup
            if (showFriendsPopup)
            {
                ImGui.OpenPopup("NewFriendModal");
            }
            if (ImGui.BeginPopupModal("NewFriendModal", ref showFriendsPopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("CharFriendsTab"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("FriendNameField"));
                ImGui.InputText("##FriendName", ref newMemberName, 100);
                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("FriendDescriptionField"));
                ImGui.InputText("##FriendDesc", ref newMemberDescription, 500);

                ImGui.Spacing();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), new Vector2(100, 0) * ImGuiHelpers.GlobalScale))
                {
                    if (!string.IsNullOrWhiteSpace(newMemberName))
                    {
                        currentCharacter.characterFriends ??= new Dictionary<string, string>();
                        currentCharacter.characterFriends[newMemberName] = newMemberDescription;
                        showFriendsPopup = false;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("CancelButton"), new Vector2(80, 0) * ImGuiHelpers.GlobalScale))
                {
                    showFriendsPopup = false;
                }

                ImGui.EndPopup();
            }

            // Enemies popup
            if (showEnemiesPopup)
            {
                ImGui.OpenPopup("NewEnemyModal");
            }
            if (ImGui.BeginPopupModal("NewEnemyModal", ref showEnemiesPopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextColored(ImGuiColors.DPSRed, LocalizationManager.Instance.GetLocalizedString("CharEnemiesTab"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("EnemyNameField"));
                ImGui.InputText("##EnemyName", ref newMemberName, 100);
                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("EnemyDescriptionField"));
                ImGui.InputText("##EnemyDesc", ref newMemberDescription, 500);

                ImGui.Spacing();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), new Vector2(100, 0) * ImGuiHelpers.GlobalScale))
                {
                    if (!string.IsNullOrWhiteSpace(newMemberName))
                    {
                        currentCharacter.characterEnnemies ??= new Dictionary<string, string>();
                        currentCharacter.characterEnnemies[newMemberName] = newMemberDescription;
                        showEnemiesPopup = false;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("CancelButton"), new Vector2(80, 0) * ImGuiHelpers.GlobalScale))
                {
                    showEnemiesPopup = false;
                }

                ImGui.EndPopup();
            }
        }
    }
}
