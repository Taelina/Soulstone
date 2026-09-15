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
            var scale = ImGuiHelpers.GlobalScale;
            string pageTitle = !string.IsNullOrWhiteSpace(diceSystem?.AugmentationTitle)
                ? diceSystem.AugmentationTitle
                : LocalizationManager.Instance.GetLocalizedString("DiceSysAugmentationsHeader");

            var equippedCount = sheet.GetEquippedAugmentationItems().Count;
            var saveLabel = LocalizationManager.Instance.GetLocalizedString("SaveStatButton");
            var createLabel = LocalizationManager.Instance.GetLocalizedString("CreateAugmentationModalTitle");

            UiUtils.DrawWindowHeroBanner(
                title: $"{sheet.CharacterFullName} — {pageTitle}",
                subtitle: LocalizationManager.Instance.GetLocalizedString("DiceSysAugmentationsHeader"),
                badgeText: $"{equippedCount} {LocalizationManager.Instance.GetLocalizedString("InstalledBadge")}",
                badgeColor: ImGuiColors.ParsedGreen,
                icon: FontAwesomeIcon.Microchip,
                accentColor: ImGuiColors.ParsedBlue,
                actionLabel: createLabel,
                onAction: () =>
                {
                    creatingAug = new GearItem("New Augmentation", "Neural", "", "Common", null, "", 0.5f, "", isAugmentation: true);
                    modEditorState = new StatModifierEditorState();
                    showCreateAugModal = true;
                });
        }

        private static FontAwesomeIcon GetAugSlotIcon(string slot)
        {
            return slot?.ToLowerInvariant() switch
            {
                "neural" or "cortex" or "brain" => FontAwesomeIcon.Brain,
                "ocular" or "eyes" => FontAwesomeIcon.Eye,
                "cardiovascular" or "heart" or "internal" => FontAwesomeIcon.Heartbeat,
                "muscular" or "arms" => FontAwesomeIcon.HandRock,
                "skeletal" or "legs" or "bones" => FontAwesomeIcon.Bone,
                "dermal" or "skin" => FontAwesomeIcon.ShieldAlt,
                "subdermal" or "circulatory" => FontAwesomeIcon.Microchip,
                _ => FontAwesomeIcon.Microchip
            };
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

            var scale = ImGuiHelpers.GlobalScale;
            var pos = ImGui.GetCursorScreenPos();
            var cardHeight = 52.0f * scale;
            var cardSize = new Vector2(width, cardHeight);

            var drawList = ImGui.GetWindowDrawList();
            bool isSelected = string.Equals(selectedSlot, slot, StringComparison.OrdinalIgnoreCase);
            bool isHovered = ImGui.IsMouseHoveringRect(pos, pos + cardSize);
            var rarityCol = item != null ? GetRarityColor(item.Rarity) : ImGuiColors.DalamudGrey;
            var slotIcon = GetAugSlotIcon(slot);

            var bgCol = isSelected
                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.18f, 0.22f, 0.32f, 0.95f))
                : (isHovered
                    ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.16f, 0.20f, 0.90f))
                    : ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.11f, 0.13f, 0.85f)));

            var borderCol = isSelected
                ? ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGold)
                : (isHovered
                    ? ImGui.ColorConvertFloat4ToU32(new Vector4(rarityCol.X, rarityCol.Y, rarityCol.Z, 0.80f))
                    : ImGui.ColorConvertFloat4ToU32(new Vector4(0.24f, 0.26f, 0.30f, 0.55f)));

            drawList.AddRectFilled(pos, pos + cardSize, bgCol, 6.0f * scale);
            drawList.AddRect(pos, pos + cardSize, borderCol, 6.0f * scale, ImDrawFlags.None, isSelected ? 1.8f : 1.0f);

            // Left accent bar
            drawList.AddRectFilled(
                pos + new Vector2(2f * scale, 5f * scale),
                pos + new Vector2(5.5f * scale, cardHeight - 5f * scale),
                ImGui.ColorConvertFloat4ToU32(item != null ? rarityCol : new Vector4(0.3f, 0.3f, 0.35f, 0.5f)),
                2.0f * scale);

            // Framed slot icon emblem
            var iconBoxSize = 36.0f * scale;
            var iconBoxPos = pos + new Vector2(10.0f * scale, (cardHeight - iconBoxSize) * 0.5f);
            drawList.AddRectFilled(iconBoxPos, iconBoxPos + new Vector2(iconBoxSize, iconBoxSize), ImGui.ColorConvertFloat4ToU32(new Vector4(0.14f, 0.15f, 0.18f, 0.95f)), 6.0f * scale);
            drawList.AddRect(iconBoxPos, iconBoxPos + new Vector2(iconBoxSize, iconBoxSize), ImGui.ColorConvertFloat4ToU32(item != null ? rarityCol : ImGuiColors.DalamudGrey), 6.0f * scale, ImDrawFlags.None, 1.0f);

            ImGui.PushFont(UiBuilder.IconFont);
            var iconStr = slotIcon.ToIconString();
            var iconSize = ImGui.CalcTextSize(iconStr);
            drawList.AddText(iconBoxPos + new Vector2((iconBoxSize - iconSize.X) * 0.5f, (iconBoxSize - iconSize.Y) * 0.5f), ImGui.ColorConvertFloat4ToU32(item != null ? rarityCol : ImGuiColors.DalamudGrey), iconStr);
            ImGui.PopFont();

            // Slot Details
            ImGui.SetCursorScreenPos(pos + new Vector2(iconBoxSize + 18.0f * scale, 6.0f * scale));
            ImGui.BeginGroup();
            {
                // Slot Label
                string localizedSlot = GetLocalizedSlotName(slot);
                ImGui.TextColored(ImGuiColors.ParsedGold, localizedSlot);

                if (item != null)
                {
                    ImGui.SameLine(0, 6.0f * scale);
                    UiUtils.PillBadge(item.Rarity, new Vector4(rarityCol.X * 0.2f, rarityCol.Y * 0.2f, rarityCol.Z * 0.2f, 0.85f), rarityCol);

                    ImGui.TextColored(rarityCol, item.Name);

                    // Modifiers preview
                    if (item.StatModifiers != null && item.StatModifiers.Count > 0)
                    {
                        ImGui.SameLine(0, 8.0f * scale);
                        string modSummary = string.Join(", ", item.StatModifiers.Take(2).Select(kv => $"{(kv.Value >= 0 ? "+" : "")}{kv.Value} {kv.Key}"));
                        if (item.StatModifiers.Count > 2) modSummary += $" (+{item.StatModifiers.Count - 2})";
                        UiUtils.PillBadge(modSummary, new Vector4(0.15f, 0.30f, 0.20f, 0.85f), ImGuiColors.ParsedGreen, FontAwesomeIcon.Bolt);
                    }
                }
                else
                {
                    ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoAugmentationsEquipped"));
                }
            }
            ImGui.EndGroup();

            // Right action buttons
            float rightButtonsWidth = item != null ? (96.0f * scale) : (64.0f * scale);
            var rightBtnX = pos.X + width - rightButtonsWidth;
            ImGui.SetCursorScreenPos(new Vector2(rightBtnX, pos.Y + 12.0f * scale));

            if (item != null)
            {
                if (UiUtils.IconButton($"UnequipAug_{slot}", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("UninstallButton"), new Vector2(26, 26) * scale))
                {
                    sheet.UnequipAugmentation(slot);
                    CharacterSheet.SaveSheet(sheet);
                }

                ImGui.SameLine(0, 4.0f * scale);
                if (UiUtils.IconButton($"ChangeAug_{slot}", FontAwesomeIcon.ExchangeAlt, LocalizationManager.Instance.GetLocalizedString("ChooseGearTitle"), new Vector2(26, 26) * scale))
                {
                    equipModalSlot = slot;
                    showEquipModal = true;
                }

                ImGui.SameLine(0, 4.0f * scale);
                if (UiUtils.IconButton($"DeleteAug_{slot}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("DeleteButton"), new Vector2(26, 26) * scale))
                {
                    sheet.RemoveItem(item.Id);
                    CharacterSheet.SaveSheet(sheet);
                }
            }
            else
            {
                if (UiUtils.IconButton($"InstallAug_{slot}", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("InstallButton"), new Vector2(26, 26) * scale))
                {
                    equipModalSlot = slot;
                    showEquipModal = true;
                }
            }

            // Card click to select slot
            ImGui.SetCursorScreenPos(pos);
            if (ImGui.InvisibleButton($"##SelectAugSlot_{slot}", cardSize))
            {
                selectedSlot = slot;
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
                    UiUtils.DrawOrnamentalDivider(accentColor: ImGuiColors.ParsedBlue);
                    ImGui.Spacing();
                    DrawEquippedAugmentationsList(sheet);
                }
            }
        }

        private void DrawTotalStatBonuses(CharacterSheet sheet)
        {
            var scale = ImGuiHelpers.GlobalScale;
            UiUtils.DrawSectionHeader(
                LocalizationManager.Instance.GetLocalizedString("TotalAugmentationBonusesLabel"),
                FontAwesomeIcon.ChartLine,
                ImGuiColors.ParsedGreen);

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

            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.12f, 0.14f, 0.85f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(8f, 6f) * scale))
            using (var bonusCard = ImRaii.Child("##TotalAugModsPanel", new Vector2(0, 0), true, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
            {
                if (bonusCard.Success)
                {
                    foreach (var kv in totalBonuses)
                    {
                        string sign = kv.Value >= 0 ? "+" : "";
                        string chipText = $"{kv.Key} {sign}{kv.Value}";
                        var chipColor = kv.Value >= 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
                        var chipBg = kv.Value >= 0 ? new Vector4(0.14f, 0.32f, 0.20f, 0.85f) : new Vector4(0.38f, 0.14f, 0.14f, 0.85f);
                        UiUtils.PillBadge(chipText, chipBg, chipColor, FontAwesomeIcon.Bolt);
                        ImGui.SameLine(0, 6.0f * scale);
                    }
                    ImGui.NewLine();
                }
            }
        }

        private void DrawEquippedAugmentationsList(CharacterSheet sheet)
        {
            var scale = ImGuiHelpers.GlobalScale;
            UiUtils.DrawSectionHeader(
                LocalizationManager.Instance.GetLocalizedString("InstalledBadge"),
                FontAwesomeIcon.Microchip,
                ImGuiColors.ParsedBlue);

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
                var rarityCol = GetRarityColor(item.Rarity);
                var slotIcon = GetAugSlotIcon(slot);

                if (UiUtils.IconButton($"DelAugSide_{slot}_{item.Id}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("DeleteButton"), new Vector2(22, 22) * scale))
                {
                    sheet.RemoveItem(item.Id);
                    CharacterSheet.SaveSheet(sheet);
                }
                ImGui.SameLine(0, 6.0f * scale);
                UiUtils.PillBadge(GetLocalizedSlotName(slot), new Vector4(0.15f, 0.25f, 0.35f, 0.85f), ImGuiColors.ParsedBlue, slotIcon);
                ImGui.SameLine(0, 6.0f * scale);
                ImGui.TextColored(rarityCol, item.Name);

                if (item.StatModifiers != null && item.StatModifiers.Count > 0)
                {
                    string mods = item.GetFormattedModifiers();
                    ImGui.SameLine(0, 8.0f * scale);
                    UiUtils.PillBadge(mods, new Vector4(0.14f, 0.32f, 0.20f, 0.85f), ImGuiColors.ParsedGreen, FontAwesomeIcon.Bolt);
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
                    .Where(g => g.isAugmentation &&
                                (string.Equals(g.Slot, equipModalSlot, StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(g.Slot, "General", StringComparison.OrdinalIgnoreCase)
                                 || string.IsNullOrWhiteSpace(g.Slot)) &&
                                !sheet.IsAugmentationEquipped(g.Id))
                    .ToList();

                if (augsInInventory.Count == 0)
                {
                    augsInInventory = sheet.CharacterInventory
                        .OfType<GearItem>()
                        .Where(g => g.isAugmentation && !sheet.IsAugmentationEquipped(g.Id))
                        .ToList();
                }

                if (augsInInventory.Count == 0)
                {
                    ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoAugmentationsInInventory"));
                    ImGui.Spacing();
                    if (UiUtils.IconTextButton("CreateAugFromModalBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("CreateAugmentationModalTitle")))
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
                                if (UiUtils.IconTextButton($"BtnInstall_{item.Id}", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("InstallButton"), size: new Vector2(75.0f * ImGuiHelpers.GlobalScale, 22.0f * ImGuiHelpers.GlobalScale)))
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
                if (UiUtils.IconTextButton("CloseInstallModalBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CloseButton")))
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
                UiUtils.StyledInputText("NewAugName", ref creatingAug.name, 100, width: -1.0f);

                var slots = diceSystem?.GetEffectiveAugmentationSlots() ?? GearItem.StandardAugmentationSlots.ToList();
                var slotsArray = slots.ToArray();

                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("GearSlotLabel"));
                int slotIdx = Array.IndexOf(slotsArray, creatingAug.Slot);
                if (slotIdx < 0) slotIdx = 0;
                if (UiUtils.StyledCombo("##NewAugSlotCombo", ref slotIdx, slotsArray, icon: FontAwesomeIcon.Microchip, width: 200.0f))
                {
                    creatingAug.Slot = slotsArray[slotIdx];
                }

                ImGui.TextColored(ImGuiColors.DalamudGrey, "Rarity:");
                int rarityIdx = Array.IndexOf(rarities, creatingAug.Rarity);
                if (rarityIdx < 0) rarityIdx = 0;
                if (UiUtils.StyledCombo("##NewAugRarityCombo", ref rarityIdx, rarities, icon: FontAwesomeIcon.Gem, width: 200.0f))
                {
                    creatingAug.Rarity = rarities[rarityIdx];
                }

                ImGui.TextColored(ImGuiColors.DalamudGrey, "Description:");
                UiUtils.StyledInputMultiline("NewAugDesc", ref creatingAug.description, 500, new Vector2(-1.0f, 50.0f * ImGuiHelpers.GlobalScale));

                ImGui.Separator();
                creatingAug.isAugmentation = true;
                UiUtils.DrawStatModifierEditor(creatingAug, sheet, diceSystem, modEditorState, "CreateAug");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (UiUtils.IconTextButton("CreateInstallBtn", FontAwesomeIcon.Check, $"{LocalizationManager.Instance.GetLocalizedString("AddConfirmButton")} & {LocalizationManager.Instance.GetLocalizedString("InstallButton")}"))
                {
                    if (string.IsNullOrWhiteSpace(creatingAug.Name)) creatingAug.Name = "New Augmentation";
                    creatingAug.isAugmentation = true;
                    sheet.AddItem(creatingAug);
                    sheet.EquipAugmentation(creatingAug.Slot, creatingAug.Id);
                    CharacterSheet.SaveSheet(sheet);
                    showCreateAugModal = false;
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("CreateOnlyAugBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton")))
                {
                    if (string.IsNullOrWhiteSpace(creatingAug.Name)) creatingAug.Name = "New Augmentation";
                    creatingAug.isAugmentation = true;
                    sheet.AddItem(creatingAug);
                    CharacterSheet.SaveSheet(sheet);
                    showCreateAugModal = false;
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("CancelCreateAugBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton")))
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
