using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Utils;

namespace Soulstone.Windows
{
    internal class FeatsWindow
    {
        private readonly Plugin plugin;
        private readonly Configuration configuration;

        private string searchQuery = string.Empty;
        private string selectedCategoryFilter = "All";
        private int selectedSortIndex = 0; // 0: Name, 1: Category, 2: Active
        private string? selectedFeatId = null;

        // Detail View Roll Options
        private bool detailAdvantage = false;
        private bool detailDisadvantage = false;
        private bool detailRollPrivate = false;

        // Modal states
        private bool showCreateEditModal = false;
        private bool isEditingExistingFeat = false;
        private Feat editingFeat = new();
        private StatModifierEditorState featModEditorState = new();
        private int editCategoryIndex = 0;
        private string customCategoryName = string.Empty;

        private bool showDeleteConfirmModal = false;
        private Feat? featToDelete = null;

        private bool showAbilityModal = false;
        private string newAbilityName = string.Empty;
        private int newAbilityValue = 0;
        private string newAbilityDescription = string.Empty;
        private string selectedAbilityAttribute = string.Empty;
        private string selectedAbilitySkill = string.Empty;

        private readonly string[] standardCategories = new[]
        {
            "General", "Combat", "Magic", "Passive", "Active", "Origin", "Racial", "Class", "Custom"
        };

        private readonly string[] sortOptions = new[]
        {
            "Name", "Category", "Active"
        };

        public FeatsWindow(Plugin _plugin)
        {
            plugin = _plugin;
            configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public void DrawFeatsTab()
        {
            var currentCharacter = CharacterManager.Instance.CharacterSheet;
            var currentDiceSystem = DiceSystemManager.Instance.CurrentDiceSystem;

            if (currentCharacter == null)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("NoCharLoadedFeatMessage"));
                return;
            }

            DrawTopBar(currentCharacter, currentDiceSystem);
            ImGui.Spacing();
            DrawAbilitiesSection(currentCharacter, currentDiceSystem);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            DrawFilterBar(currentCharacter);
            ImGui.Spacing();

            using (var table = ImRaii.Table("##FeatsColumns", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Resizable))
            {
                if (table.Success)
                {
                    ImGui.TableSetupColumn("FeatList", ImGuiTableColumnFlags.WidthStretch, 0.48f);
                    ImGui.TableSetupColumn("FeatDetail", ImGuiTableColumnFlags.WidthStretch, 0.52f);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    DrawFeatList(currentCharacter, currentDiceSystem);

                    ImGui.TableNextColumn();
                    DrawFeatDetail(currentCharacter, currentDiceSystem);
                }
            }

            DrawModals(currentCharacter, currentDiceSystem);
            DrawAbilityModal(currentCharacter, currentDiceSystem);
        }

        private void DrawAbilitiesSection(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            var abilities = sheet.GetEffectiveAbilities(diceSystem);

            if (!UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("AbilityLabel"), defaultOpen: true, icon: FontAwesomeIcon.Bolt, accentColor: ImGuiColors.TankBlue))
                return;

            if (UiUtils.IconTextButton("AddAbilityBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddButton")))
            {
                newAbilityName = string.Empty;
                newAbilityValue = 0;
                newAbilityDescription = string.Empty;
                selectedAbilityAttribute = string.Empty;
                selectedAbilitySkill = string.Empty;
                showAbilityModal = true;
            }

            if (abilities == null || abilities.Count == 0)
            {
                ImGui.SameLine();
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoAbilitiesDefined"));
                return;
            }

            string? abilityToRemove = null;
            string? abilityToMoveUp = null;
            string? abilityToMoveDown = null;
            var abilityList = abilities.ToList();
            for (var i = 0; i < abilityList.Count; i++)
            {
                var ability = abilityList[i];
                ImGui.PushID($"FeatAbility_{ability.Key}");

                if (i > 0 && UiUtils.IconButton("MoveUp", FontAwesomeIcon.ChevronUp, LocalizationManager.Instance.GetLocalizedString("MoveUpTooltip")))
                    abilityToMoveUp = ability.Key;
                if (i > 0) ImGui.SameLine();
                if (i < abilityList.Count - 1 && UiUtils.IconButton("MoveDown", FontAwesomeIcon.ChevronDown, LocalizationManager.Instance.GetLocalizedString("MoveDownTooltip")))
                    abilityToMoveDown = ability.Key;
                if (i < abilityList.Count - 1) ImGui.SameLine();

                ImGui.TextColored(ImGuiColors.TankBlue, ability.Value.abilityName);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(70.0f * ImGuiHelpers.GlobalScale);
                ImGui.InputInt("##AbilityModifier", ref ability.Value.abilityModifier, 0);

                if (!string.IsNullOrWhiteSpace(ability.Value.linkedAttribute))
                {
                    ImGui.SameLine();
                    UiUtils.Badge(ability.Value.linkedAttribute, new Vector4(0.28f, 0.22f, 0.12f, 0.6f), ImGuiColors.ParsedGold);
                }
                if (ability.Value.linkedSkill != null && !string.IsNullOrWhiteSpace(ability.Value.linkedSkill.skillName))
                {
                    ImGui.SameLine();
                    UiUtils.Badge(ability.Value.linkedSkill.skillName, new Vector4(0.15f, 0.28f, 0.18f, 0.6f), ImGuiColors.ParsedGreen);
                }

                ImGui.SameLine();
                if (UiUtils.IconButton("Roll", FontAwesomeIcon.DiceD20, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")} {ability.Value.abilityName}"))
                {
                    var modifier = ability.Value.abilityModifier + sheet.GetGearStatBonus(ability.Value.abilityName) + sheet.GetBuffStatBonus(ability.Value.abilityName) + sheet.GetFeatStatBonus(ability.Value.abilityName);
                    if (!string.IsNullOrWhiteSpace(ability.Value.linkedAttribute) && sheet.GetEffectiveAttributes(diceSystem)?.TryGetValue(ability.Value.linkedAttribute, out var attribute) == true)
                    {
                        modifier += attribute.Value;
                        if (diceSystem == null || diceSystem.systemHasBonusTemp) modifier += attribute.TempBonus;
                        if (diceSystem == null || diceSystem.systemHasBonusPerm) modifier += attribute.PermBonus;
                        modifier += sheet.GetGearStatBonus(ability.Value.linkedAttribute) + sheet.GetBuffStatBonus(ability.Value.linkedAttribute) + sheet.GetFeatStatBonus(ability.Value.linkedAttribute);
                    }
                    if (ability.Value.linkedSkill != null)
                    {
                        modifier += ability.Value.linkedSkill.skillModifier;
                        modifier += sheet.GetGearStatBonus(ability.Value.linkedSkill.skillName) + sheet.GetBuffStatBonus(ability.Value.linkedSkill.skillName) + sheet.GetFeatStatBonus(ability.Value.linkedSkill.skillName);
                    }
                    DiceRoll.RollDice(modifier, modifier, rollName: ability.Value.abilityName, detailedRoll: configuration.detailedRolls, target: modifier);
                }

                ImGui.SameLine();
                if (UiUtils.IconButton("Delete", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("DeleteButton")))
                    abilityToRemove = ability.Key;

                if (!string.IsNullOrWhiteSpace(ability.Value.abilityDescription) && ImGui.IsItemHovered())
                    ImGui.SetTooltip(ability.Value.abilityDescription);

                ImGui.PopID();
            }

            if (abilityToMoveUp != null)
            {
                sheet.MoveAbility(abilityToMoveUp, -1);
                diceSystem?.MoveAbility(abilityToMoveUp, -1);
            }
            if (abilityToMoveDown != null)
            {
                sheet.MoveAbility(abilityToMoveDown, 1);
                diceSystem?.MoveAbility(abilityToMoveDown, 1);
            }
            if (abilityToRemove != null)
            {
                sheet.characterAbilities.Remove(abilityToRemove);
                diceSystem?.SystemAbilities.Remove(abilityToRemove);
            }
        }

        private void DrawAbilityModal(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            if (showAbilityModal)
                ImGui.OpenPopup("NewAbilityModal");

            if (!ImGui.BeginPopupModal("NewAbilityModal", ref showAbilityModal, ImGuiWindowFlags.AlwaysAutoResize))
                return;

            ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAbilityName"));
            UiUtils.StyledInputText("NewAbilityName", ref newAbilityName, 100, width: 260.0f);
            ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAbilityValue"));
            UiUtils.StyledInputInt("NewAbilityValue", ref newAbilityValue, 1, width: 100.0f);
            ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAbilityDescription"));
            UiUtils.StyledInputText("NewAbilityDescription", ref newAbilityDescription, 200, width: 260.0f);

            var noneLabel = LocalizationManager.Instance.GetLocalizedString("NoneOption");
            ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewLinkedAttribute"));
            var attributes = new List<string> { string.Empty };
            attributes.AddRange(sheet.GetEffectiveAttributes(diceSystem)?.Keys ?? Enumerable.Empty<string>());
            UiUtils.StyledCombo("##AbilityAttribute", ref selectedAbilityAttribute, attributes, emptyLabel: noneLabel);

            ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewLinkedSkill"));
            var skills = new List<string> { string.Empty };
            skills.AddRange(sheet.GetEffectiveSkills(diceSystem)?.Keys ?? Enumerable.Empty<string>());
            UiUtils.StyledCombo("##AbilitySkill", ref selectedAbilitySkill, skills, emptyLabel: noneLabel);

            if (UiUtils.IconTextButton("ConfirmAbility", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton")) && !string.IsNullOrWhiteSpace(newAbilityName))
            {
                Skill? linkedSkill = null;
                sheet.GetEffectiveSkills(diceSystem)?.TryGetValue(selectedAbilitySkill, out linkedSkill);
                var ability = new Ability
                {
                    abilityName = newAbilityName,
                    abilityModifier = newAbilityValue,
                    abilityDescription = newAbilityDescription,
                    linkedAttribute = selectedAbilityAttribute,
                    linkedSkill = linkedSkill
                };
                sheet.characterAbilities ??= new Dictionary<string, Ability>(StringComparer.OrdinalIgnoreCase);
                sheet.characterAbilities[newAbilityName] = ability;
                if (diceSystem != null)
                    diceSystem.SystemAbilities[newAbilityName] = ability;
                showAbilityModal = false;
            }
            ImGui.SameLine();
            if (UiUtils.IconTextButton("CancelAbility", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton")))
                showAbilityModal = false;

            ImGui.EndPopup();
        }

        private void DrawTopBar(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            var totalCount = sheet.CharacterFeats?.Count ?? 0;
            var activeCount = sheet.CharacterFeats?.Count(f => f.IsActive) ?? 0;
            var addLabel = LocalizationManager.Instance.GetLocalizedString("FeatAddButton");

            UiUtils.DrawWindowHeroBanner(
                title: $"{sheet.CharacterFullName} — {LocalizationManager.Instance.GetLocalizedString("FeatTab")}",
                subtitle: LocalizationManager.Instance.GetLocalizedString("FeatHeroSubtitle"),
                badgeText: $"{activeCount}/{totalCount} {LocalizationManager.Instance.GetLocalizedString("FeatActiveBadge")}",
                badgeColor: activeCount > 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudGrey,
                icon: FontAwesomeIcon.Award,
                accentColor: ImGuiColors.ParsedGold,
                actionLabel: addLabel,
                onAction: () =>
                {
                    editingFeat = new Feat();
                    featModEditorState = new StatModifierEditorState();
                    editCategoryIndex = 0;
                    customCategoryName = string.Empty;
                    isEditingExistingFeat = false;
                    showCreateEditModal = true;
                });
        }

        private void DrawFilterBar(CharacterSheet sheet)
        {
            var scale = ImGuiHelpers.GlobalScale;

            UiUtils.StyledInputText("FeatSearch", ref searchQuery, 64, width: 170.0f, hint: LocalizationManager.Instance.GetLocalizedString("FeatSearchHint"), icon: FontAwesomeIcon.Search);

            ImGui.SameLine(0, 8.0f * scale);

            // Category filter chips
            var categories = new List<string> { "All" };
            if (sheet.CharacterFeats != null)
            {
                foreach (var f in sheet.CharacterFeats)
                {
                    if (!string.IsNullOrWhiteSpace(f.Category) && !categories.Contains(f.Category, StringComparer.OrdinalIgnoreCase))
                    {
                        categories.Add(f.Category);
                    }
                }
            }
            foreach (var std in standardCategories)
            {
                if (std != "Custom" && !categories.Contains(std, StringComparer.OrdinalIgnoreCase))
                {
                    categories.Add(std);
                }
            }

            // Draw up to first 5 category chips
            int displayedChips = Math.Min(6, categories.Count);
            for (int i = 0; i < displayedChips; i++)
            {
                var cat = categories[i];
                bool isSelected = string.Equals(selectedCategoryFilter, cat, StringComparison.OrdinalIgnoreCase);
                var bgCol = isSelected ? new Vector4(0.20f, 0.45f, 0.70f, 0.95f) : new Vector4(0.18f, 0.20f, 0.24f, 0.75f);
                var textCol = isSelected ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey;

                using (ImRaii.PushColor(ImGuiCol.Button, bgCol))
                using (ImRaii.PushColor(ImGuiCol.Text, textCol))
                using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 10.0f * scale))
                {
                    string label = cat == "All" ? LocalizationManager.Instance.GetLocalizedString("FilterAll") : cat;
                    if (ImGui.SmallButton($"{label}##CatChip_{cat}"))
                    {
                        selectedCategoryFilter = cat;
                    }
                }
                ImGui.SameLine(0, 4.0f * scale);
            }

            // Right side sort combo
            var sortLabel = LocalizationManager.Instance.GetLocalizedString("SortByLabel");
            var sortWidth = 100.0f * scale;
            var rightX = ImGui.GetWindowContentRegionMax().X - sortWidth;
            if (ImGui.GetCursorPosX() < rightX)
            {
                ImGui.SameLine(rightX);
            }
            UiUtils.StyledCombo("##FeatSortCombo", ref selectedSortIndex, sortOptions, width: 100.0f);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(sortLabel);
        }

        private void DrawFeatList(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var availHeight = Math.Max(200.0f * scale, ImGui.GetContentRegionAvail().Y - 4.0f);

            var feats = sheet.CharacterFeats ?? new List<Feat>();
            var filtered = feats.Where(f =>
            {
                if (!string.Equals(selectedCategoryFilter, "All", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(f.Category, selectedCategoryFilter, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    bool matchName = f.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchDesc = f.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchCat = f.Category.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchFormula = f.RollFormula.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchMod = f.StatModifiers != null && f.StatModifiers.Any(kv => kv.Key.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
                    if (!matchName && !matchDesc && !matchCat && !matchFormula && !matchMod) return false;
                }
                return true;
            }).ToList();

            // Sort
            filtered = selectedSortIndex switch
            {
                1 => filtered.OrderBy(f => f.Category).ThenBy(f => f.Name).ToList(),
                2 => filtered.OrderByDescending(f => f.IsActive).ThenBy(f => f.Name).ToList(),
                _ => filtered.OrderBy(f => f.Name).ToList()
            };

            using var listChild = ImRaii.Child("##FeatListContainer", new Vector2(0, availHeight), true);
            if (!listChild.Success) return;

            if (filtered.Count == 0)
            {
                ImGui.Spacing();
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoFeatsFoundMessage"));
                ImGui.Spacing();
                if (UiUtils.IconTextButton("CreateFirstFeatBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("FeatAddButton")))
                {
                    editingFeat = new Feat();
                    featModEditorState = new StatModifierEditorState();
                    editCategoryIndex = 0;
                    customCategoryName = string.Empty;
                    isEditingExistingFeat = false;
                    showCreateEditModal = true;
                }
                return;
            }

            for (int i = 0; i < filtered.Count; i++)
            {
                var feat = filtered[i];
                bool isSelected = string.Equals(feat.Id, selectedFeatId, StringComparison.OrdinalIgnoreCase);
                if (selectedFeatId == null && i == 0)
                {
                    selectedFeatId = feat.Id;
                    isSelected = true;
                }

                ImGui.PushID($"FeatCard_{feat.Id}");

                float cardHeight = 56.0f * scale;
                var pos = ImGui.GetCursorScreenPos();
                var availWidth = ImGui.GetContentRegionAvail().X;
                var drawList = ImGui.GetWindowDrawList();

                var bgCol = isSelected
                    ? new Vector4(0.18f, 0.28f, 0.42f, 0.85f)
                    : (feat.IsActive ? new Vector4(0.12f, 0.14f, 0.18f, 0.65f) : new Vector4(0.10f, 0.10f, 0.12f, 0.45f));
                var borderCol = isSelected
                    ? ImGuiColors.ParsedGold
                    : (feat.IsActive ? new Vector4(0.25f, 0.32f, 0.45f, 0.5f) : new Vector4(0.2f, 0.2f, 0.2f, 0.3f));

                drawList.AddRectFilled(pos, pos + new Vector2(availWidth, cardHeight), ImGui.ColorConvertFloat4ToU32(bgCol), 6.0f * scale);
                drawList.AddRect(pos, pos + new Vector2(availWidth, cardHeight), ImGui.ColorConvertFloat4ToU32(borderCol), 6.0f * scale, ImDrawFlags.None, isSelected ? 1.5f : 1.0f);

                ImGui.SetCursorScreenPos(pos + new Vector2(8.0f * scale, 6.0f * scale));

                // Active toggle
                bool active = feat.IsActive;
                if (ImGui.Checkbox($"##Active_{feat.Id}", ref active))
                {
                    feat.IsActive = active;
                    CharacterSheet.SaveSheet(sheet);
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("FeatToggleActiveTooltip"));

                ImGui.SameLine(0, 6.0f * scale);

                // Feat Name & Category
                var nameCol = feat.IsActive ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey;
                ImGui.TextColored(nameCol, feat.Name);

                if (!string.IsNullOrWhiteSpace(feat.Category))
                {
                    ImGui.SameLine(0, 6.0f * scale);
                    var catCol = GetCategoryColor(feat.Category);
                    var catBg = new Vector4(catCol.X * 0.2f, catCol.Y * 0.2f, catCol.Z * 0.2f, 0.85f);
                    UiUtils.Badge(feat.Category, catBg, catCol);
                }

                // Quick roll button on the right if formula exists
                if (!string.IsNullOrWhiteSpace(feat.RollFormula))
                {
                    var rollBtnWidth = 26.0f * scale;
                    var rollX = pos.X + availWidth - rollBtnWidth - 8.0f * scale;
                    if (ImGui.GetCursorScreenPos().X < rollX)
                    {
                        ImGui.SetCursorScreenPos(new Vector2(rollX, pos.Y + 6.0f * scale));
                    }
                    if (UiUtils.IconButton($"QuickRoll_{feat.Id}", FontAwesomeIcon.DiceD20, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")} {feat.RollFormula}", new Vector2(24, 20) * scale))
                    {
                        feat.RollFeat(sheet, diceSystem, detailAdvantage, detailDisadvantage, configuration.detailedRolls, detailRollPrivate);
                    }
                }

                // Sub-row: Modifiers badges preview or short description
                ImGui.SetCursorScreenPos(pos + new Vector2(32.0f * scale, 28.0f * scale));
                if (feat.StatModifiers != null && feat.StatModifiers.Count > 0)
                {
                    int shown = 0;
                    foreach (var mod in feat.StatModifiers)
                    {
                        if (shown >= 3)
                        {
                            UiUtils.Badge($"+{feat.StatModifiers.Count - shown}...", new Vector4(0.2f, 0.2f, 0.2f, 0.6f), ImGuiColors.DalamudGrey);
                            break;
                        }
                        var mCol = mod.Value >= 0 ? ImGuiColors.ParsedBlue : ImGuiColors.DalamudRed;
                        var mBg = mod.Value >= 0 ? new Vector4(0.12f, 0.22f, 0.38f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                        UiUtils.Badge($"{(mod.Value >= 0 ? "+" : "")}{mod.Value} {mod.Key}", mBg, mCol);
                        ImGui.SameLine(0, 4.0f * scale);
                        shown++;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(feat.Description))
                {
                    string shortDesc = feat.Description.Length > 45 ? feat.Description.Substring(0, 42) + "..." : feat.Description;
                    ImGui.TextDisabled(shortDesc);
                }

                // Click to select
                ImGui.SetCursorScreenPos(pos);
                if (ImGui.InvisibleButton($"SelectFeat_{feat.Id}", new Vector2(availWidth - 30.0f * scale, cardHeight)))
                {
                    selectedFeatId = feat.Id;
                }

                ImGui.SetCursorScreenPos(pos + new Vector2(0, cardHeight + 6.0f * scale));
                ImGui.PopID();
            }
        }

        private void DrawFeatDetail(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var availHeight = Math.Max(200.0f * scale, ImGui.GetContentRegionAvail().Y - 4.0f);

            var feat = sheet.CharacterFeats?.FirstOrDefault(f => string.Equals(f.Id, selectedFeatId, StringComparison.OrdinalIgnoreCase));

            using var detailChild = ImRaii.Child("##FeatDetailContainer", new Vector2(0, availHeight), true);
            if (!detailChild.Success) return;

            if (feat == null)
            {
                ImGui.Spacing();
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("SelectFeatPrompt"));
                return;
            }

            // Header Banner
            using (var headerCard = ImRaii.Child("##FeatDetailHeaderCard", new Vector2(0, 54.0f * scale), true))
            {
                if (headerCard.Success)
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.Award.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * scale);

                    ImGui.TextColored(ImGuiColors.ParsedGold, feat.Name);
                    ImGui.SameLine(0, 8.0f * scale);

                    var catCol = GetCategoryColor(feat.Category);
                    UiUtils.PillBadge(feat.Category, new Vector4(catCol.X * 0.2f, catCol.Y * 0.2f, catCol.Z * 0.2f, 0.85f), catCol);

                    ImGui.SameLine(0, 6.0f * scale);
                    if (feat.IsActive)
                    {
                        UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("FeatActiveBadge"), new Vector4(0.14f, 0.38f, 0.20f, 0.85f), ImGuiColors.ParsedGreen, FontAwesomeIcon.Check);
                    }
                    else
                    {
                        UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("FeatInactiveBadge"), new Vector4(0.25f, 0.25f, 0.25f, 0.85f), ImGuiColors.DalamudGrey, FontAwesomeIcon.Times);
                    }

                    // Action buttons: Edit, Duplicate, Delete
                    var editLabel = LocalizationManager.Instance.GetLocalizedString("EditButton");
                    var dupLabel = LocalizationManager.Instance.GetLocalizedString("DuplicateButton");
                    var delLabel = LocalizationManager.Instance.GetLocalizedString("DeleteButton");

                    var editW = ImGui.CalcTextSize(editLabel).X + 26.0f * scale;
                    var dupW = ImGui.CalcTextSize(dupLabel).X + 26.0f * scale;
                    var delW = ImGui.CalcTextSize(delLabel).X + 26.0f * scale;
                    var rightPos = ImGui.GetWindowContentRegionMax().X - (editW + dupW + delW + 12.0f * scale);

                    if (ImGui.GetCursorPosX() < rightPos)
                        ImGui.SameLine(rightPos);
                    else
                        ImGui.SameLine();

                    if (UiUtils.IconButton("EditFeatBtn", FontAwesomeIcon.Pen, editLabel))
                    {
                        editingFeat = feat.Clone();
                        featModEditorState = new StatModifierEditorState();
                        editCategoryIndex = Array.IndexOf(standardCategories, editingFeat.Category);
                        if (editCategoryIndex < 0)
                        {
                            editCategoryIndex = standardCategories.Length - 1; // Custom
                            customCategoryName = editingFeat.Category;
                        }
                        else
                        {
                            customCategoryName = string.Empty;
                        }
                        isEditingExistingFeat = true;
                        showCreateEditModal = true;
                    }

                    ImGui.SameLine(0, 4.0f * scale);
                    if (UiUtils.IconButton("DupFeatBtn", FontAwesomeIcon.Clone, dupLabel))
                    {
                        var dup = feat.Clone();
                        dup.Name = $"{feat.Name} (Copy)";
                        sheet.AddFeat(dup);
                        selectedFeatId = dup.Id;
                        CharacterSheet.SaveSheet(sheet);
                    }

                    ImGui.SameLine(0, 4.0f * scale);
                    using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.45f, 0.15f, 0.15f, 0.7f)))
                    {
                        if (UiUtils.IconButton("DelFeatBtn", FontAwesomeIcon.Trash, delLabel))
                        {
                            featToDelete = feat;
                            showDeleteConfirmModal = true;
                        }
                    }
                }
            }

            ImGui.Spacing();

            // Description Section
            using (var descCard = ImRaii.Child("##FeatDescCard", new Vector2(0, 90.0f * scale), true))
            {
                if (descCard.Success)
                {
                    ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("FeatDescriptionHeader"));
                    ImGui.Separator();
                    ImGui.Spacing();

                    if (!string.IsNullOrWhiteSpace(feat.Description))
                    {
                        ImGui.TextWrapped(feat.Description);
                    }
                    else
                    {
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("FeatNoDescription"));
                    }
                }
            }

            ImGui.Spacing();

            // Roll Formula & Roll Control Section
            using (var rollCard = ImRaii.Child("##FeatRollCard", new Vector2(0, 112.0f * scale), true))
            {
                if (rollCard.Success)
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.DiceD20.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * scale);

                    ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("FeatRollFormulaHeader"));

                    if (!string.IsNullOrWhiteSpace(feat.RollFormula))
                    {
                        ImGui.SameLine(0, 8.0f * scale);
                        UiUtils.Badge(feat.RollFormula, new Vector4(0.18f, 0.28f, 0.48f, 0.85f), ImGuiColors.ParsedBlue);

                        string preview = feat.ResolveFormulaPreview(sheet, diceSystem);
                        if (!string.Equals(preview, feat.RollFormula, StringComparison.OrdinalIgnoreCase))
                        {
                            ImGui.SameLine(0, 6.0f * scale);
                            UiUtils.Badge($"-> {preview}", new Vector4(0.15f, 0.35f, 0.22f, 0.85f), ImGuiColors.ParsedGreen);
                        }

                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();

                        // Advantage / Disadvantage toggles if enabled
                        if (diceSystem?.systemHasAdvantageDisadvantage == true)
                        {
                            if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("AdvantageCheckbox")}##FeatAdvCheck", ref detailAdvantage))
                            {
                                if (detailAdvantage) detailDisadvantage = false;
                            }
                            ImGui.SameLine(0, 10.0f * scale);
                            if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("DisadvantageCheckbox")}##FeatDisadvCheck", ref detailDisadvantage))
                            {
                                if (detailDisadvantage) detailAdvantage = false;
                            }
                            ImGui.SameLine(0, 14.0f * scale);
                        }

                        // Private Roll toggle
                        ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("RollPrivateCheck")}##FeatRollPrivateCheck", ref detailRollPrivate);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("RollPrivateTooltip"));
                        }

                        ImGui.SameLine(0, 14.0f * scale);
                        using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.2f, 0.5f, 0.3f, 0.8f)))
                        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.65f, 0.4f, 0.95f)))
                        {
                            string rollLabel = $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")} {feat.Name}";
                            if (UiUtils.IconButton("ExecuteFeatRollBtn", FontAwesomeIcon.DiceD20, rollLabel))
                            {
                                feat.RollFeat(sheet, diceSystem, detailAdvantage, detailDisadvantage, configuration.detailedRolls, detailRollPrivate);
                            }
                        }
                    }
                    else
                    {
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("FeatNoFormula"));
                    }
                }
            }

            ImGui.Spacing();

            // Stat Modifiers Breakdown Section
            using (var statCard = ImRaii.Child("##FeatStatModsCard", new Vector2(0, 0), true))
            {
                if (statCard.Success)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.ChartLine.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * scale);

                    ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("FeatStatModifiersHeader"));
                    ImGui.Separator();
                    ImGui.Spacing();

                    if (feat.StatModifiers != null && feat.StatModifiers.Count > 0)
                    {
                        foreach (var mod in feat.StatModifiers)
                        {
                            var modCol = mod.Value >= 0 ? ImGuiColors.ParsedBlue : ImGuiColors.DalamudRed;
                            var modBg = mod.Value >= 0 ? new Vector4(0.12f, 0.22f, 0.38f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                            string modText = $"{(mod.Value >= 0 ? "+" : "")}{mod.Value} {mod.Key}";
                            UiUtils.Badge(modText, modBg, modCol);
                            ImGui.SameLine(0, 6.0f * scale);

                            // Stat type category deduction
                            string statCategory = "Stat";
                            if (sheet.CharacterAttributes != null && sheet.CharacterAttributes.ContainsKey(mod.Key))
                                statCategory = LocalizationManager.Instance.GetLocalizedString("StatCategoryAttribute");
                            else if (sheet.CharacterSkills != null && sheet.CharacterSkills.ContainsKey(mod.Key))
                                statCategory = LocalizationManager.Instance.GetLocalizedString("StatCategorySkill");
                            else if (sheet.CharacterAbilities != null && sheet.CharacterAbilities.ContainsKey(mod.Key))
                                statCategory = LocalizationManager.Instance.GetLocalizedString("StatCategoryAbility");
                            else if (sheet.CharacterResources != null && sheet.CharacterResources.ContainsKey(mod.Key))
                                statCategory = LocalizationManager.Instance.GetLocalizedString("StatCategoryResource");

                            UiUtils.Badge(statCategory, new Vector4(0.2f, 0.2f, 0.2f, 0.6f), ImGuiColors.DalamudGrey);
                            ImGui.Spacing();
                        }
                    }
                    else
                    {
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("FeatNoModifiers"));
                    }
                }
            }
        }

        private void DrawModals(CharacterSheet sheet, DiceSystem? diceSystem)
        {
            var scale = ImGuiHelpers.GlobalScale;

            // Create / Edit Modal
            if (showCreateEditModal)
            {
                ImGui.OpenPopup("##CreateEditFeatModal");
            }

            var modalFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings;
            var center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(480.0f * scale, 0), ImGuiCond.Always);

            if (ImGui.BeginPopupModal("##CreateEditFeatModal", ref showCreateEditModal, modalFlags))
            {
                string modalTitle = isEditingExistingFeat
                    ? LocalizationManager.Instance.GetLocalizedString("FeatModalEditTitle")
                    : LocalizationManager.Instance.GetLocalizedString("FeatModalCreateTitle");

                ImGui.TextColored(ImGuiColors.ParsedGold, modalTitle);
                ImGui.Separator();
                ImGui.Spacing();

                // Name
                ImGui.TextColored(ImGuiColors.DalamudWhite, LocalizationManager.Instance.GetLocalizedString("FeatNameLabel"));
                string featName = editingFeat.Name;
                UiUtils.StyledInputText("##EditFeatName", ref featName, 80, width: 440.0f, hint: LocalizationManager.Instance.GetLocalizedString("FeatNameHint"));
                editingFeat.Name = featName;

                ImGui.Spacing();

                // Category
                ImGui.TextColored(ImGuiColors.DalamudWhite, LocalizationManager.Instance.GetLocalizedString("FeatCategoryLabel"));
                if (UiUtils.StyledCombo("##EditFeatCategoryCombo", ref editCategoryIndex, standardCategories, width: 200.0f))
                {
                    if (editCategoryIndex < standardCategories.Length - 1)
                    {
                        editingFeat.Category = standardCategories[editCategoryIndex];
                    }
                }

                if (editCategoryIndex == standardCategories.Length - 1) // Custom
                {
                    ImGui.SameLine(0, 6.0f * scale);
                    UiUtils.StyledInputText("##CustomFeatCategory", ref customCategoryName, 40, width: 230.0f, hint: LocalizationManager.Instance.GetLocalizedString("FeatCategoryCustomHint"));
                    editingFeat.Category = customCategoryName.Trim();
                }
                else
                {
                    editingFeat.Category = standardCategories[editCategoryIndex];
                }

                ImGui.Spacing();

                // Active toggle
                bool editActive = editingFeat.IsActive;
                if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("FeatActiveCheck")}##ModalFeatActive", ref editActive))
                {
                    editingFeat.IsActive = editActive;
                }

                ImGui.Spacing();

                // Description
                ImGui.TextColored(ImGuiColors.DalamudWhite, LocalizationManager.Instance.GetLocalizedString("FeatDescriptionHeader"));
                string featDesc = editingFeat.Description;
                UiUtils.StyledInputMultiline("##EditFeatDesc", ref featDesc, 1000, new Vector2(440.0f * scale, 70.0f * scale));
                editingFeat.Description = featDesc;

                ImGui.Spacing();

                // Roll Formula
                ImGui.TextColored(ImGuiColors.DalamudWhite, LocalizationManager.Instance.GetLocalizedString("FeatRollFormulaHeader"));
                string featFormula = editingFeat.RollFormula;
                UiUtils.StyledInputText("##EditFeatFormula", ref featFormula, 100, width: 440.0f, hint: LocalizationManager.Instance.GetLocalizedString("FeatFormulaHint"), icon: FontAwesomeIcon.DiceD20);
                editingFeat.RollFormula = featFormula;

                if (!string.IsNullOrWhiteSpace(featFormula))
                {
                    string resolved = editingFeat.ResolveFormulaPreview(sheet, diceSystem);
                    if (!string.Equals(resolved, featFormula, StringComparison.OrdinalIgnoreCase))
                    {
                        ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("DiceSysResourcePreviewHeader")}: {resolved}");
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Stat Modifiers Editor
                UiUtils.DrawStatModifierEditor(editingFeat, sheet, diceSystem, featModEditorState, "FeatModalModEditor");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Save & Cancel Buttons
                var saveLabel = LocalizationManager.Instance.GetLocalizedString("SaveButton");
                var cancelLabel = LocalizationManager.Instance.GetLocalizedString("CancelButton");

                bool canSave = !string.IsNullOrWhiteSpace(editingFeat.Name);
                if (!canSave) ImGui.BeginDisabled();

                if (UiUtils.IconButton("SaveFeatModalBtn", FontAwesomeIcon.Check, saveLabel))
                {
                    if (isEditingExistingFeat)
                    {
                        var target = sheet.GetFeat(editingFeat.Id);
                        if (target != null)
                        {
                            target.Name = editingFeat.Name;
                            target.Description = editingFeat.Description;
                            target.Category = editingFeat.Category;
                            target.RollFormula = editingFeat.RollFormula;
                            target.StatModifiers = new Dictionary<string, int>(editingFeat.StatModifiers, StringComparer.OrdinalIgnoreCase);
                            target.IsActive = editingFeat.IsActive;
                        }
                    }
                    else
                    {
                        sheet.AddFeat(editingFeat);
                        selectedFeatId = editingFeat.Id;
                    }

                    CharacterSheet.SaveSheet(sheet);
                    showCreateEditModal = false;
                    ImGui.CloseCurrentPopup();
                }

                if (!canSave) ImGui.EndDisabled();

                ImGui.SameLine(0, 8.0f * scale);
                if (UiUtils.IconButton("CancelFeatModalBtn", FontAwesomeIcon.Times, cancelLabel))
                {
                    showCreateEditModal = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            // Delete Confirmation Modal
            if (showDeleteConfirmModal)
            {
                ImGui.OpenPopup("##DeleteFeatConfirmModal");
            }

            if (ImGui.BeginPopupModal("##DeleteFeatConfirmModal", ref showDeleteConfirmModal, modalFlags))
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, LocalizationManager.Instance.GetLocalizedString("FeatDeleteConfirmTitle"));
                ImGui.Separator();
                ImGui.Spacing();

                string confirmText = LocalizationManager.Instance.GetLocalizedString("FeatDeleteConfirmMessage", featToDelete?.Name ?? "");
                ImGui.TextWrapped(confirmText);

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (UiUtils.IconButton("ConfirmDelFeatBtn", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("DeleteButton")))
                {
                    if (featToDelete != null)
                    {
                        sheet.RemoveFeat(featToDelete.Id);
                        if (string.Equals(selectedFeatId, featToDelete.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            selectedFeatId = sheet.CharacterFeats?.FirstOrDefault()?.Id;
                        }
                        CharacterSheet.SaveSheet(sheet);
                    }
                    showDeleteConfirmModal = false;
                    featToDelete = null;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine(0, 8.0f * scale);
                if (UiUtils.IconButton("CancelDelFeatBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton")))
                {
                    showDeleteConfirmModal = false;
                    featToDelete = null;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        private static Vector4 GetCategoryColor(string category)
        {
            return category?.ToLowerInvariant() switch
            {
                "combat" => ImGuiColors.DPSRed,
                "magic" => ImGuiColors.ParsedPurple,
                "passive" => ImGuiColors.ParsedGreen,
                "active" => ImGuiColors.ParsedBlue,
                "origin" or "racial" => ImGuiColors.ParsedGold,
                "class" => ImGuiColors.TankBlue,
                _ => ImGuiColors.DalamudWhite
            };
        }
    }
}
