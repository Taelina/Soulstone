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
        private struct RollHistoryEntry
        {
            public DateTime Timestamp;
            public string Formula;
            public string ResultText;
        }

        private bool detailedRoll = false;
        private string rollInputText = "";
        private bool advantage = false;
        private bool disadvantage = false;

        private readonly List<RollHistoryEntry> rollHistory = new();
        private const int MaxHistoryCount = 30;

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
            string statName = currentSystem != null && currentSystem.initiativeStatType != InitiativeStatType.None && !string.IsNullOrEmpty(currentSystem.initiativeStatName)
                ? currentSystem.initiativeStatName
                : LocalizationManager.Instance.GetLocalizedString("InitiativeNone");

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

                    UiUtils.Badge($"{statName} ({(mod >= 0 ? $"+{mod}" : $"{mod}")})", new Vector4(0.14f, 0.38f, 0.20f, 0.85f), ImGuiColors.ParsedGreen);

                    ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                    if (UiUtils.IconButton("RollInitDiceTabBtn", FontAwesomeIcon.DiceD20, LocalizationManager.Instance.GetLocalizedString("InitiativeRollInitiative")))
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
                if (ImGui.Button($"{dice}##Quick_{dice}", new Vector2(btnWidth, 24.0f * ImGuiHelpers.GlobalScale)))
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
                if (ImGui.Button($"{mod}##Mod_{mod}", new Vector2(32.0f * ImGuiHelpers.GlobalScale, 24.0f * ImGuiHelpers.GlobalScale)))
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
            using (var card = ImRaii.Child("##RollControlCard", new Vector2(0, 95.0f * ImGuiHelpers.GlobalScale), true))
            {
                if (card.Success)
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("RollInputLabel"));
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                    var rollLabel = LocalizationManager.Instance.GetLocalizedString("ThrowButton");
                    var rollBtnWidth = 32.0f * ImGuiHelpers.GlobalScale;
                    var clearBtnWidth = 26.0f * ImGuiHelpers.GlobalScale;
                    var labelWidth = ImGui.CalcTextSize(LocalizationManager.Instance.GetLocalizedString("RollInputLabel")).X + 10.0f * ImGuiHelpers.GlobalScale;
                    var spacing = 18.0f * ImGuiHelpers.GlobalScale;

                    float inputWidth = Math.Max(120.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().X - clearBtnWidth - rollBtnWidth - spacing);

                    ImGui.SetNextItemWidth(inputWidth);
                    ImGui.InputTextWithHint("##RollInput", LocalizationManager.Instance.GetLocalizedString("DiceRollFormulaHint"), ref rollInputText, 100);

                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    if (ImGui.Button("x##ClearFormula", new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                    {
                        rollInputText = "";
                    }
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("ClearFormulaTooltip"));

                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.5f, 0.3f, 0.7f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.65f, 0.4f, 0.9f));
                    if (UiUtils.IconButton("ExecuteRoll", FontAwesomeIcon.DiceD20, rollLabel, new Vector2(rollBtnWidth, 24.0f * ImGuiHelpers.GlobalScale)))
                    {
                        ExecuteRoll();
                    }
                    ImGui.PopStyleColor(2);

                    ImGui.Spacing();
                    ImGui.Separator();

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
                        ImGui.SameLine(0, 16.0f * ImGuiHelpers.GlobalScale);
                    }

                    if (advantage)
                    {
                        UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("BadgeAdvantage"), new Vector4(0.2f, 0.6f, 0.3f, 0.3f), ImGuiColors.ParsedGreen);
                    }
                    else if (disadvantage)
                    {
                        UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("BadgeDisadvantage"), new Vector4(0.7f, 0.2f, 0.2f, 0.3f), ImGuiColors.DPSRed);
                    }
                    else
                    {
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NormalRollText"));
                    }
                }
            }
        }

        private void DrawHistoryCard()
        {
            var availHeight = Math.Max(150.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - 4.0f);

            using (var card = ImRaii.Child("##RollHistoryCard", new Vector2(0, availHeight), true))
            {
                if (card.Success)
                {
                    ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("RollHistoryHeader"));
                    ImGui.SameLine();
                    UiUtils.Badge(rollHistory.Count.ToString(), new Vector4(0.2f, 0.2f, 0.2f, 0.5f), ImGuiColors.DalamudGrey);

                    var clearHistLabel = LocalizationManager.Instance.GetLocalizedString("ClearHistoryButton");
                    var clearHistWidth = ImGui.CalcTextSize(clearHistLabel).X + 16.0f * ImGuiHelpers.GlobalScale;
                    var rightX = ImGui.GetWindowContentRegionMax().X - clearHistWidth;
                    if (ImGui.GetCursorPosX() < rightX)
                        ImGui.SameLine(rightX);
                    else
                        ImGui.SameLine();

                    if (rollHistory.Count == 0) ImGui.BeginDisabled();
                    if (ImGui.Button($"{clearHistLabel}###ClearHistBtn"))
                    {
                        rollHistory.Clear();
                    }
                    if (rollHistory.Count == 0) ImGui.EndDisabled();

                    ImGui.Separator();

                    if (rollHistory.Count == 0)
                    {
                        ImGui.Spacing();
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoRollHistoryMessage"));
                    }
                    else
                    {
                        for (int i = rollHistory.Count - 1; i >= 0; i--)
                        {
                            var entry = rollHistory[i];
                            ImGui.PushID($"History_{i}");
                            ImGui.TextDisabled($"[{entry.Timestamp:HH:mm:ss}]");
                            ImGui.SameLine();
                            UiUtils.Badge(entry.Formula, new Vector4(0.25f, 0.3f, 0.45f, 0.4f), ImGuiColors.ParsedBlue);
                            ImGui.SameLine();
                            ImGui.TextColored(ImGuiColors.DalamudWhite, entry.ResultText);
                            ImGui.PopID();
                        }
                    }
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
                    XivChatEntry rollMessage = new XivChatEntry
                    {
                        Message = resultSeString,
                        Type = XivChatType.Echo
                    };
                    Messages.SendMessage(rollMessage);
                    PartySyncManager.Instance.BroadcastDiceRoll(
                        rollInputText,
                        DR.RollResult,
                        string.Join(", ", DR.IndividualRolls),
                        echoText: $"[Soulstone] {rollInputText}: {resultSeString.TextValue}");

                    rollHistory.Add(new RollHistoryEntry
                    {
                        Timestamp = DateTime.Now,
                        Formula = rollInputText,
                        ResultText = resultSeString.TextValue
                    });

                    if (rollHistory.Count > MaxHistoryCount)
                    {
                        rollHistory.RemoveAt(0);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to execute roll for '{rollInputText}' in DiceWindow");
            }
        }
    }
}
