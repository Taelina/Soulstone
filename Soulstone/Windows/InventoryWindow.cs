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
    internal class InventoryWindow
    {
        private readonly Plugin plugin;
        private readonly Configuration configuration;

        private string searchQuery = string.Empty;
        private string selectedTypeFilter = "All";
        private int selectedSortIndex = 0; // 0: Name, 1: Type, 2: Quantity, 3: Rarity
        private string? selectedItemId = null;

        // Modal states
        private bool showCreateEditModal = false;
        private bool isEditingExistingItem = false;
        private Item editingItem = new();
        private bool isItemGear = false;
        private StatModifierEditorState invModEditorState = new();
        private string newPropKey = string.Empty;
        private string newPropValue = string.Empty;

        private bool showManageTypesModal = false;
        private string newCustomTypeName = string.Empty;

        private bool showDeleteConfirmModal = false;
        private Item? itemToDelete = null;

        private bool showImportModal = false;
        private string importRawText = string.Empty;
        private string importStatusMessage = string.Empty;
        private bool importStatusIsError = false;

        private readonly string[] standardItemTypes = new[]
        {
            "General", "Consumable", "Weapon", "Armor", "Accessory", "Material", "Key Item", "Quest", "Miscellaneous"
        };

        private readonly string[] rarities = new[]
        {
            "Common", "Uncommon", "Rare", "Epic", "Legendary", "Artifact"
        };

        private readonly string[] sortOptions = new[]
        {
            "Name", "Type", "Quantity", "Rarity"
        };

        public InventoryWindow(Plugin _plugin)
        {
            plugin = _plugin;
            configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public void DrawInventoryTab()
        {
            var currentCharacter = CharacterManager.Instance.CharacterSheet;
            var currentDiceSystem = DiceSystemManager.Instance.CurrentDiceSystem;

            if (currentCharacter == null)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("NoCharLoadedInventoryMessage"));
                return;
            }

            DrawTopBar(currentCharacter, currentDiceSystem);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            DrawFilterBar(currentCharacter);
            ImGui.Spacing();

            using (var table = ImRaii.Table("##InventoryColumns", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Resizable))
            {
                if (table.Success)
                {
                    ImGui.TableSetupColumn("ItemList", ImGuiTableColumnFlags.WidthStretch, 0.48f);
                    ImGui.TableSetupColumn("ItemDetail", ImGuiTableColumnFlags.WidthStretch, 0.52f);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    DrawItemList(currentCharacter);

                    ImGui.TableNextColumn();
                    DrawItemDetail(currentCharacter);
                }
            }

            DrawModals(currentCharacter);
        }

        private void DrawTopBar(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            // Action Buttons
            if (UiUtils.IconTextButton("AddItemBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("InventoryAddItem")))
            {
                editingItem = new Item();
                isEditingExistingItem = false;
                isItemGear = false;
                newPropKey = string.Empty;
                newPropValue = string.Empty;
                invModEditorState = new StatModifierEditorState();
                showCreateEditModal = true;
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconTextButton("ManageTypesBtn", FontAwesomeIcon.Tags, LocalizationManager.Instance.GetLocalizedString("InventoryManageTypes")))
            {
                newCustomTypeName = string.Empty;
                showManageTypesModal = true;
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconTextButton("ImportItemsBtn", FontAwesomeIcon.FileImport, LocalizationManager.Instance.GetLocalizedString("InventoryImportJson")))
            {
                showImportModal = true;
                importRawText = string.Empty;
                importStatusMessage = string.Empty;
                importStatusIsError = false;
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconButton("SaveInventoryBtn", FontAwesomeIcon.Save, LocalizationManager.Instance.GetLocalizedString("SaveCharsheetButton")))
            {
                CharacterSheet.SaveSheet(sheet);
            }

            // Right side: Capacity Display
            var capacity = sheet.GetEffectiveInventoryCapacity(diceSystem);
            var used = sheet.CharacterInventory.Count;

            var rightText = capacity > 0 
                ? string.Format(LocalizationManager.Instance.GetLocalizedString("InventorySlotsUsed"), used, capacity)
                : $"{LocalizationManager.Instance.GetLocalizedString("InventoryCapacityLabel")} {used} ({LocalizationManager.Instance.GetLocalizedString("InventoryUnlimited")})";

            var badgeWidth = ImGui.CalcTextSize(rightText).X + 24.0f * ImGuiHelpers.GlobalScale;
            var currentX = ImGui.GetCursorPosX();
            var availWidth = ImGui.GetContentRegionAvail().X;

            if (availWidth > badgeWidth + 8.0f * ImGuiHelpers.GlobalScale)
            {
                ImGui.SameLine(0, 0);
                ImGui.SetCursorPosX(currentX + availWidth - badgeWidth);
            }
            else
            {
                ImGui.NewLine();
            }

            if (capacity > 0)
            {
                var ratio = (float)used / capacity;
                var badgeBg = ratio >= 1.0f ? new Vector4(0.8f, 0.2f, 0.2f, 0.4f) :
                              ratio >= 0.8f ? new Vector4(0.8f, 0.5f, 0.1f, 0.4f) :
                                             new Vector4(0.2f, 0.5f, 0.3f, 0.4f);
                var badgeTextCol = ratio >= 1.0f ? ImGuiColors.DPSRed :
                                   ratio >= 0.8f ? ImGuiColors.DalamudOrange :
                                                   ImGuiColors.ParsedGreen;
                UiUtils.Badge(rightText, badgeBg, badgeTextCol);
            }
            else
            {
                UiUtils.Badge(rightText, new Vector4(0.2f, 0.3f, 0.5f, 0.4f), ImGuiColors.ParsedBlue);
            }
        }

        private void DrawFilterBar(CharacterSheet sheet)
        {
            // Search input
            UiUtils.DrawSearchInput("InvSearch", ref searchQuery, LocalizationManager.Instance.GetLocalizedString("InventorySearchPlaceholder"), 160.0f);

            // Type filter
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
            ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryFilterType"));
            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);

            var allTypes = GetAllAvailableTypes(sheet);
            var filterOptions = new List<string> { "All" };
            filterOptions.AddRange(allTypes);

            var currentFilterIdx = Math.Max(0, filterOptions.IndexOf(selectedTypeFilter));
            var localizedFilterOptions = filterOptions.Select(GetLocalizedItemType).ToArray();

            ImGui.SetNextItemWidth(130.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.StyledCombo("##TypeFilterCombo", ref currentFilterIdx, localizedFilterOptions, icon: FontAwesomeIcon.Filter, width: 130.0f))
            {
                selectedTypeFilter = filterOptions[currentFilterIdx];
            }

            // Sort combo
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
            ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventorySortBy"));
            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            var localizedSortOptions = sortOptions.Select(GetLocalizedSortOption).ToArray();
            UiUtils.StyledCombo("##SortCombo", ref selectedSortIndex, localizedSortOptions, icon: FontAwesomeIcon.SortAmountDown, width: 110.0f);
        }

        private string GetLocalizedItemType(string itemType)
        {
            return itemType switch
            {
                "All" => LocalizationManager.Instance.GetLocalizedString("InventoryFilterAll"),
                "General" => LocalizationManager.Instance.GetLocalizedString("ItemTypeGeneral"),
                "Consumable" => LocalizationManager.Instance.GetLocalizedString("ItemTypeConsumable"),
                "Weapon" => LocalizationManager.Instance.GetLocalizedString("ItemTypeWeapon"),
                "Armor" => LocalizationManager.Instance.GetLocalizedString("ItemTypeArmor"),
                "Accessory" => LocalizationManager.Instance.GetLocalizedString("ItemTypeAccessory"),
                "Material" => LocalizationManager.Instance.GetLocalizedString("ItemTypeMaterial"),
                "Key Item" => LocalizationManager.Instance.GetLocalizedString("ItemTypeKeyItem"),
                "Quest" => LocalizationManager.Instance.GetLocalizedString("ItemTypeQuest"),
                "Miscellaneous" => LocalizationManager.Instance.GetLocalizedString("ItemTypeMiscellaneous"),
                _ => itemType
            };
        }

        private string GetLocalizedRarity(string rarity)
        {
            return rarity switch
            {
                "Common" => LocalizationManager.Instance.GetLocalizedString("RarityCommon"),
                "Uncommon" => LocalizationManager.Instance.GetLocalizedString("RarityUncommon"),
                "Rare" => LocalizationManager.Instance.GetLocalizedString("RarityRare"),
                "Epic" => LocalizationManager.Instance.GetLocalizedString("RarityEpic"),
                "Legendary" => LocalizationManager.Instance.GetLocalizedString("RarityLegendary"),
                "Artifact" => LocalizationManager.Instance.GetLocalizedString("RarityArtifact"),
                _ => rarity
            };
        }

        private string GetLocalizedSortOption(string sortOption)
        {
            return sortOption switch
            {
                "Name" => LocalizationManager.Instance.GetLocalizedString("InventorySortName"),
                "Type" => LocalizationManager.Instance.GetLocalizedString("InventorySortType"),
                "Quantity" => LocalizationManager.Instance.GetLocalizedString("InventorySortQty"),
                "Rarity" => LocalizationManager.Instance.GetLocalizedString("InventorySortRarity"),
                _ => sortOption
            };
        }

        private List<string> GetAllAvailableTypes(CharacterSheet sheet)
        {
            var types = new HashSet<string>(standardItemTypes);
            if (sheet.customItemTypes != null)
            {
                foreach (var t in sheet.customItemTypes)
                {
                    if (!string.IsNullOrWhiteSpace(t)) types.Add(t);
                }
            }
            if (sheet.CharacterInventory != null)
            {
                foreach (var item in sheet.CharacterInventory)
                {
                    if (!string.IsNullOrWhiteSpace(item.ItemType)) types.Add(item.ItemType);
                }
            }
            return types.OrderBy(t => t).ToList();
        }

        private IEnumerable<Item> GetFilteredItems(CharacterSheet sheet)
        {
            if (sheet.CharacterInventory == null) return Enumerable.Empty<Item>();

            var items = sheet.CharacterInventory.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var q = searchQuery.Trim().ToLowerInvariant();
                items = items.Where(i => 
                    (i.Name != null && i.Name.ToLowerInvariant().Contains(q)) ||
                    (i.Description != null && i.Description.ToLowerInvariant().Contains(q)) ||
                    (i.Effect != null && i.Effect.ToLowerInvariant().Contains(q)) ||
                    (i.ItemType != null && i.ItemType.ToLowerInvariant().Contains(q)));
            }

            if (!string.IsNullOrWhiteSpace(selectedTypeFilter) && selectedTypeFilter != "All")
            {
                items = items.Where(i => string.Equals(i.ItemType, selectedTypeFilter, StringComparison.OrdinalIgnoreCase));
            }

            items = selectedSortIndex switch
            {
                0 => items.OrderBy(i => i.Name),
                1 => items.OrderBy(i => i.ItemType).ThenBy(i => i.Name),
                2 => items.OrderByDescending(i => i.Quantity),
                3 => items.OrderBy(i => Array.IndexOf(rarities, i.Rarity)).ThenBy(i => i.Name),
                _ => items
            };

            return items;
        }

        private void DrawItemList(CharacterSheet sheet)
        {
            using var listChild = ImRaii.Child("##ItemListScroll", new Vector2(0, 0), true);
            if (!listChild.Success) return;

            var items = GetFilteredItems(sheet).ToList();
            if (items.Count == 0)
            {
                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryNoItems"));
                return;
            }

            var itemCardHeight = 44.0f * ImGuiHelpers.GlobalScale;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var isSelected = item.Id == selectedItemId;

                ImGui.PushID($"ItemRow_{item.Id}");

                var pos = ImGui.GetCursorScreenPos();
                var availWidth = ImGui.GetContentRegionAvail().X;
                var cardSize = new Vector2(availWidth, itemCardHeight);

                var drawList = ImGui.GetWindowDrawList();
                bool isHovered = ImGui.IsMouseHoveringRect(pos, pos + cardSize);
                var rarityCol = GetRarityColor(item.Rarity);

                var bgCol = isSelected
                    ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.32f, 0.48f, 0.85f))
                    : (isHovered
                        ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.18f, 0.20f, 0.26f, 0.80f))
                        : ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.13f, 0.16f, 0.75f)));

                var borderCol = isSelected
                    ? ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGold)
                    : (isHovered
                        ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.60f, 0.60f, 0.65f, 0.70f))
                        : ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.27f, 0.33f, 0.45f)));

                drawList.AddRectFilled(pos, pos + cardSize, bgCol, 5.0f * ImGuiHelpers.GlobalScale);
                drawList.AddRect(pos, pos + cardSize, borderCol, 5.0f * ImGuiHelpers.GlobalScale, ImDrawFlags.None, isSelected ? 1.8f : 1.0f);

                // Left accent bar
                drawList.AddRectFilled(
                    pos + new Vector2(2f * ImGuiHelpers.GlobalScale, 4f * ImGuiHelpers.GlobalScale),
                    pos + new Vector2(4.5f * ImGuiHelpers.GlobalScale, itemCardHeight - 4f * ImGuiHelpers.GlobalScale),
                    ImGui.ColorConvertFloat4ToU32(rarityCol),
                    2.0f * ImGuiHelpers.GlobalScale);

                // Invisible button over card for selection
                if (ImGui.InvisibleButton($"##CardBtn_{item.Id}", cardSize))
                {
                    selectedItemId = item.Id;
                }

                // Render content inside card
                ImGui.SetCursorScreenPos(pos + new Vector2(4.0f, 4.0f) * ImGuiHelpers.GlobalScale);

                // Mini Thumbnail
                var thumbSize = new Vector2(36.0f, 36.0f) * ImGuiHelpers.GlobalScale;
                var placeholderInitial = !string.IsNullOrWhiteSpace(item.Name) ? item.Name[..1].ToUpper() : "?";
                ImageHelper.DrawThumbnailOrPlaceholder(item.ImageUrl, thumbSize, placeholderInitial, GetRarityColor(item.Rarity), 3.0f);

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

                // Text info
                ImGui.BeginGroup();
                {
                    // Row 1: Name and Quantity
                    var nameCol = GetRarityColor(item.Rarity);
                    var itemName = !string.IsNullOrWhiteSpace(item.Name) ? item.Name : LocalizationManager.Instance.GetLocalizedString("InventoryUnnamedItem");
                    ImGui.TextColored(nameCol, itemName);

                    ImGui.SameLine();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, $"x{item.Quantity}");

                    // Row 2: Type badge & effect snippet
                    UiUtils.Badge(GetLocalizedItemType(item.ItemType), new Vector4(0.2f, 0.25f, 0.35f, 0.5f), ImGuiColors.DalamudGrey2);

                    if (sheet.IsItemEquipped(item.Id))
                    {
                        ImGui.SameLine();
                        UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("EquippedBadge"), new Vector4(0.18f, 0.35f, 0.22f, 0.8f), ImGuiColors.ParsedGreen);
                    }

                    if (item is GearItem gear && gear.StatModifiers != null && gear.StatModifiers.Count > 0)
                    {
                        ImGui.SameLine();
                        var modSummary = gear.GetFormattedModifiers();
                        if (modSummary.Length > 20) modSummary = modSummary.Substring(0, 17) + "...";
                        UiUtils.Badge(modSummary, new Vector4(0.15f, 0.30f, 0.20f, 0.75f), ImGuiColors.ParsedGreen);
                    }
                    else if (!string.IsNullOrWhiteSpace(item.Effect))
                    {
                        ImGui.SameLine();
                        var snippet = item.Effect.Length > 25 ? item.Effect[..22] + "..." : item.Effect;
                        ImGui.TextColored(ImGuiColors.ParsedGreen, snippet);
                    }
                }
                ImGui.EndGroup();

                // Quick buttons on the far right
                var quickBtnWidth = 22.0f * ImGuiHelpers.GlobalScale;
                var quickBtnHeight = 22.0f * ImGuiHelpers.GlobalScale;
                var rightBtnsX = pos.X + availWidth - (quickBtnWidth * 2 + 8.0f * ImGuiHelpers.GlobalScale);

                ImGui.SetCursorScreenPos(new Vector2(rightBtnsX, pos.Y + (itemCardHeight - quickBtnHeight) * 0.5f));
                if (UiUtils.IconButton("DecrQty", FontAwesomeIcon.Minus, "-", new Vector2(quickBtnWidth, quickBtnHeight)))
                {
                    if (item.Quantity > 1)
                    {
                        item.Quantity--;
                    }
                    else
                    {
                        itemToDelete = item;
                        showDeleteConfirmModal = true;
                    }
                }

                ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconButton("IncrQty", FontAwesomeIcon.Plus, "+", new Vector2(quickBtnWidth, quickBtnHeight)))
                {
                    if (item.Quantity < item.MaxStack)
                    {
                        item.Quantity++;
                    }
                }

                ImGui.PopID();
                ImGui.SetCursorScreenPos(new Vector2(pos.X, pos.Y + itemCardHeight + 4.0f * ImGuiHelpers.GlobalScale));
            }
        }

        private void DrawItemDetail(CharacterSheet sheet)
        {
            using var detailChild = ImRaii.Child("##ItemDetailScroll", new Vector2(0, 0), true);
            if (!detailChild.Success) return;

            var item = sheet.CharacterInventory?.FirstOrDefault(i => i.Id == selectedItemId);
            if (item == null)
            {
                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventorySelectItemPrompt"));
                return;
            }

            // Top Header: Large image & Name / Badges
            var imgSize = new Vector2(90.0f, 90.0f) * ImGuiHelpers.GlobalScale;
            var placeholder = !string.IsNullOrWhiteSpace(item.Name) ? item.Name[..1].ToUpper() : "?";
            ImageHelper.DrawThumbnailOrPlaceholder(item.ImageUrl, imgSize, placeholder, GetRarityColor(item.Rarity), 6.0f);

            ImGui.SameLine(0, 12.0f * ImGuiHelpers.GlobalScale);
            ImGui.BeginGroup();
            {
                var rarityCol = GetRarityColor(item.Rarity);
                ImGui.TextColored(rarityCol, !string.IsNullOrWhiteSpace(item.Name) ? item.Name : LocalizationManager.Instance.GetLocalizedString("InventoryUnnamedItem"));
                ImGui.Spacing();

                UiUtils.Badge(GetLocalizedRarity(item.Rarity), new Vector4(0.2f, 0.2f, 0.2f, 0.6f), rarityCol);
                ImGui.SameLine();
                UiUtils.Badge(GetLocalizedItemType(item.ItemType), new Vector4(0.2f, 0.3f, 0.45f, 0.5f), ImGuiColors.ParsedBlue);
                ImGui.SameLine();
                UiUtils.Badge(string.Format(LocalizationManager.Instance.GetLocalizedString("InventoryItemQtyFormat"), item.Quantity, item.MaxStack), new Vector4(0.2f, 0.4f, 0.3f, 0.5f), ImGuiColors.ParsedGreen);

                if (item.Weight > 0.0f)
                {
                    ImGui.SameLine();
                    UiUtils.Badge(string.Format(LocalizationManager.Instance.GetLocalizedString("InventoryItemWeightFormat"), item.Weight), new Vector4(0.3f, 0.3f, 0.3f, 0.5f), ImGuiColors.DalamudGrey2);
                }

                if (item.IsUsable)
                {
                    ImGui.SameLine();
                    UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("InventoryItemUsableBadge"), new Vector4(0.2f, 0.5f, 0.3f, 0.5f), ImGuiColors.ParsedGreen);
                }

                if (item is GearItem gear)
                {
                    ImGui.SameLine();
                    UiUtils.Badge($"Slot: {gear.Slot}", new Vector4(0.25f, 0.25f, 0.40f, 0.6f), ImGuiColors.ParsedBlue);

                    if (sheet.IsItemEquipped(gear.Id))
                    {
                        ImGui.SameLine();
                        UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("EquippedBadge"), new Vector4(0.18f, 0.35f, 0.22f, 0.8f), ImGuiColors.ParsedGreen);
                    }
                }
            }
            ImGui.EndGroup();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Effect Section
            if (!string.IsNullOrWhiteSpace(item.Effect))
            {
                ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("InventoryItemEffect"));
                using (var effectCard = ImRaii.Child($"##EffectCard_{item.Id}", new Vector2(0, 48.0f * ImGuiHelpers.GlobalScale), true))
                {
                    if (effectCard.Success)
                    {
                        ImGui.TextColored(ImGuiColors.ParsedGreen, item.Effect);
                    }
                }
                ImGui.Spacing();
            }

            // Use Formula Section
            if (item.IsUsable && !string.IsNullOrWhiteSpace(item.UseFormula))
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemFormula"));
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                UiUtils.Badge(item.UseFormula, new Vector4(0.2f, 0.4f, 0.6f, 0.4f), ImGuiColors.ParsedBlue);
                ImGui.Spacing();
            }

            // Description Section
            ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemDescription"));
            using (var descCard = ImRaii.Child($"##DescCard_{item.Id}", new Vector2(0, 70.0f * ImGuiHelpers.GlobalScale), true))
            {
                if (descCard.Success)
                {
                    if (!string.IsNullOrWhiteSpace(item.Description))
                    {
                        ImGui.TextWrapped(item.Description);
                    }
                    else
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, "—");
                    }
                }
            }
            ImGui.Spacing();

            // Gear Stat Modifiers Section
            if (item is GearItem gearItem && gearItem.StatModifiers != null && gearItem.StatModifiers.Count > 0)
            {
                ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("StatModifiersLabel"));
                using (var modTable = ImRaii.Table($"##GearModsTable_{gearItem.Id}", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
                {
                    if (modTable.Success)
                    {
                        ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("TableHeaderLabel"), ImGuiTableColumnFlags.WidthFixed, 120.0f * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("TableHeaderValue"), ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableHeadersRow();

                        foreach (var kvp in gearItem.StatModifiers)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.TextColored(ImGuiColors.DalamudWhite, kvp.Key);
                            ImGui.TableNextColumn();
                            string modStr = kvp.Value >= 0 ? $"+{kvp.Value}" : kvp.Value.ToString();
                            ImGui.TextColored(kvp.Value >= 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed, modStr);
                        }
                    }
                }
                ImGui.Spacing();
            }

            // Custom Properties Section
            if (item.CustomProperties != null && item.CustomProperties.Count > 0)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemCustomProps"));
                using (var propTable = ImRaii.Table($"##CustomPropsTable_{item.Id}", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
                {
                    if (propTable.Success)
                    {
                        ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("TableHeaderLabel"), ImGuiTableColumnFlags.WidthFixed, 100.0f * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("TableHeaderValue"), ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableHeadersRow();

                        foreach (var kvp in item.CustomProperties)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.TextColored(ImGuiColors.DalamudGrey2, kvp.Key);
                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted(kvp.Value);
                        }
                    }
                }
                ImGui.Spacing();
            }

            // Action Buttons Toolbar
            ImGui.Separator();
            ImGui.Spacing();

            // Equip / Unequip Button if Gear / Augmentation
            if (item is GearItem gearToEquip)
            {
                bool isEquipped = sheet.IsItemEquipped(gearToEquip.Id);
                string equipBtnLabel = gearToEquip.isAugmentation
                    ? LocalizationManager.Instance.GetLocalizedString("InstallButton")
                    : LocalizationManager.Instance.GetLocalizedString("EquipButton");
                string unequipBtnLabel = gearToEquip.isAugmentation
                    ? LocalizationManager.Instance.GetLocalizedString("UninstallButton")
                    : LocalizationManager.Instance.GetLocalizedString("UnequipButton");

                if (isEquipped)
                {
                    if (UiUtils.IconTextButton("InvUnequipBtn", FontAwesomeIcon.SignOutAlt, unequipBtnLabel))
                    {
                        sheet.UnequipItem(gearToEquip.Id);
                        CharacterSheet.SaveSheet(sheet);
                    }
                }
                else
                {
                    if (UiUtils.IconTextButton("InvEquipBtn", FontAwesomeIcon.ShieldAlt, equipBtnLabel))
                    {
                        if (gearToEquip.isAugmentation)
                        {
                            sheet.EquipAugmentation(gearToEquip);
                        }
                        else
                        {
                            sheet.EquipGear(gearToEquip);
                        }
                        CharacterSheet.SaveSheet(sheet);
                    }
                }
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            }

            // Use Button
            if (item.IsUsable)
            {
                if (UiUtils.IconTextButton("UseBtn", FontAwesomeIcon.Magic, LocalizationManager.Instance.GetLocalizedString("InventoryItemUse")))
                {
                    item.Use(sheet);
                    if (item.Quantity <= 0)
                    {
                        selectedItemId = null;
                    }
                }
            }
            else
            {
                UiUtils.IconTextButton("UseBtnDisabled", FontAwesomeIcon.Magic, LocalizationManager.Instance.GetLocalizedString("InventoryItemUse"), enabled: false);
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("InventoryItemNotUsable"));
                }
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconTextButton("EditBtn", FontAwesomeIcon.Edit, LocalizationManager.Instance.GetLocalizedString("InventoryItemEdit")))
            {
                isItemGear = (item is GearItem);
                editingItem = item.Clone();
                editingItem.Id = item.Id; // keep original ID
                isEditingExistingItem = true;
                newPropKey = string.Empty;
                newPropValue = string.Empty;
                invModEditorState = new StatModifierEditorState();
                showCreateEditModal = true;
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconTextButton("DupBtn", FontAwesomeIcon.Clone, LocalizationManager.Instance.GetLocalizedString("InventoryItemDuplicate")))
            {
                var clone = item.Clone();
                clone.Name += LocalizationManager.Instance.GetLocalizedString("InventoryItemCopySuffix");
                sheet.AddItem(clone);
                selectedItemId = clone.Id;
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconTextButton("ExportBtn", FontAwesomeIcon.Clipboard, LocalizationManager.Instance.GetLocalizedString("InventoryExportJson")))
            {
                ImGui.SetClipboardText(item.ToJson());
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconTextButton("DeleteBtn", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("InventoryItemDelete")))
            {
                itemToDelete = item;
                showDeleteConfirmModal = true;
            }
        }

        private void DrawModals(CharacterSheet sheet)
        {
            DrawCreateEditModal(sheet);
            DrawManageTypesModal(sheet);
            DrawDeleteConfirmModal(sheet);
            DrawImportModal(sheet);
        }

        private void DrawCreateEditModal(CharacterSheet sheet)
        {
            if (!showCreateEditModal) return;

            var title = isEditingExistingItem
                ? $"{LocalizationManager.Instance.GetLocalizedString("InventoryEditItemTitle")}###CreateEditModal"
                : $"{LocalizationManager.Instance.GetLocalizedString("InventoryCreateItemTitle")}###CreateEditModal";

            ImGui.SetNextWindowSize(new Vector2(520.0f, 620.0f) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);

            if (ImGui.Begin(title, ref showCreateEditModal, ImGuiWindowFlags.NoCollapse))
            {
                // Item Name
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemName"));
                UiUtils.StyledInputText("EditItemName", ref editingItem.name, 100, width: -1.0f);

                // Type & Rarity
                using (var row = ImRaii.Table("##TypeRarityRow", 2, ImGuiTableFlags.SizingStretchSame))
                {
                    if (row.Success)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemType"));
                        var allTypes = GetAllAvailableTypes(sheet);
                        var typeIdx = Math.Max(0, allTypes.IndexOf(editingItem.itemType));
                        var localizedAllTypes = allTypes.Select(GetLocalizedItemType).ToArray();
                        if (UiUtils.StyledCombo("##EditItemTypeCombo", ref typeIdx, localizedAllTypes, icon: FontAwesomeIcon.Tags))
                        {
                            editingItem.itemType = allTypes[typeIdx];
                        }

                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemRarity"));
                        var rarityIdx = Math.Max(0, Array.IndexOf(rarities, editingItem.rarity));
                        var localizedRarities = rarities.Select(GetLocalizedRarity).ToArray();
                        if (UiUtils.StyledCombo("##EditItemRarityCombo", ref rarityIdx, localizedRarities, icon: FontAwesomeIcon.Gem))
                        {
                            editingItem.rarity = rarities[rarityIdx];
                        }
                    }
                }

                // Quantity, Max Stack & Weight
                using (var numRow = ImRaii.Table("##NumRow", 3, ImGuiTableFlags.SizingStretchSame))
                {
                    if (numRow.Success)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemQuantity"));
                        UiUtils.StyledInputInt("EditItemQty", ref editingItem.quantity, step: 1, width: -1.0f, min: 1);

                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemMaxStack"));
                        UiUtils.StyledInputInt("EditItemMaxStack", ref editingItem.maxStack, step: 1, width: -1.0f, min: 1);

                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemWeight"));
                        UiUtils.StyledInputFloat("EditItemWeight", ref editingItem.weight, step: 0.1f, format: "%.1f", width: -1.0f, min: 0.0f);
                    }
                }

                ImGui.Spacing();

                // Image Section
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemImage"));
                float imgInputW = Math.Max(120.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().X - 170.0f * ImGuiHelpers.GlobalScale);
                UiUtils.StyledInputText("EditItemImageUrl", ref editingItem.imageUrl, 500, width: imgInputW / ImGuiHelpers.GlobalScale);

                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("BrowseItemPic", FontAwesomeIcon.FolderOpen, LocalizationManager.Instance.GetLocalizedString("CharPictureBrowse")))
                {
                    plugin.OpenFilePicker(LocalizationManager.Instance.GetLocalizedString("InventoryChoosePicPickerTitle"), ".png;.jpg;.jpeg;.bmp;.webp;.gif", (path) =>
                    {
                        var localCopy = ImageHelper.CopyImageToLocalFolder(path, "items");
                        editingItem.imageUrl = localCopy;
                    });
                }

                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("ClearItemPic", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CharPictureClear")))
                {
                    editingItem.imageUrl = string.Empty;
                }

                // Image preview
                if (!string.IsNullOrWhiteSpace(editingItem.imageUrl))
                {
                    ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                    ImageHelper.DrawThumbnailOrPlaceholder(editingItem.imageUrl, new Vector2(30.0f, 30.0f) * ImGuiHelpers.GlobalScale, "?", GetRarityColor(editingItem.Rarity), 3.0f);
                }

                ImGui.Spacing();

                // Effect
                ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("InventoryItemEffect"));
                UiUtils.StyledInputMultiline("EditItemEffect", ref editingItem.effect, 1000, new Vector2(-1.0f, 50.0f * ImGuiHelpers.GlobalScale));

                ImGui.Spacing();

                // Usable Flag & Use Formula
                ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("InventoryItemIsUsable"), ref editingItem.isUsable);
                if (editingItem.isUsable)
                {
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemFormula"));
                    UiUtils.StyledInputText("EditItemFormula", ref editingItem.useFormula, 100, width: -1.0f, hint: LocalizationManager.Instance.GetLocalizedString("InventoryItemFormulaHint"));
                }

                ImGui.Spacing();

                // Description
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemDescription"));
                UiUtils.StyledInputMultiline("EditItemDesc", ref editingItem.description, 2000, new Vector2(-1.0f, 65.0f * ImGuiHelpers.GlobalScale));

                ImGui.Spacing();

                // Is Gear Checkbox and Gear Settings
                if (ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("ItemIsGearCheckbox"), ref isItemGear))
                {
                    if (isItemGear && editingItem is not GearItem)
                    {
                        var gear = new GearItem(editingItem.Name, "Head", editingItem.Description, editingItem.Rarity, null, editingItem.Effect, editingItem.Weight, editingItem.ImageUrl);
                        gear.Id = editingItem.Id;
                        gear.Quantity = editingItem.Quantity;
                        gear.MaxStack = 1;
                        gear.IsUsable = editingItem.IsUsable;
                        gear.UseFormula = editingItem.UseFormula;
                        gear.CustomProperties = new Dictionary<string, string>(editingItem.CustomProperties);
                        editingItem = gear;
                    }
                }

                if (isItemGear && editingItem is GearItem gearEdit)
                {
                    var diceSystem = DiceSystemManager.Instance.CurrentDiceSystem;

                    // Cyberware / Augmentation toggle
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("ItemIsAugmentationCheckbox"), ref gearEdit.isAugmentation);

                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("GearSlotLabel"));

                    var slotList = gearEdit.isAugmentation
                        ? (diceSystem?.GetEffectiveAugmentationSlots() ?? GearItem.StandardAugmentationSlots.ToList())
                        : (diceSystem?.GetEffectiveEquipmentSlots() ?? GearItem.StandardSlots.ToList());
                    var slotArray = slotList.ToArray();
                    int slotIdx = Array.IndexOf(slotArray, gearEdit.Slot);
                    if (slotIdx < 0)
                    {
                        slotIdx = 0;
                        gearEdit.Slot = slotArray[0];
                    }
                    if (UiUtils.StyledCombo("##EditGearSlotCombo", ref slotIdx, slotArray, icon: FontAwesomeIcon.ShieldAlt, width: 200.0f))
                    {
                        gearEdit.Slot = slotArray[slotIdx];
                    }

                    UiUtils.DrawStatModifierEditor(gearEdit, sheet, diceSystem, invModEditorState, "InvGearEdit");
                    ImGui.Spacing();
                }

                ImGui.Spacing();

                // Custom Properties Editor
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryItemCustomProps"));
                
                // Add Property row
                UiUtils.StyledInputText("NewPropKey", ref newPropKey, 50, width: 120.0f, hint: LocalizationManager.Instance.GetLocalizedString("InventoryPropKey"));
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                UiUtils.StyledInputText("NewPropVal", ref newPropValue, 200, width: 160.0f, hint: LocalizationManager.Instance.GetLocalizedString("InventoryPropValue"));
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("AddPropBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("InventoryAddProperty")))
                {
                    if (!string.IsNullOrWhiteSpace(newPropKey))
                    {
                        editingItem.CustomProperties[newPropKey.Trim()] = newPropValue.Trim();
                        newPropKey = string.Empty;
                        newPropValue = string.Empty;
                    }
                }

                // List of existing properties in modal
                string? propToRemove = null;
                foreach (var kvp in editingItem.CustomProperties)
                {
                    ImGui.BulletText($"{kvp.Key}: {kvp.Value}");
                    ImGui.SameLine();
                    if (UiUtils.IconButton($"RemoveProp_{kvp.Key}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                    {
                        propToRemove = kvp.Key;
                    }
                }
                if (propToRemove != null)
                {
                    editingItem.CustomProperties.Remove(propToRemove);
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Modal Action Buttons
                if (UiUtils.IconTextButton("SaveItemModalBtn", FontAwesomeIcon.Save, LocalizationManager.Instance.GetLocalizedString("InventorySaveItem"), size: new Vector2(140.0f * ImGuiHelpers.GlobalScale, 28.0f * ImGuiHelpers.GlobalScale)))
                {
                    if (string.IsNullOrWhiteSpace(editingItem.name))
                    {
                        editingItem.name = LocalizationManager.Instance.GetLocalizedString("InventoryUnnamedItem");
                    }

                    if (isItemGear && editingItem is not GearItem)
                    {
                        var gear = new GearItem(editingItem.Name, "Head", editingItem.Description, editingItem.Rarity, null, editingItem.Effect, editingItem.Weight, editingItem.ImageUrl);
                        gear.Id = editingItem.Id;
                        gear.Quantity = editingItem.Quantity;
                        gear.MaxStack = 1;
                        gear.IsUsable = editingItem.IsUsable;
                        gear.UseFormula = editingItem.UseFormula;
                        gear.CustomProperties = new Dictionary<string, string>(editingItem.CustomProperties);
                        editingItem = gear;
                    }
                    else if (!isItemGear && editingItem is GearItem)
                    {
                        var plainItem = new Item(editingItem.Name, editingItem.Description, editingItem.Effect, editingItem.ItemType, editingItem.Quantity, editingItem.ImageUrl, editingItem.IsUsable, editingItem.UseFormula);
                        plainItem.Id = editingItem.Id;
                        plainItem.Rarity = editingItem.Rarity;
                        plainItem.Weight = editingItem.Weight;
                        plainItem.MaxStack = editingItem.MaxStack;
                        plainItem.CustomProperties = new Dictionary<string, string>(editingItem.CustomProperties);
                        editingItem = plainItem;
                    }

                    if (isEditingExistingItem)
                    {
                        var index = sheet.CharacterInventory.FindIndex(i => i.Id == editingItem.Id);
                        if (index >= 0)
                        {
                            sheet.CharacterInventory[index] = editingItem;
                        }
                        else
                        {
                            sheet.AddItem(editingItem);
                        }
                    }
                    else
                    {
                        sheet.AddItem(editingItem);
                    }

                    selectedItemId = editingItem.Id;
                    showCreateEditModal = false;
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("CancelItemModalBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90.0f * ImGuiHelpers.GlobalScale, 28.0f * ImGuiHelpers.GlobalScale)))
                {
                    showCreateEditModal = false;
                }
            }
            ImGui.End();
        }

        private void DrawManageTypesModal(CharacterSheet sheet)
        {
            if (!showManageTypesModal) return;

            ImGui.SetNextWindowSize(new Vector2(380.0f, 360.0f) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);

            if (ImGui.Begin($"{LocalizationManager.Instance.GetLocalizedString("InventoryManageTypesTitle")}###ManageTypesModal", ref showManageTypesModal, ImGuiWindowFlags.NoCollapse))
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryNewTypeName"));
                float typeInputW = Math.Max(120.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().X - 110.0f * ImGuiHelpers.GlobalScale);
                UiUtils.StyledInputText("NewCustomTypeInput", ref newCustomTypeName, 50, width: typeInputW / ImGuiHelpers.GlobalScale);

                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("AddTypeBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("InventoryAddTypeBtn")))
                {
                    if (!string.IsNullOrWhiteSpace(newCustomTypeName))
                    {
                        var trimmed = newCustomTypeName.Trim();
                        if (!sheet.customItemTypes.Contains(trimmed))
                        {
                            sheet.customItemTypes.Add(trimmed);
                        }
                        newCustomTypeName = string.Empty;
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("InventoryStandardTypesHeader"));
                foreach (var std in standardItemTypes)
                {
                    ImGui.BulletText(GetLocalizedItemType(std));
                }

                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("InventoryCustomTypesHeader"));
                string? typeToRemove = null;
                if (sheet.customItemTypes == null || sheet.customItemTypes.Count == 0)
                {
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryNoCustomTypesMessage"));
                }
                else
                {
                    foreach (var ct in sheet.customItemTypes)
                    {
                        ImGui.BulletText(ct);
                        ImGui.SameLine();
                        if (UiUtils.IconButton($"RemoveType_{ct}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                        {
                            typeToRemove = ct;
                        }
                    }
                }
                if (typeToRemove != null)
                {
                    sheet.customItemTypes?.Remove(typeToRemove);
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (UiUtils.IconTextButton("CloseTypesModalBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CloseButton"), size: new Vector2(80.0f * ImGuiHelpers.GlobalScale, 24.0f * ImGuiHelpers.GlobalScale)))
                {
                    showManageTypesModal = false;
                }
            }
            ImGui.End();
        }

        private void DrawDeleteConfirmModal(CharacterSheet sheet)
        {
            if (!showDeleteConfirmModal || itemToDelete == null) return;

            ImGui.SetNextWindowSize(new Vector2(320.0f, 150.0f) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);

            if (ImGui.Begin($"{LocalizationManager.Instance.GetLocalizedString("InventoryDeleteConfirmTitle")}###DeleteConfirmModal", ref showDeleteConfirmModal, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize))
            {
                ImGui.TextWrapped(string.Format(LocalizationManager.Instance.GetLocalizedString("InventoryDeleteConfirmPrompt"), itemToDelete.Name));
                ImGui.Spacing();
                ImGui.Spacing();

                if (UiUtils.IconTextButton("ConfirmDeleteBtn", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("DeleteButton"), size: new Vector2(100.0f * ImGuiHelpers.GlobalScale, 26.0f * ImGuiHelpers.GlobalScale)))
                {
                    sheet.RemoveItem(itemToDelete.Id);
                    if (selectedItemId == itemToDelete.Id)
                    {
                        selectedItemId = null;
                    }
                    itemToDelete = null;
                    showDeleteConfirmModal = false;
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("CancelDeleteBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(80.0f * ImGuiHelpers.GlobalScale, 26.0f * ImGuiHelpers.GlobalScale)))
                {
                    itemToDelete = null;
                    showDeleteConfirmModal = false;
                }
            }
            ImGui.End();
        }

        private void DrawImportModal(CharacterSheet sheet)
        {
            if (!showImportModal) return;

            ImGui.SetNextWindowSize(new Vector2(480.0f, 380.0f) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);

            if (ImGui.Begin($"{LocalizationManager.Instance.GetLocalizedString("InventoryImportTitle")}###ImportItemsModal", ref showImportModal, ImGuiWindowFlags.NoCollapse))
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryImportSelectJsonPrompt"));
                if (UiUtils.IconTextButton("ImportChooseFileBtn", FontAwesomeIcon.FolderOpen, LocalizationManager.Instance.GetLocalizedString("InventoryImportFileBtn"), size: new Vector2(160.0f * ImGuiHelpers.GlobalScale, 26.0f * ImGuiHelpers.GlobalScale)))
                {
                    plugin.OpenFilePicker(LocalizationManager.Instance.GetLocalizedString("InventoryImportPickerTitle"), ".json", (filePath) =>
                    {
                        try
                        {
                            if (System.IO.File.Exists(filePath))
                            {
                                var json = System.IO.File.ReadAllText(filePath);
                                if (Item.TryImportFromJson(json, out var importedItems, out var err))
                                {
                                    int count = 0;
                                    foreach (var it in importedItems)
                                    {
                                        sheet.AddItem(it);
                                        count++;
                                    }
                                    importStatusIsError = false;
                                    importStatusMessage = string.Format(LocalizationManager.Instance.GetLocalizedString("InventoryImportSuccess"), count);
                                }
                                else
                                {
                                    importStatusIsError = true;
                                    importStatusMessage = string.Format(LocalizationManager.Instance.GetLocalizedString("InventoryImportError"), err);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log?.Error(ex, $"Failed to import items from file '{filePath}'");
                            importStatusIsError = true;
                            importStatusMessage = string.Format(LocalizationManager.Instance.GetLocalizedString("InventoryImportError"), ex.Message);
                        }
                    });
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InventoryImportRawText"));
                UiUtils.StyledInputMultiline("ImportRawJsonInput", ref importRawText, 100000, new Vector2(-1.0f, 150.0f * ImGuiHelpers.GlobalScale));

                ImGui.Spacing();

                if (UiUtils.IconTextButton("ConfirmImportBtn", FontAwesomeIcon.FileImport, LocalizationManager.Instance.GetLocalizedString("InventoryImportConfirmBtn"), size: new Vector2(180.0f * ImGuiHelpers.GlobalScale, 28.0f * ImGuiHelpers.GlobalScale)))
                {
                    try
                    {
                        if (Item.TryImportFromJson(importRawText, out var importedItems, out var err))
                        {
                            int count = 0;
                            foreach (var it in importedItems)
                            {
                                sheet.AddItem(it);
                                count++;
                            }
                            importStatusIsError = false;
                            importStatusMessage = string.Format(LocalizationManager.Instance.GetLocalizedString("InventoryImportSuccess"), count);
                            importRawText = string.Empty;
                        }
                        else
                        {
                            Plugin.Log?.Warning($"Failed to import items from raw text: {err}");
                            importStatusIsError = true;
                            importStatusMessage = string.Format(LocalizationManager.Instance.GetLocalizedString("InventoryImportError"), err);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Error(ex, "Failed to import items from raw text input");
                        importStatusIsError = true;
                        importStatusMessage = string.Format(LocalizationManager.Instance.GetLocalizedString("InventoryImportError"), ex.Message);
                    }
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("CloseImportModalBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CloseButton"), size: new Vector2(80.0f * ImGuiHelpers.GlobalScale, 28.0f * ImGuiHelpers.GlobalScale)))
                {
                    showImportModal = false;
                }

                if (!string.IsNullOrWhiteSpace(importStatusMessage))
                {
                    ImGui.Spacing();
                    ImGui.TextColored(importStatusIsError ? ImGuiColors.DPSRed : ImGuiColors.ParsedGreen, importStatusMessage);
                }
            }
            ImGui.End();
        }

        private Vector4 GetRarityColor(string rarity)
        {
            return rarity switch
            {
                "Uncommon" => ImGuiColors.ParsedGreen,
                "Rare" => ImGuiColors.ParsedBlue,
                "Epic" => ImGuiColors.DalamudViolet,
                "Legendary" => ImGuiColors.DalamudOrange,
                "Artifact" => ImGuiColors.ParsedGold,
                _ => ImGuiColors.DalamudWhite
            };
        }
    }
}
