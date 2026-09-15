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
                using (var emptyCard = ImRaii.Child("##NoCharCard", new Vector2(0, 120.0f * ImGuiHelpers.GlobalScale), true))
                {
                    if (emptyCard.Success)
                    {
                        ImGui.Spacing();
                        ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("NoCharLoadedMessage"));
                        ImGui.Spacing();
                        if (UiUtils.IconButton("CreateFirstCharBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("NewCharButton")))
                        {
                            showCreateCharPopup = true;
                            newCharname = LocalizationManager.Instance.GetLocalizedString("NewCharnameDefault");
                        }
                        ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                        if (UiUtils.IconButton("LoadFirstCharBtn", FontAwesomeIcon.FolderOpen, LocalizationManager.Instance.GetLocalizedString("CharsheetChoose")))
                        {
                            OpenSheetPicker();
                        }
                    }
                }
                DrawModals();
                return;
            }

            DrawHeroCard();
            ImGui.Spacing();
            DrawResourcesCollapsibleSection();
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

        private void DrawResourcesCollapsibleSection()
        {
            if (currentCharacter == null) return;
            var currentDiceSys = DiceSystemManager.Instance.CurrentDiceSystem;
            var resources = currentCharacter.GetEffectiveResources(currentDiceSys);
            if (resources.Count == 0) return;

            var title = LocalizationManager.Instance.GetLocalizedString("ResourcesSectionTitle");
            if (string.IsNullOrEmpty(title) || title == "ResourcesSectionTitle")
                title = LocalizationManager.Instance.GetLocalizedString("DiceSysResourcesHeader");

            if (UiUtils.StyledCollapsingHeader(title.Replace(":", "").Trim(), defaultOpen: true, icon: FontAwesomeIcon.Heartbeat, accentColor: ImGuiColors.ParsedGreen))
            {
                var scale = ImGuiHelpers.GlobalScale;
                foreach (var res in resources)
                {
                    var def = currentDiceSys?.SystemResources.FirstOrDefault(d => string.Equals(d.Name, res.Name, StringComparison.OrdinalIgnoreCase));
                    var resCol = GetResourceColor(res.Name, def?.ColorHex);
                    int effectiveMax = currentCharacter.GetEffectiveResourceMax(res.Name, currentDiceSys);
                    int gearBonus = currentCharacter.GetGearStatBonus(res.Name) + currentCharacter.GetGearStatBonus($"Max {res.Name}") + currentCharacter.GetGearStatBonus($"Max{res.Name}");

                    using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.11f, 0.12f, 0.15f, 0.90f)))
                    using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(resCol.X, resCol.Y, resCol.Z, 0.45f)))
                    using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * scale))
                    using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 6.0f) * scale))
                    using (var card = ImRaii.Child($"##CharRes_{res.Name}", new Vector2(0, 42.0f * scale), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                    {
                        if (card.Success)
                        {
                            var drawList = ImGui.GetWindowDrawList();
                            var cardPos = ImGui.GetWindowPos();
                            var cardSize = ImGui.GetWindowSize();

                            drawList.AddRectFilled(
                                cardPos + new Vector2(2.0f * scale, 4.0f * scale),
                                cardPos + new Vector2(5.0f * scale, cardSize.Y - 4.0f * scale),
                                ImGui.ColorConvertFloat4ToU32(resCol),
                                1.5f * scale);

                            ImGui.AlignTextToFramePadding();
                            ImGui.TextColored(resCol, res.Name);
                            ImGui.SameLine(0, 12.0f * scale);

                            if (res.ResourceType == ResourceType.FlatNumber)
                            {
                                string valText = $"{effectiveMax}{(gearBonus != 0 ? $" (+{gearBonus})" : "")}";
                                UiUtils.PillBadge(valText, new Vector4(0.24f, 0.20f, 0.12f, 0.85f), ImGuiColors.ParsedGold);

                                if (res.IsRollable)
                                {
                                    ImGui.SameLine(0, 8.0f * scale);
                                    if (UiUtils.IconButton($"RollRes_{res.Name}", FontAwesomeIcon.DiceD20, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")} {res.Name}", new Vector2(24, 22) * scale))
                                    {
                                        currentCharacter.RollResource(res.Name, currentDiceSys, false, false, configuration.detailedRolls);
                                    }
                                }
                            }
                            else if (res.ResourceType == ResourceType.Counter)
                            {
                                float btnSize = 22.0f * scale;
                                if (UiUtils.IconButton($"ResDec_{res.Name}", FontAwesomeIcon.Minus, "-", new Vector2(btnSize, btnSize)))
                                {
                                    if (res.CurrentValue > 0)
                                    {
                                        currentCharacter.SetResourceCurrent(res.Name, res.CurrentValue - 1);
                                    }
                                }

                                ImGui.SameLine(0, 6.0f * scale);
                                float barW = Math.Max(80.0f * scale, ImGui.GetContentRegionAvail().X - btnSize - 8.0f * scale);
                                string overlay = effectiveMax > 0
                                    ? $"{res.CurrentValue} / {effectiveMax}{(gearBonus != 0 ? $" (+{gearBonus})" : "")}"
                                    : $"{res.CurrentValue}";

                                UiUtils.DrawSectionedBar(
                                    res.CurrentValue,
                                    effectiveMax > 0 ? effectiveMax : 1,
                                    overlay,
                                    new Vector2(barW, 20.0f * scale),
                                    activeColor: resCol,
                                    onSegmentClick: newVal => currentCharacter.SetResourceCurrent(res.Name, newVal));

                                ImGui.SameLine(0, 6.0f * scale);
                                if (UiUtils.IconButton($"ResInc_{res.Name}", FontAwesomeIcon.Plus, "+", new Vector2(btnSize, btnSize)))
                                {
                                    if (res.CurrentValue < effectiveMax)
                                    {
                                        currentCharacter.SetResourceCurrent(res.Name, res.CurrentValue + 1);
                                    }
                                }
                            }
                            else // Bar
                            {
                                string overlay = effectiveMax > 0
                                    ? $"{res.CurrentValue} / {effectiveMax}{(gearBonus != 0 ? $" (+{gearBonus})" : "")}"
                                    : $"{res.CurrentValue}";
                                UiUtils.DrawProgressBar(res.CurrentValue, effectiveMax > 0 ? effectiveMax : 1, overlay, new Vector2(-1, 20.0f * scale), resCol);
                            }
                        }
                    }
                    ImGui.Spacing();
                }
            }
        }

        private static Vector4 GetResourceColor(string name, string? colorHex = null)
        {
            if (!string.IsNullOrWhiteSpace(colorHex) && colorHex.StartsWith("#") && colorHex.Length >= 7)
            {
                try
                {
                    byte r = Convert.ToByte(colorHex.Substring(1, 2), 16);
                    byte g = Convert.ToByte(colorHex.Substring(3, 2), 16);
                    byte b = Convert.ToByte(colorHex.Substring(5, 2), 16);
                    return new Vector4(r / 255f, g / 255f, b / 255f, 0.85f);
                }
                catch { }
            }

            return name.ToLowerInvariant() switch
            {
                "health" or "hp" or "vie" or "santé" => new Vector4(0.2f, 0.7f, 0.3f, 0.85f),
                "mana" or "mp" => new Vector4(0.2f, 0.45f, 0.85f, 0.85f),
                "stamina" or "endurance" or "energy" => new Vector4(0.85f, 0.60f, 0.15f, 0.85f),
                "rage" => new Vector4(0.85f, 0.20f, 0.20f, 0.85f),
                "focus" or "sanity" => new Vector4(0.60f, 0.25f, 0.85f, 0.85f),
                _ => new Vector4(0.25f, 0.65f, 0.65f, 0.85f)
            };
        }

        private void DrawPropertyCard(string label, string? value, FontAwesomeIcon icon, Vector4 accentColor, float width = -1f)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var cardWidth = width > 0 ? width : ImGui.GetContentRegionAvail().X;
            var cardHeight = 44.0f * scale;

            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.11f, 0.12f, 0.15f, 0.90f)))
            using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(0.22f, 0.25f, 0.32f, 0.65f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 6.0f) * scale))
            using (var child = ImRaii.Child($"##PropCard_{label}", new Vector2(cardWidth, cardHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (child.Success)
                {
                    var drawList = ImGui.GetWindowDrawList();
                    var pos = ImGui.GetWindowPos();
                    var size = ImGui.GetWindowSize();

                    // Left mini accent line
                    drawList.AddRectFilled(
                        pos + new Vector2(2.0f * scale, 4.0f * scale),
                        pos + new Vector2(4.5f * scale, size.Y - 4.0f * scale),
                        ImGui.ColorConvertFloat4ToU32(accentColor),
                        1.5f * scale);

                    // Icon + Label on top
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(accentColor, icon.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * scale);
                    ImGui.TextColored(ImGuiColors.DalamudGrey, label.Replace(":", "").Trim());

                    // Value line
                    var displayVal = !string.IsNullOrWhiteSpace(value) ? value : "-";
                    var valCol = !string.IsNullOrWhiteSpace(value) ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey2;
                    ImGui.TextColored(valCol, displayVal);
                }
            }
        }

        private void DrawStoryBlock(string title, string? content, FontAwesomeIcon icon, Vector4 accentColor)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var availWidth = ImGui.GetContentRegionAvail().X;

            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.11f, 0.14f, 0.90f)))
            using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(accentColor.X, accentColor.Y, accentColor.Z, 0.45f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(12.0f, 8.0f) * scale))
            using (var child = ImRaii.Child($"##StoryBlock_{title}", new Vector2(availWidth, 0), true, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (child.Success)
                {
                    var drawList = ImGui.GetWindowDrawList();
                    var pos = ImGui.GetWindowPos();
                    var size = ImGui.GetWindowSize();

                    // Left vertical accent
                    drawList.AddRectFilled(
                        pos + new Vector2(2.5f * scale, 5.0f * scale),
                        pos + new Vector2(5.5f * scale, size.Y - 5.0f * scale),
                        ImGui.ColorConvertFloat4ToU32(accentColor),
                        1.5f * scale);

                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(accentColor, icon.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * scale);
                    ImGui.TextColored(accentColor, title.Replace(":", "").Trim());
                    ImGui.Separator();
                    ImGui.Spacing();

                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        ImGui.TextWrapped(content);
                    }
                    else
                    {
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoneText"));
                    }
                }
            }
        }

        private void DrawTopActionBar()
        {
            var scale = ImGuiHelpers.GlobalScale;
            var pos = ImGui.GetCursorScreenPos();
            var availWidth = ImGui.GetContentRegionAvail().X;
            var barHeight = 44.0f * scale;
            var drawList = ImGui.GetWindowDrawList();

            // Background card
            var bgCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.12f, 0.15f, 0.95f));
            var borderCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.24f, 0.28f, 0.35f, 0.75f));
            drawList.AddRectFilled(pos, pos + new Vector2(availWidth, barHeight), bgCol, 6.0f * scale);
            drawList.AddRect(pos, pos + new Vector2(availWidth, barHeight), borderCol, 6.0f * scale, ImDrawFlags.None, 1.2f);

            ImGui.SetCursorScreenPos(pos + new Vector2(10.0f * scale, 8.0f * scale));

            ImGui.BeginGroup();
            {
                // Edit mode toggle
                ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("EditCharsheetCheck")}###EditCheck", ref editingCharsheet);
                ImGui.SameLine(0, 8.0f * scale);

                if (editingCharsheet)
                {
                    UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("BadgeEditing"), new Vector4(0.55f, 0.28f, 0.10f, 0.9f), ImGuiColors.DalamudOrange, FontAwesomeIcon.Edit);
                }
                else
                {
                    UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("BadgeViewing"), new Vector4(0.18f, 0.32f, 0.50f, 0.85f), ImGuiColors.ParsedBlue, FontAwesomeIcon.Eye);
                }

                ImGui.SameLine(0, 16.0f * scale);
                if (UiUtils.IconButton("NewCharBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("NewCharButton")))
                {
                    showCreateCharPopup = true;
                    newCharname = LocalizationManager.Instance.GetLocalizedString("NewCharnameDefault");
                }

                if (currentCharacter != null)
                {
                    ImGui.SameLine(0, 6.0f * scale);
                    if (UiUtils.IconButton("SaveCharBtn", FontAwesomeIcon.Save, LocalizationManager.Instance.GetLocalizedString("SaveCharsheetButton")))
                    {
                        CharacterSheet.SaveSheet(currentCharacter);
                    }
                }

                ImGui.SameLine(0, 6.0f * scale);
                if (UiUtils.IconButton("ChooseSheetBtn", FontAwesomeIcon.FolderOpen, LocalizationManager.Instance.GetLocalizedString("CharsheetChoose")))
                {
                    OpenSheetPicker();
                }
            }
            ImGui.EndGroup();

            ImGui.SetCursorScreenPos(pos);
            ImGui.Dummy(new Vector2(availWidth, barHeight));
        }

        private void OpenSheetPicker()
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

        private void DrawHeroCard()
        {
            if (currentCharacter == null) return;

            var scale = ImGuiHelpers.GlobalScale;
            var portraitWidth = 135.0f * scale;
            var portraitHeight = 165.0f * scale;

            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.11f, 0.14f, 0.95f)))
            using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(0.80f, 0.65f, 0.25f, 0.85f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 8.0f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(14.0f, 12.0f) * scale))
            using (var card = ImRaii.Child("##HeroCard", new Vector2(0, 190.0f * scale), true, ImGuiWindowFlags.NoScrollbar))
            {
                if (!card.Success) return;

                var drawList = ImGui.GetWindowDrawList();
                var cardPos = ImGui.GetWindowPos();
                var cardSize = ImGui.GetWindowSize();

                // Left gold accent stripe
                drawList.AddRectFilled(
                    cardPos + new Vector2(2.5f * scale, 6.0f * scale),
                    cardPos + new Vector2(6.0f * scale, cardSize.Y - 6.0f * scale),
                    ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGold),
                    2.0f * scale);

                // Portrait column
                ImGui.BeginGroup();
                {
                    var placeholder = !string.IsNullOrWhiteSpace(currentCharacter.characterFullName)
                        ? (currentCharacter.characterFullName.Length > 2 ? currentCharacter.characterFullName[..2].ToUpper() : currentCharacter.characterFullName.ToUpper())
                        : "RP";
                    ImageHelper.DrawThumbnailOrPlaceholder(currentCharacter.characterPictureUrl, new Vector2(portraitWidth, portraitHeight), placeholder, ImGuiColors.ParsedGold, 6.0f);
                }
                ImGui.EndGroup();

                ImGui.SameLine(0, 18.0f * scale);

                // Character Details Column
                ImGui.BeginGroup();
                {
                    var displayName = !string.IsNullOrWhiteSpace(currentCharacter.characterFullName) ? currentCharacter.characterFullName : LocalizationManager.Instance.GetLocalizedString("UnnamedCharacter");
                    ImGui.TextColored(ImGuiColors.ParsedGold, displayName);

                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterNickName))
                    {
                        ImGui.SameLine(0, 8.0f * scale);
                        ImGui.TextColored(ImGuiColors.DalamudGrey, $"\"{currentCharacter.characterNickName}\"");
                    }

                    ImGui.Spacing();

                    // Badges row
                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterJob))
                    {
                        UiUtils.PillBadge(currentCharacter.characterJob, new Vector4(0.20f, 0.35f, 0.60f, 0.85f), ImGuiColors.ParsedBlue, FontAwesomeIcon.UserShield);
                        ImGui.SameLine(0, 6.0f * scale);
                    }
                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterRace))
                    {
                        var raceText = !string.IsNullOrWhiteSpace(currentCharacter.characterSubRace) ? $"{currentCharacter.characterRace} ({currentCharacter.characterSubRace})" : currentCharacter.characterRace;
                        UiUtils.PillBadge(raceText, new Vector4(0.35f, 0.20f, 0.50f, 0.85f), ImGuiColors.DalamudViolet, FontAwesomeIcon.Dna);
                        ImGui.SameLine(0, 6.0f * scale);
                    }
                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterGender) || !string.IsNullOrWhiteSpace(currentCharacter.characterPronouns))
                    {
                        var genderText = !string.IsNullOrWhiteSpace(currentCharacter.characterPronouns) ? $"{currentCharacter.characterGender} ({currentCharacter.characterPronouns})" : currentCharacter.characterGender;
                        UiUtils.PillBadge(genderText, new Vector4(0.18f, 0.40f, 0.28f, 0.85f), ImGuiColors.ParsedGreen, FontAwesomeIcon.VenusMars);
                        ImGui.SameLine(0, 6.0f * scale);
                    }
                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterAge))
                    {
                        UiUtils.PillBadge(string.Format(LocalizationManager.Instance.GetLocalizedString("AgeYearsFormat"), currentCharacter.characterAge), new Vector4(0.28f, 0.28f, 0.35f, 0.85f), ImGuiColors.DalamudWhite, FontAwesomeIcon.HourglassHalf);
                    }

                    ImGui.NewLine();
                    ImGui.Spacing();

                    if (editingCharsheet)
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharPictureField"));
                        float picInputWidth = Math.Max(120.0f * scale, ImGui.GetContentRegionAvail().X - 190.0f * scale);
                        UiUtils.StyledInputText("HeroPicUrlInput", ref currentCharacter.characterPictureUrl, 500, width: picInputWidth);

                        ImGui.SameLine(0, 6.0f * scale);
                        if (UiUtils.IconTextButton("BrowseHeroPic", FontAwesomeIcon.FolderOpen, LocalizationManager.Instance.GetLocalizedString("CharPictureBrowse")))
                        {
                            plugin.OpenFilePicker(LocalizationManager.Instance.GetLocalizedString("ChooseCharPicPickerTitle"), ".png;.jpg;.jpeg;.bmp;.webp;.gif", (path) =>
                            {
                                var localCopy = ImageHelper.CopyImageToLocalFolder(path, "portraits");
                                currentCharacter.characterPictureUrl = localCopy;
                            });
                        }

                        ImGui.SameLine(0, 4.0f * scale);
                        if (UiUtils.IconTextButton("ClearHeroPic", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CharPictureClear")))
                        {
                            currentCharacter.characterPictureUrl = string.Empty;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(currentCharacter.characterOccupation))
                        {
                            ImGui.TextColored(ImGuiColors.DalamudGrey, $"{LocalizationManager.Instance.GetLocalizedString("CharWorkField")} ");
                            ImGui.SameLine(0, 4.0f * scale);
                            ImGui.TextUnformatted(currentCharacter.characterOccupation);
                        }
                        if (!string.IsNullOrWhiteSpace(currentCharacter.characterAffiliation))
                        {
                            ImGui.TextColored(ImGuiColors.DalamudGrey, $"{LocalizationManager.Instance.GetLocalizedString("CharAffiliationField")} ");
                            ImGui.SameLine(0, 4.0f * scale);
                            ImGui.TextUnformatted(currentCharacter.characterAffiliation);
                        }
                        if (!string.IsNullOrWhiteSpace(currentCharacter.characterHomeland))
                        {
                            ImGui.TextColored(ImGuiColors.DalamudGrey, $"{LocalizationManager.Instance.GetLocalizedString("CharBirthplaceField")} ");
                            ImGui.SameLine(0, 4.0f * scale);
                            ImGui.TextUnformatted(currentCharacter.characterHomeland);
                        }
                    }
                }
                ImGui.EndGroup();
            }
        }

        private void DrawIdentitySection()
        {
            if (currentCharacter == null) return;

            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("CharFullnameField").Replace(":", "").Trim(), defaultOpen: true, icon: FontAwesomeIcon.IdCard, accentColor: ImGuiColors.ParsedGold))
            {
                if (editingCharsheet)
                {
                    using var table = ImRaii.Table("##IdentityEditTable", 4, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
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

                        // Row 5: Pronouns & Linked System
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharPronounsField"));
                        ImGui.TableNextColumn();
                        UiUtils.ManageInputField(ref currentCharacter.characterPronouns, "CharacterPronouns", editingCharsheet, -1f);
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("DiceSysLinkedLabel"));
                        ImGui.TableNextColumn();
                        UiUtils.ManageInputField(ref currentCharacter.linkedDiceSystem, "CharacterLinkedSystem", editingCharsheet, -1f);
                    }
                }
                else
                {
                    using var table = ImRaii.Table("##IdentityViewGrid", 2, ImGuiTableFlags.SizingStretchSame);
                    if (table.Success)
                    {
                        ImGui.TableNextColumn();
                        DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharFullnameField"), currentCharacter.characterFullName, FontAwesomeIcon.IdCard, ImGuiColors.ParsedGold);
                        ImGui.TableNextColumn();
                        DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharNicknameField"), currentCharacter.characterNickName, FontAwesomeIcon.QuoteRight, ImGuiColors.ParsedGold);

                        ImGui.TableNextColumn();
                        DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharSpecieField"), currentCharacter.characterRace, FontAwesomeIcon.Dna, ImGuiColors.DalamudViolet);
                        ImGui.TableNextColumn();
                        DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharSubSpecieField"), currentCharacter.characterSubRace, FontAwesomeIcon.Dna, ImGuiColors.DalamudViolet);

                        ImGui.TableNextColumn();
                        DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharClassField"), currentCharacter.characterJob, FontAwesomeIcon.UserShield, ImGuiColors.ParsedBlue);
                        ImGui.TableNextColumn();
                        DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharAgeField"), currentCharacter.characterAge, FontAwesomeIcon.HourglassHalf, ImGuiColors.DalamudWhite);

                        ImGui.TableNextColumn();
                        DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharSexField"), currentCharacter.characterSex, FontAwesomeIcon.VenusMars, ImGuiColors.ParsedGreen);
                        ImGui.TableNextColumn();
                        DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharGenderField"), currentCharacter.characterGender, FontAwesomeIcon.VenusMars, ImGuiColors.ParsedGreen);

                        ImGui.TableNextColumn();
                        DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharPronounsField"), currentCharacter.characterPronouns, FontAwesomeIcon.CommentDots, ImGuiColors.ParsedGreen);
                        ImGui.TableNextColumn();
                        DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("DiceSysLinkedLabel"), currentCharacter.linkedDiceSystem, FontAwesomeIcon.DiceD20, ImGuiColors.ParsedGold);
                    }
                }
            }
        }

        private void DrawOocSection()
        {
            if (currentCharacter == null) return;

            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("PlayerOOCInfo").Replace(":", "").Trim(), defaultOpen: false, icon: FontAwesomeIcon.UserFriends, accentColor: ImGuiColors.ParsedBlue))
            {
                if (editingCharsheet)
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
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("PlayerAvailability"));
                        ImGui.TableNextColumn();
                        UiUtils.ManageInputField(ref currentCharacter.playerAvailability, "PlayerAvailability", editingCharsheet, -1f);
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("PlayerTimezone"));
                        ImGui.TableNextColumn();
                        UiUtils.ManageInputField(ref currentCharacter.playerTimezone, "PlayerTimezone", editingCharsheet, -1f);

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("PlayerOOCInfo"));
                        ImGui.TableNextColumn();
                        UiUtils.ManageInputField(ref currentCharacter.characterInfo, "CharacterInfo", editingCharsheet, -1f);
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharNotesField"));
                        ImGui.TableNextColumn();
                        UiUtils.ManageInputField(ref currentCharacter.characterNotes, "CharacterNotes", editingCharsheet, -1f);
                    }

                    ImGui.Spacing();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("PlayerOOCNotes"));
                    UiUtils.ManageBigInputField(ref currentCharacter.playerNotes, "PlayerNotes", editingCharsheet, 60.0f);
                }
                else
                {
                    using (var table = ImRaii.Table("##OOCViewGrid", 2, ImGuiTableFlags.SizingStretchSame))
                    {
                        if (table.Success)
                        {
                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("PlayerAvailability"), currentCharacter.playerAvailability, FontAwesomeIcon.Clock, ImGuiColors.ParsedBlue);
                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("PlayerTimezone"), currentCharacter.playerTimezone, FontAwesomeIcon.Globe, ImGuiColors.ParsedBlue);

                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("PlayerOOCInfo"), currentCharacter.characterInfo, FontAwesomeIcon.UserCircle, ImGuiColors.ParsedBlue);
                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharNotesField"), currentCharacter.characterNotes, FontAwesomeIcon.StickyNote, ImGuiColors.ParsedBlue);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(currentCharacter.playerNotes))
                    {
                        ImGui.Spacing();
                        DrawStoryBlock(LocalizationManager.Instance.GetLocalizedString("PlayerOOCNotes"), currentCharacter.playerNotes, FontAwesomeIcon.StickyNote, ImGuiColors.ParsedBlue);
                    }
                }
            }
        }

        private void DrawAppearanceSection()
        {
            if (currentCharacter == null) return;

            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("PhysicalAppearanceTab").Replace(":", "").Trim(), defaultOpen: false, icon: FontAwesomeIcon.User, accentColor: ImGuiColors.DalamudViolet))
            {
                if (editingCharsheet)
                {
                    using var table = ImRaii.Table("##AppearanceTable", 4, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
                    if (table.Success)
                    {
                        ImGui.TableSetupColumn("Label1", ImGuiTableColumnFlags.WidthFixed, 100.0f * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("Value1", ImGuiTableColumnFlags.WidthStretch, 1.0f);
                        ImGui.TableSetupColumn("Label2", ImGuiTableColumnFlags.WidthFixed, 100.0f * ImGuiHelpers.GlobalScale);
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

                        // Row 2: Body type & Complexion
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharBuildField"));
                        ImGui.TableNextColumn();
                        UiUtils.ManageInputField(ref currentCharacter.characterBuild, "CharacterBuild", editingCharsheet, -1f);
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("CharSkinColorField"));
                        ImGui.TableNextColumn();
                        UiUtils.ManageInputField(ref currentCharacter.characterSkinTone, "CharacterSkinTone", editingCharsheet, -1f);

                        // Row 3: Eye color & Hair color
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
                else
                {
                    using (var table = ImRaii.Table("##AppearanceViewGrid", 2, ImGuiTableFlags.SizingStretchSame))
                    {
                        if (table.Success)
                        {
                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharHeightField"), currentCharacter.characterHeight, FontAwesomeIcon.RulerVertical, ImGuiColors.DalamudViolet);
                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharWeightField"), currentCharacter.characterWeight, FontAwesomeIcon.WeightHanging, ImGuiColors.DalamudViolet);

                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharBuildField"), currentCharacter.characterBuild, FontAwesomeIcon.UserTag, ImGuiColors.DalamudViolet);
                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharSkinColorField"), currentCharacter.characterSkinTone, FontAwesomeIcon.Palette, ImGuiColors.DalamudViolet);

                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharEyeColorField"), currentCharacter.characterEyeColor, FontAwesomeIcon.Eye, ImGuiColors.DalamudViolet);
                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharHairColorField"), currentCharacter.characterHairColor, FontAwesomeIcon.Magic, ImGuiColors.DalamudViolet);

                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharScarsField"), currentCharacter.characterScars, FontAwesomeIcon.Cut, ImGuiColors.DalamudViolet);
                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharTatooField"), currentCharacter.characterTattoos, FontAwesomeIcon.PaintBrush, ImGuiColors.DalamudViolet);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterDistinctiveFeatures))
                    {
                        ImGui.Spacing();
                        DrawStoryBlock(LocalizationManager.Instance.GetLocalizedString("CharOtherQuirkField"), currentCharacter.characterDistinctiveFeatures, FontAwesomeIcon.Star, ImGuiColors.DalamudViolet);
                    }
                }
            }
        }

        private void DrawQuickLookSection()
        {
            if (currentCharacter == null) return;

            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("QuickLookSectionTitle").Replace(":", "").Trim(), defaultOpen: false, icon: FontAwesomeIcon.Eye, accentColor: ImGuiColors.ParsedGreen))
            {
                if (editingCharsheet)
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
                else
                {
                    string[] qlValues = {
                        currentCharacter.characterQuickLook1,
                        currentCharacter.characterQuickLook2,
                        currentCharacter.characterQuickLook3,
                        currentCharacter.characterQuickLook4,
                        currentCharacter.characterQuickLook5,
                    };

                    var scale = ImGuiHelpers.GlobalScale;
                    bool anyFound = false;
                    for (int i = 0; i < 5; i++)
                    {
                        var val = qlValues[i];
                        if (string.IsNullOrWhiteSpace(val)) continue;
                        anyFound = true;

                        using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.12f, 0.14f, 0.85f)))
                        using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(0.20f, 0.45f, 0.30f, 0.6f)))
                        using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * scale))
                        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 6.0f) * scale))
                        using (var card = ImRaii.Child($"##QLCard_{i}", new Vector2(0, 36.0f * scale), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                        {
                            if (card.Success)
                            {
                                UiUtils.Badge($"#{i + 1}", new Vector4(0.18f, 0.40f, 0.28f, 0.85f), ImGuiColors.ParsedGreen);
                                ImGui.SameLine(0, 8.0f * scale);
                                ImGui.TextColored(ImGuiColors.DalamudWhite, val);
                            }
                        }
                        ImGui.Spacing();
                    }

                    if (!anyFound)
                    {
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoneText"));
                    }
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

            if (UiUtils.StyledCollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("CharBackgroundField").Replace(":", "").Trim()} & {LocalizationManager.Instance.GetLocalizedString("CharFamilyRelationTab").Replace(":", "").Trim()}", defaultOpen: false, icon: FontAwesomeIcon.BookOpen, accentColor: ImGuiColors.ParsedGold))
            {
                if (editingCharsheet)
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
                }
                else
                {
                    using (var table = ImRaii.Table("##BgViewGrid", 2, ImGuiTableFlags.SizingStretchSame))
                    {
                        if (table.Success)
                        {
                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharBirthplaceField"), currentCharacter.characterHomeland, FontAwesomeIcon.MapMarkerAlt, ImGuiColors.ParsedGold);
                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharOriginField"), currentCharacter.characterOrigin, FontAwesomeIcon.GlobeAmericas, ImGuiColors.ParsedGold);

                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharAffiliationField"), currentCharacter.characterAffiliation, FontAwesomeIcon.Building, ImGuiColors.ParsedGold);
                            ImGui.TableNextColumn();
                            DrawPropertyCard(LocalizationManager.Instance.GetLocalizedString("CharWorkField"), currentCharacter.characterOccupation, FontAwesomeIcon.Briefcase, ImGuiColors.ParsedGold);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterReputation))
                    {
                        ImGui.Spacing();
                        DrawStoryBlock(LocalizationManager.Instance.GetLocalizedString("CharReputationField"), currentCharacter.characterReputation, FontAwesomeIcon.Award, ImGuiColors.ParsedGold);
                    }

                    if (!string.IsNullOrWhiteSpace(currentCharacter.characterBackground))
                    {
                        ImGui.Spacing();
                        DrawStoryBlock(LocalizationManager.Instance.GetLocalizedString("CharBackgroundField"), currentCharacter.characterBackground, FontAwesomeIcon.BookOpen, ImGuiColors.ParsedGold);
                    }
                }

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

                    if (UiUtils.IconButton($"Add_{id}", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddButton"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                    {
                        newMemberName = "";
                        newMemberDescription = "";
                        onAddClick();
                    }

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
                                    ImGui.TableSetupColumn("Del", ImGuiTableColumnFlags.WidthFixed, 24.0f * ImGuiHelpers.GlobalScale);
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
                                        if (UiUtils.IconButton($"Del_{kvp.Key}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                                        {
                                            keyToRemove = kvp.Key;
                                        }
                                    }

                                    ImGui.TableNextColumn();
                                    ImGui.TextColored(color, kvp.Key);

                                    ImGui.TableNextColumn();
                                    var desc = kvp.Value;
                                    if (editingCharsheet)
                                    {
                                        if (UiUtils.StyledInputText($"desc_{kvp.Key}", ref desc, 300, width: -1f))
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

            var scale = ImGuiHelpers.GlobalScale;

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
                UiUtils.StyledInputText("NewCharNameInput", ref newCharname, 100, width: 260.0f);

                ImGui.Spacing();
                if (UiUtils.IconTextButton("CreateCharConfirmBtn", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), size: new Vector2(110, 0) * scale))
                {
                    if (!string.IsNullOrWhiteSpace(newCharname))
                    {
                        CharacterSheet.CreateNewSheet(newCharname);
                        currentCharacter = CharacterManager.Instance.CharacterSheet;
                        showCreateCharPopup = false;
                    }
                }
                ImGui.SameLine(0, 8.0f * scale);
                if (UiUtils.IconTextButton("CreateCharCancelBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90, 0) * scale))
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
                UiUtils.StyledInputText("FMName", ref newMemberName, 100, width: 280.0f);
                ImGui.Spacing();
                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("MemberDescriptionField"));
                UiUtils.StyledInputMultiline("FMDesc", ref newMemberDescription, 500, size: new Vector2(280.0f * scale, 60.0f * scale));

                ImGui.Spacing();
                if (UiUtils.IconTextButton("AddFamilyConfirmBtn", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), size: new Vector2(110, 0) * scale))
                {
                    if (currentCharacter != null && !string.IsNullOrWhiteSpace(newMemberName))
                    {
                        currentCharacter.characterFamily ??= new Dictionary<string, string>();
                        currentCharacter.characterFamily[newMemberName] = newMemberDescription;
                        showFamilyPopup = false;
                    }
                }
                ImGui.SameLine(0, 8.0f * scale);
                if (UiUtils.IconTextButton("AddFamilyCancelBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90, 0) * scale))
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
                UiUtils.StyledInputText("FriendName", ref newMemberName, 100, width: 280.0f);
                ImGui.Spacing();
                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("FriendDescriptionField"));
                UiUtils.StyledInputMultiline("FriendDesc", ref newMemberDescription, 500, size: new Vector2(280.0f * scale, 60.0f * scale));

                ImGui.Spacing();
                if (UiUtils.IconTextButton("AddFriendConfirmBtn", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), size: new Vector2(110, 0) * scale))
                {
                    if (currentCharacter != null && !string.IsNullOrWhiteSpace(newMemberName))
                    {
                        currentCharacter.characterFriends ??= new Dictionary<string, string>();
                        currentCharacter.characterFriends[newMemberName] = newMemberDescription;
                        showFriendsPopup = false;
                    }
                }
                ImGui.SameLine(0, 8.0f * scale);
                if (UiUtils.IconTextButton("AddFriendCancelBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90, 0) * scale))
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
                UiUtils.StyledInputText("EnemyName", ref newMemberName, 100, width: 280.0f);
                ImGui.Spacing();
                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("EnemyDescriptionField"));
                UiUtils.StyledInputMultiline("EnemyDesc", ref newMemberDescription, 500, size: new Vector2(280.0f * scale, 60.0f * scale));

                ImGui.Spacing();
                if (UiUtils.IconTextButton("AddEnemyConfirmBtn", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), size: new Vector2(110, 0) * scale))
                {
                    if (currentCharacter != null && !string.IsNullOrWhiteSpace(newMemberName))
                    {
                        currentCharacter.characterEnnemies ??= new Dictionary<string, string>();
                        currentCharacter.characterEnnemies[newMemberName] = newMemberDescription;
                        showEnemiesPopup = false;
                    }
                }
                ImGui.SameLine(0, 8.0f * scale);
                if (UiUtils.IconTextButton("AddEnemyCancelBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90, 0) * scale))
                {
                    showEnemiesPopup = false;
                }

                ImGui.EndPopup();
            }
        }
    }
}
