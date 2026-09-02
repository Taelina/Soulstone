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
    internal class AugmentationsWindow
    {
        private readonly Plugin plugin;
        private readonly Configuration configuration;

        private string? selectedSlot = null;
        private bool showEquipModal = false;
        private string equipModalSlot = "Neural";

        private bool showCreateAugModal = false;
        private GearItem creatingAug = new();
        private StatModifierEditorState modEditorState = new();

        private readonly string[] rarities = new[]
        {
            "Common", "Uncommon", "Rare", "Epic", "Legendary", "Artifact"
        };

        public AugmentationsWindow(Plugin _plugin)
        {
            plugin = _plugin;
            configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public void DrawAugmentationsTab()
        {
            var currentCharacter = CharacterManager.Instance.CharacterSheet;
            var currentDiceSystem = DiceSystemManager.Instance.CurrentDiceSystem;

            if (currentCharacter == null)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("NoCharLoadedGearMessage"));
                return;
            }

            DrawTopBar(currentCharacter, currentDiceSystem);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            using (var table = ImRaii.Table("##AugmentationsLayoutTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Resizable))
            {
                if (table.Success)
                {
                    ImGui.TableSetupColumn("SlotsColumn", ImGuiTableColumnFlags.WidthStretch, 0.55f);
                    ImGui.TableSetupColumn("SummaryColumn", ImGuiTableColumnFlags.WidthStretch, 0.45f);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    DrawAugmentationSlots(currentCharacter, currentDiceSystem);

                    ImGui.TableNextColumn();
                    DrawSidePanel(currentCharacter);
                }
            }

            DrawModals(currentCharacter, currentDiceSystem);
        }

        private void DrawTopBar(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            string pageTitle = !string.IsNullOrWhiteSpace(diceSystem?.AugmentationTitle)
                ? diceSystem.AugmentationTitle
                : LocalizationManager.Instance.GetLocalizedString("DiceSysAugmentationsHeader");

            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.Microchip.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            ImGui.TextColored(ImGuiColors.ParsedGold, $"{sheet.CharacterFullName} — {pageTitle}");

            var equippedCount = sheet.GetEquippedAugmentationItems().Count;
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
            UiUtils.Badge($"{equippedCount} {LocalizationManager.Instance.GetLocalizedString("InstalledBadge")}", new Vector4(0.18f, 0.35f, 0.22f, 0.8f), ImGuiColors.ParsedGreen);

            var saveLabel = LocalizationManager.Instance.GetLocalizedString("SaveStatButton");
            var createLabel = LocalizationManager.Instance.GetLocalizedString("CreateAugmentationModalTitle");
            var createWidth = ImGui.CalcTextSize(createLabel).X + 24.0f * ImGuiHelpers.GlobalScale;
            var saveWidth = 28.0f * ImGuiHelpers.GlobalScale;
            var totalRightWidth = createWidth + saveWidth + 8.0f * ImGuiHelpers.GlobalScale;
            var rightX = ImGui.GetWindowContentRegionMax().X - totalRightWidth;

            if (ImGui.GetCursorPosX() < rightX)
                ImGui.SameLine(rightX);
            else
                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

            if (ImGui.Button($"{createLabel}###CreateAugTopBtn"))
            {
                creatingAug = new GearItem("New Augmentation", "Neural", "", "Common", null, "", 0.5f, "", isAugmentation: true);
                modEditorState = new StatModifierEditorState();
                showCreateAugModal = true;
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconButton("SaveAugBtn", FontAwesomeIcon.Save, saveLabel))
            {
                CharacterSheet.SaveSheet(sheet);
            }
        }

        private void DrawAugmentationSlots(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            var slots = diceSystem?.GetEffectiveAugmentationSlots() ?? GearItem.StandardAugmentationSlots.ToList();

            using (var child = ImRaii.Child("##AugmentationSlotsChild", new Vector2(0, 0), true))
            {
                if (child.Success)
                {
                    string header = !string.IsNullOrWhiteSpace(diceSystem?.AugmentationTitle)
                        ? diceSystem.AugmentationTitle
                        : LocalizationManager.Instance.GetLocalizedString("DiceSysAugmentationsHeader");
                    ImGui.TextColored(ImGuiColors.DalamudWhite, header);
                    ImGui.Separator();
                    ImGui.Spacing();

                    var availWidth = ImGui.GetContentRegionAvail().X;

                    foreach (var slot in slots)
                    {
                        var equippedItem = sheet.GetEquippedAugmentation(slot);
                        DrawSlotCard(sheet, slot, equippedItem, availWidth);
                        ImGui.Spacing();
                    }
                }
            }
        }

        private void DrawSlotCard(CharacterSheet sheet, string slot, GearItem? item, float width)
        {
            ImGui.PushID($"AugSlot_{slot}");

            var pos = ImGui.GetCursorScreenPos();
            var cardHeight = 52.0f * ImGuiHelpers.GlobalScale;
            var cardSize = new Vector2(width, cardHeight);

            var drawList = ImGui.GetWindowDrawList();
            bool isSelected = string.Equals(selectedSlot, slot, StringComparison.OrdinalIgnoreCase);
            bool isHovered = ImGui.IsMouseHoveringRect(pos, pos + cardSize);

            var bgCol = isSelected
                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.18f, 0.28f, 0.40f, 0.85f))
                : (isHovered
                    ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.16f, 0.20f, 0.26f, 0.70f))
                    : ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.14f, 0.18f, 0.55f)));

            var borderCol = isSelected
                ? ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGold)
                : (isHovered
                    ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.35f, 0.55f, 0.80f, 0.60f))
                    : ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.26f, 0.35f, 0.40f)));

            drawList.AddRectFilled(pos, pos + cardSize, bgCol, 4.0f * ImGuiHelpers.GlobalScale);
            drawList.AddRect(pos, pos + cardSize, borderCol, 4.0f * ImGuiHelpers.GlobalScale, ImDrawFlags.None, isSelected ? 2.0f : 1.0f);

            ImGui.SetCursorScreenPos(pos + new Vector2(8.0f * ImGuiHelpers.GlobalScale, 6.0f * ImGuiHelpers.GlobalScale));

            // Slot name badge
            string localizedSlot = GetLocalizedSlotName(slot);
            UiUtils.Badge(localizedSlot, new Vector4(0.15f, 0.25f, 0.35f, 0.9f), ImGuiColors.ParsedBlue);

            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

            if (item != null)
            {
                var rarityCol = GetRarityColor(item.Rarity);
                ImGui.TextColored(rarityCol, item.Name);

                // Row 2: Stat modifiers summary & action buttons
                ImGui.SetCursorScreenPos(pos + new Vector2(8.0f * ImGuiHelpers.GlobalScale, 28.0f * ImGuiHelpers.GlobalScale));

                if (item.StatModifiers != null && item.StatModifiers.Count > 0)
                {
                    string modsSummary = string.Join(", ", item.StatModifiers.Take(3).Select(kv => $"{(kv.Value >= 0 ? "+" : "")}{kv.Value} {kv.Key}"));
                    if (item.StatModifiers.Count > 3) modsSummary += $" (+{item.StatModifiers.Count - 3})";
                    ImGui.TextColored(ImGuiColors.ParsedGreen, modsSummary);
                }
                else
                {
                    ImGui.TextDisabled(item.Rarity);
                }

                // Right action buttons
                var btnHeight = 22.0f * ImGuiHelpers.GlobalScale;
                var unequipLabel = LocalizationManager.Instance.GetLocalizedString("UninstallButton");
                var unequipBtnWidth = ImGui.CalcTextSize(unequipLabel).X + 16.0f * ImGuiHelpers.GlobalScale;
                var changeLabel = LocalizationManager.Instance.GetLocalizedString("ChooseGearTitle");
                var changeBtnWidth = ImGui.CalcTextSize(changeLabel).X + 16.0f * ImGuiHelpers.GlobalScale;

                var rightStartX = pos.X + width - unequipBtnWidth - changeBtnWidth - 14.0f * ImGuiHelpers.GlobalScale;
                if (ImGui.GetCursorScreenPos().X < rightStartX)
                {
                    ImGui.SetCursorScreenPos(new Vector2(rightStartX, pos.Y + 14.0f * ImGuiHelpers.GlobalScale));
                }

                if (ImGui.Button($"{changeLabel}###ChangeAug_{slot}", new Vector2(changeBtnWidth, btnHeight)))
                {
                    equipModalSlot = slot;
                    showEquipModal = true;
                }

                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.Button($"{unequipLabel}###UnequipAug_{slot}", new Vector2(unequipBtnWidth, btnHeight)))
                {
                    sheet.UnequipAugmentation(slot);
                    CharacterSheet.SaveSheet(sheet);
                }
            }
            else
            {
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoAugmentationsEquipped"));

                var installLabel = LocalizationManager.Instance.GetLocalizedString("InstallButton");
                var installBtnWidth = ImGui.CalcTextSize(installLabel).X + 20.0f * ImGuiHelpers.GlobalScale;
                var rightStartX = pos.X + width - installBtnWidth - 8.0f * ImGuiHelpers.GlobalScale;
                if (ImGui.GetCursorScreenPos().X < rightStartX)
                {
                    ImGui.SetCursorScreenPos(new Vector2(rightStartX, pos.Y + 14.0f * ImGuiHelpers.GlobalScale));
                }

                if (ImGui.Button($"{installLabel}###InstallAug_{slot}", new Vector2(installBtnWidth, 22.0f * ImGuiHelpers.GlobalScale)))
                {
                    equipModalSlot = slot;
                    showEquipModal = true;
                }
            }

            ImGui.PopID();
            ImGui.SetCursorScreenPos(new Vector2(pos.X, pos.Y + cardHeight));
        }

        private void DrawSidePanel(CharacterSheet sheet)
        {
            using (var child = ImRaii.Child("##AugSidePanelChild", new Vector2(0, 0), true))
            {
                if (child.Success)
                {
                    DrawTotalStatBonuses(sheet);
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                    DrawEquippedAugmentationsList(sheet);
                }
            }
        }

        private void DrawTotalStatBonuses(CharacterSheet sheet)
        {
            ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("TotalAugmentationBonusesLabel"));
            ImGui.Separator();
            ImGui.Spacing();

            var equippedAugs = sheet.GetEquippedAugmentationItems();
            var totalBonuses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var aug in equippedAugs.Values)
            {
                if (aug.StatModifiers == null) continue;
                foreach (var kv in aug.StatModifiers)
                {
                    if (totalBonuses.ContainsKey(kv.Key))
                        totalBonuses[kv.Key] += kv.Value;
                    else
                        totalBonuses[kv.Key] = kv.Value;
                }
            }

            if (totalBonuses.Count == 0)
            {
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoAugmentationsEquipped"));
                return;
            }

            using (var table = ImRaii.Table("##AugTotalBonusesTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
            {
                if (table.Success)
                {
                    ImGui.TableSetupColumn("Stat", ImGuiTableColumnFlags.WidthStretch, 0.65f);
                    ImGui.TableSetupColumn("Bonus", ImGuiTableColumnFlags.WidthStretch, 0.35f);
                    ImGui.TableHeadersRow();

                    foreach (var kv in totalBonuses)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudWhite, kv.Key);

                        ImGui.TableNextColumn();
                        var col = kv.Value >= 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
                        ImGui.TextColored(col, $"{(kv.Value >= 0 ? "+" : "")}{kv.Value}");
                    }
                }
            }
        }

        private void DrawEquippedAugmentationsList(CharacterSheet sheet)
        {
            ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("InstalledBadge"));
            ImGui.Separator();
            ImGui.Spacing();

            var equipped = sheet.GetEquippedAugmentationItems();
            if (equipped.Count == 0)
            {
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoAugmentationsEquipped"));
                return;
            }

            foreach (var kv in equipped)
            {
                var slot = kv.Key;
                var item = kv.Value;

                UiUtils.Badge(GetLocalizedSlotName(slot), new Vector4(0.15f, 0.25f, 0.35f, 0.8f), ImGuiColors.ParsedBlue);
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                ImGui.TextColored(GetRarityColor(item.Rarity), item.Name);

                if (item.StatModifiers != null && item.StatModifiers.Count > 0)
                {
                    string mods = item.GetFormattedModifiers();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, $"  └ {mods}");
                }
                ImGui.Spacing();
            }
        }

        private void DrawModals(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            DrawInstallModal(sheet, diceSystem);
            DrawCreateAugmentationModal(sheet, diceSystem);
        }

        private void DrawInstallModal(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            if (!showEquipModal) return;

            string title = $"{LocalizationManager.Instance.GetLocalizedString("ChooseAugmentationTitle")}: {GetLocalizedSlotName(equipModalSlot)}###InstallAugModal";
            ImGui.SetNextWindowSize(new Vector2(450.0f, 380.0f) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);

            if (ImGui.Begin(title, ref showEquipModal, ImGuiWindowFlags.NoCollapse))
            {
                var augsInInventory = sheet.CharacterInventory
                    .OfType<GearItem>()
                    .Where(g => (g.isAugmentation || string.Equals(g.Slot, equipModalSlot, StringComparison.OrdinalIgnoreCase)) &&
                                !sheet.IsAugmentationEquipped(g.Id))
                    .ToList();

                if (augsInInventory.Count == 0)
                {
                    ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoAugmentationsInInventory"));
                    ImGui.Spacing();
                    if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("CreateAugmentationModalTitle")}###CreateAugFromModalBtn"))
                    {
                        creatingAug = new GearItem("New Augmentation", equipModalSlot, "", "Common", null, "", 0.5f, "", isAugmentation: true);
                        modEditorState = new StatModifierEditorState();
                        showCreateAugModal = true;
                    }
                }
                else
                {
                    using (var listChild = ImRaii.Child("##InstallAugListChild", new Vector2(0, -36.0f * ImGuiHelpers.GlobalScale), true))
                    {
                        if (listChild.Success)
                        {
                            foreach (var item in augsInInventory)
                            {
                                ImGui.PushID($"Install_{item.Id}");

                                var rarityCol = GetRarityColor(item.Rarity);
                                ImGui.TextColored(rarityCol, item.Name);
                                ImGui.SameLine();
                                UiUtils.Badge(item.Rarity, new Vector4(0.2f, 0.2f, 0.2f, 0.7f), rarityCol);

                                if (item.StatModifiers != null && item.StatModifiers.Count > 0)
                                {
                                    ImGui.TextColored(ImGuiColors.ParsedGreen, item.GetFormattedModifiers());
                                }

                                ImGui.SameLine(ImGui.GetContentRegionAvail().X - 80.0f * ImGuiHelpers.GlobalScale);
                                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("InstallButton")}###BtnInstall_{item.Id}", new Vector2(75.0f * ImGuiHelpers.GlobalScale, 22.0f * ImGuiHelpers.GlobalScale)))
                                {
                                    sheet.EquipAugmentation(equipModalSlot, item.Id);
                                    CharacterSheet.SaveSheet(sheet);
                                    showEquipModal = false;
                                }

                                ImGui.Separator();
                                ImGui.PopID();
                            }
                        }
                    }
                }

                ImGui.Spacing();
                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("CloseButton")}###CloseInstallModalBtn"))
                {
                    showEquipModal = false;
                }
            }
            ImGui.End();
        }

        private void DrawCreateAugmentationModal(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            if (!showCreateAugModal) return;

            string title = $"{LocalizationManager.Instance.GetLocalizedString("CreateAugmentationModalTitle")}###CreateAugModal";
            ImGui.SetNextWindowSize(new Vector2(480.0f, 480.0f) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);

            if (ImGui.Begin(title, ref showCreateAugModal, ImGuiWindowFlags.NoCollapse))
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, "Name:");
                ImGui.SetNextItemWidth(-1.0f);
                ImGui.InputText("##NewAugName", ref creatingAug.name, 100);

                var slots = diceSystem?.GetEffectiveAugmentationSlots() ?? GearItem.StandardAugmentationSlots.ToList();
                var slotsArray = slots.ToArray();

                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("GearSlotLabel"));
                ImGui.SetNextItemWidth(200.0f * ImGuiHelpers.GlobalScale);
                int slotIdx = Array.IndexOf(slotsArray, creatingAug.Slot);
                if (slotIdx < 0) slotIdx = 0;
                if (ImGui.Combo("##NewAugSlotCombo", ref slotIdx, slotsArray, slotsArray.Length))
                {
                    creatingAug.Slot = slotsArray[slotIdx];
                }

                ImGui.TextColored(ImGuiColors.DalamudGrey, "Rarity:");
                ImGui.SetNextItemWidth(200.0f * ImGuiHelpers.GlobalScale);
                int rarityIdx = Array.IndexOf(rarities, creatingAug.Rarity);
                if (rarityIdx < 0) rarityIdx = 0;
                if (ImGui.Combo("##NewAugRarityCombo", ref rarityIdx, rarities))
                {
                    creatingAug.Rarity = rarities[rarityIdx];
                }

                ImGui.TextColored(ImGuiColors.DalamudGrey, "Description:");
                ImGui.SetNextItemWidth(-1.0f);
                ImGui.InputTextMultiline("##NewAugDesc", ref creatingAug.description, 500, new Vector2(-1.0f, 50.0f * ImGuiHelpers.GlobalScale));

                ImGui.Separator();
                creatingAug.isAugmentation = true;
                UiUtils.DrawStatModifierEditor(creatingAug, sheet, diceSystem, modEditorState, "CreateAug");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddConfirmButton")} & {LocalizationManager.Instance.GetLocalizedString("InstallButton")}###CreateInstallBtn"))
                {
                    if (string.IsNullOrWhiteSpace(creatingAug.Name)) creatingAug.Name = "New Augmentation";
                    creatingAug.isAugmentation = true;
                    sheet.AddItem(creatingAug);
                    sheet.EquipAugmentation(creatingAug.Slot, creatingAug.Id);
                    CharacterSheet.SaveSheet(sheet);
                    showCreateAugModal = false;
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddConfirmButton")}###CreateOnlyAugBtn"))
                {
                    if (string.IsNullOrWhiteSpace(creatingAug.Name)) creatingAug.Name = "New Augmentation";
                    creatingAug.isAugmentation = true;
                    sheet.AddItem(creatingAug);
                    CharacterSheet.SaveSheet(sheet);
                    showCreateAugModal = false;
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("CancelButton")}###CancelCreateAugBtn"))
                {
                    showCreateAugModal = false;
                }
            }
            ImGui.End();
        }

        private string GetLocalizedSlotName(string slot)
        {
            string key = $"Slot{slot}";
            string loc = LocalizationManager.Instance.GetLocalizedString(key);
            if (loc != key) return loc;
            return slot;
        }

        private static Vector4 GetRarityColor(string rarity)
        {
            return rarity switch
            {
                "Uncommon" => ImGuiColors.ParsedGreen,
                "Rare" => ImGuiColors.ParsedBlue,
                "Epic" => ImGuiColors.ParsedPurple,
                "Legendary" => ImGuiColors.ParsedOrange,
                "Artifact" => ImGuiColors.ParsedGold,
                _ => ImGuiColors.DalamudWhite
            };
        }
    }
}
