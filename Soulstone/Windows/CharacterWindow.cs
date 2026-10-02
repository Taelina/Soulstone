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
using System.Threading.Tasks;

namespace Soulstone.Windows
{
    internal partial class CharacterWindow
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

        private async Task PublishCurrentCharacterAsync(CharacterSheet sheet)
        {
            bool success = await PartySyncManager.Instance.PublishCharacterSheetAsync(sheet).ConfigureAwait(false);
            await FrameworkDispatcher.RunAsync(() =>
            {
                if (plugin.IsDisposed) return;
                Messages.PrintEcho(LocalizationManager.Instance.GetLocalizedString(success ? "SheetPublishedSuccess" : "SheetPublishFailed"));
            }).ConfigureAwait(false);
        }

        public void Dispose() { }

        public void DrawCharTab()
        {
            using var sheetBackground = ImRaii.PushColor(ImGuiCol.ChildBg, SheetBackground);
            using var sheetButtons = ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.14f, 0.17f, 0.21f, 1));
            using var sheetButtonHover = ImRaii.PushColor(ImGuiCol.ButtonHovered, new Vector4(0.28f, 0.25f, 0.18f, 1));
            using var sheetButtonActive = ImRaii.PushColor(ImGuiCol.ButtonActive, new Vector4(0.39f, 0.31f, 0.16f, 1));
            using var sheetRounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 5.0f * ImGuiHelpers.GlobalScale);
            ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(),
                ImGui.ColorConvertFloat4ToU32(SheetBackground));
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
                        ImGui.TextColored(SoulstoneTheme.Gold, LocalizationManager.Instance.GetLocalizedString("NoCharLoadedMessage"));
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

            DrawSheetHero();
            ImGui.Spacing();
            DrawSheetColumns(DrawSheetIdentity, DrawSheetDescription);
            ImGui.Spacing();
            DrawSheetColumns(DrawSheetAppearance, DrawSheetQuickLook);
            ImGui.Spacing();
            DrawSheetColumns(DrawSheetOoc, DrawSheetBackground);
            ImGui.Spacing();
            DrawSheetRelationships();

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

                    using (ImRaii.PushColor(ImGuiCol.ChildBg, SoulstoneTheme.Field))
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
                                UiUtils.PillBadge(valText, new Vector4(0.24f, 0.20f, 0.12f, 0.85f), SoulstoneTheme.Gold);

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
            return UiUtils.GetResourceColor(name, colorHex);
        }

        private void DrawVisibilityToggle(string fieldName)
        {
            if (currentCharacter == null) return;
            var scale = ImGuiHelpers.GlobalScale;
            var isHidden = currentCharacter.IsFieldHidden(fieldName);
            var icon = isHidden ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye;
            var tooltip = isHidden
                ? LocalizationManager.Instance.GetLocalizedString("FieldVisibilityHiddenTooltip")
                : LocalizationManager.Instance.GetLocalizedString("FieldVisibilityVisibleTooltip");

            if (UiUtils.IconButton($"##Vis_{fieldName}", icon, tooltip, new Vector2(20, 20) * scale))
            {
                currentCharacter.ToggleFieldHidden(fieldName);
            }
        }

        private void DrawFieldLabelWithToggle(string label, string fieldName)
        {
            ImGui.TextColored(SoulstoneTheme.Muted, label);
            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            DrawVisibilityToggle(fieldName);
        }

        private void DrawTopActionBar()
        {
            var scale = ImGuiHelpers.GlobalScale;
            var pos = ImGui.GetCursorScreenPos();
            var availWidth = ImGui.GetContentRegionAvail().X;
            var barHeight = 44.0f * scale;
            var drawList = ImGui.GetWindowDrawList();

            // Background card
            var bgCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.07f, 0.085f, 0.11f, 1));
            var borderCol = ImGui.ColorConvertFloat4ToU32(SheetGold with { W = 0.45f });
            drawList.AddRectFilled(pos, pos + new Vector2(availWidth, barHeight), bgCol, 6.0f * scale);
            drawList.AddRect(pos, pos + new Vector2(availWidth, barHeight), borderCol, 6.0f * scale, ImDrawFlags.None, 1.2f);

            ImGui.SetCursorScreenPos(pos + new Vector2(10.0f * scale, 8.0f * scale));

            ImGui.BeginGroup();
            {
                // Edit mode toggle
                var editColor = editingCharsheet ? ImGuiColors.DalamudOrange : SoulstoneTheme.Muted;
                if (UiUtils.IconButton("EditCheck", FontAwesomeIcon.PencilAlt, LocalizationManager.Instance.GetLocalizedString("EditCharsheetCheck"), customColor: editColor))
                {
                    editingCharsheet = !editingCharsheet;
                }
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

                    ImGui.SameLine(0, 6.0f * scale);
                    if (UiUtils.IconButton("PublishCharBtn", FontAwesomeIcon.CloudUploadAlt, LocalizationManager.Instance.GetLocalizedString("PublishSheetToServer")))
                    {
                        _ = PublishCurrentCharacterAsync(currentCharacter);
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
                ImGui.TextColored(SoulstoneTheme.Gold, LocalizationManager.Instance.GetLocalizedString("NewCharButton"));
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
                ImGui.TextColored(SoulstoneTheme.Gold, LocalizationManager.Instance.GetLocalizedString("CharFamilyRelationTab"));
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
