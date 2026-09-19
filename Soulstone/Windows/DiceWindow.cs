using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
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
    internal class DiceWindow
    {
        private bool detailedRoll = false;
        private string rollInputText = "";
        private bool advantage = false;
        private bool disadvantage = false;
        private bool rollPrivate = false;

        // History Filters
        private string historySearch = "";
        private int historyFilter = 0; // 0: All, 1: Mine, 2: Party, 3: Private

        private readonly Plugin plugin;
        private readonly Configuration configuration;

        public DiceWindow(Plugin _plugin)
        {
            plugin = _plugin;
            configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public void DrawDiceTab()
        {
            detailedRoll = configuration.detailedRolls;
            DiceSystem? currentSystem = DiceSystemManager.Instance.CurrentDiceSystem;

            DrawQuickDiceBar();
            ImGui.Spacing();
            DrawRollControlCard(currentSystem);
            ImGui.Spacing();
            DrawInitiativeQuickCard(currentSystem);
            ImGui.Spacing();
            DrawHistoryCard();
        }

        private void DrawInitiativeQuickCard(DiceSystem? currentSystem)
        {
            var sheet = CharacterManager.Instance.CharacterSheet;
            int mod = sheet?.GetInitiativeModifier(currentSystem) ?? 0;
            string statSource;
            if (currentSystem == null || currentSystem.initiativeStatType == InitiativeStatType.None)
            {
                statSource = LocalizationManager.Instance.GetLocalizedString("InitiativeNone");
            }
            else if (currentSystem.initiativeStatType == InitiativeStatType.Formula)
            {
                statSource = !string.IsNullOrWhiteSpace(currentSystem.initiativeFormula)
                    ? $"{LocalizationManager.Instance.GetLocalizedString("DiceSysResourceFormulaHeader")}: {currentSystem.initiativeFormula}"
                    : LocalizationManager.Instance.GetLocalizedString("InitiativeNone");
            }
            else if (currentSystem.initiativeStatType == InitiativeStatType.Attribute)
            {
                string attrLabel = LocalizationManager.Instance.GetLocalizedString("AttributeLabel").TrimEnd(' ', ':');
                statSource = $"{attrLabel}: {currentSystem.initiativeStatName}";
            }
            else if (currentSystem.initiativeStatType == InitiativeStatType.Skill)
            {
                string skillLabel = LocalizationManager.Instance.GetLocalizedString("SkillLabel").TrimEnd(' ', ':');
                statSource = $"{skillLabel}: {currentSystem.initiativeStatName}";
            }
            else
            {
                statSource = currentSystem.initiativeStatName;
            }

            string diceNotation = currentSystem != null ? $"1d{DiceRoll.GetSystemSides(currentSystem)}" : "1d20";
            var diceIcon = currentSystem?.diceType == DiceType.d20 ? FontAwesomeIcon.DiceD20 : FontAwesomeIcon.Dice;

            using (var card = ImRaii.Child("##InitiativeQuickCard", new Vector2(0, 42.0f * ImGuiHelpers.GlobalScale), true))
            {
                if (card.Success)
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.Stopwatch.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                    ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("InitiativeTab"));
                    ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

                    string badgeText = $"{diceNotation} | {statSource} ({(mod >= 0 ? $"+{mod}" : $"{mod}")})";
                    UiUtils.Badge(badgeText, new Vector4(0.14f, 0.38f, 0.20f, 0.85f), ImGuiColors.ParsedGreen);

                    ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                    string rollBtnLabel = LocalizationManager.Instance.GetLocalizedString("InitiativeRollInitiative");
                    if (UiUtils.IconButton("RollInitDiceTabBtn", diceIcon, rollBtnLabel))
                    {
                        if (sheet != null)
                        {
                            var roll = sheet.RollInitiative(currentSystem, advantage, disadvantage, detailedRoll);
                            InitiativeTrackerManager.Instance.AddOrUpdateCurrentCharacter(sheet, currentSystem, roll.RollResult, mod);
                        }
                    }

                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    if (UiUtils.IconButton("OpenTrackerFromDiceBtn", FontAwesomeIcon.ExternalLinkAlt, LocalizationManager.Instance.GetLocalizedString("InitiativeOpenTracker")))
                    {
                        plugin.ToggleInitiativeTrackerUi();
                    }
                }
            }
        }

        private void DrawQuickDiceBar()
        {
            ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("QuickDiceHeader"));
            ImGui.Spacing();

            var diceList = new[] { "1d4", "1d6", "1d8", "1d10", "1d12", "1d20", "1d100" };
            var btnWidth = 52.0f * ImGuiHelpers.GlobalScale;

            for (int i = 0; i < diceList.Length; i++)
            {
                if (i > 0) ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                var dice = diceList[i];
                if (UiUtils.IconTextButton($"Quick_{dice}", FontAwesomeIcon.DiceD20, dice, size: new Vector2(btnWidth, 24.0f * ImGuiHelpers.GlobalScale)))
                {
                    if (string.IsNullOrWhiteSpace(rollInputText))
                    {
                        rollInputText = dice;
                    }
                    else
                    {
                        rollInputText += $" + {dice}";
                    }
                }
            }

            ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
            var modBtns = new[] { "+1", "+2", "+5", "-1" };
            foreach (var mod in modBtns)
            {
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.SmallButton(mod, size: new Vector2(32.0f * ImGuiHelpers.GlobalScale, 24.0f * ImGuiHelpers.GlobalScale)))
                {
                    if (string.IsNullOrWhiteSpace(rollInputText))
                        rollInputText = "1d20" + (mod.StartsWith("+") ? mod : mod);
                    else
                        rollInputText += mod.StartsWith("+") ? $" + {mod.TrimStart('+')}" : $" - {mod.TrimStart('-')}";
                }
            }
        }

        private void DrawRollControlCard(DiceSystem? currentSystem)
        {
            using (var card = ImRaii.Child("##RollControlCard", new Vector2(0, 108.0f * ImGuiHelpers.GlobalScale), true))
            {
                if (card.Success)
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("RollInputLabel"));
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                    var rollLabel = LocalizationManager.Instance.GetLocalizedString("ThrowButton");
                    var rollBtnWidth = 40.0f * ImGuiHelpers.GlobalScale;
                    var clearBtnWidth = 26.0f * ImGuiHelpers.GlobalScale;
                    var spacing = 18.0f * ImGuiHelpers.GlobalScale;

                    float inputWidth = Math.Max(120.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().X - clearBtnWidth - rollBtnWidth - spacing);

                    UiUtils.StyledInputText("RollInput", ref rollInputText, 100, width: inputWidth / ImGuiHelpers.GlobalScale, hint: LocalizationManager.Instance.GetLocalizedString("DiceRollFormulaHint"));

                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    if (UiUtils.IconButton("ClearFormula", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("ClearFormulaTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                    {
                        rollInputText = "";
                    }

                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.2f, 0.5f, 0.3f, 0.7f)))
                    using (ImRaii.PushColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.65f, 0.4f, 0.9f)))
                    {
                        if (UiUtils.IconButton("ExecuteRoll", FontAwesomeIcon.DiceD20, rollLabel, new Vector2(rollBtnWidth, 24.0f * ImGuiHelpers.GlobalScale)))
                        {
                            ExecuteRoll();
                        }
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    // Advantage / Disadvantage toggles if enabled
                    if (currentSystem?.systemHasAdvantageDisadvantage == true)
                    {
                        if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("AdvantageCheckbox")}##AdvantageCheck", ref advantage))
                        {
                            if (advantage) disadvantage = false;
                        }
                        ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                        if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("DisadvantageCheckbox")}##DisadvantageCheck", ref disadvantage))
                        {
                            if (disadvantage) advantage = false;
                        }
                        ImGui.SameLine(0, 14.0f * ImGuiHelpers.GlobalScale);
                    }

                    // Private Roll (DM & Player) toggle
                    ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("RollPrivateCheck")}##RollPrivateCheck", ref rollPrivate);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("RollPrivateTooltip"));
                    }

                    ImGui.SameLine(0, 12.0f * ImGuiHelpers.GlobalScale);
                    if (rollPrivate)
                    {
                        UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("RollPrivateTag"), new Vector4(0.38f, 0.20f, 0.48f, 0.9f), ImGuiColors.DalamudViolet, FontAwesomeIcon.UserSecret);
                    }
                    else
                    {
                        UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("RollPublicTag"), new Vector4(0.18f, 0.35f, 0.25f, 0.85f), ImGuiColors.ParsedGreen, FontAwesomeIcon.Globe);
                    }

                    if (advantage)
                    {
                        ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                        UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("BadgeAdvantage"), new Vector4(0.15f, 0.40f, 0.20f, 0.85f), ImGuiColors.ParsedGreen, FontAwesomeIcon.ArrowUp);
                    }
                    else if (disadvantage)
                    {
                        ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                        UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("BadgeDisadvantage"), new Vector4(0.45f, 0.15f, 0.15f, 0.85f), ImGuiColors.DPSRed, FontAwesomeIcon.ArrowDown);
                    }
                }
            }
        }

        private void DrawHistoryCard()
        {
            var availHeight = Math.Max(180.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - 4.0f);
            var entries = DiceHistoryManager.Instance.GetHistory();
            var localPlayer = PartySyncManager.Instance.GetLocalPlayerName();

            using (var card = ImRaii.Child("##RollHistoryCard", new Vector2(0, availHeight), true))
            {
                if (card.Success)
                {
                    // Header Row
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.History.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                    ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("RollHistoryHeader"));
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    UiUtils.Badge(entries.Count.ToString(), new Vector4(0.2f, 0.2f, 0.2f, 0.5f), ImGuiColors.DalamudGrey);

                    // Clear button on the right
                    var clearHistLabel = LocalizationManager.Instance.GetLocalizedString("ClearHistoryButton");
                    var clearHistWidth = ImGui.CalcTextSize(clearHistLabel).X + 24.0f * ImGuiHelpers.GlobalScale;
                    var rightX = ImGui.GetWindowContentRegionMax().X - clearHistWidth;
                    if (ImGui.GetCursorPosX() < rightX)
                        ImGui.SameLine(rightX);
                    else
                        ImGui.SameLine();

                    if (entries.Count == 0) ImGui.BeginDisabled();
                    if (UiUtils.IconButton("ClearHistBtn", FontAwesomeIcon.Trash, clearHistLabel))
                    {
                        DiceHistoryManager.Instance.Clear();
                    }
                    if (entries.Count == 0) ImGui.EndDisabled();

                    ImGui.Spacing();

                    // Search & Filter Row
                    UiUtils.StyledInputText("HistorySearch", ref historySearch, 64, width: 180.0f, hint: LocalizationManager.Instance.GetLocalizedString("RollHistorySearchHint"), icon: FontAwesomeIcon.Search);

                    ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                    DrawHistoryFilterChip(0, LocalizationManager.Instance.GetLocalizedString("RollHistoryFilterAll"));
                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    DrawHistoryFilterChip(1, LocalizationManager.Instance.GetLocalizedString("RollHistoryFilterMine"));
                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    DrawHistoryFilterChip(2, LocalizationManager.Instance.GetLocalizedString("RollHistoryFilterParty"));
                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    DrawHistoryFilterChip(3, LocalizationManager.Instance.GetLocalizedString("RollHistoryFilterPrivate"));

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    var filtered = entries.Where(e =>
                    {
                        if (historyFilter == 1 && !e.IsLocal && !string.Equals(e.CharacterName, localPlayer, StringComparison.OrdinalIgnoreCase)) return false;
                        if (historyFilter == 2 && (e.IsLocal || string.Equals(e.CharacterName, localPlayer, StringComparison.OrdinalIgnoreCase))) return false;
                        if (historyFilter == 3 && !e.IsPrivate) return false;

                        if (!string.IsNullOrWhiteSpace(historySearch))
                        {
                            bool matchName = e.CharacterName.Contains(historySearch, StringComparison.OrdinalIgnoreCase);
                            bool matchRoll = e.RollName.Contains(historySearch, StringComparison.OrdinalIgnoreCase);
                            bool matchResult = e.ResultDisplay.Contains(historySearch, StringComparison.OrdinalIgnoreCase);
                            bool matchDetails = e.Details.Contains(historySearch, StringComparison.OrdinalIgnoreCase);
                            if (!matchName && !matchRoll && !matchResult && !matchDetails) return false;
                        }
                        return true;
                    }).ToList();

                    if (filtered.Count == 0)
                    {
                        ImGui.Spacing();
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoRollHistoryMessage"));
                    }
                    else
                    {
                        using var listChild = ImRaii.Child("##RollHistoryList", new Vector2(0, 0), false);
                        if (listChild.Success)
                        {
                            for (int i = 0; i < filtered.Count; i++)
                            {
                                var entry = filtered[i];
                                ImGui.PushID($"HistEntry_{entry.Id}");

                                ImGui.TextDisabled($"[{entry.Timestamp:HH:mm:ss}]");
                                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                                // Character badge
                                string charLabel = !string.IsNullOrWhiteSpace(entry.CharacterName) ? entry.CharacterName : "Self";
                                UiUtils.PillBadge(charLabel, new Vector4(0.18f, 0.24f, 0.38f, 0.85f), ImGuiColors.ParsedBlue, FontAwesomeIcon.User);

                                if (entry.IsPrivate)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("RollPrivateTag"), new Vector4(0.35f, 0.18f, 0.45f, 0.85f), ImGuiColors.DalamudViolet, FontAwesomeIcon.UserSecret);
                                }

                                if (!string.IsNullOrWhiteSpace(entry.RollName))
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.PillBadge(entry.RollName, new Vector4(0.24f, 0.20f, 0.12f, 0.85f), ImGuiColors.ParsedGold, FontAwesomeIcon.DiceD20);
                                }

                                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                                Vector4 resultCol = entry.IsCriticalSuccess ? ImGuiColors.ParsedGreen : (entry.IsCriticalFailure ? ImGuiColors.DalamudRed : ImGuiColors.DalamudWhite);
                                ImGui.TextColored(resultCol, entry.ResultDisplay);

                                // Quick actions: Re-roll & Copy
                                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                                if (UiUtils.IconButton($"Reroll_{entry.Id}", FontAwesomeIcon.Redo, LocalizationManager.Instance.GetLocalizedString("RollHistoryReroll"), new Vector2(22, 20) * ImGuiHelpers.GlobalScale))
                                {
                                    if (!string.IsNullOrWhiteSpace(entry.RollName))
                                    {
                                        rollInputText = entry.RollName;
                                    }
                                }

                                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                if (UiUtils.IconButton($"Copy_{entry.Id}", FontAwesomeIcon.Clipboard, LocalizationManager.Instance.GetLocalizedString("RollHistoryCopy"), new Vector2(22, 20) * ImGuiHelpers.GlobalScale))
                                {
                                    ImGui.SetClipboardText($"{charLabel}: {entry.RollName} -> {entry.ResultDisplay}");
                                }

                                ImGui.PopID();
                                ImGui.Spacing();
                            }
                        }
                    }
                }
            }
        }

        private void DrawHistoryFilterChip(int index, string label)
        {
            bool isSelected = historyFilter == index;
            var bgCol = isSelected ? new Vector4(0.20f, 0.45f, 0.70f, 0.95f) : new Vector4(0.18f, 0.20f, 0.24f, 0.75f);
            var textCol = isSelected ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey;

            using (ImRaii.PushColor(ImGuiCol.Button, bgCol))
            using (ImRaii.PushColor(ImGuiCol.Text, textCol))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 10.0f * ImGuiHelpers.GlobalScale))
            {
                if (ImGui.SmallButton(label))
                {
                    historyFilter = index;
                }
            }
        }

        private void ExecuteRoll()
        {
            if (string.IsNullOrWhiteSpace(rollInputText)) return;

            try
            {
                Plugin.Log?.Information($"Rolling dice with input: {rollInputText}");
                DiceRoll? DR = DiceRoll.ParseDiceRollString(rollInputText, advantage, disadvantage);
                if (DR != null)
                {
                    var resultSeString = !detailedRoll ? DR.RollResultString : DR.RollDetailedResultString;
                    PartySyncManager.Instance.BroadcastDiceRoll(
                        rollInputText,
                        DR.RollResult,
                        string.Join(", ", DR.IndividualRolls),
                        echoText: LocalizationManager.Instance.GetLocalizedString("RollEchoResult", rollInputText, resultSeString.TextValue),
                        isPrivate: rollPrivate
                    );
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to roll dice with formula: '{rollInputText}'");
            }
        }
    }
}
