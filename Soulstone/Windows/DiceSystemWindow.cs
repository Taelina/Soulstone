using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Soulstone.Windows
{
    internal class DiceSystemWindow
    {
        private readonly Plugin plugin;
        private readonly Configuration configuration;

        private int selectedDiceTypeIndex = 0;
        private int selectedSystemTypeIndex = 0;
        private string newAugSlotName = string.Empty;

        // Resource Modal State
        private bool showResourceModal = false;
        private bool isEditingResource = false;
        private string originalResourceName = string.Empty;
        private string modalResourceName = string.Empty;
        private int modalResourceMax = 100;
        private string modalResourceFormula = string.Empty;
        private string modalResourceColorHex = "#2ecc71";
        private Vector3 modalResourceColorVec = new(0.18f, 0.80f, 0.44f);
        private string modalResourceDesc = string.Empty;
        private bool modalResourceIsRequired = false;
        private bool modalResourceIsRollable = true;
        private bool modalResourceShowInGroup = true;
        private int modalResourceTypeIndex = 0;
        private string modalErrorMessage = string.Empty;

        private static readonly (string Name, string Hex, Vector3 Color)[] ColorPresets = new[]
        {
            ("Green", "#2ecc71", new Vector3(0.18f, 0.80f, 0.44f)),
            ("Blue", "#3498db", new Vector3(0.20f, 0.60f, 0.86f)),
            ("Orange", "#e67e22", new Vector3(0.90f, 0.49f, 0.13f)),
            ("Red", "#e74c3c", new Vector3(0.91f, 0.30f, 0.24f)),
            ("Purple", "#9b59b6", new Vector3(0.61f, 0.35f, 0.71f)),
            ("Cyan", "#1abc9c", new Vector3(0.10f, 0.74f, 0.61f)),
            ("Yellow", "#f1c40f", new Vector3(0.95f, 0.77f, 0.06f)),
            ("Grey", "#7f8c8d", new Vector3(0.50f, 0.55f, 0.55f))
        };

        public DiceSystemWindow(Plugin _plugin)
        {
            plugin = _plugin;
            configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public void DrawDiceSystemTab()
        {
            DiceSystem? currentSystem = DiceSystemManager.Instance.CurrentDiceSystem;
            if (currentSystem == null)
            {
                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.DalamudWhite, LocalizationManager.Instance.GetLocalizedString("NoDiceSysLoadedMessage"));
                return;
            }

            selectedDiceTypeIndex = (int)currentSystem.diceType;
            selectedSystemTypeIndex = (int)currentSystem.systemType;

            DrawTopBar(currentSystem);
            ImGui.Spacing();

            using (var parent = ImRaii.Child("##DiceSystemContent", Vector2.Zero))
            {
                if (parent.Success)
                {
                    DrawGeneralSettings(currentSystem);
                    ImGui.Spacing();
                    DrawInitiativeCard(currentSystem);
                    ImGui.Spacing();
                    DrawResourcesCard(currentSystem);
                    ImGui.Spacing();
                    DrawAugmentationsCard(currentSystem);
                    ImGui.Spacing();
                    DrawThresholdsCard(currentSystem);
                    ImGui.Spacing();
                    DrawFeaturesCard(currentSystem);
                }
            }

            DrawResourceModal(currentSystem);
        }

        private void DrawTopBar(DiceSystem currentSystem)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var pos = ImGui.GetCursorScreenPos();
            var availWidth = ImGui.GetContentRegionAvail().X;
            var bannerHeight = 56.0f * scale;
            var drawList = ImGui.GetWindowDrawList();

            var bgCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.11f, 0.14f, 0.95f));
            var borderCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.55f, 0.42f, 0.18f, 0.75f));
            var accentCol = ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGold);

            drawList.AddRectFilled(pos, pos + new Vector2(availWidth, bannerHeight), bgCol, 8.0f * scale);
            drawList.AddRect(pos, pos + new Vector2(availWidth, bannerHeight), borderCol, 8.0f * scale, ImDrawFlags.None, 1.2f);

            // Left gold trim
            drawList.AddRectFilled(
                pos + new Vector2(2.5f * scale, 5.0f * scale),
                pos + new Vector2(6.0f * scale, bannerHeight - 5.0f * scale),
                accentCol,
                2.0f * scale);

            // Framed Dice Emblem Box
            var emblemSize = 36.0f * scale;
            var emblemPos = pos + new Vector2(10.0f * scale, (bannerHeight - emblemSize) * 0.5f);
            drawList.AddRectFilled(emblemPos, emblemPos + new Vector2(emblemSize, emblemSize), ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.16f, 0.10f, 0.95f)), 6.0f * scale);
            drawList.AddRect(emblemPos, emblemPos + new Vector2(emblemSize, emblemSize), accentCol, 6.0f * scale, ImDrawFlags.None, 1.2f);

            ImGui.PushFont(UiBuilder.IconFont);
            var iconStr = FontAwesomeIcon.DiceD20.ToIconString();
            var iconSize = ImGui.CalcTextSize(iconStr);
            drawList.AddText(emblemPos + new Vector2((emblemSize - iconSize.X) * 0.5f, (emblemSize - iconSize.Y) * 0.5f), accentCol, iconStr);
            ImGui.PopFont();

            ImGui.SetCursorScreenPos(pos + new Vector2(emblemSize + 18.0f * scale, 8.0f * scale));

            ImGui.BeginGroup();
            {
                ImGui.TextColored(ImGuiColors.DalamudWhite, currentSystem.systemName);
                ImGui.SameLine(0, 8.0f * scale);

                UiUtils.PillBadge(Enum.GetName<SystemType>(currentSystem.systemType) ?? "Standard", new Vector4(0.18f, 0.32f, 0.50f, 0.85f), ImGuiColors.ParsedBlue, FontAwesomeIcon.Cogs);
                ImGui.SameLine(0, 6.0f * scale);

                string diceLabel = Enum.GetName<DiceType>(currentSystem.diceType) ?? "d20";
                UiUtils.PillBadge(diceLabel, new Vector4(0.30f, 0.18f, 0.45f, 0.85f), ImGuiColors.DalamudViolet, FontAwesomeIcon.Dice);

                if (currentSystem.systemHasAugmentations)
                {
                    ImGui.SameLine(0, 6.0f * scale);
                    UiUtils.PillBadge("Cyberware Active", new Vector4(0.15f, 0.35f, 0.22f, 0.85f), ImGuiColors.ParsedGreen, FontAwesomeIcon.Microchip);
                }

                if (DiceSystemManager.Instance.IsSessionRulesetActive)
                {
                    ImGui.SameLine(0, 6.0f * scale);
                    UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("GroupSyncedFromDM"), new Vector4(0.14f, 0.38f, 0.20f, 0.85f), ImGuiColors.ParsedGreen, FontAwesomeIcon.Link);
                }
            }
            ImGui.EndGroup();

            var saveLabel = LocalizationManager.Instance.GetLocalizedString("DiceSystemSaveButton");
            var chooseLabel = LocalizationManager.Instance.GetLocalizedString("DiceSystemChoose");
            var revertLabel = LocalizationManager.Instance.GetLocalizedString("GroupRevertRuleset");
            var templateTooltip = LocalizationManager.Instance.GetLocalizedString("DiceSysMakeSheetTemplateTooltip");

            float btnWidth = 32.0f * scale;
            float spacing = 6.0f * scale;
            int buttonCount = (DiceSystemManager.Instance.IsSessionRulesetActive ? 1 : 0) + 3;
            float totalButtonsWidth = buttonCount * btnWidth + (buttonCount - 1) * spacing;

            float rightStartX = pos.X + availWidth - totalButtonsWidth - 10.0f * scale;
            if (rightStartX > pos.X + emblemSize + 20.0f * scale)
            {
                ImGui.SetCursorScreenPos(new Vector2(rightStartX, pos.Y + 12.0f * scale));
            }
            else
            {
                ImGui.SameLine(0, 8.0f * scale);
            }

            ImGui.BeginGroup();
            {
                if (DiceSystemManager.Instance.IsSessionRulesetActive)
                {
                    if (UiUtils.IconButton("RevertDiceSysBtn", FontAwesomeIcon.Undo, revertLabel, new Vector2(btnWidth, 24.0f * scale)))
                    {
                        DiceSystemManager.Instance.RevertToLocalRuleset();
                    }
                    ImGui.SameLine(0, spacing);
                }

                if (UiUtils.IconButton("MakeSheetTemplateTopBtn", FontAwesomeIcon.FileSignature, templateTooltip, new Vector2(btnWidth, 24.0f * scale)))
                {
                    var sheet = CharacterManager.Instance.CharacterSheet;
                    if (sheet != null)
                    {
                        currentSystem.CaptureTemplateFromSheet(sheet);
                        DiceSystem.SaveDiceSystem(currentSystem);
                        CharacterSheet.SaveSheet(sheet);
                        string msg = LocalizationManager.Instance.GetLocalizedString("DiceSysSavedTemplateEcho", sheet.CharacterFullName, currentSystem.systemName);
                        Messages.PrintEcho(msg);
                        try
                        {
                            if (Plugin.ToastGui != null)
                            {
                                var toastOptions = new Dalamud.Game.Gui.Toast.QuestToastOptions
                                {
                                    PlaySound = true,
                                    DisplayCheckmark = true,
                                    IconId = 0
                                };
                                Plugin.ToastGui.ShowQuest(msg, toastOptions);
                            }
                        }
                        catch { }
                    }
                }

                ImGui.SameLine(0, spacing);
                if (UiUtils.IconButton("SaveDiceSysBtn", FontAwesomeIcon.Save, saveLabel, new Vector2(btnWidth, 24.0f * scale)))
                {
                    DiceSystem.SaveDiceSystem(currentSystem);
                }

                ImGui.SameLine(0, spacing);
                if (UiUtils.IconButton("ChooseDiceSysBtn", FontAwesomeIcon.FolderOpen, chooseLabel, new Vector2(btnWidth, 24.0f * scale)))
                {
                    plugin.OpenFilePicker(LocalizationManager.Instance.GetLocalizedString("ChooseDiceSysPickerTitle"), ".json", (path) =>
                    {
                        try
                        {
                            Plugin.Log?.Information($"Selected file: {path}");
                            DiceSystem? loadedSystem = DiceSystem.LoadDiceSystem(path, true);
                            if (loadedSystem != null)
                            {
                                DiceSystemManager.Instance.SwitchDiceSystem(loadedSystem);
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log?.Error(ex, $"Failed to load dice system from '{path}' in file picker callback");
                        }
                    });
                }
            }
            ImGui.EndGroup();

            ImGui.SetCursorScreenPos(pos);
            ImGui.Dummy(new Vector2(availWidth, bannerHeight));
        }

        private void DrawGeneralSettings(DiceSystem currentSystem)
        {
            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("DiceSysGeneralConfigHeader"), defaultOpen: true, icon: FontAwesomeIcon.Cogs, accentColor: ImGuiColors.ParsedGold))
            {
                ImGui.TextColored(new Vector4(0.85f, 0.85f, 0.9f, 0.9f), LocalizationManager.Instance.GetLocalizedString("DiceSysGeneralSubtitle"));
                ImGui.Spacing();

                using var table = ImRaii.Table("##GeneralSysTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
                if (table.Success)
                {
                    ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("TableHeaderLabel"), ImGuiTableColumnFlags.WidthFixed, 320.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("TableHeaderValue"), ImGuiTableColumnFlags.WidthStretch, 1.0f);

                    // System Name
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("DiceSystemNameLabel"));
                    ImGui.TableNextColumn();
                    UiUtils.StyledInputText("DiceSystemName", ref currentSystem.systemName, 100, width: 300.0f);

                    // System Type
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("SystemTypeCombo"));
                    ImGui.TableNextColumn();
                    if (UiUtils.StyledCombo("##DiceSystemTypeCombo", ref selectedSystemTypeIndex, Enum.GetNames<SystemType>(), icon: FontAwesomeIcon.Cogs, width: 220.0f))
                    {
                        currentSystem.systemType = (SystemType)selectedSystemTypeIndex;
                    }

                    // Dice Type
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("DiceTypeCombo"));
                    ImGui.TableNextColumn();
                    if (UiUtils.StyledCombo("##DiceTypeCombo", ref selectedDiceTypeIndex, Enum.GetNames<DiceType>(), icon: FontAwesomeIcon.DiceD20, width: 140.0f))
                    {
                        currentSystem.diceType = (DiceType)selectedDiceTypeIndex;
                    }

                    // Inventory Capacity Limit
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("SystemInventoryLimitCheckbox"));
                    ImGui.TableNextColumn();
                    ImGui.Checkbox("##SystemInventoryLimitGen", ref currentSystem.systemHasInventoryLimit);

                    if (currentSystem.systemHasInventoryLimit)
                    {
                        ImGui.SameLine(0, 16.0f * ImGuiHelpers.GlobalScale);
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted(LocalizationManager.Instance.GetLocalizedString("SystemInventorySlotsLabel"));
                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                        UiUtils.StyledInputInt("SystemInventoryMaxSlotsGen", ref currentSystem.inventoryMaxSlots, step: 5, width: 90.0f, min: 1);
                    }

                    // Make Current Sheet a Template Row
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("DiceSysMakeSheetTemplateLabel"));
                    ImGui.TableNextColumn();
                    if (UiUtils.IconButton("MakeSheetTemplateGenBtn", FontAwesomeIcon.FileSignature, LocalizationManager.Instance.GetLocalizedString("DiceSysMakeSheetTemplate")))
                    {
                        var sheet = CharacterManager.Instance.CharacterSheet;
                        if (sheet != null)
                        {
                            currentSystem.CaptureTemplateFromSheet(sheet);
                            DiceSystem.SaveDiceSystem(currentSystem);
                            CharacterSheet.SaveSheet(sheet);
                            string msg = LocalizationManager.Instance.GetLocalizedString("DiceSysSavedTemplateEcho", sheet.CharacterFullName, currentSystem.systemName);
                            Messages.PrintEcho(msg);
                            try
                            {
                                if (Plugin.ToastGui != null)
                                {
                                    var toastOptions = new Dalamud.Game.Gui.Toast.QuestToastOptions
                                    {
                                        PlaySound = true,
                                        DisplayCheckmark = true,
                                        IconId = 0
                                    };
                                    Plugin.ToastGui.ShowQuest(msg, toastOptions);
                                }
                            }
                            catch { }
                        }
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("DiceSysMakeSheetTemplateTooltip"));
                    }
                }
            }
        }

        private void DrawInitiativeCard(DiceSystem currentSystem)
        {
            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("InitiativeConfigHeader"), defaultOpen: true, icon: FontAwesomeIcon.Stopwatch, accentColor: ImGuiColors.ParsedBlue))
            {
                ImGui.TextColored(new Vector4(0.85f, 0.85f, 0.9f, 0.9f), LocalizationManager.Instance.GetLocalizedString("InitiativeConfigSubtitle"));
                ImGui.Spacing();

                using var table = ImRaii.Table("##InitiativeConfigTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
                if (table.Success)
                {
                    ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("TableHeaderLabel"), ImGuiTableColumnFlags.WidthFixed, 320.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("TableHeaderValue"), ImGuiTableColumnFlags.WidthStretch, 1.0f);

                    // Initiative Source (None / Attribute / Skill)
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("InitiativeStatTypeLabel"));
                    ImGui.TableNextColumn();

                    string[] typeOptions = new string[]
                    {
                        LocalizationManager.Instance.GetLocalizedString("InitiativeNone"),
                        LocalizationManager.Instance.GetLocalizedString("InitiativeAttribute"),
                        LocalizationManager.Instance.GetLocalizedString("InitiativeSkill"),
                        LocalizationManager.Instance.GetLocalizedString("InitiativeFormula")
                    };

                    int currentTypeIndex = (int)currentSystem.initiativeStatType;
                    if (UiUtils.StyledCombo("##InitiativeTypeCombo", ref currentTypeIndex, typeOptions, icon: FontAwesomeIcon.Stopwatch, width: 200.0f))
                    {
                        currentSystem.initiativeStatType = (InitiativeStatType)currentTypeIndex;
                    }

                    // Stat selector based on current initiativeStatType
                    if (currentSystem.initiativeStatType == InitiativeStatType.Formula)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("InitiativeFormulaLabel"));
                        ImGui.TableNextColumn();

                        UiUtils.StyledInputText("InitiativeFormulaInput", ref currentSystem.initiativeFormula, 100, width: 260.0f, hint: "e.g. 10 + [Dexterity] / 2");
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("InitiativeFormulaTooltip"));
                        }

                        var sheet = CharacterManager.Instance.CharacterSheet;
                        if (sheet != null)
                        {
                            int mod = sheet.GetInitiativeModifier(currentSystem);
                            ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                            UiUtils.Badge(mod >= 0 ? $"+{mod}" : $"{mod}", new Vector4(0.14f, 0.38f, 0.20f, 0.85f), ImGuiColors.ParsedGreen);

                            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                            if (UiUtils.IconButton("RollInitPreviewBtn", FontAwesomeIcon.DiceD20, LocalizationManager.Instance.GetLocalizedString("InitiativeRollInitiative")))
                            {
                                sheet.RollInitiative(currentSystem);
                            }
                        }
                    }
                    else if (currentSystem.initiativeStatType != InitiativeStatType.None)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("InitiativeStatNameLabel"));
                        ImGui.TableNextColumn();

                        var sheet = CharacterManager.Instance.CharacterSheet;
                        List<string> options = new();

                        if (currentSystem.initiativeStatType == InitiativeStatType.Attribute)
                        {
                            var attrs = currentSystem.SystemAttributes != null && currentSystem.SystemAttributes.Count > 0
                                ? currentSystem.SystemAttributes.Keys
                                : sheet?.characterAttributes?.Keys;
                            if (attrs != null)
                            {
                                options.AddRange(attrs);
                            }
                        }
                        else if (currentSystem.initiativeStatType == InitiativeStatType.Skill)
                        {
                            var skills = currentSystem.SystemSkills != null && currentSystem.SystemSkills.Count > 0
                                ? currentSystem.SystemSkills.Keys
                                : sheet?.characterSkills?.Keys;
                            if (skills != null)
                            {
                                options.AddRange(skills);
                            }
                        }

                        if (!string.IsNullOrEmpty(currentSystem.initiativeStatName) && !options.Contains(currentSystem.initiativeStatName))
                        {
                            options.Insert(0, currentSystem.initiativeStatName);
                        }

                        if (options.Count > 0)
                        {
                            int selectedIdx = options.IndexOf(currentSystem.initiativeStatName);
                            if (selectedIdx < 0) selectedIdx = 0;

                            if (UiUtils.StyledCombo("##InitiativeStatCombo", ref selectedIdx, options.ToArray(), icon: FontAwesomeIcon.Tag, width: 220.0f))
                            {
                                currentSystem.initiativeStatName = options[selectedIdx];
                            }
                        }
                        else
                        {
                            UiUtils.StyledInputText("InitiativeStatInput", ref currentSystem.initiativeStatName, 50, width: 220.0f, hint: "Stat name...");
                        }

                        // Preview / Test Roll if sheet is loaded
                        if (sheet != null)
                        {
                            int mod = sheet.GetInitiativeModifier(currentSystem);
                            ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                            UiUtils.Badge(mod >= 0 ? $"+{mod}" : $"{mod}", new Vector4(0.14f, 0.38f, 0.20f, 0.85f), ImGuiColors.ParsedGreen);

                            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                            if (UiUtils.IconButton("RollInitPreviewBtn", FontAwesomeIcon.DiceD20, LocalizationManager.Instance.GetLocalizedString("InitiativeRollInitiative")))
                            {
                                sheet.RollInitiative(currentSystem);
                            }
                        }
                    }
                }
            }
        }

        private void DrawResourcesCard(DiceSystem currentSystem)
        {
            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("DiceSysResourcesHeader"), defaultOpen: true, icon: FontAwesomeIcon.Heart, accentColor: ImGuiColors.ParsedGreen))
            {
                ImGui.TextColored(new Vector4(0.85f, 0.85f, 0.9f, 0.9f), LocalizationManager.Instance.GetLocalizedString("DiceSysResourcesSubtitle"));
                ImGui.Spacing();

                var resources = currentSystem.GetEffectiveResources();

                // Add Resource Button
                if (UiUtils.IconTextButton("OpenAddResModalBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("DiceSysAddResourceBtn")))
                {
                    OpenAddResourceModal();
                }

                ImGui.Spacing();

                if (resources.Count == 0)
                {
                    ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("DiceSysNoResources"));
                }
                else
                {
                    string? resToRemove = null;
                    ResourceDefinition? resToEdit = null;

                    using (var table = ImRaii.Table("##ResourcesTableNew", 6, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH))
                    {
                        if (table.Success)
                        {
                            ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceColor"), ImGuiTableColumnFlags.WidthFixed, 45.0f * ImGuiHelpers.GlobalScale);
                            ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceName"), ImGuiTableColumnFlags.WidthStretch, 0.30f);
                            ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("ResourceTypeLabel"), ImGuiTableColumnFlags.WidthStretch, 0.20f);
                            ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceMax"), ImGuiTableColumnFlags.WidthStretch, 0.25f);
                            ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceDescription"), ImGuiTableColumnFlags.WidthStretch, 0.35f);
                            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 130.0f * ImGuiHelpers.GlobalScale);
                            ImGui.TableHeadersRow();

                            for (int i = 0; i < resources.Count; i++)
                            {
                                var res = resources[i];
                                ImGui.PushID($"ResDefRow_{res.Name}");
                                ImGui.TableNextRow();

                                // Color swatch
                                ImGui.TableNextColumn();
                                Vector4 colVec = HexToVector4(res.ColorHex);
                                ImGui.ColorButton($"##ResColorBtn_{res.Name}", colVec, ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoPicker, new Vector2(24, 22) * ImGuiHelpers.GlobalScale);

                                // Name
                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                ImGui.TextColored(ImGuiColors.DalamudWhite, res.Name);
                                if (res.IsRequired)
                                {
                                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge("Core", new Vector4(0.35f, 0.28f, 0.12f, 0.7f), ImGuiColors.ParsedGold);
                                }
                                if (res.ShowInGroup)
                                {
                                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("GroupOpenWindow"), new Vector4(0.15f, 0.35f, 0.25f, 0.7f), ImGuiColors.ParsedGreen);
                                }

                                // Type
                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                string typeLabel = res.ResourceType switch
                                {
                                    ResourceType.Counter => LocalizationManager.Instance.GetLocalizedString("ResourceTypeCounter"),
                                    ResourceType.FlatNumber => LocalizationManager.Instance.GetLocalizedString("ResourceTypeFlatNumber"),
                                    _ => LocalizationManager.Instance.GetLocalizedString("ResourceTypeBar")
                                };
                                Vector4 typeBg = res.ResourceType switch
                                {
                                    ResourceType.Counter => new Vector4(0.35f, 0.25f, 0.12f, 0.7f),
                                    ResourceType.FlatNumber => new Vector4(0.30f, 0.18f, 0.38f, 0.7f),
                                    _ => new Vector4(0.15f, 0.28f, 0.38f, 0.7f)
                                };
                                Vector4 typeCol = res.ResourceType switch
                                {
                                    ResourceType.Counter => ImGuiColors.ParsedOrange,
                                    ResourceType.FlatNumber => ImGuiColors.DalamudViolet,
                                    _ => ImGuiColors.ParsedBlue
                                };
                                UiUtils.Badge(typeLabel, typeBg, typeCol);

                                // Max / Formula
                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                if (!string.IsNullOrWhiteSpace(res.Formula))
                                {
                                    UiUtils.Badge(res.Formula, new Vector4(0.2f, 0.35f, 0.45f, 0.7f), ImGuiColors.ParsedBlue);
                                    if (ImGui.IsItemHovered())
                                    {
                                        ImGui.SetTooltip($"{LocalizationManager.Instance.GetLocalizedString("DiceSysResourceMax")}: {res.DefaultMax}\n{LocalizationManager.Instance.GetLocalizedString("DiceSysResourceFormulaTooltip")}");
                                    }
                                }
                                else
                                {
                                    UiUtils.Badge($"{res.DefaultMax}", new Vector4(0.15f, 0.25f, 0.35f, 0.7f), ImGuiColors.ParsedBlue);
                                }

                                // Description
                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                if (!string.IsNullOrWhiteSpace(res.Description))
                                {
                                    ImGui.TextUnformatted(res.Description);
                                }
                                else
                                {
                                    ImGui.TextDisabled("—");
                                }

                                // Actions (Move Up / Move Down / Edit / Delete)
                                ImGui.TableNextColumn();
                                if (i > 0)
                                {
                                    if (UiUtils.IconButton($"MoveUpRes_{res.Name}", FontAwesomeIcon.ChevronUp, LocalizationManager.Instance.GetLocalizedString("MoveUpTooltip"), new Vector2(24, 24) * ImGuiHelpers.GlobalScale))
                                    {
                                        currentSystem.MoveResource(res.Name, -1);
                                    }
                                    ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
                                }
                                if (i < resources.Count - 1)
                                {
                                    if (UiUtils.IconButton($"MoveDownRes_{res.Name}", FontAwesomeIcon.ChevronDown, LocalizationManager.Instance.GetLocalizedString("MoveDownTooltip"), new Vector2(24, 24) * ImGuiHelpers.GlobalScale))
                                    {
                                        currentSystem.MoveResource(res.Name, 1);
                                    }
                                    ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
                                }

                                if (UiUtils.IconButton($"EditRes_{res.Name}", FontAwesomeIcon.Edit, LocalizationManager.Instance.GetLocalizedString("DiceSysEditResource"), new Vector2(24, 24) * ImGuiHelpers.GlobalScale))
                                {
                                    resToEdit = res;
                                }

                                ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
                                if (UiUtils.IconButton($"DelRes_{res.Name}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("DiceSysDeleteResource"), new Vector2(24, 24) * ImGuiHelpers.GlobalScale))
                                {
                                    resToRemove = res.Name;
                                }

                                ImGui.PopID();
                            }
                        }
                    }

                    if (resToEdit != null)
                    {
                        OpenEditResourceModal(resToEdit);
                    }

                    if (resToRemove != null)
                    {
                        currentSystem.RemoveResource(resToRemove);
                    }
                }
            }
        }

        private void OpenAddResourceModal()
        {
            isEditingResource = false;
            originalResourceName = string.Empty;
            modalResourceName = string.Empty;
            modalResourceMax = 100;
            modalResourceFormula = string.Empty;
            modalResourceColorHex = "#2ecc71";
            modalResourceColorVec = HexToVector3(modalResourceColorHex);
            modalResourceDesc = string.Empty;
            modalResourceIsRequired = false;
            modalResourceIsRollable = true;
            modalResourceShowInGroup = true;
            modalResourceTypeIndex = 0;
            modalErrorMessage = string.Empty;
            showResourceModal = true;
        }

        private void OpenEditResourceModal(ResourceDefinition res)
        {
            isEditingResource = true;
            originalResourceName = res.Name;
            modalResourceName = res.Name;
            modalResourceMax = res.DefaultMax;
            modalResourceFormula = res.Formula ?? string.Empty;
            modalResourceColorHex = string.IsNullOrWhiteSpace(res.ColorHex) ? "#2ecc71" : res.ColorHex;
            modalResourceColorVec = HexToVector3(modalResourceColorHex);
            modalResourceDesc = res.Description ?? string.Empty;
            modalResourceIsRequired = res.IsRequired;
            modalResourceIsRollable = res.IsRollable;
            modalResourceShowInGroup = res.ShowInGroup;
            modalResourceTypeIndex = (int)res.ResourceType;
            modalErrorMessage = string.Empty;
            showResourceModal = true;
        }

        private void DrawResourceModal(DiceSystem currentSystem)
        {
            if (showResourceModal)
            {
                ImGui.OpenPopup("ResourceModal");
            }

            if (ImGui.BeginPopupModal("ResourceModal", ref showResourceModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors.ParsedGreen, FontAwesomeIcon.Heart.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

                string modalTitle = isEditingResource
                    ? LocalizationManager.Instance.GetLocalizedString("DiceSysResourceModalTitleEdit")
                    : LocalizationManager.Instance.GetLocalizedString("DiceSysResourceModalTitleAdd");
                ImGui.TextColored(ImGuiColors.ParsedGreen, modalTitle);
                ImGui.Separator();
                ImGui.Spacing();

                if (!string.IsNullOrEmpty(modalErrorMessage))
                {
                    ImGui.TextColored(ImGuiColors.DalamudRed, modalErrorMessage);
                    ImGui.Spacing();
                }

                // Name
                ImGui.TextUnformatted(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceName"));
                UiUtils.StyledInputText("ModalResName", ref modalResourceName, 50, width: 260.0f);

                // Resource Type
                ImGui.Spacing();
                ImGui.TextUnformatted(LocalizationManager.Instance.GetLocalizedString("ResourceTypeLabel"));
                if (ImGui.RadioButton(LocalizationManager.Instance.GetLocalizedString("ResourceTypeBar"), modalResourceTypeIndex == 0)) { modalResourceTypeIndex = 0; }
                ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.RadioButton(LocalizationManager.Instance.GetLocalizedString("ResourceTypeCounter"), modalResourceTypeIndex == 1)) { modalResourceTypeIndex = 1; }
                ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.RadioButton(LocalizationManager.Instance.GetLocalizedString("ResourceTypeFlatNumber"), modalResourceTypeIndex == 2)) { modalResourceTypeIndex = 2; }

                // Formula
                ImGui.Spacing();
                ImGui.TextUnformatted(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceFormula"));
                UiUtils.StyledInputText("ModalResFormula", ref modalResourceFormula, 150, width: 260.0f, hint: LocalizationManager.Instance.GetLocalizedString("DiceSysResourceFormulaHint"));
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceFormulaTooltip"));
                }

                // Max
                ImGui.Spacing();
                ImGui.TextUnformatted(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceMax"));
                UiUtils.StyledInputInt("ModalResMax", ref modalResourceMax, step: 5, width: 120.0f, min: 1);

                // Color Picker
                ImGui.Spacing();
                ImGui.TextUnformatted(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceColorPicker"));
                if (ImGui.ColorEdit3("##ModalResColorPicker", ref modalResourceColorVec, ImGuiColorEditFlags.NoInputs))
                {
                    modalResourceColorHex = Vector3ToHex(modalResourceColorVec);
                }
                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.StyledInputText("ModalResColorHex", ref modalResourceColorHex, 10, width: 90.0f))
                {
                    modalResourceColorVec = HexToVector3(modalResourceColorHex);
                }

                // Color Presets
                ImGui.Spacing();
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("DiceSysResourcePresets"));
                for (int i = 0; i < ColorPresets.Length; i++)
                {
                    if (i > 0) ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    var preset = ColorPresets[i];
                    Vector4 pCol = new(preset.Color.X, preset.Color.Y, preset.Color.Z, 1.0f);
                    if (ImGui.ColorButton($"##Preset_{preset.Name}", pCol, ImGuiColorEditFlags.NoTooltip, new Vector2(24, 22) * ImGuiHelpers.GlobalScale))
                    {
                        modalResourceColorHex = preset.Hex;
                        modalResourceColorVec = preset.Color;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(preset.Name);
                    }
                }

                // Description
                ImGui.Spacing();
                ImGui.TextUnformatted(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceDescription"));
                UiUtils.StyledInputText("ModalResDesc", ref modalResourceDesc, 100, width: 260.0f);

                // Is Required
                ImGui.Spacing();
                ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceIsRequired"), ref modalResourceIsRequired);

                // Show in Group Management
                ImGui.Spacing();
                ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("ResourceShowInGroupLabel"), ref modalResourceShowInGroup);

                if (modalResourceTypeIndex == 2)
                {
                    ImGui.Spacing();
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceIsRollable"), ref modalResourceIsRollable);
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Save / Cancel buttons
                if (UiUtils.IconTextButton("SaveResModalBtn", FontAwesomeIcon.Save, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), size: new Vector2(100, 0) * ImGuiHelpers.GlobalScale))
                {
                    string trimmedName = modalResourceName.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedName))
                    {
                        modalErrorMessage = "Resource name cannot be empty.";
                    }
                    else
                    {
                        if (isEditingResource && !string.Equals(originalResourceName, trimmedName, StringComparison.OrdinalIgnoreCase))
                        {
                            currentSystem.RemoveResource(originalResourceName);
                        }

                        var resType = (ResourceType)modalResourceTypeIndex;
                        int defaultCur = modalResourceMax;
                        if (resType == ResourceType.Counter)
                        {
                            defaultCur = 0;
                        }

                        currentSystem.AddResource(new ResourceDefinition(
                            trimmedName,
                            modalResourceMax,
                            defaultCur,
                            modalResourceColorHex.Trim(),
                            modalResourceDesc.Trim(),
                            modalResourceIsRequired,
                            modalResourceFormula.Trim(),
                            resType,
                            modalResourceIsRollable,
                            modalResourceShowInGroup
                        ));

                        showResourceModal = false;
                    }
                }

                ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("CancelResModalBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(80, 0) * ImGuiHelpers.GlobalScale))
                {
                    showResourceModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private void DrawAugmentationsCard(DiceSystem currentSystem)
        {
            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("DiceSysAugmentationsHeader"), defaultOpen: true, icon: FontAwesomeIcon.Microchip, accentColor: ImGuiColors.ParsedPurple))
            {
                ImGui.TextColored(new Vector4(0.85f, 0.85f, 0.9f, 0.9f), LocalizationManager.Instance.GetLocalizedString("DiceSysAugmentationsSubtitle"));
                ImGui.Spacing();

                using var table = ImRaii.Table("##AugmentationsSysTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
                if (table.Success)
                {
                    ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("TableHeaderLabel"), ImGuiTableColumnFlags.WidthFixed, 360.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("TableHeaderValue"), ImGuiTableColumnFlags.WidthStretch, 1.0f);

                    // Toggle
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("SystemAugmentationsCheckbox"));
                    ImGui.TableNextColumn();
                    ImGui.Checkbox("##SystemAugmentationsCheck", ref currentSystem.systemHasAugmentations);

                    if (currentSystem.systemHasAugmentations)
                    {
                        // Tab Title
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("AugmentationTitleLabel"));
                        ImGui.TableNextColumn();
                        UiUtils.StyledInputText("AugmentationTitle", ref currentSystem.augmentationTitle, 100, width: 260.0f);

                        // Slot list
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("AugmentationSlotsHeader"));
                        ImGui.TableNextColumn();

                        var slots = currentSystem.GetEffectiveAugmentationSlots();
                        string? slotToRemove = null;
                        string? slotToMoveUp = null;
                        string? slotToMoveDown = null;

                        for (int i = 0; i < slots.Count; i++)
                        {
                            var slot = slots[i];
                            UiUtils.Badge(slot, new Vector4(0.2f, 0.25f, 0.35f, 0.7f), ImGuiColors.ParsedBlue);
                            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);

                            if (i > 0)
                            {
                                if (UiUtils.IconButton($"MoveUpAugSlot_{slot}", FontAwesomeIcon.ChevronLeft, LocalizationManager.Instance.GetLocalizedString("MoveLeftTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                                {
                                    slotToMoveUp = slot;
                                }
                                ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                            }
                            if (i < slots.Count - 1)
                            {
                                if (UiUtils.IconButton($"MoveDownAugSlot_{slot}", FontAwesomeIcon.ChevronRight, LocalizationManager.Instance.GetLocalizedString("MoveRightTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                                {
                                    slotToMoveDown = slot;
                                }
                                ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                            }

                            if (UiUtils.IconButton($"DelAugSlot_{slot}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                            {
                                slotToRemove = slot;
                            }
                            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                        }
                        ImGui.NewLine();

                        if (slotToMoveUp != null)
                        {
                            currentSystem.MoveAugmentationSlot(slotToMoveUp, -1);
                        }
                        if (slotToMoveDown != null)
                        {
                            currentSystem.MoveAugmentationSlot(slotToMoveDown, 1);
                        }
                        if (slotToRemove != null)
                        {
                            if (currentSystem.customAugmentationSlots == null || currentSystem.customAugmentationSlots.Count == 0)
                            {
                                currentSystem.customAugmentationSlots = GearItem.StandardAugmentationSlots.ToList();
                            }
                            currentSystem.customAugmentationSlots.Remove(slotToRemove);
                        }

                        // Add slot
                        UiUtils.StyledInputText("NewAugSlotName", ref newAugSlotName, 50, width: 160.0f, hint: "Slot name...");
                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                        if (UiUtils.IconTextButton("AddAugSlotBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddAugmentationSlot")))
                        {
                            if (!string.IsNullOrWhiteSpace(newAugSlotName))
                            {
                                if (currentSystem.customAugmentationSlots == null || currentSystem.customAugmentationSlots.Count == 0)
                                {
                                    currentSystem.customAugmentationSlots = GearItem.StandardAugmentationSlots.ToList();
                                }
                                if (!currentSystem.customAugmentationSlots.Contains(newAugSlotName.Trim()))
                                {
                                    currentSystem.customAugmentationSlots.Add(newAugSlotName.Trim());
                                }
                                newAugSlotName = string.Empty;
                            }
                        }
                    }
                }
            }
        }

        private void DrawThresholdsCard(DiceSystem currentSystem)
        {
            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("DiceSysThresholdsHeader"), defaultOpen: true, icon: FontAwesomeIcon.SlidersH, accentColor: ImGuiColors.ParsedOrange))
            {
                ImGui.TextColored(new Vector4(0.85f, 0.85f, 0.9f, 0.9f), LocalizationManager.Instance.GetLocalizedString("DiceSysThresholdsSubtitle"));
                ImGui.Spacing();

                using var table = ImRaii.Table("##ThresholdsTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
                if (table.Success)
                {
                    ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("TableHeaderLabel"), ImGuiTableColumnFlags.WidthFixed, 360.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("TableHeaderValue"), ImGuiTableColumnFlags.WidthStretch, 1.0f);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("SuccessThresholdLabel"));
                    ImGui.TableNextColumn();
                    UiUtils.StyledInputInt("SuccessThreshold", ref currentSystem.successThreshold, step: 1, width: 90.0f);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("SuccessIntervalLabel"));
                    ImGui.TableNextColumn();
                    UiUtils.StyledInputInt("SuccessInterval", ref currentSystem.successInterval, step: 1, width: 90.0f);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("DicePoolMaxSuccessCountLabel"));
                    ImGui.TableNextColumn();
                    UiUtils.StyledInputInt("DicePoolMaxSuccessCount", ref currentSystem.dicePoolMaxSuccessCount, step: 1, width: 90.0f, min: 1);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("DicePoolMaxSuccessCountTooltip"));
                    }
                }
            }
        }

        private void DrawFeaturesCard(DiceSystem currentSystem)
        {
            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("DiceSysFeaturesHeader"), defaultOpen: true, icon: FontAwesomeIcon.CheckSquare, accentColor: ImGuiColors.ParsedGold))
            {
                ImGui.TextColored(new Vector4(0.85f, 0.85f, 0.9f, 0.9f), LocalizationManager.Instance.GetLocalizedString("DiceSysFeaturesSubtitle"));
                ImGui.Spacing();

                using var table = ImRaii.Table("##FeaturesTableNew", 2, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.RowBg);
                if (table.Success)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("DnDStyleAdvDisadvCheckbox"), ref currentSystem.systemHasAdvantageDisadvantage);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("DnDStyleManaCheckbox"), ref currentSystem.systemHasManaOrResourcePoints);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("DnDStyleClassesCheckbox"), ref currentSystem.systemHasClasses);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("DnDStyleSavesCheckbox"), ref currentSystem.systemHasSaves);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("TempBonusCheckbox"), ref currentSystem.systemHasBonusTemp);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("PermBonusCheckbox"), ref currentSystem.systemHasBonusPerm);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("EpicAttributesCheckbox"), ref currentSystem.systemHasEpicAttributes);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("SystemInventoryLimitCheckbox"), ref currentSystem.systemHasInventoryLimit);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("DynamicSkillAttributeLinkingCheckbox"), ref currentSystem.dynamicSkillAttributeLinking);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("DynamicSkillAttributeLinkingTooltip"));
                    }

                    ImGui.TableNextColumn();
                }
            }
        }

        private static Vector3 HexToVector3(string hex)
        {
            if (!string.IsNullOrWhiteSpace(hex))
            {
                hex = hex.Trim().TrimStart('#');
                if (hex.Length == 6)
                {
                    try
                    {
                        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                        return new Vector3(r / 255f, g / 255f, b / 255f);
                    }
                    catch { }
                }
            }
            return new Vector3(0.18f, 0.80f, 0.44f);
        }

        private static Vector4 HexToVector4(string hex, float alpha = 1.0f)
        {
            var v3 = HexToVector3(hex);
            return new Vector4(v3.X, v3.Y, v3.Z, alpha);
        }

        private static string Vector3ToHex(Vector3 col)
        {
            int r = Math.Clamp((int)(col.X * 255f + 0.5f), 0, 255);
            int g = Math.Clamp((int)(col.Y * 255f + 0.5f), 0, 255);
            int b = Math.Clamp((int)(col.Z * 255f + 0.5f), 0, 255);
            return $"#{r:X2}{g:X2}{b:X2}".ToLowerInvariant();
        }
    }
}
