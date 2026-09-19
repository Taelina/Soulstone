using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Utils;
using System;
using System.Collections.Generic;
using System.IO;
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
        private bool newParticipantIsNpc = false;
        private int participantFilterIndex = 0; // 0: All, 1: PCs, 2: NPCs
        private CharacterSheet? newParticipantSheet = null;
        private string newParticipantSheetPath = string.Empty;
        private int selectedPremadeSheetIndex = 0;
        private List<string> premadeSheetFiles = new();
        private DateTime lastPremadeFilesRefresh = DateTime.MinValue;

        // Add Buff Modal State
        private string addBuffTargetParticipantId = string.Empty;
        private bool showAddBuffModal = false;
        private string newBuffName = string.Empty;
        private int newBuffDuration = 3;
        private string newBuffTargetStat = string.Empty;
        private int newBuffValue = 1;
        private bool newBuffIsDebuff = false;
        private string newBuffDescription = string.Empty;

        // Attach Sheet Modal State
        private bool showAttachSheetModal = false;
        private string attachSheetParticipantId = string.Empty;
        private int attachModalSheetIndex = 0;

        // NPC Sheet Inspector Modal State
        private bool showNpcSheetModal = false;
        private InitiativeParticipant? selectedNpcParticipant = null;

        public InitiativeTrackerWindow(Plugin plugin)
            : base("Initiative Tracker###SoulstoneInitiativeTracker", ImGuiWindowFlags.None)
        {
            this.plugin = plugin;
            manager = InitiativeTrackerManager.Instance;

            Size = new Vector2(760, 540);
            SizeCondition = ImGuiCond.FirstUseEver;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(520, 340),
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
            DrawAddBuffModal();
            DrawAttachSheetModal();
            DrawNpcSheetModal();
        }

        private void RefreshPremadeSheetFiles()
        {
            if ((DateTime.Now - lastPremadeFilesRefresh).TotalSeconds > 2)
            {
                premadeSheetFiles = InitiativeTrackerManager.GetAvailablePremadeSheetFiles();
                lastPremadeFilesRefresh = DateTime.Now;
            }
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

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (PartySyncManager.Instance.IsLocalPlayerPartyLeader())
            {
                UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("InitiativeDMBadge"), new Vector4(0.35f, 0.28f, 0.12f, 0.85f), ImGuiColors.ParsedGold);
            }
            else
            {
                UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("InitiativeMemberBadge"), new Vector4(0.2f, 0.4f, 0.6f, 0.85f), ImGuiColors.ParsedBlue);
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

            // Group Management button
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconButton("OpenGroupFromInitBtn", FontAwesomeIcon.Users, LocalizationManager.Instance.GetLocalizedString("GroupOpenWindow")))
            {
                plugin.ToggleGroupUi();
            }

            // PC / NPC Filters & Counts
            int totalCount = manager.Participants.Count;
            int pcCount = manager.Participants.Count(p => !p.IsNpc);
            int npcCount = manager.Participants.Count(p => p.IsNpc);

            ImGui.SameLine(0, 14.0f * ImGuiHelpers.GlobalScale);
            DrawFilterChip(0, $"{LocalizationManager.Instance.GetLocalizedString("InitiativeFilterAll")} ({totalCount})");
            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            DrawFilterChip(1, $"{LocalizationManager.Instance.GetLocalizedString("InitiativeFilterPc")} ({pcCount})");
            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            DrawFilterChip(2, $"{LocalizationManager.Instance.GetLocalizedString("InitiativeFilterNpc")} ({npcCount})");
        }

        private void DrawFilterChip(int index, string label)
        {
            bool isSelected = participantFilterIndex == index;
            var bgCol = isSelected ? new Vector4(0.20f, 0.45f, 0.70f, 0.95f) : new Vector4(0.18f, 0.20f, 0.24f, 0.75f);
            var textCol = isSelected ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey;

            using (ImRaii.PushColor(ImGuiCol.Button, bgCol))
            using (ImRaii.PushColor(ImGuiCol.Text, textCol))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 10.0f * ImGuiHelpers.GlobalScale))
            {
                if (ImGui.SmallButton(label))
                {
                    participantFilterIndex = index;
                }
            }
        }

        private void DrawAddParticipantBar()
        {
            RefreshPremadeSheetFiles();
            var diceSys = DiceSystemManager.Instance.CurrentDiceSystem;

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InitiativeAddParticipant"));
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

            // Name
            UiUtils.StyledInputText("NewInitName", ref newParticipantName, 50, width: 120.0f, hint: LocalizationManager.Instance.GetLocalizedString("InitiativeParticipantName"));

            // Sheet template / premade file selector
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            ImGui.SetNextItemWidth(130.0f * ImGuiHelpers.GlobalScale);

            var options = new List<string>
            {
                LocalizationManager.Instance.GetLocalizedString("InitiativeNoSheet"),
                LocalizationManager.Instance.GetLocalizedString("InitiativeCreateNpcSheet")
            };
            foreach (var f in premadeSheetFiles)
            {
                options.Add(Path.GetFileNameWithoutExtension(f));
            }

            if (selectedPremadeSheetIndex >= options.Count) selectedPremadeSheetIndex = 0;

            if (UiUtils.StyledCombo("##NewParticipantSheetCombo", ref selectedPremadeSheetIndex, options.ToArray(), icon: FontAwesomeIcon.FileAlt, width: 130.0f))
            {
                if (selectedPremadeSheetIndex == 0)
                {
                    newParticipantSheet = null;
                    newParticipantSheetPath = string.Empty;
                    newParticipantIsNpc = false;
                }
                else if (selectedPremadeSheetIndex == 1)
                {
                    string npcName = !string.IsNullOrWhiteSpace(newParticipantName) ? newParticipantName.Trim() : "NPC";
                    newParticipantSheet = new CharacterSheet { CharacterFullName = npcName };
                    if (diceSys != null)
                    {
                        newParticipantSheet.ApplyRulesetTemplate(diceSys);
                    }
                    newParticipantSheetPath = string.Empty;
                    newParticipantBonus = newParticipantSheet.GetInitiativeModifier(diceSys);
                    newParticipantIsNpc = true;
                }
                else
                {
                    int fileIdx = selectedPremadeSheetIndex - 2;
                    if (fileIdx >= 0 && fileIdx < premadeSheetFiles.Count)
                    {
                        string filePath = premadeSheetFiles[fileIdx];
                        var loaded = CharacterSheet.LoadSheet(filePath, isFullPath: true);
                        if (loaded != null)
                        {
                            newParticipantSheet = loaded;
                            newParticipantSheetPath = filePath;
                            if (string.IsNullOrWhiteSpace(newParticipantName) && !string.IsNullOrWhiteSpace(loaded.CharacterFullName))
                            {
                                newParticipantName = loaded.CharacterFullName;
                            }
                            newParticipantBonus = loaded.GetInitiativeModifier(diceSys);
                            newParticipantIsNpc = true;
                        }
                    }
                }
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("InitiativeSheetSelectorHint"));
            }

            // Initiative Value
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            UiUtils.StyledInputInt("NewInitVal", ref newParticipantInit, step: 0, width: 60.0f);

            // System roll button for this participant
            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            var diceIcon = diceSys?.diceType == DiceType.d20 ? FontAwesomeIcon.DiceD20 : FontAwesomeIcon.Dice;
            string rollTooltip = LocalizationManager.Instance.GetLocalizedString("InitiativeRollTooltip");
            if (UiUtils.IconButton("QuickRollNewInitBtn", diceIcon, rollTooltip, new Vector2(24, 22) * ImGuiHelpers.GlobalScale))
            {
                if (newParticipantSheet != null)
                {
                    newParticipantBonus = newParticipantSheet.GetInitiativeModifier(diceSys);
                    var roll = newParticipantSheet.RollInitiative(diceSys);
                    newParticipantInit = roll.RollResult;
                }
                else
                {
                    var roll = DiceRoll.RollStatWithSystem(diceSys, "Initiative", newParticipantBonus)
                        ?? DiceRoll.RollDiceRegular(1, DiceRoll.GetSystemSides(diceSys), newParticipantBonus, "Initiative");
                    newParticipantInit = roll.RollResult;
                }
            }

            // Bonus Modifier
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            ImGui.AlignTextToFramePadding();
            ImGui.TextDisabled("+");
            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            UiUtils.StyledInputInt("NewInitBonus", ref newParticipantBonus, step: 0, width: 45.0f);

            // NPC Checkbox
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            ImGui.Checkbox("NPC##NewInitIsNpc", ref newParticipantIsNpc);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("InitiativeIsNpcTooltip"));
            }

            // Add button
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconButton("AddInitParticipantBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddButton")))
            {
                string name = string.IsNullOrWhiteSpace(newParticipantName) ? $"Actor {manager.Participants.Count + 1}" : newParticipantName.Trim();
                if (newParticipantSheet != null)
                {
                    newParticipantSheet.CharacterFullName = name;
                }
                manager.AddParticipant(name, newParticipantInit, newParticipantBonus, false, newParticipantNotes, null, newParticipantSheet, newParticipantSheetPath, autoSort: true, isNpc: newParticipantIsNpc);
                newParticipantName = string.Empty;
                newParticipantNotes = string.Empty;
                newParticipantSheet = null;
                newParticipantSheetPath = string.Empty;
                selectedPremadeSheetIndex = 0;
                newParticipantIsNpc = false;
            }
        }

        private void OpenAddBuffModal(string participantId)
        {
            addBuffTargetParticipantId = participantId;
            newBuffName = string.Empty;
            newBuffDuration = 3;
            newBuffTargetStat = string.Empty;
            newBuffValue = 1;
            newBuffIsDebuff = false;
            newBuffDescription = string.Empty;
            showAddBuffModal = true;
        }

        private void DrawAddBuffModal()
        {
            if (!showAddBuffModal) return;

            ImGui.OpenPopup("AddBuffModal###SoulstoneAddBuffModal");
            var targetParticipant = manager.Participants.FirstOrDefault(p => p.Id == addBuffTargetParticipantId);

            var center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(380.0f * ImGuiHelpers.GlobalScale, 0), ImGuiCond.Always);

            if (ImGui.BeginPopupModal("AddBuffModal###SoulstoneAddBuffModal", ref showAddBuffModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextColored(ImGuiColors.ParsedGold, targetParticipant != null 
                    ? $"{LocalizationManager.Instance.GetLocalizedString("BuffModalTitle")}: {targetParticipant.Name}" 
                    : LocalizationManager.Instance.GetLocalizedString("BuffModalTitle"));
                ImGui.Separator();
                ImGui.Spacing();

                // Name
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("BuffNameLabel"));
                UiUtils.StyledInputText("NewBuffName", ref newBuffName, 60, width: -1.0f, hint: "e.g. Haste, Bless, Poison, Weakness");

                // Duration (turns)
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("BuffDurationLabel"));
                UiUtils.StyledInputInt("NewBuffDuration", ref newBuffDuration, step: 1, width: 100.0f, min: 1);

                // Target Stat
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("BuffTargetStatLabel"));
                UiUtils.StyledInputText("NewBuffTargetStat", ref newBuffTargetStat, 60, width: -1.0f, hint: LocalizationManager.Instance.GetLocalizedString("BuffStatNameHint"));

                // Value / Modifier
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("BuffValueLabel"));
                if (UiUtils.StyledInputInt("NewBuffValue", ref newBuffValue, step: 1, width: 100.0f))
                {
                    if (newBuffValue < 0) newBuffIsDebuff = true;
                }

                // Debuff checkbox
                ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("BuffIsDebuffLabel"), ref newBuffIsDebuff);

                // Description
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("DiceSysResourceDescription"));
                UiUtils.StyledInputText("NewBuffDesc", ref newBuffDescription, 120, width: -1.0f);

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (UiUtils.IconTextButton("ConfirmAddBuffBtn", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), size: new Vector2(120.0f * ImGuiHelpers.GlobalScale, 0)))
                {
                    if (!string.IsNullOrWhiteSpace(newBuffName) && targetParticipant != null)
                    {
                        int val = newBuffValue;
                        if (newBuffIsDebuff && val > 0)
                        {
                            val = -val;
                        }
                        var buff = new Buff(newBuffName.Trim(), Math.Max(1, newBuffDuration), newBuffTargetStat.Trim(), val, newBuffDescription.Trim(), newBuffIsDebuff);
                        manager.AddBuffToParticipant(targetParticipant.Id, buff);
                        showAddBuffModal = false;
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.SameLine();
                if (UiUtils.IconTextButton("CancelAddBuffBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90.0f * ImGuiHelpers.GlobalScale, 0)))
                {
                    showAddBuffModal = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
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

            using var table = ImRaii.Table("##InitiativeTable", 7, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.ScrollY);
            if (table.Success)
            {
                ImGui.TableSetupColumn("Active", ImGuiTableColumnFlags.WidthFixed, 32.0f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("InitiativeParticipantName"), ImGuiTableColumnFlags.WidthStretch, 0.22f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("InitiativeValue"), ImGuiTableColumnFlags.WidthStretch, 0.12f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("InitiativeBonus"), ImGuiTableColumnFlags.WidthStretch, 0.10f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("BuffsColumn"), ImGuiTableColumnFlags.WidthStretch, 0.32f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("InitiativeNotes"), ImGuiTableColumnFlags.WidthStretch, 0.14f);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 115.0f * ImGuiHelpers.GlobalScale);
                ImGui.TableHeadersRow();

                string? participantToRemove = null;
                bool needsSort = false;
                var diceSys = DiceSystemManager.Instance.CurrentDiceSystem;
                var diceIcon = diceSys?.diceType == DiceType.d20 ? FontAwesomeIcon.DiceD20 : FontAwesomeIcon.Dice;

                for (int i = 0; i < manager.Participants.Count; i++)
                {
                    var p = manager.Participants[i];
                    if (participantFilterIndex == 1 && p.IsNpc) continue;
                    if (participantFilterIndex == 2 && !p.IsNpc) continue;

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
                        var roleIcon = p.IsNpc ? FontAwesomeIcon.Skull : FontAwesomeIcon.User;
                        if (UiUtils.IconButton($"SetActive_{p.Id}", roleIcon, LocalizationManager.Instance.GetLocalizedString("InitiativeSetActiveTooltip"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                        {
                            manager.SetActiveIndex(i);
                            manager.AnnounceTurn(p.Name, manager.CurrentRound);
                        }
                    }

                    // Column 2: Name + Sheet badge/indicator + PC/NPC badge
                    ImGui.TableNextColumn();

                    // PC / NPC badge button (clickable to toggle between PC and NPC)
                    if (p.IsNpc)
                    {
                        using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.48f, 0.18f, 0.18f, 0.9f)))
                        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
                        using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 6.0f * ImGuiHelpers.GlobalScale))
                        {
                            if (ImGui.SmallButton($"NPC##ToggleNpc_{p.Id}"))
                            {
                                p.IsNpc = false;
                                manager.SyncParticipantWithCharacterSheet(p);
                                if (PartySyncManager.Instance.IsSessionHost || PartySyncManager.Instance.IsLocalPlayerPartyLeader())
                                {
                                    PartySyncManager.Instance.BroadcastParticipantUpsert(p);
                                }
                            }
                        }
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("InitiativeToggleToPcTooltip"));
                    }
                    else
                    {
                        using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.18f, 0.32f, 0.48f, 0.9f)))
                        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedBlue))
                        using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 6.0f * ImGuiHelpers.GlobalScale))
                        {
                            if (ImGui.SmallButton($"PC##ToggleNpc_{p.Id}"))
                            {
                                p.IsNpc = true;
                                manager.SyncParticipantWithCharacterSheet(p);
                                if (PartySyncManager.Instance.IsSessionHost || PartySyncManager.Instance.IsLocalPlayerPartyLeader())
                                {
                                    PartySyncManager.Instance.BroadcastParticipantUpsert(p);
                                }
                            }
                        }
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("InitiativeToggleToNpcTooltip"));
                    }

                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    string nameVal = p.Name;
                    float nameInputWidth = (p.CharacterSheet != null || p.IsCurrentCharacter) 
                        ? Math.Max(50.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().X - 26.0f * ImGuiHelpers.GlobalScale) 
                        : -1;
                    if (UiUtils.StyledInputText($"Name_{p.Id}", ref nameVal, 50, width: nameInputWidth > 0 ? nameInputWidth / ImGuiHelpers.GlobalScale : -1.0f))
                    {
                        p.Name = nameVal;
                        if (p.CharacterSheet != null)
                        {
                            p.CharacterSheet.CharacterFullName = nameVal;
                        }
                    }
                    if (p.CharacterSheet != null || p.IsCurrentCharacter)
                    {
                        ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                        if (UiUtils.IconButton($"OpenSheetRow_{p.Id}", FontAwesomeIcon.AddressCard, LocalizationManager.Instance.GetLocalizedString("InitiativeNpcSheet"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                        {
                            selectedNpcParticipant = p;
                            showNpcSheetModal = true;
                        }
                    }

                    // Column 3: Initiative Value
                    ImGui.TableNextColumn();
                    int initVal = p.InitiativeValue;
                    if (UiUtils.StyledInputInt($"Init_{p.Id}", ref initVal, step: 1, width: -1.0f))
                    {
                        p.InitiativeValue = initVal;
                        needsSort = true;
                    }

                    // Column 4: Bonus Modifier
                    ImGui.TableNextColumn();
                    int bonusVal = p.BonusModifier;
                    if (UiUtils.StyledInputInt($"Bonus_{p.Id}", ref bonusVal, step: 1, width: -1.0f))
                    {
                        p.BonusModifier = bonusVal;
                        needsSort = true;
                    }

                    // Column 5: Buffs & Debuffs
                    ImGui.TableNextColumn();
                    string? buffToRemove = null;
                    if (p.Buffs != null && p.Buffs.Count > 0)
                    {
                        for (int bIdx = 0; bIdx < p.Buffs.Count; bIdx++)
                        {
                            var buff = p.Buffs[bIdx];
                            if (bIdx > 0)
                            {
                                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                            }

                            string badgeText = $"{buff.Name} ({buff.Duration}t)";
                            var badgeBg = buff.IsDebuff ? new Vector4(0.35f, 0.12f, 0.12f, 0.85f) : new Vector4(0.12f, 0.30f, 0.16f, 0.85f);
                            var badgeCol = buff.IsDebuff ? ImGuiColors.DalamudRed : ImGuiColors.ParsedGreen;

                            UiUtils.Badge(badgeText, badgeBg, badgeCol);
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.TextColored(badgeCol, $"{buff.Name} {(buff.IsDebuff ? "[Debuff]" : "[Buff]")}");
                                ImGui.Separator();
                                ImGui.Text(string.Format(LocalizationManager.Instance.GetLocalizedString("BuffDurationRemaining"), buff.Duration));
                                if (!string.IsNullOrWhiteSpace(buff.Description))
                                {
                                    ImGui.TextDisabled(buff.Description);
                                }
                                string mods = buff.GetFormattedModifiers();
                                if (!string.IsNullOrWhiteSpace(mods))
                                {
                                    ImGui.TextColored(ImGuiColors.ParsedGold, $"{LocalizationManager.Instance.GetLocalizedString("StatModifiersLabel")} {mods}");
                                }
                                ImGui.Separator();
                                ImGui.TextDisabled("Right click to manage");
                                ImGui.EndTooltip();
                            }

                            if (ImGui.BeginPopupContextItem($"BuffCtx_{buff.Id}"))
                            {
                                ImGui.TextColored(badgeCol, buff.Name);
                                ImGui.Separator();
                                if (ImGui.MenuItem("+1 Turn"))
                                {
                                    buff.Duration++;
                                    manager.SyncParticipantWithCharacterSheet(p);
                                }
                                if (ImGui.MenuItem("-1 Turn"))
                                {
                                    if (buff.Tick(1))
                                    {
                                        buffToRemove = buff.Id;
                                    }
                                    else
                                    {
                                        manager.SyncParticipantWithCharacterSheet(p);
                                    }
                                }
                                ImGui.Separator();
                                if (ImGui.MenuItem(LocalizationManager.Instance.GetLocalizedString("SupprButton") == "-" ? "Remove" : LocalizationManager.Instance.GetLocalizedString("SupprButton")))
                                {
                                    buffToRemove = buff.Id;
                                }
                                ImGui.EndPopup();
                            }
                        }
                        ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    }

                    if (UiUtils.IconButton($"AddBuffTo_{p.Id}", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddBuffButton"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                    {
                        OpenAddBuffModal(p.Id);
                    }

                    if (buffToRemove != null)
                    {
                        manager.RemoveBuffFromParticipant(p.Id, buffToRemove);
                    }

                    // Column 6: Notes
                    ImGui.TableNextColumn();
                    string notesVal = p.Notes;
                    if (UiUtils.StyledInputText($"Notes_{p.Id}", ref notesVal, 50, width: -1.0f))
                    {
                        p.Notes = notesVal;
                    }

                    // Column 7: Actions (Re-roll / Sheet / Add Buff / Delete)
                    ImGui.TableNextColumn();
                    string rerollTooltip = LocalizationManager.Instance.GetLocalizedString("InitiativeRerollTooltip");
                    if (UiUtils.IconButton($"Reroll_{p.Id}", diceIcon, rerollTooltip, new Vector2(24, 22) * ImGuiHelpers.GlobalScale))
                    {
                        manager.RerollParticipant(p.Id, diceSys);
                        needsSort = true;
                    }

                    ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
                    if (p.CharacterSheet != null || p.IsCurrentCharacter)
                    {
                        if (UiUtils.IconButton($"SheetAction_{p.Id}", FontAwesomeIcon.AddressCard, LocalizationManager.Instance.GetLocalizedString("InitiativeNpcSheet"), new Vector2(24, 22) * ImGuiHelpers.GlobalScale))
                        {
                            selectedNpcParticipant = p;
                            showNpcSheetModal = true;
                        }
                    }
                    else
                    {
                        if (UiUtils.IconButton($"AttachSheet_{p.Id}", FontAwesomeIcon.FileMedical, LocalizationManager.Instance.GetLocalizedString("InitiativeAttachSheet"), new Vector2(24, 22) * ImGuiHelpers.GlobalScale))
                        {
                            attachSheetParticipantId = p.Id;
                            showAttachSheetModal = true;
                        }
                    }

                    ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
                    if (UiUtils.IconButton($"AddBuffAction_{p.Id}", FontAwesomeIcon.Magic, LocalizationManager.Instance.GetLocalizedString("BuffModalTitle"), new Vector2(24, 22) * ImGuiHelpers.GlobalScale))
                    {
                        OpenAddBuffModal(p.Id);
                    }

                    ImGui.SameLine(0, 3.0f * ImGuiHelpers.GlobalScale);
                    if (UiUtils.IconButton($"Delete_{p.Id}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("SupprButton"), new Vector2(24, 22) * ImGuiHelpers.GlobalScale))
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

        private void DrawAttachSheetModal()
        {
            if (!showAttachSheetModal) return;

            var targetParticipant = manager.Participants.FirstOrDefault(p => p.Id == attachSheetParticipantId);
            if (targetParticipant == null)
            {
                showAttachSheetModal = false;
                return;
            }

            ImGui.OpenPopup("AttachSheetModal###SoulstoneAttachSheetModal");
            var center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(400.0f * ImGuiHelpers.GlobalScale, 0), ImGuiCond.Always);

            if (ImGui.BeginPopupModal("AttachSheetModal###SoulstoneAttachSheetModal", ref showAttachSheetModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                string modalTitle = string.Format(LocalizationManager.Instance.GetLocalizedString("InitiativeAttachSheetModalTitle"), targetParticipant.Name);
                ImGui.TextColored(ImGuiColors.ParsedGold, modalTitle);
                ImGui.Separator();
                ImGui.Spacing();

                RefreshPremadeSheetFiles();
                var options = new List<string>
                {
                    LocalizationManager.Instance.GetLocalizedString("InitiativeCreateNpcSheet")
                };
                foreach (var f in premadeSheetFiles)
                {
                    options.Add(Path.GetFileNameWithoutExtension(f));
                }

                if (attachModalSheetIndex >= options.Count) attachModalSheetIndex = 0;

                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("InitiativeSheetSelectorHint"));
                UiUtils.StyledCombo("##AttachModalCombo", ref attachModalSheetIndex, options.ToArray(), icon: FontAwesomeIcon.FileAlt);

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                var diceSys = DiceSystemManager.Instance.CurrentDiceSystem;

                if (UiUtils.IconTextButton("ConfirmAttachSheetBtn", FontAwesomeIcon.Link, LocalizationManager.Instance.GetLocalizedString("InitiativeAttachSheet"), size: new Vector2(130.0f * ImGuiHelpers.GlobalScale, 0)))
                {
                    if (attachModalSheetIndex == 0)
                    {
                        var newSheet = new CharacterSheet { CharacterFullName = targetParticipant.Name };
                        if (diceSys != null) newSheet.ApplyRulesetTemplate(diceSys);
                        manager.AttachSheetToParticipant(targetParticipant.Id, newSheet);
                    }
                    else
                    {
                        int fileIdx = attachModalSheetIndex - 1;
                        if (fileIdx >= 0 && fileIdx < premadeSheetFiles.Count)
                        {
                            string filePath = premadeSheetFiles[fileIdx];
                            var loaded = CharacterSheet.LoadSheet(filePath, isFullPath: true);
                            if (loaded != null)
                            {
                                manager.AttachSheetToParticipant(targetParticipant.Id, loaded, filePath);
                            }
                        }
                    }
                    showAttachSheetModal = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (UiUtils.IconTextButton("CancelAttachSheetBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90.0f * ImGuiHelpers.GlobalScale, 0)))
                {
                    showAttachSheetModal = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        private void DrawNpcSheetModal()
        {
            if (!showNpcSheetModal || selectedNpcParticipant == null) return;

            var target = selectedNpcParticipant;
            var sheet = target.IsCurrentCharacter ? CharacterManager.Instance.CharacterSheet : target.CharacterSheet;
            if (sheet == null)
            {
                showNpcSheetModal = false;
                return;
            }

            var diceSys = DiceSystemManager.Instance.CurrentDiceSystem;

            ImGui.OpenPopup("NpcSheetModal###SoulstoneNpcSheetModal");
            var center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(620.0f * ImGuiHelpers.GlobalScale, 520.0f * ImGuiHelpers.GlobalScale), ImGuiCond.FirstUseEver);

            if (ImGui.BeginPopupModal("NpcSheetModal###SoulstoneNpcSheetModal", ref showNpcSheetModal, ImGuiWindowFlags.None))
            {
                // Header
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.UserShield.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

                string actorName = !string.IsNullOrWhiteSpace(sheet.CharacterFullName) ? sheet.CharacterFullName : target.Name;
                string sheetTitle = string.Format(LocalizationManager.Instance.GetLocalizedString("InitiativeNpcSheetTitle"), actorName);
                ImGui.TextColored(ImGuiColors.ParsedGold, sheetTitle);

                float headerBtnWidth = 240.0f * ImGuiHelpers.GlobalScale;
                ImGui.SameLine(Math.Max(300.0f * ImGuiHelpers.GlobalScale, ImGui.GetWindowWidth() - headerBtnWidth));

                if (UiUtils.IconButton("SaveNpcSheetBtn", FontAwesomeIcon.Save, LocalizationManager.Instance.GetLocalizedString("InitiativeSaveSheet")))
                {
                    CharacterSheet.SaveSheet(sheet);
                }

                if (!target.IsCurrentCharacter)
                {
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    if (UiUtils.IconButton("DetachNpcSheetBtn", FontAwesomeIcon.Unlink, LocalizationManager.Instance.GetLocalizedString("InitiativeDetachSheet")))
                    {
                        manager.DetachSheetFromParticipant(target.Id);
                        showNpcSheetModal = false;
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.Separator();
                ImGui.Spacing();

                using var tabBar = ImRaii.TabBar("##NpcSheetTabs", ImGuiTabBarFlags.None);
                if (tabBar.Success)
                {
                    // Tab 1: Vitals & Resources
                    if (ImGui.BeginTabItem($"{LocalizationManager.Instance.GetLocalizedString("DiceSysResourcesHeader")}###NpcVitalsTab"))
                    {
                        DrawNpcVitalsTab(sheet, diceSys);
                        ImGui.EndTabItem();
                    }

                    // Tab 2: Attributes & Skills
                    if (ImGui.BeginTabItem($"{LocalizationManager.Instance.GetLocalizedString("StatSheetTab")}###NpcStatsTab"))
                    {
                        DrawNpcStatsTab(sheet, diceSys);
                        ImGui.EndTabItem();
                    }

                    // Tab 3: Abilities & Actions
                    if (ImGui.BeginTabItem($"{LocalizationManager.Instance.GetLocalizedString("AbilityLabel").TrimEnd(' ', ':')}###NpcAbilitiesTab"))
                    {
                        DrawNpcAbilitiesTab(sheet, diceSys);
                        ImGui.EndTabItem();
                    }

                    // Tab 4: Buffs
                    if (ImGui.BeginTabItem($"{LocalizationManager.Instance.GetLocalizedString("BuffsHeader")}###NpcBuffsTab"))
                    {
                        DrawNpcBuffsTab(target, sheet);
                        ImGui.EndTabItem();
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (UiUtils.IconTextButton("CloseNpcSheetModalBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CloseButton"), size: new Vector2(100.0f * ImGuiHelpers.GlobalScale, 0)))
                {
                    showNpcSheetModal = false;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        private void DrawNpcVitalsTab(CharacterSheet sheet, DiceSystem? diceSys)
        {
            using var child = ImRaii.Child("##NpcVitalsChild", new Vector2(0, 360.0f * ImGuiHelpers.GlobalScale), false);
            if (!child.Success) return;

            // Recalculate button
            if (UiUtils.IconButton("RecalcNpcVitalsBtn", FontAwesomeIcon.Sync, LocalizationManager.Instance.GetLocalizedString("RecalculateResourcesBtn")))
            {
                sheet.RecalculateAllResourceMaxes(diceSys);
            }
            ImGui.Spacing();

            var resources = sheet.GetEffectiveResources(diceSys);
            if (resources == null || resources.Count == 0)
            {
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("DiceSysNoResources"));
                return;
            }

            foreach (var res in resources)
            {
                int curRes = res.CurrentValue;
                int maxRes = res.MaxValue > 0 ? res.MaxValue : 100;
                var resDef = diceSys?.SystemResources.FirstOrDefault(d => string.Equals(d.Name, res.Name, StringComparison.OrdinalIgnoreCase));
                var resColor = UiUtils.GetResourceColor(res.Name, resDef?.ColorHex);

                ImGui.TextColored(resColor, res.Name);
                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.StyledInputInt($"NpcCur_{res.Name}", ref curRes, step: 0, width: 70.0f)) sheet.SetResourceCurrent(res.Name, curRes);
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                ImGui.TextDisabled("/");
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.StyledInputInt($"NpcMax_{res.Name}", ref maxRes, step: 0, width: 70.0f)) sheet.SetResourceMax(res.Name, Math.Max(1, maxRes));

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.SmallButton($"-5##{res.Name}M5")) sheet.SetResourceCurrent(res.Name, Math.Max(0, res.CurrentValue - 5));
                ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.SmallButton($"-1##{res.Name}M1")) sheet.SetResourceCurrent(res.Name, Math.Max(0, res.CurrentValue - 1));
                ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.SmallButton($"+1##{res.Name}P1")) sheet.SetResourceCurrent(res.Name, res.CurrentValue + 1);
                ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.SmallButton($"+5##{res.Name}P5")) sheet.SetResourceCurrent(res.Name, res.CurrentValue + 5);

                UiUtils.DrawProgressBar(res.CurrentValue, maxRes, $"{res.CurrentValue} / {maxRes}", new Vector2(-1, 16.0f * ImGuiHelpers.GlobalScale), resColor);
                ImGui.Spacing();
            }
        }

        private void DrawNpcStatsTab(CharacterSheet sheet, DiceSystem? diceSys)
        {
            using var child = ImRaii.Child("##NpcStatsChild", new Vector2(0, 360.0f * ImGuiHelpers.GlobalScale), false);
            if (!child.Success) return;

            var diceIcon = diceSys?.diceType == DiceType.d20 ? FontAwesomeIcon.DiceD20 : FontAwesomeIcon.Dice;

            // Attributes
            ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("AttributeLabel"));
            ImGui.Spacing();

            var attrs = sheet.GetEffectiveAttributes(diceSys);
            if (attrs != null && attrs.Count > 0)
            {
                using var table = ImRaii.Table("##NpcAttrTable", 4, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp);
                if (table.Success)
                {
                    ImGui.TableSetupColumn("Attribute", ImGuiTableColumnFlags.WidthStretch, 0.4f);
                    ImGui.TableSetupColumn("Base", ImGuiTableColumnFlags.WidthFixed, 60.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Total", ImGuiTableColumnFlags.WidthFixed, 60.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 36.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableHeadersRow();

                    foreach (var kv in attrs)
                    {
                        var attr = kv.Value;
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted(attr.Name);

                        ImGui.TableNextColumn();
                        int baseVal = attr.Value;
                        if (UiUtils.StyledInputInt($"NpcAttr_{attr.Name}", ref baseVal, step: 0, width: -1.0f))
                        {
                            attr.Value = baseVal;
                        }

                        ImGui.TableNextColumn();
                        int totalVal = sheet.GetEffectiveAttributeValue(attr.Name);
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextColored(ImGuiColors.ParsedGreen, totalVal >= 0 ? $"+{totalVal}" : $"{totalVal}");

                        ImGui.TableNextColumn();
                        if (UiUtils.IconButton($"RollNpcAttr_{attr.Name}", diceIcon, $"Roll {attr.Name}", new Vector2(26, 24) * ImGuiHelpers.GlobalScale))
                        {
                            var roll = DiceRoll.RollStatWithSystem(diceSys, attr.Name, totalVal)
                                ?? DiceRoll.RollDiceRegular(1, DiceRoll.GetSystemSides(diceSys), totalVal, attr.Name);
                            try
                            {
                                string actor = !string.IsNullOrWhiteSpace(sheet.CharacterFullName) ? sheet.CharacterFullName : "NPC";
                                string echo = LocalizationManager.Instance.GetLocalizedString("InitiativeRollEchoFormat", actor, $"{attr.Name} -> {roll.RollResultString.TextValue}");
                                PartySyncManager.Instance.BroadcastDiceRoll(attr.Name, roll.RollResult, string.Join(", ", roll.IndividualRolls), echoText: echo, characterName: actor);
                            }
                            catch { }
                        }
                    }
                }
            }
            else
            {
                ImGui.TextDisabled("No attributes defined.");
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Skills
            ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("SkillLabel"));
            ImGui.Spacing();

            var skills = sheet.GetEffectiveSkills(diceSys);
            if (skills != null && skills.Count > 0)
            {
                using var table = ImRaii.Table("##NpcSkillTable", 5, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp);
                if (table.Success)
                {
                    ImGui.TableSetupColumn("Skill", ImGuiTableColumnFlags.WidthStretch, 0.35f);
                    ImGui.TableSetupColumn("Linked", ImGuiTableColumnFlags.WidthStretch, 0.25f);
                    ImGui.TableSetupColumn("Base", ImGuiTableColumnFlags.WidthFixed, 60.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Total", ImGuiTableColumnFlags.WidthFixed, 60.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 36.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableHeadersRow();

                    foreach (var kv in skills)
                    {
                        var skill = kv.Value;
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted(skill.skillName);

                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextDisabled(skill.linkedAttribute ?? "");

                        ImGui.TableNextColumn();
                        int baseVal = skill.skillModifier;
                        if (UiUtils.StyledInputInt($"NpcSkill_{skill.skillName}", ref baseVal, step: 0, width: -1.0f))
                        {
                            skill.skillModifier = baseVal;
                        }

                        ImGui.TableNextColumn();
                        int totalVal = sheet.GetEffectiveSkillTotal(skill.skillName, diceSys);
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextColored(ImGuiColors.ParsedGreen, totalVal >= 0 ? $"+{totalVal}" : $"{totalVal}");

                        ImGui.TableNextColumn();
                        if (UiUtils.IconButton($"RollNpcSkill_{skill.skillName}", diceIcon, $"Roll {skill.skillName}", new Vector2(26, 24) * ImGuiHelpers.GlobalScale))
                        {
                            var roll = DiceRoll.RollStatWithSystem(diceSys, skill.skillName, totalVal)
                                ?? DiceRoll.RollDiceRegular(1, DiceRoll.GetSystemSides(diceSys), totalVal, skill.skillName);
                            try
                            {
                                string actor = !string.IsNullOrWhiteSpace(sheet.CharacterFullName) ? sheet.CharacterFullName : "NPC";
                                string echo = LocalizationManager.Instance.GetLocalizedString("InitiativeRollEchoFormat", actor, $"{skill.skillName} -> {roll.RollResultString.TextValue}");
                                PartySyncManager.Instance.BroadcastDiceRoll(skill.skillName, roll.RollResult, string.Join(", ", roll.IndividualRolls), echoText: echo, characterName: actor);
                            }
                            catch { }
                        }
                    }
                }
            }
            else
            {
                ImGui.TextDisabled("No skills defined.");
            }
        }

        private void DrawNpcAbilitiesTab(CharacterSheet sheet, DiceSystem? diceSys)
        {
            using var child = ImRaii.Child("##NpcAbilitiesChild", new Vector2(0, 360.0f * ImGuiHelpers.GlobalScale), false);
            if (!child.Success) return;

            var diceIcon = diceSys?.diceType == DiceType.d20 ? FontAwesomeIcon.DiceD20 : FontAwesomeIcon.Dice;

            var abils = sheet.GetEffectiveAbilities(diceSys);
            if (abils != null && abils.Count > 0)
            {
                using var table = ImRaii.Table("##NpcAbilTable", 4, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp);
                if (table.Success)
                {
                    ImGui.TableSetupColumn("Ability", ImGuiTableColumnFlags.WidthStretch, 0.45f);
                    ImGui.TableSetupColumn("Base", ImGuiTableColumnFlags.WidthFixed, 60.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Total", ImGuiTableColumnFlags.WidthFixed, 60.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 36.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TableHeadersRow();

                    foreach (var kv in abils)
                    {
                        var abil = kv.Value;
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted(abil.abilityName);

                        ImGui.TableNextColumn();
                        int baseVal = abil.abilityModifier;
                        if (UiUtils.StyledInputInt($"NpcAbil_{abil.abilityName}", ref baseVal, step: 0, width: -1.0f))
                        {
                            abil.abilityModifier = baseVal;
                        }

                        ImGui.TableNextColumn();
                        int totalVal = sheet.GetEffectiveAbilityModifier(abil.abilityName);
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextColored(ImGuiColors.ParsedGreen, totalVal >= 0 ? $"+{totalVal}" : $"{totalVal}");

                        ImGui.TableNextColumn();
                        if (UiUtils.IconButton($"RollNpcAbil_{abil.abilityName}", diceIcon, $"Roll {abil.abilityName}", new Vector2(26, 24) * ImGuiHelpers.GlobalScale))
                        {
                            var roll = DiceRoll.RollStatWithSystem(diceSys, abil.abilityName, totalVal)
                                ?? DiceRoll.RollDiceRegular(1, DiceRoll.GetSystemSides(diceSys), totalVal, abil.abilityName);
                            try
                            {
                                string actor = !string.IsNullOrWhiteSpace(sheet.CharacterFullName) ? sheet.CharacterFullName : "NPC";
                                string echo = LocalizationManager.Instance.GetLocalizedString("InitiativeRollEchoFormat", actor, $"{abil.abilityName} -> {roll.RollResultString.TextValue}");
                                PartySyncManager.Instance.BroadcastDiceRoll(abil.abilityName, roll.RollResult, string.Join(", ", roll.IndividualRolls), echoText: echo, characterName: actor);
                            }
                            catch { }
                        }
                    }
                }
            }
            else
            {
                ImGui.TextDisabled("No abilities defined.");
            }
        }

        private void DrawNpcBuffsTab(InitiativeParticipant participant, CharacterSheet sheet)
        {
            using var child = ImRaii.Child("##NpcBuffsChild", new Vector2(0, 360.0f * ImGuiHelpers.GlobalScale), false);
            if (!child.Success) return;

            if (UiUtils.IconButton("AddBuffNpcTabBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddBuffButton")))
            {
                OpenAddBuffModal(participant.Id);
            }
            ImGui.Spacing();

            var buffs = participant.Buffs;
            if (buffs != null && buffs.Count > 0)
            {
                string? buffToRemove = null;
                for (int i = 0; i < buffs.Count; i++)
                {
                    var b = buffs[i];
                    ImGui.PushID($"NpcBuff_{b.Id}");
                    string badgeText = $"{b.Name} ({b.Duration}t)";
                    var badgeBg = b.IsDebuff ? new Vector4(0.35f, 0.12f, 0.12f, 0.85f) : new Vector4(0.12f, 0.30f, 0.16f, 0.85f);
                    var badgeCol = b.IsDebuff ? ImGuiColors.DalamudRed : ImGuiColors.ParsedGreen;

                    UiUtils.Badge(badgeText, badgeBg, badgeCol);
                    ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                    ImGui.AlignTextToFramePadding();
                    string mods = b.GetFormattedModifiers();
                    if (!string.IsNullOrWhiteSpace(mods))
                    {
                        ImGui.TextColored(ImGuiColors.ParsedGold, mods);
                        ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                    }
                    if (!string.IsNullOrWhiteSpace(b.Description))
                    {
                        ImGui.TextDisabled(b.Description);
                        ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                    }

                    if (UiUtils.IconButton($"RemNpcBuff_{b.Id}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("SupprButton"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                    {
                        buffToRemove = b.Id;
                    }

                    ImGui.PopID();
                }

                if (buffToRemove != null)
                {
                    manager.RemoveBuffFromParticipant(participant.Id, buffToRemove);
                }
            }
            else
            {
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoActiveBuffs"));
            }
        }
    }
}
