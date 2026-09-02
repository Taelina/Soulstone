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
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.ShieldAlt.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            ImGui.TextColored(ImGuiColors.ParsedGold, sheet.CharacterFullName);

            var equippedCount = sheet.GetEquippedGearItems().Count;
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
            UiUtils.Badge($"{equippedCount} {LocalizationManager.Instance.GetLocalizedString("EquippedBadge")}", new Vector4(0.18f, 0.35f, 0.22f, 0.8f), ImGuiColors.ParsedGreen);

            var saveLabel = LocalizationManager.Instance.GetLocalizedString("SaveStatButton");
            var createLabel = LocalizationManager.Instance.GetLocalizedString("CreateGearModalTitle");
            var createWidth = ImGui.CalcTextSize(createLabel).X + 24.0f * ImGuiHelpers.GlobalScale;
            var saveWidth = 28.0f * ImGuiHelpers.GlobalScale;
            var totalRightWidth = createWidth + saveWidth + 8.0f * ImGuiHelpers.GlobalScale;
            var rightX = ImGui.GetWindowContentRegionMax().X - totalRightWidth;

            if (ImGui.GetCursorPosX() < rightX)
                ImGui.SameLine(rightX);
            else
                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

            if (ImGui.Button($"{createLabel}###CreateGearTopBtn"))
            {
                creatingGear = new GearItem("New Gear", "Head", "", "Common");
                modEditorState = new StatModifierEditorState();
                showCreateGearModal = true;
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconButton("SaveGearBtn", FontAwesomeIcon.Save, saveLabel))
            {
                CharacterSheet.SaveSheet(sheet);
            }
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
            var pos = ImGui.GetCursorScreenPos();
            var cardHeight = 46.0f * ImGuiHelpers.GlobalScale;
            var cardSize = new Vector2(width, cardHeight);

            var drawList = ImGui.GetWindowDrawList();
            bool isHovered = ImGui.IsMouseHoveringRect(pos, pos + cardSize);

            var bgCol = isSelected
                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.28f, 0.38f, 0.75f))
                : (isHovered
                    ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.22f, 0.28f, 0.65f))
                    : ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.14f, 0.18f, 0.50f)));

            var borderCol = isSelected
                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.85f, 0.75f, 0.35f, 0.90f))
                : (isHovered
                    ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.60f, 0.60f, 0.60f, 0.60f))
                    : ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.28f, 0.35f, 0.40f)));

            drawList.AddRectFilled(pos, pos + cardSize, bgCol, 4.0f * ImGuiHelpers.GlobalScale);
            drawList.AddRect(pos, pos + cardSize, borderCol, 4.0f * ImGuiHelpers.GlobalScale, ImDrawFlags.None, isSelected ? 2.0f : 1.0f);

            ImGui.SetCursorScreenPos(pos + new Vector2(8.0f * ImGuiHelpers.GlobalScale, 6.0f * ImGuiHelpers.GlobalScale));

            // Slot label
            string localizedSlot = GetLocalizedSlotName(slot);
            ImGui.TextColored(ImGuiColors.ParsedGold, localizedSlot);

            ImGui.SetCursorScreenPos(pos + new Vector2(8.0f * ImGuiHelpers.GlobalScale, 22.0f * ImGuiHelpers.GlobalScale));

            if (item != null)
            {
                var rarityCol = GetRarityColor(item.Rarity);
                ImGui.TextColored(rarityCol, item.Name);

                // Modifiers preview
                if (item.StatModifiers != null && item.StatModifiers.Count > 0)
                {
                    ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                    string modSummary = item.GetFormattedModifiers();
                    if (modSummary.Length > 30) modSummary = modSummary.Substring(0, 27) + "...";
                    UiUtils.Badge(modSummary, new Vector4(0.15f, 0.30f, 0.20f, 0.75f), ImGuiColors.ParsedGreen);
                }
            }
            else
            {
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoGearEquipped"));
            }

            // Right side buttons
            float rightButtonsWidth = item != null ? (80.0f * ImGuiHelpers.GlobalScale) : (60.0f * ImGuiHelpers.GlobalScale);
            var rightBtnX = pos.X + width - rightButtonsWidth;
            ImGui.SetCursorScreenPos(new Vector2(rightBtnX, pos.Y + 10.0f * ImGuiHelpers.GlobalScale));

            if (item != null)
            {
                if (UiUtils.IconButton($"Unequip_{slot}", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("UnequipButton"), new Vector2(24, 24) * ImGuiHelpers.GlobalScale))
                {
                    sheet.UnequipGear(slot);
                    CharacterSheet.SaveSheet(sheet);
                }

                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconButton($"Change_{slot}", FontAwesomeIcon.ExchangeAlt, LocalizationManager.Instance.GetLocalizedString("ChooseGearTitle"), new Vector2(24, 24) * ImGuiHelpers.GlobalScale))
                {
                    equipModalSlot = slot;
                    showEquipModal = true;
                }
            }
            else
            {
                if (UiUtils.IconButton($"Equip_{slot}", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("EquipButton"), new Vector2(24, 24) * ImGuiHelpers.GlobalScale))
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
            using (var child = ImRaii.Child("##GearSidePanel", new Vector2(0, 0), true))
            {
                if (child.Success)
                {
                    // Section 1: Total Stat Alterations
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGreen, FontAwesomeIcon.ChartLine.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("TotalGearBonusesLabel"));
                    ImGui.Separator();
                    ImGui.Spacing();

                    var totalBonuses = sheet.GetAllGearStatBonuses();
                    if (totalBonuses.Count == 0)
                    {
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoGearEquipped"));
                    }
                    else
                    {
                        using (var modTable = ImRaii.Table("##TotalModsTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
                        {
                            if (modTable.Success)
                            {
                                ImGui.TableSetupColumn("Stat", ImGuiTableColumnFlags.WidthStretch, 0.6f);
                                ImGui.TableSetupColumn("Bonus", ImGuiTableColumnFlags.WidthStretch, 0.4f);

                                foreach (var kv in totalBonuses)
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.TextColored(ImGuiColors.DalamudWhite, kv.Key);

                                    ImGui.TableNextColumn();
                                    string modText = $"{(kv.Value >= 0 ? "+" : "")}{kv.Value}";
                                    var modColor = kv.Value >= 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
                                    ImGui.TextColored(modColor, modText);
                                }
                            }
                        }
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    // Section 2: Selected Gear Item Inspector
                    if (!string.IsNullOrEmpty(selectedSlot))
                    {
                        var equipped = sheet.GetEquippedGear(selectedSlot);
                        string localizedSlot = GetLocalizedSlotName(selectedSlot);
                        ImGui.TextColored(ImGuiColors.ParsedGold, $"{localizedSlot} - Details");
                        ImGui.Separator();
                        ImGui.Spacing();

                        if (equipped != null)
                        {
                            var rarityCol = GetRarityColor(equipped.Rarity);
                            ImGui.TextColored(rarityCol, equipped.Name);
                            ImGui.SameLine();
                            UiUtils.Badge(equipped.Rarity, new Vector4(rarityCol.X * 0.25f, rarityCol.Y * 0.25f, rarityCol.Z * 0.25f, 0.8f), rarityCol);

                            if (!string.IsNullOrWhiteSpace(equipped.Description))
                            {
                                ImGui.Spacing();
                                ImGui.TextWrapped(equipped.Description);
                            }

                            if (!string.IsNullOrWhiteSpace(equipped.Effect))
                            {
                                ImGui.Spacing();
                                ImGui.TextColored(ImGuiColors.TankBlue, $"Effect: {equipped.Effect}");
                            }

                            ImGui.Spacing();
                            ImGui.TextColored(ImGuiColors.DalamudGrey, $"{LocalizationManager.Instance.GetLocalizedString("DurabilityLabel")} {equipped.Durability} / {equipped.MaxDurability}");

                            if (equipped.StatModifiers != null && equipped.StatModifiers.Count > 0)
                            {
                                ImGui.Spacing();
                                ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("StatModifiersLabel"));
                                foreach (var mod in equipped.StatModifiers)
                                {
                                    string sign = mod.Value >= 0 ? "+" : "";
                                    ImGui.BulletText($"{sign}{mod.Value} {mod.Key}");
                                }
                            }

                            ImGui.Spacing();
                            if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("UnequipButton")}###InspectUnequipBtn"))
                            {
                                sheet.UnequipGear(selectedSlot);
                                CharacterSheet.SaveSheet(sheet);
                            }
                        }
                        else
                        {
                            ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoGearEquipped"));
                            ImGui.Spacing();
                            if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("EquipButton")}###InspectEquipBtn"))
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
                    .Where(g => string.Equals(g.Slot, equipModalSlot, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(g.Slot, "General", StringComparison.OrdinalIgnoreCase)
                             || string.IsNullOrWhiteSpace(g.Slot))
                    .ToList();

                if (gearInInventory.Count == 0)
                {
                    // Also show any gear in inventory if none match specifically
                    gearInInventory = sheet.CharacterInventory.OfType<GearItem>().ToList();
                }

                if (gearInInventory.Count == 0)
                {
                    ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoGearInInventory"));
                    ImGui.Spacing();
                    if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("CreateGearModalTitle")}###CreateGearFromModalBtn"))
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
                                    if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("EquipButton")}###Btn_{gear.Id}"))
                                    {
                                        sheet.EquipGear(equipModalSlot, gear.Id);
                                        CharacterSheet.SaveSheet(sheet);
                                        showEquipModal = false;
                                    }
                                }
                                else
                                {
                                    if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("UnequipButton")}###Btn_{gear.Id}"))
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
                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("CloseButton")}###CloseEquipModalBtn"))
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
                ImGui.SetNextItemWidth(-1.0f);
                ImGui.InputText("##NewGearName", ref creatingGear.name, 100);

                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("GearSlotLabel"));
                ImGui.SetNextItemWidth(200.0f * ImGuiHelpers.GlobalScale);
                int slotIdx = Array.IndexOf(GearItem.StandardSlots, creatingGear.Slot);
                if (slotIdx < 0) slotIdx = 0;
                if (ImGui.Combo("##NewGearSlotCombo", ref slotIdx, GearItem.StandardSlots))
                {
                    creatingGear.Slot = GearItem.StandardSlots[slotIdx];
                }

                ImGui.TextColored(ImGuiColors.DalamudGrey, "Rarity:");
                ImGui.SetNextItemWidth(200.0f * ImGuiHelpers.GlobalScale);
                int rarityIdx = Array.IndexOf(rarities, creatingGear.Rarity);
                if (rarityIdx < 0) rarityIdx = 0;
                if (ImGui.Combo("##NewGearRarityCombo", ref rarityIdx, rarities))
                {
                    creatingGear.Rarity = rarities[rarityIdx];
                }

                ImGui.TextColored(ImGuiColors.DalamudGrey, "Description:");
                ImGui.SetNextItemWidth(-1.0f);
                ImGui.InputTextMultiline("##NewGearDesc", ref creatingGear.description, 500, new Vector2(-1.0f, 50.0f * ImGuiHelpers.GlobalScale));

                ImGui.Separator();
                UiUtils.DrawStatModifierEditor(creatingGear, sheet, DiceSystemManager.Instance.CurrentDiceSystem, modEditorState, "CreateGear");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddConfirmButton")} & {LocalizationManager.Instance.GetLocalizedString("EquipButton")}###CreateEquipBtn"))
                {
                    if (string.IsNullOrWhiteSpace(creatingGear.Name)) creatingGear.Name = "New Gear";
                    sheet.AddItem(creatingGear);
                    sheet.EquipGear(creatingGear.Slot, creatingGear.Id);
                    CharacterSheet.SaveSheet(sheet);
                    showCreateGearModal = false;
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddConfirmButton")}###CreateOnlyBtn"))
                {
                    if (string.IsNullOrWhiteSpace(creatingGear.Name)) creatingGear.Name = "New Gear";
                    sheet.AddItem(creatingGear);
                    CharacterSheet.SaveSheet(sheet);
                    showCreateGearModal = false;
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("CancelButton")}###CancelCreateGearBtn"))
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
