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
    internal class GearWindow
    {
        private readonly Plugin plugin;
        private readonly Configuration configuration;

        private string? selectedSlot = null;
        private bool showEquipModal = false;
        private string equipModalSlot = "Head";

        private bool showCreateGearModal = false;
        private GearItem creatingGear = new();
        private StatModifierEditorState modEditorState = new();

        private readonly string[] rarities = new[]
        {
            "Common", "Uncommon", "Rare", "Epic", "Legendary", "Artifact"
        };

        public GearWindow(Plugin _plugin)
        {
            plugin = _plugin;
            configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public void DrawGearTab()
        {
            var currentCharacter = CharacterManager.Instance.CharacterSheet;
            var currentDiceSystem = DiceSystemManager.Instance.CurrentDiceSystem;

            if (currentCharacter == null)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("NoCharLoadedGearMessage"));
                return;
            }

            DrawTopBar(currentCharacter);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            using (var table = ImRaii.Table("##GearLayoutTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Resizable))
            {
                if (table.Success)
                {
                    ImGui.TableSetupColumn("SlotsColumn", ImGuiTableColumnFlags.WidthStretch, 0.55f);
                    ImGui.TableSetupColumn("SummaryColumn", ImGuiTableColumnFlags.WidthStretch, 0.45f);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    DrawEquipmentSlots(currentCharacter, currentDiceSystem);

                    ImGui.TableNextColumn();
                    DrawSidePanel(currentCharacter);
                }
            }

            DrawModals(currentCharacter);
        }

        private void DrawTopBar(CharacterSheet sheet)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var equippedCount = sheet.GetEquippedGearItems().Count;
            var saveLabel = LocalizationManager.Instance.GetLocalizedString("SaveStatButton");
            var createLabel = LocalizationManager.Instance.GetLocalizedString("CreateGearModalTitle");

            UiUtils.DrawWindowHeroBanner(
                title: sheet.CharacterFullName,
                subtitle: LocalizationManager.Instance.GetLocalizedString("GearTab"),
                badgeText: $"{equippedCount} {LocalizationManager.Instance.GetLocalizedString("EquippedBadge")}",
                badgeColor: ImGuiColors.ParsedGreen,
                icon: FontAwesomeIcon.ShieldAlt,
                accentColor: ImGuiColors.ParsedGold,
                actionLabel: createLabel,
                onAction: () =>
                {
                    creatingGear = new GearItem("New Gear", "Head", "", "Common");
                    modEditorState = new StatModifierEditorState();
                    showCreateGearModal = true;
                });
        }

        private static FontAwesomeIcon GetSlotIcon(string slot)
        {
            return slot?.ToLowerInvariant() switch
            {
                "head" => FontAwesomeIcon.Crown,
                "body" or "chest" => FontAwesomeIcon.ShieldAlt,
                "hands" or "arms" => FontAwesomeIcon.HandRock,
                "legs" or "pants" => FontAwesomeIcon.Walking,
                "feet" or "boots" => FontAwesomeIcon.ShoePrints,
                "mainhand" or "weapon" => FontAwesomeIcon.Gavel,
                "offhand" or "shield" => FontAwesomeIcon.ShieldAlt,
                "ears" or "earring" => FontAwesomeIcon.Gem,
                "neck" or "necklace" => FontAwesomeIcon.Ring,
                "wrists" or "bracelets" => FontAwesomeIcon.CircleNotch,
                "finger" or "ring" or "ring1" or "ring2" or "leftring" or "rightring" => FontAwesomeIcon.Ring,
                _ => FontAwesomeIcon.ShieldAlt
            };
        }

        private void DrawEquipmentSlots(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            var slots = diceSystem?.GetEffectiveEquipmentSlots() ?? GearItem.StandardSlots.ToList();

            using (var child = ImRaii.Child("##EquipmentSlotsChild", new Vector2(0, 0), true))
            {
                if (child.Success)
                {
                    ImGui.TextColored(ImGuiColors.DalamudWhite, LocalizationManager.Instance.GetLocalizedString("GearTab"));
                    ImGui.Separator();
                    ImGui.Spacing();

                    var availWidth = ImGui.GetContentRegionAvail().X;

                    foreach (var slot in slots)
                    {
                        var equipped = sheet.GetEquippedGear(slot);
                        bool isSelected = string.Equals(selectedSlot, slot, StringComparison.OrdinalIgnoreCase);

                        ImGui.PushID($"SlotRow_{slot}");
                        DrawSlotCard(sheet, slot, equipped, isSelected, availWidth);
                        ImGui.PopID();

                        ImGui.Spacing();
                    }
                }
            }
        }

        private void DrawSlotCard(CharacterSheet sheet, string slot, GearItem? item, bool isSelected, float width)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var pos = ImGui.GetCursorScreenPos();
            var cardHeight = 52.0f * scale;
            var cardSize = new Vector2(width, cardHeight);

            var drawList = ImGui.GetWindowDrawList();
            bool isHovered = ImGui.IsMouseHoveringRect(pos, pos + cardSize);
            var rarityCol = item != null ? GetRarityColor(item.Rarity) : ImGuiColors.DalamudGrey;
            var slotIcon = GetSlotIcon(slot);

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
                        string modSummary = item.GetFormattedModifiers();
                        if (modSummary.Length > 25) modSummary = modSummary.Substring(0, 22) + "...";
                        UiUtils.PillBadge(modSummary, new Vector4(0.15f, 0.30f, 0.20f, 0.85f), ImGuiColors.ParsedGreen, FontAwesomeIcon.Bolt);
                    }
                }
                else
                {
                    ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoGearEquipped"));
                }
            }
            ImGui.EndGroup();

            // Right side buttons
            float rightButtonsWidth = item != null ? (96.0f * scale) : (64.0f * scale);
            var rightBtnX = pos.X + width - rightButtonsWidth;
            ImGui.SetCursorScreenPos(new Vector2(rightBtnX, pos.Y + 12.0f * scale));

            if (item != null)
            {
                if (UiUtils.IconButton($"Unequip_{slot}", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("UnequipButton"), new Vector2(26, 26) * scale))
                {
                    sheet.UnequipGear(slot);
                    CharacterSheet.SaveSheet(sheet);
                }

                ImGui.SameLine(0, 4.0f * scale);
                if (UiUtils.IconButton($"Change_{slot}", FontAwesomeIcon.ExchangeAlt, LocalizationManager.Instance.GetLocalizedString("ChooseGearTitle"), new Vector2(26, 26) * scale))
                {
                    equipModalSlot = slot;
                    showEquipModal = true;
                }

                ImGui.SameLine(0, 4.0f * scale);
                if (UiUtils.IconButton($"Delete_{slot}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("DeleteButton"), new Vector2(26, 26) * scale))
                {
                    sheet.RemoveItem(item.Id);
                    CharacterSheet.SaveSheet(sheet);
                }
            }
            else
            {
                if (UiUtils.IconButton($"Equip_{slot}", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("EquipButton"), new Vector2(26, 26) * scale))
                {
                    equipModalSlot = slot;
                    showEquipModal = true;
                }
            }

            // Card click to select slot
            ImGui.SetCursorScreenPos(pos);
            if (ImGui.InvisibleButton($"##SelectSlot_{slot}", cardSize))
            {
                selectedSlot = slot;
            }

            ImGui.SetCursorScreenPos(new Vector2(pos.X, pos.Y + cardHeight));
        }

        private void DrawSidePanel(CharacterSheet sheet)
        {
            var scale = ImGuiHelpers.GlobalScale;
            using (var child = ImRaii.Child("##GearSidePanel", new Vector2(0, 0), true))
            {
                if (child.Success)
                {
                    // Section 1: Total Stat Alterations
                    UiUtils.DrawSectionHeader(
                        LocalizationManager.Instance.GetLocalizedString("TotalGearBonusesLabel"),
                        FontAwesomeIcon.ChartLine,
                        ImGuiColors.ParsedGreen);

                    var totalBonuses = sheet.GetAllGearStatBonuses();
                    if (totalBonuses.Count == 0)
                    {
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoGearEquipped"));
                    }
                    else
                    {
                        using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.12f, 0.14f, 0.85f)))
                        using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * scale))
                        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(8f, 6f) * scale))
                        using (var bonusCard = ImRaii.Child("##TotalModsPanel", new Vector2(0, 0), true, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
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

                    ImGui.Spacing();
                    UiUtils.DrawOrnamentalDivider(accentColor: ImGuiColors.ParsedGold);
                    ImGui.Spacing();

                    // Section 2: Selected Gear Item Inspector
                    if (!string.IsNullOrEmpty(selectedSlot))
                    {
                        var equipped = sheet.GetEquippedGear(selectedSlot);
                        string localizedSlot = GetLocalizedSlotName(selectedSlot);
                        var slotIcon = GetSlotIcon(selectedSlot);

                        UiUtils.DrawSectionHeader(
                            $"{localizedSlot} - Details",
                            slotIcon,
                            ImGuiColors.ParsedGold);

                        if (equipped != null)
                        {
                            var rarityCol = GetRarityColor(equipped.Rarity);

                            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.11f, 0.14f, 0.90f)))
                            using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(rarityCol.X, rarityCol.Y, rarityCol.Z, 0.65f)))
                            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * scale))
                            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10f, 8f) * scale))
                            using (var itemCard = ImRaii.Child("##SelectedItemCard", new Vector2(0, 0), true, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
                            {
                                if (itemCard.Success)
                                {
                                    ImGui.TextColored(rarityCol, equipped.Name);
                                    ImGui.SameLine(0, 8.0f * scale);
                                    UiUtils.PillBadge(equipped.Rarity, new Vector4(rarityCol.X * 0.2f, rarityCol.Y * 0.2f, rarityCol.Z * 0.2f, 0.85f), rarityCol);

                                    if (!string.IsNullOrWhiteSpace(equipped.Description))
                                    {
                                        ImGui.Spacing();
                                        ImGui.TextWrapped(equipped.Description);
                                    }

                                    if (!string.IsNullOrWhiteSpace(equipped.Effect))
                                    {
                                        ImGui.Spacing();
                                        UiUtils.PillBadge($"Effect: {equipped.Effect}", new Vector4(0.15f, 0.25f, 0.40f, 0.85f), ImGuiColors.TankBlue, FontAwesomeIcon.Magic);
                                    }

                                    if (equipped.MaxDurability > 0)
                                    {
                                        ImGui.Spacing();
                                        string durOverlay = $"{LocalizationManager.Instance.GetLocalizedString("DurabilityLabel")}: {equipped.Durability} / {equipped.MaxDurability}";
                                        UiUtils.DrawProgressBar(equipped.Durability, equipped.MaxDurability, durOverlay, new Vector2(-1.0f, 16.0f * scale), new Vector4(0.30f, 0.55f, 0.80f, 0.9f));
                                    }

                                    if (equipped.StatModifiers != null && equipped.StatModifiers.Count > 0)
                                    {
                                        ImGui.Spacing();
                                        ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("StatModifiersLabel"));
                                        ImGui.Spacing();
                                        foreach (var mod in equipped.StatModifiers)
                                        {
                                            string sign = mod.Value >= 0 ? "+" : "";
                                            string modText = $"{sign}{mod.Value} {mod.Key}";
                                            UiUtils.PillBadge(modText, new Vector4(0.14f, 0.32f, 0.20f, 0.85f), ImGuiColors.ParsedGreen, FontAwesomeIcon.Bolt);
                                            ImGui.SameLine(0, 4.0f * scale);
                                        }
                                        ImGui.NewLine();
                                    }

                                    ImGui.Spacing();
                                    if (UiUtils.IconTextButton("InspectUnequipBtn", FontAwesomeIcon.SignOutAlt, LocalizationManager.Instance.GetLocalizedString("UnequipButton")))
                                    {
                                        sheet.UnequipGear(selectedSlot);
                                        CharacterSheet.SaveSheet(sheet);
                                    }
                                    ImGui.SameLine(0, 8.0f * scale);
                                    if (UiUtils.IconTextButton("InspectDeleteBtn", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("DeleteButton")))
                                    {
                                        sheet.RemoveItem(equipped.Id);
                                        CharacterSheet.SaveSheet(sheet);
                                    }
                                }
                            }
                        }
                        else
                        {
                            ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoGearEquipped"));
                            ImGui.Spacing();
                            if (UiUtils.IconTextButton("InspectEquipBtn", FontAwesomeIcon.ShieldAlt, LocalizationManager.Instance.GetLocalizedString("EquipButton")))
                            {
                                equipModalSlot = selectedSlot;
                                showEquipModal = true;
                            }
                        }
                    }
                    else
                    {
                        ImGui.TextDisabled("Select an equipment slot on the left to inspect details.");
                    }
                }
            }
        }

        private void DrawModals(CharacterSheet sheet)
        {
            DrawEquipModal(sheet);
            DrawCreateGearModal(sheet);
        }

        private void DrawEquipModal(CharacterSheet sheet)
        {
            if (!showEquipModal) return;

            string localizedSlot = GetLocalizedSlotName(equipModalSlot);
            string title = $"{LocalizationManager.Instance.GetLocalizedString("ChooseGearTitle")} - {localizedSlot}###ChooseGearModal";

            ImGui.SetNextWindowSize(new Vector2(460.0f, 380.0f) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);
            if (ImGui.Begin(title, ref showEquipModal, ImGuiWindowFlags.NoCollapse))
            {
                var gearInInventory = sheet.CharacterInventory
                    .OfType<GearItem>()
                    .Where(g => !g.isAugmentation && (string.Equals(g.Slot, equipModalSlot, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(g.Slot, "General", StringComparison.OrdinalIgnoreCase)
                             || string.IsNullOrWhiteSpace(g.Slot)))
                    .ToList();

                if (gearInInventory.Count == 0)
                {
                    // Also show any gear in inventory if none match specifically
                    gearInInventory = sheet.CharacterInventory.OfType<GearItem>().Where(g => !g.isAugmentation).ToList();
                }

                if (gearInInventory.Count == 0)
                {
                    ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoGearInInventory"));
                    ImGui.Spacing();
                    if (UiUtils.IconTextButton("CreateGearFromModalBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("CreateGearModalTitle")))
                    {
                        creatingGear = new GearItem("New Gear", equipModalSlot, "", "Common");
                        modEditorState = new StatModifierEditorState();
                        showCreateGearModal = true;
                    }
                }
                else
                {
                    using (var listChild = ImRaii.Child("##EquipGearListChild", new Vector2(0, -36.0f * ImGuiHelpers.GlobalScale), true))
                    {
                        if (listChild.Success)
                        {
                            foreach (var gear in gearInInventory)
                            {
                                ImGui.PushID($"EquipChoice_{gear.Id}");

                                bool isCurrentlyEquipped = sheet.IsItemEquipped(gear.Id);
                                var rarityCol = GetRarityColor(gear.Rarity);

                                ImGui.TextColored(rarityCol, gear.Name);
                                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                                UiUtils.Badge(gear.Slot, new Vector4(0.2f, 0.2f, 0.3f, 0.7f), ImGuiColors.ParsedBlue);

                                if (isCurrentlyEquipped)
                                {
                                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("EquippedBadge"), new Vector4(0.18f, 0.35f, 0.22f, 0.8f), ImGuiColors.ParsedGreen);
                                }

                                if (gear.StatModifiers != null && gear.StatModifiers.Count > 0)
                                {
                                    ImGui.TextColored(ImGuiColors.ParsedGreen, gear.GetFormattedModifiers());
                                }

                                ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - 70.0f * ImGuiHelpers.GlobalScale);
                                if (!isCurrentlyEquipped)
                                {
                                    if (UiUtils.IconTextButton($"Btn_{gear.Id}", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("EquipButton")))
                                    {
                                        sheet.EquipGear(equipModalSlot, gear.Id);
                                        CharacterSheet.SaveSheet(sheet);
                                        showEquipModal = false;
                                    }
                                }
                                else
                                {
                                    if (UiUtils.IconTextButton($"Btn_{gear.Id}", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("UnequipButton")))
                                    {
                                        sheet.UnequipItem(gear.Id);
                                        CharacterSheet.SaveSheet(sheet);
                                    }
                                }

                                ImGui.Separator();
                                ImGui.PopID();
                            }
                        }
                    }
                }

                ImGui.Spacing();
                if (UiUtils.IconTextButton("CloseEquipModalBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CloseButton")))
                {
                    showEquipModal = false;
                }
            }
            ImGui.End();
        }

        private void DrawCreateGearModal(CharacterSheet sheet)
        {
            if (!showCreateGearModal) return;

            string title = $"{LocalizationManager.Instance.GetLocalizedString("CreateGearModalTitle")}###CreateGearModal";
            ImGui.SetNextWindowSize(new Vector2(480.0f, 480.0f) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);

            if (ImGui.Begin(title, ref showCreateGearModal, ImGuiWindowFlags.NoCollapse))
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, "Name:");
                UiUtils.StyledInputText("NewGearName", ref creatingGear.name, 100, width: -1.0f);

                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("GearSlotLabel"));
                int slotIdx = Array.IndexOf(GearItem.StandardSlots, creatingGear.Slot);
                if (slotIdx < 0) slotIdx = 0;
                if (UiUtils.StyledCombo("##NewGearSlotCombo", ref slotIdx, GearItem.StandardSlots, icon: FontAwesomeIcon.ShieldAlt, width: 200.0f))
                {
                    creatingGear.Slot = GearItem.StandardSlots[slotIdx];
                }

                ImGui.TextColored(ImGuiColors.DalamudGrey, "Rarity:");
                int rarityIdx = Array.IndexOf(rarities, creatingGear.Rarity);
                if (rarityIdx < 0) rarityIdx = 0;
                if (UiUtils.StyledCombo("##NewGearRarityCombo", ref rarityIdx, rarities, icon: FontAwesomeIcon.Gem, width: 200.0f))
                {
                    creatingGear.Rarity = rarities[rarityIdx];
                }

                ImGui.TextColored(ImGuiColors.DalamudGrey, "Description:");
                UiUtils.StyledInputMultiline("NewGearDesc", ref creatingGear.description, 500, new Vector2(-1.0f, 50.0f * ImGuiHelpers.GlobalScale));

                ImGui.Separator();
                UiUtils.DrawStatModifierEditor(creatingGear, sheet, DiceSystemManager.Instance.CurrentDiceSystem, modEditorState, "CreateGear");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (UiUtils.IconTextButton("CreateEquipBtn", FontAwesomeIcon.Check, $"{LocalizationManager.Instance.GetLocalizedString("AddConfirmButton")} & {LocalizationManager.Instance.GetLocalizedString("EquipButton")}"))
                {
                    if (string.IsNullOrWhiteSpace(creatingGear.Name)) creatingGear.Name = "New Gear";
                    sheet.AddItem(creatingGear);
                    sheet.EquipGear(creatingGear.Slot, creatingGear.Id);
                    CharacterSheet.SaveSheet(sheet);
                    showCreateGearModal = false;
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("CreateOnlyBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton")))
                {
                    if (string.IsNullOrWhiteSpace(creatingGear.Name)) creatingGear.Name = "New Gear";
                    sheet.AddItem(creatingGear);
                    CharacterSheet.SaveSheet(sheet);
                    showCreateGearModal = false;
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("CancelCreateGearBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton")))
                {
                    showCreateGearModal = false;
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
                "Artifact" => ImGuiColors.ParsedPink,
                _ => ImGuiColors.DalamudWhite
            };
        }
    }
}
