using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Utils;
using System;
using System.Linq;
using System.Numerics;

namespace Soulstone.Windows
{
    public class InitiativeTrackerWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly InitiativeTrackerManager manager;

        private string newParticipantName = string.Empty;
        private int newParticipantInit = 10;
        private int newParticipantBonus = 0;
        private string newParticipantNotes = string.Empty;

        public InitiativeTrackerWindow(Plugin plugin)
            : base("Initiative Tracker###SoulstoneInitiativeTracker", ImGuiWindowFlags.None)
        {
            this.plugin = plugin;
            manager = InitiativeTrackerManager.Instance;

            Size = new Vector2(620, 500);
            SizeCondition = ImGuiCond.FirstUseEver;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(450, 320),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public void Dispose() { }

        public override void Draw()
        {
            DrawControlsHeader();
            ImGui.Separator();
            ImGui.Spacing();

            DrawAddParticipantBar();
            ImGui.Separator();
            ImGui.Spacing();

            DrawParticipantsList();
        }

        private void DrawControlsHeader()
        {
            // Title and Round/Turn stats
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.Stopwatch.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

            ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("InitiativeTrackerTitle"));
            ImGui.SameLine(0, 12.0f * ImGuiHelpers.GlobalScale);

            string roundText = string.Format(LocalizationManager.Instance.GetLocalizedString("InitiativeRound"), manager.CurrentRound);
            UiUtils.Badge(roundText, new Vector4(0.2f, 0.4f, 0.6f, 0.85f), ImGuiColors.ParsedBlue);

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            string turnText = string.Format(LocalizationManager.Instance.GetLocalizedString("InitiativeTurn"), manager.CurrentTurnNumber);
            UiUtils.Badge(turnText, new Vector4(0.35f, 0.25f, 0.5f, 0.85f), ImGuiColors.DalamudViolet);

            if (manager.ActiveParticipant != null)
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                string activeText = $"{LocalizationManager.Instance.GetLocalizedString("InitiativeActiveTurn")}: {manager.ActiveParticipant.Name}";
                UiUtils.Badge(activeText, new Vector4(0.14f, 0.38f, 0.20f, 0.85f), ImGuiColors.ParsedGreen);
            }

            ImGui.Spacing();

            // Turn control buttons
            if (UiUtils.IconButton("NextTurnBtn", FontAwesomeIcon.StepForward, LocalizationManager.Instance.GetLocalizedString("InitiativeNextTurn")))
            {
                manager.NextTurn();
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconButton("PrevTurnBtn", FontAwesomeIcon.StepBackward, LocalizationManager.Instance.GetLocalizedString("InitiativePrevTurn")))
            {
                manager.PreviousTurn();
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconButton("ResetTurnsBtn", FontAwesomeIcon.Redo, LocalizationManager.Instance.GetLocalizedString("InitiativeReset")))
            {
                manager.ResetTurns();
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconButton("ClearAllInitBtn", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("InitiativeClearAll")))
            {
                manager.FullReset();
            }

            // Order toggle button
            ImGui.SameLine(0, 16.0f * ImGuiHelpers.GlobalScale);
            var sortIcon = manager.IsAscendingOrder ? FontAwesomeIcon.SortAmountUp : FontAwesomeIcon.SortAmountDown;
            var sortLabel = manager.IsAscendingOrder
                ? LocalizationManager.Instance.GetLocalizedString("InitiativeSortAsc")
                : LocalizationManager.Instance.GetLocalizedString("InitiativeSortDesc");
            if (UiUtils.IconButton("SortOrderBtn", sortIcon, sortLabel))
            {
                manager.SortParticipants(!manager.IsAscendingOrder);
            }

            // Add self / Roll for character
            var sheet = CharacterManager.Instance.CharacterSheet;
            if (sheet != null)
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconButton("AddSelfInitBtn", FontAwesomeIcon.UserPlus, LocalizationManager.Instance.GetLocalizedString("InitiativeAddSelf")))
                {
                    var diceSys = DiceSystemManager.Instance.CurrentDiceSystem;
                    var roll = sheet.RollInitiative(diceSys);
                    int bonus = sheet.GetInitiativeModifier(diceSys);
                    manager.AddOrUpdateCurrentCharacter(sheet, diceSys, roll.RollResult, bonus);
                }
            }
        }

        private void DrawAddParticipantBar()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InitiativeAddParticipant"));
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

            // Name
            ImGui.SetNextItemWidth(140.0f * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint("##NewInitName", LocalizationManager.Instance.GetLocalizedString("InitiativeParticipantName"), ref newParticipantName, 50);

            // Initiative Value
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            ImGui.SetNextItemWidth(70.0f * ImGuiHelpers.GlobalScale);
            ImGui.InputInt("##NewInitVal", ref newParticipantInit, 0);

            // Quick d20 roll button for this participant
            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconButton("QuickRollNewInitBtn", FontAwesomeIcon.DiceD20, "Roll d20 + Bonus", new Vector2(24, 20) * ImGuiHelpers.GlobalScale))
            {
                var rand = new Random();
                newParticipantInit = rand.Next(1, 21) + newParticipantBonus;
            }

            // Bonus Modifier
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            ImGui.AlignTextToFramePadding();
            ImGui.TextDisabled("+");
            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
            ImGui.InputInt("##NewInitBonus", ref newParticipantBonus, 0);

            // Add button
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconButton("AddInitParticipantBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddButton")))
            {
                string name = string.IsNullOrWhiteSpace(newParticipantName) ? $"Actor {manager.Participants.Count + 1}" : newParticipantName.Trim();
                manager.AddParticipant(name, newParticipantInit, newParticipantBonus, false, newParticipantNotes);
                newParticipantName = string.Empty;
                newParticipantNotes = string.Empty;
            }
        }

        private void DrawParticipantsList()
        {
            if (manager.Participants.Count == 0)
            {
                ImGui.Spacing();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors.DalamudGrey, FontAwesomeIcon.InfoCircle.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("InitiativeNoParticipants"));
                return;
            }

            using var table = ImRaii.Table("##InitiativeTable", 6, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.ScrollY);
            if (table.Success)
            {
                ImGui.TableSetupColumn("Active", ImGuiTableColumnFlags.WidthFixed, 32.0f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("InitiativeParticipantName"), ImGuiTableColumnFlags.WidthStretch, 0.35f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("InitiativeValue"), ImGuiTableColumnFlags.WidthStretch, 0.20f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("InitiativeBonus"), ImGuiTableColumnFlags.WidthStretch, 0.15f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("InitiativeNotes"), ImGuiTableColumnFlags.WidthStretch, 0.20f);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 65.0f * ImGuiHelpers.GlobalScale);
                ImGui.TableHeadersRow();

                string? participantToRemove = null;
                bool needsSort = false;

                for (int i = 0; i < manager.Participants.Count; i++)
                {
                    var p = manager.Participants[i];
                    bool isActive = (i == manager.ActiveParticipantIndex);

                    ImGui.PushID($"InitRow_{p.Id}");
                    ImGui.TableNextRow();

                    if (isActive)
                    {
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.14f, 0.38f, 0.20f, 0.40f)));
                    }

                    // Column 1: Active Turn Indicator / Selector
                    ImGui.TableNextColumn();
                    if (isActive)
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(ImGuiColors.ParsedGreen, FontAwesomeIcon.ChevronRight.ToIconString());
                        ImGui.PopFont();
                    }
                    else
                    {
                        if (UiUtils.IconButton($"SetActive_{p.Id}", FontAwesomeIcon.Circle, "Set Active", new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                        {
                            manager.SetActiveIndex(i);
                            manager.AnnounceTurn(p.Name, manager.CurrentRound);
                        }
                    }

                    // Column 2: Name
                    ImGui.TableNextColumn();
                    string nameVal = p.Name;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputText($"##Name_{p.Id}", ref nameVal, 50))
                    {
                        p.Name = nameVal;
                    }

                    // Column 3: Initiative Value
                    ImGui.TableNextColumn();
                    int initVal = p.InitiativeValue;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputInt($"##Init_{p.Id}", ref initVal, 1))
                    {
                        p.InitiativeValue = initVal;
                        needsSort = true;
                    }

                    // Column 4: Bonus Modifier
                    ImGui.TableNextColumn();
                    int bonusVal = p.BonusModifier;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputInt($"##Bonus_{p.Id}", ref bonusVal, 1))
                    {
                        p.BonusModifier = bonusVal;
                        needsSort = true;
                    }

                    // Column 5: Notes
                    ImGui.TableNextColumn();
                    string notesVal = p.Notes;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputText($"##Notes_{p.Id}", ref notesVal, 50))
                    {
                        p.Notes = notesVal;
                    }

                    // Column 6: Actions (Re-roll / Delete)
                    ImGui.TableNextColumn();
                    if (UiUtils.IconButton($"Reroll_{p.Id}", FontAwesomeIcon.DiceD20, "Re-roll d20 + Bonus", new Vector2(24, 20) * ImGuiHelpers.GlobalScale))
                    {
                        var rand = new Random();
                        p.InitiativeValue = rand.Next(1, 21) + p.BonusModifier;
                        needsSort = true;
                    }

                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    if (UiUtils.IconButton($"Delete_{p.Id}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("SupprButton"), new Vector2(24, 20) * ImGuiHelpers.GlobalScale))
                    {
                        participantToRemove = p.Id;
                    }

                    ImGui.PopID();
                }

                if (participantToRemove != null)
                {
                    manager.RemoveParticipant(participantToRemove);
                }

                if (needsSort)
                {
                    manager.SortParticipants(manager.IsAscendingOrder);
                }
            }
        }
    }
}
