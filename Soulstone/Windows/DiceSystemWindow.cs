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
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.DiceD20.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

            ImGui.TextColored(ImGuiColors.DalamudWhite, currentSystem.systemName);
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

            UiUtils.Badge(Enum.GetName<SystemType>(currentSystem.systemType) ?? "Standard", new Vector4(0.2f, 0.4f, 0.6f, 0.7f), ImGuiColors.ParsedBlue);
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

            string diceLabel = Enum.GetName<DiceType>(currentSystem.diceType) ?? "d20";
            UiUtils.Badge(diceLabel, new Vector4(0.35f, 0.25f, 0.5f, 0.7f), ImGuiColors.DalamudViolet);

            if (currentSystem.systemHasAugmentations)
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                UiUtils.Badge("Cyberware Active", new Vector4(0.15f, 0.35f, 0.25f, 0.7f), ImGuiColors.ParsedGreen);
            }

            if (DiceSystemManager.Instance.IsSessionRulesetActive)
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("GroupSyncedFromDM"), new Vector4(0.14f, 0.38f, 0.20f, 0.85f), ImGuiColors.ParsedGreen);
            }

            var saveLabel = LocalizationManager.Instance.GetLocalizedString("DiceSystemSaveButton");
            var chooseLabel = LocalizationManager.Instance.GetLocalizedString("DiceSystemChoose");
            var revertLabel = LocalizationManager.Instance.GetLocalizedString("GroupRevertRuleset");
            var templateTooltip = LocalizationManager.Instance.GetLocalizedString("DiceSysMakeSheetTemplateTooltip");

            float btnWidth = 28.0f * ImGuiHelpers.GlobalScale;
            float spacing = 6.0f * ImGuiHelpers.GlobalScale;
            int buttonCount = (DiceSystemManager.Instance.IsSessionRulesetActive ? 1 : 0) + 3;
            float totalButtonsWidth = buttonCount * btnWidth + (buttonCount - 1) * spacing;

            float avail = ImGui.GetContentRegionAvail().X;
            if (avail >= totalButtonsWidth)
            {
                ImGui.SameLine(ImGui.GetCursorPosX() + avail - totalButtonsWidth);
            }
            else
            {
                float nextAvail = ImGui.GetContentRegionAvail().X;
                if (nextAvail > totalButtonsWidth)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + nextAvail - totalButtonsWidth);
                }
            }

            if (DiceSystemManager.Instance.IsSessionRulesetActive)
            {
                if (UiUtils.IconButton("RevertDiceSysBtn", FontAwesomeIcon.Undo, revertLabel, new Vector2(btnWidth, 0)))
                {
                    DiceSystemManager.Instance.RevertToLocalRuleset();
                }
                ImGui.SameLine(0, spacing);
            }

            if (UiUtils.IconButton("MakeSheetTemplateTopBtn", FontAwesomeIcon.FileSignature, templateTooltip, new Vector2(btnWidth, 0)))
            {
                var sheet = CharacterManager.Instance.CharacterSheet;
                if (sheet != null)
                {
                    currentSystem.CaptureTemplateFromSheet(sheet);
                    DiceSystem.SaveDiceSystem(currentSystem);
                    CharacterSheet.SaveSheet(sheet);
                    string msg = $"[Soulstone] Character '{sheet.CharacterFullName}' saved as template for '{currentSystem.systemName}'.";
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
            if (UiUtils.IconButton("SaveDiceSysBtn", FontAwesomeIcon.Save, saveLabel, new Vector2(btnWidth, 0)))
            {
                DiceSystem.SaveDiceSystem(currentSystem);
            }

            ImGui.SameLine(0, spacing);
            if (UiUtils.IconButton("ChooseDiceSysBtn", FontAwesomeIcon.FolderOpen, chooseLabel, new Vector2(btnWidth, 0)))
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

        private void DrawGeneralSettings(DiceSystem currentSystem)
        {
            var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("DiceSysGeneralConfigHeader")}###GeneralConfigHeader", flags))
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
                    ImGui.SetNextItemWidth(300.0f * ImGuiHelpers.GlobalScale);
                    ImGui.InputText("##DiceSystemName", ref currentSystem.systemName, 100);

                    // System Type
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("SystemTypeCombo"));
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(220.0f * ImGuiHelpers.GlobalScale);
                    if (ImGui.Combo("##DiceSystemTypeCombo", ref selectedSystemTypeIndex, Enum.GetNames<SystemType>()))
                    {
                        currentSystem.systemType = (SystemType)selectedSystemTypeIndex;
                    }

                    // Dice Type
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("DiceTypeCombo"));
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(140.0f * ImGuiHelpers.GlobalScale);
                    if (ImGui.Combo("##DiceTypeCombo", ref selectedDiceTypeIndex, Enum.GetNames<DiceType>()))
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
                        ImGui.SetNextItemWidth(90.0f * ImGuiHelpers.GlobalScale);
                        ImGui.InputInt("##SystemInventoryMaxSlotsGen", ref currentSystem.inventoryMaxSlots, 5);
                        if (currentSystem.inventoryMaxSlots < 1) currentSystem.inventoryMaxSlots = 1;
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
                            string msg = $"[Soulstone] Character '{sheet.CharacterFullName}' saved as template for '{currentSystem.systemName}'.";
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
            var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("InitiativeConfigHeader")}###InitiativeConfigHeader", flags))
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
                        LocalizationManager.Instance.GetLocalizedString("InitiativeSkill")
                    };

                    int currentTypeIndex = (int)currentSystem.initiativeStatType;
                    ImGui.SetNextItemWidth(200.0f * ImGuiHelpers.GlobalScale);
                    if (ImGui.Combo("##InitiativeTypeCombo", ref currentTypeIndex, typeOptions, typeOptions.Length))
                    {
                        currentSystem.initiativeStatType = (InitiativeStatType)currentTypeIndex;
                    }

                    // Stat selector based on current initiativeStatType
                    if (currentSystem.initiativeStatType != InitiativeStatType.None)
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
                            if (sheet?.characterAttributes != null && sheet.characterAttributes.Count > 0)
                            {
                                options.AddRange(sheet.characterAttributes.Keys);
                            }
                        }
                        else if (currentSystem.initiativeStatType == InitiativeStatType.Skill)
                        {
                            if (sheet?.characterSkills != null && sheet.characterSkills.Count > 0)
                            {
                                options.AddRange(sheet.characterSkills.Keys);
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

                            ImGui.SetNextItemWidth(220.0f * ImGuiHelpers.GlobalScale);
                            if (ImGui.Combo("##InitiativeStatCombo", ref selectedIdx, options.ToArray(), options.Count))
                            {
                                currentSystem.initiativeStatName = options[selectedIdx];
                            }
                        }
                        else
                        {
                            ImGui.SetNextItemWidth(220.0f * ImGuiHelpers.GlobalScale);
                            ImGui.InputTextWithHint("##InitiativeStatInput", "Stat name...", ref currentSystem.initiativeStatName, 50);
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
            var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("DiceSysResourcesHeader")}###ResourcesHeader", flags))
            {
                ImGui.TextColored(new Vector4(0.85f, 0.85f, 0.9f, 0.9f), LocalizationManager.Instance.GetLocalizedString("DiceSysResourcesSubtitle"));
                ImGui.Spacing();

                var resources = currentSystem.GetEffectiveResources();

                // Add Resource Button
                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("DiceSysAddResourceBtn")}###OpenAddResModalBtn"))
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

                    using (var table = ImRaii.Table("##ResourcesTableNew", 5, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH))
                    {
                        if (table.Success)
                        {
                            ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceColor"), ImGuiTableColumnFlags.WidthFixed, 45.0f * ImGuiHelpers.GlobalScale);
                            ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceName"), ImGuiTableColumnFlags.WidthStretch, 0.35f);
                            ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceMax"), ImGuiTableColumnFlags.WidthStretch, 0.25f);
                            ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceDescription"), ImGuiTableColumnFlags.WidthStretch, 0.40f);
                            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 65.0f * ImGuiHelpers.GlobalScale);
                            ImGui.TableHeadersRow();

                            foreach (var res in resources)
                            {
                                ImGui.PushID($"ResDefRow_{res.Name}");
                                ImGui.TableNextRow();

                                // Color swatch
                                ImGui.TableNextColumn();
                                Vector4 colVec = HexToVector4(res.ColorHex);
                                ImGui.ColorButton($"##ResColorBtn_{res.Name}", colVec, ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoPicker, new Vector2(24, 20) * ImGuiHelpers.GlobalScale);

                                // Name
                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                ImGui.TextColored(ImGuiColors.DalamudWhite, res.Name);
                                if (res.IsRequired)
                                {
                                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge("Core", new Vector4(0.35f, 0.28f, 0.12f, 0.7f), ImGuiColors.ParsedGold);
                                }

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

                                // Actions (Edit / Delete)
                                ImGui.TableNextColumn();
                                if (UiUtils.IconButton($"EditRes_{res.Name}", FontAwesomeIcon.Edit, LocalizationManager.Instance.GetLocalizedString("DiceSysEditResource"), new Vector2(24, 20) * ImGuiHelpers.GlobalScale))
                                {
                                    resToEdit = res;
                                }

                                if (!res.IsRequired && !string.Equals(res.Name, "Health", StringComparison.OrdinalIgnoreCase))
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    if (UiUtils.IconButton($"DelRes_{res.Name}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("DiceSysDeleteResource"), new Vector2(24, 20) * ImGuiHelpers.GlobalScale))
                                    {
                                        resToRemove = res.Name;
                                    }
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
                ImGui.SetNextItemWidth(260.0f * ImGuiHelpers.GlobalScale);
                ImGui.InputText("##ModalResName", ref modalResourceName, 50);

                // Formula
                ImGui.Spacing();
                ImGui.TextUnformatted(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceFormula"));
                ImGui.SetNextItemWidth(260.0f * ImGuiHelpers.GlobalScale);
                ImGui.InputTextWithHint("##ModalResFormula", LocalizationManager.Instance.GetLocalizedString("DiceSysResourceFormulaHint"), ref modalResourceFormula, 150);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceFormulaTooltip"));
                }

                // Max
                ImGui.Spacing();
                ImGui.TextUnformatted(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceMax"));
                ImGui.SetNextItemWidth(120.0f * ImGuiHelpers.GlobalScale);
                ImGui.InputInt("##ModalResMax", ref modalResourceMax, 5);
                if (modalResourceMax < 1) modalResourceMax = 1;

                // Color Picker
                ImGui.Spacing();
                ImGui.TextUnformatted(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceColorPicker"));
                if (ImGui.ColorEdit3("##ModalResColorPicker", ref modalResourceColorVec, ImGuiColorEditFlags.NoInputs))
                {
                    modalResourceColorHex = Vector3ToHex(modalResourceColorVec);
                }
                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                ImGui.SetNextItemWidth(90.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputText("##ModalResColorHex", ref modalResourceColorHex, 10))
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
                    if (ImGui.ColorButton($"##Preset_{preset.Name}", pCol, ImGuiColorEditFlags.NoTooltip, new Vector2(22, 20) * ImGuiHelpers.GlobalScale))
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
                ImGui.SetNextItemWidth(260.0f * ImGuiHelpers.GlobalScale);
                ImGui.InputText("##ModalResDesc", ref modalResourceDesc, 100);

                // Is Required
                if (!string.Equals(modalResourceName, "Health", StringComparison.OrdinalIgnoreCase))
                {
                    ImGui.Spacing();
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceIsRequired"), ref modalResourceIsRequired);
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Save / Cancel buttons
                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddConfirmButton")}###SaveResModalBtn", new Vector2(100, 0) * ImGuiHelpers.GlobalScale))
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

                        currentSystem.AddResource(new ResourceDefinition(
                            trimmedName,
                            modalResourceMax,
                            modalResourceMax,
                            modalResourceColorHex.Trim(),
                            modalResourceDesc.Trim(),
                            modalResourceIsRequired,
                            modalResourceFormula.Trim()
                        ));

                        showResourceModal = false;
                    }
                }

                ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("CancelButton")}###CancelResModalBtn", new Vector2(80, 0) * ImGuiHelpers.GlobalScale))
                {
                    showResourceModal = false;
                }

                ImGui.EndPopup();
            }
        }

        private void DrawAugmentationsCard(DiceSystem currentSystem)
        {
            var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("DiceSysAugmentationsHeader")}###AugmentationsHeader", flags))
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
                        ImGui.SetNextItemWidth(260.0f * ImGuiHelpers.GlobalScale);
                        ImGui.InputText("##AugmentationTitle", ref currentSystem.augmentationTitle, 100);

                        // Slot list
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("AugmentationSlotsHeader"));
                        ImGui.TableNextColumn();

                        var slots = currentSystem.GetEffectiveAugmentationSlots();
                        string? slotToRemove = null;
                        foreach (var slot in slots)
                        {
                            UiUtils.Badge(slot, new Vector4(0.2f, 0.25f, 0.35f, 0.7f), ImGuiColors.ParsedBlue);
                            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                            if (UiUtils.IconButton($"DelAugSlot_{slot}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"), new Vector2(18, 18) * ImGuiHelpers.GlobalScale))
                            {
                                slotToRemove = slot;
                            }
                            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                        }
                        ImGui.NewLine();

                        if (slotToRemove != null)
                        {
                            if (currentSystem.customAugmentationSlots == null || currentSystem.customAugmentationSlots.Count == 0)
                            {
                                currentSystem.customAugmentationSlots = GearItem.StandardAugmentationSlots.ToList();
                            }
                            currentSystem.customAugmentationSlots.Remove(slotToRemove);
                        }

                        // Add slot
                        ImGui.SetNextItemWidth(160.0f * ImGuiHelpers.GlobalScale);
                        ImGui.InputTextWithHint("##NewAugSlotName", "Slot name...", ref newAugSlotName, 50);
                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                        if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddAugmentationSlot")}###AddAugSlotBtn"))
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
            var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("DiceSysThresholdsHeader")}###ThresholdsHeader", flags))
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
                    ImGui.SetNextItemWidth(90.0f * ImGuiHelpers.GlobalScale);
                    ImGui.InputInt("##SuccessThreshold", ref currentSystem.successThreshold, 1);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("SuccessIntervalLabel"));
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(90.0f * ImGuiHelpers.GlobalScale);
                    ImGui.InputInt("##SuccessInterval", ref currentSystem.successInterval, 1);
                }
            }
        }

        private void DrawFeaturesCard(DiceSystem currentSystem)
        {
            var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("DiceSysFeaturesHeader")}###FeaturesHeader", flags))
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
