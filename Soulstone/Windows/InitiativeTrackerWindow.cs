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

        // Add Buff Modal State
        private string addBuffTargetParticipantId = string.Empty;
        private bool showAddBuffModal = false;
        private string newBuffName = string.Empty;
        private int newBuffDuration = 3;
        private string newBuffTargetStat = string.Empty;
        private int newBuffValue = 1;
        private bool newBuffIsDebuff = false;
        private string newBuffDescription = string.Empty;

        public InitiativeTrackerWindow(Plugin plugin)
            : base("Initiative Tracker###SoulstoneInitiativeTracker", ImGuiWindowFlags.None)
        {
            this.plugin = plugin;
            manager = InitiativeTrackerManager.Instance;

            Size = new Vector2(720, 520);
            SizeCondition = ImGuiCond.FirstUseEver;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(500, 320),
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
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##NewBuffName", "e.g. Haste, Bless, Poison, Weakness", ref newBuffName, 60);

                // Duration (turns)
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("BuffDurationLabel"));
                ImGui.SetNextItemWidth(100.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputInt("##NewBuffDuration", ref newBuffDuration, 1))
                {
                    if (newBuffDuration < 1) newBuffDuration = 1;
                }

                // Target Stat
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("BuffTargetStatLabel"));
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##NewBuffTargetStat", LocalizationManager.Instance.GetLocalizedString("BuffStatNameHint"), ref newBuffTargetStat, 60);

                // Value / Modifier
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("BuffValueLabel"));
                ImGui.SetNextItemWidth(100.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputInt("##NewBuffValue", ref newBuffValue, 1))
                {
                    if (newBuffValue < 0) newBuffIsDebuff = true;
                }

                // Debuff checkbox
                ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("BuffIsDebuffLabel"), ref newBuffIsDebuff);

                // Description
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("DiceSysResourceDescription"));
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##NewBuffDesc", ref newBuffDescription, 120);

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), new Vector2(120.0f * ImGuiHelpers.GlobalScale, 0)))
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
                if (ImGui.Button("Cancel", new Vector2(90.0f * ImGuiHelpers.GlobalScale, 0)))
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
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 90.0f * ImGuiHelpers.GlobalScale);
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
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputText($"##Notes_{p.Id}", ref notesVal, 50))
                    {
                        p.Notes = notesVal;
                    }

                    // Column 7: Actions (Re-roll / Add Buff / Delete)
                    ImGui.TableNextColumn();
                    if (UiUtils.IconButton($"Reroll_{p.Id}", FontAwesomeIcon.DiceD20, "Re-roll d20 + Bonus", new Vector2(24, 20) * ImGuiHelpers.GlobalScale))
                    {
                        var rand = new Random();
                        p.InitiativeValue = rand.Next(1, 21) + p.BonusModifier;
                        needsSort = true;
                    }

                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    if (UiUtils.IconButton($"AddBuffAction_{p.Id}", FontAwesomeIcon.Magic, LocalizationManager.Instance.GetLocalizedString("BuffModalTitle"), new Vector2(24, 20) * ImGuiHelpers.GlobalScale))
                    {
                        OpenAddBuffModal(p.Id);
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
