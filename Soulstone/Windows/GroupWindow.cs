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
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Soulstone.Windows
{
    public class GroupWindow : Window, IDisposable
    {
        private readonly Plugin plugin;

        public GroupWindow(Plugin plugin)
            : base("Group Management###SoulstoneGroupManagement", ImGuiWindowFlags.None)
        {
            this.plugin = plugin;

            Size = new Vector2(780, 560);
            SizeCondition = ImGuiCond.FirstUseEver;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(520, 360),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public void Dispose() { }

        public override void Draw()
        {
            DrawTopBar();
            ImGui.Separator();
            ImGui.Spacing();

            DrawRosterContent();
        }

        private void DrawTopBar()
        {
            using (var group = ImRaii.Group())
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.Users.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

                ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("GroupManagementTitle"));
                ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);

                int memberCount = PartySyncManager.Instance.ConnectedPartyMembers.Count;
                string countText = $"{memberCount} {(memberCount > 1 ? "Members" : "Member")}";
                UiUtils.Badge(countText, new Vector4(0.2f, 0.4f, 0.6f, 0.85f), ImGuiColors.ParsedBlue);

                if (PartySyncManager.Instance.IsLocalPlayerPartyLeader())
                {
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("GroupBadgeLeader"), new Vector4(0.35f, 0.28f, 0.12f, 0.85f), ImGuiColors.ParsedGold);
                }

                if (DiceSystemManager.Instance.IsSessionRulesetActive)
                {
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("GroupSyncedFromDM"), new Vector4(0.14f, 0.38f, 0.20f, 0.85f), ImGuiColors.ParsedGreen);
                }
            }

            ImGui.TextColored(new Vector4(0.85f, 0.85f, 0.9f, 0.85f), LocalizationManager.Instance.GetLocalizedString("GroupManagementSubtitle"));
            ImGui.Spacing();

            // Action Toolbar
            bool isLeader = PartySyncManager.Instance.IsLocalPlayerPartyLeader();

            if (isLeader)
            {
                if (UiUtils.IconButton("GroupBroadcastRulesetBtn", FontAwesomeIcon.ShareAlt, LocalizationManager.Instance.GetLocalizedString("GroupBroadcastRuleset")))
                {
                    var diceSys = DiceSystemManager.Instance.CurrentDiceSystem;
                    if (diceSys != null)
                    {
                        PartySyncManager.Instance.BroadcastRuleset(diceSys);
                    }
                }

                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconButton("GroupSyncInitBtn", FontAwesomeIcon.Stopwatch, LocalizationManager.Instance.GetLocalizedString("GroupSyncInitiative")))
                {
                    var members = PartySyncManager.Instance.ConnectedPartyMembers.Values;
                    var diceSys = DiceSystemManager.Instance.CurrentDiceSystem;
                    InitiativeTrackerManager.Instance.ImportPartyMembers(members, diceSys);
                    plugin.InitiativeTrackerWindow.IsOpen = true;
                }

                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            }

            if (UiUtils.IconButton("GroupShareVitalsBtn", FontAwesomeIcon.Heartbeat, LocalizationManager.Instance.GetLocalizedString("GroupBroadcastVitals")))
            {
                PartySyncManager.Instance.BroadcastPresence();
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconButton("GroupRefreshBtn", FontAwesomeIcon.Sync, LocalizationManager.Instance.GetLocalizedString("GroupRefreshRoster")))
            {
                PartySyncManager.Instance.RequestRosterRefresh();
            }

            if (DiceSystemManager.Instance.IsSessionRulesetActive)
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconButton("GroupRevertRulesetBtn", FontAwesomeIcon.Undo, LocalizationManager.Instance.GetLocalizedString("GroupRevertRuleset")))
                {
                    DiceSystemManager.Instance.RevertToLocalRuleset();
                }
            }
        }

        private void DrawRosterContent()
        {
            var members = PartySyncManager.Instance.ConnectedPartyMembers.Values
                .OrderByDescending(m => m.IsPartyLeader)
                .ThenBy(m => m.CharacterName)
                .ToList();

            if (members.Count == 0)
            {
                ImGui.Spacing();
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("GroupNoMembers"));
                return;
            }

            using var child = ImRaii.Child("##GroupRosterScroll", new Vector2(0, 0), false);
            if (!child.Success) return;

            foreach (var member in members)
            {
                DrawMemberCard(member);
                ImGui.Spacing();
            }
        }

        private void DrawMemberCard(PartyMemberSyncData member)
        {
            ImGui.PushID($"MemberCard_{member.CharacterName}");

            Vector4 cardBg = member.IsPartyLeader
                ? new Vector4(0.18f, 0.16f, 0.10f, 0.6f)
                : new Vector4(0.12f, 0.13f, 0.16f, 0.6f);

            using (ImRaii.PushColor(ImGuiCol.ChildBg, cardBg))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f))
            using (var card = ImRaii.Child($"##CardFrame_{member.CharacterName}", new Vector2(0, 0), true, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (card.Success)
                {
                    // Row 1: Header - Name, World, Job, Badges
                    ImGui.PushFont(UiBuilder.IconFont);
                    var icon = member.IsPartyLeader ? FontAwesomeIcon.Crown : FontAwesomeIcon.User;
                    var iconColor = member.IsPartyLeader ? ImGuiColors.ParsedGold : ImGuiColors.DalamudWhite;
                    ImGui.TextColored(iconColor, icon.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

                    ImGui.TextColored(ImGuiColors.DalamudWhite, member.CharacterName);

                    if (!string.IsNullOrWhiteSpace(member.WorldName))
                    {
                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                        ImGui.TextDisabled($"({member.WorldName})");
                    }

                    if (!string.IsNullOrWhiteSpace(member.JobName))
                    {
                        ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                        UiUtils.Badge(member.JobName, new Vector4(0.2f, 0.25f, 0.35f, 0.8f), ImGuiColors.ParsedBlue);
                    }

                    if (member.IsPartyLeader)
                    {
                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                        UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("GroupBadgeLeader"), new Vector4(0.35f, 0.28f, 0.12f, 0.85f), ImGuiColors.ParsedGold);
                    }

                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    if (member.HasSoulstone)
                    {
                        UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("GroupStatusConnected"), new Vector4(0.14f, 0.38f, 0.20f, 0.85f), ImGuiColors.ParsedGreen);
                    }
                    else
                    {
                        UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("GroupStatusNoSoulstone"), new Vector4(0.25f, 0.25f, 0.25f, 0.85f), ImGuiColors.DalamudGrey);
                    }

                    if (!string.IsNullOrWhiteSpace(member.ActiveRulesetName))
                    {
                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                        string rulesetBadge = $"Ruleset: {member.ActiveRulesetName}";
                        var rulesetBg = member.IsRulesetInSync ? new Vector4(0.15f, 0.30f, 0.45f, 0.85f) : new Vector4(0.45f, 0.25f, 0.10f, 0.85f);
                        var rulesetCol = member.IsRulesetInSync ? ImGuiColors.ParsedBlue : ImGuiColors.ParsedOrange;
                        UiUtils.Badge(rulesetBadge, rulesetBg, rulesetCol);
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    // Row 2: Vitals & Resource Bars
                    int maxHp = member.MaxHp > 0 ? member.MaxHp : 100;
                    float hpFraction = Math.Clamp((float)member.CurrentHp / maxHp, 0.0f, 1.0f);
                    string hpOverlay = $"{LocalizationManager.Instance.GetLocalizedString("GroupHealth")}: {member.CurrentHp} / {maxHp} ({(int)(hpFraction * 100)}%)";

                    using (ImRaii.PushColor(ImGuiCol.PlotHistogram, new Vector4(0.25f, 0.70f, 0.30f, 0.9f)))
                    {
                        ImGui.ProgressBar(hpFraction, new Vector2(-1.0f, 18.0f * ImGuiHelpers.GlobalScale), hpOverlay);
                    }

                    int maxMana = member.MaxMana > 0 ? member.MaxMana : 100;
                    float manaFraction = Math.Clamp((float)member.CurrentMana / maxMana, 0.0f, 1.0f);
                    string manaOverlay = $"{LocalizationManager.Instance.GetLocalizedString("GroupMana")}: {member.CurrentMana} / {maxMana} ({(int)(manaFraction * 100)}%)";

                    using (ImRaii.PushColor(ImGuiCol.PlotHistogram, new Vector4(0.20f, 0.50f, 0.85f, 0.9f)))
                    {
                        ImGui.ProgressBar(manaFraction, new Vector2(-1.0f, 16.0f * ImGuiHelpers.GlobalScale), manaOverlay);
                    }

                    // Custom Resources
                    if (member.CustomResources != null && member.CustomResources.Count > 0)
                    {
                        ImGui.Spacing();
                        foreach (var kv in member.CustomResources)
                        {
                            int resMax = member.CustomResourceMaxes.TryGetValue(kv.Key, out int mVal) && mVal > 0 ? mVal : 100;
                            float fraction = Math.Clamp((float)kv.Value / resMax, 0.0f, 1.0f);
                            string overlay = $"{kv.Key}: {kv.Value} / {resMax}";

                            using (ImRaii.PushColor(ImGuiCol.PlotHistogram, new Vector4(0.60f, 0.35f, 0.75f, 0.9f)))
                            {
                                ImGui.ProgressBar(fraction, new Vector2(-1.0f, 14.0f * ImGuiHelpers.GlobalScale), overlay);
                            }
                        }
                    }

                    // Active Buffs
                    if (member.ActiveBuffs != null && member.ActiveBuffs.Count > 0)
                    {
                        ImGui.Spacing();
                        ImGui.TextColored(ImGuiColors.DalamudWhite, LocalizationManager.Instance.GetLocalizedString("GroupActiveBuffs"));
                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                        foreach (var buff in member.ActiveBuffs)
                        {
                            string buffLabel = buff.Duration >= 0 ? $"{buff.Name} ({buff.Duration}t)" : buff.Name;
                            var buffBg = buff.IsDebuff ? new Vector4(0.45f, 0.15f, 0.15f, 0.85f) : new Vector4(0.15f, 0.35f, 0.20f, 0.85f);
                            var buffCol = buff.IsDebuff ? ImGuiColors.DalamudRed : ImGuiColors.ParsedGreen;
                            UiUtils.Badge(buffLabel, buffBg, buffCol);
                            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                        }
                        ImGui.NewLine();
                    }

                    // Last Roll Summary
                    if (!string.IsNullOrWhiteSpace(member.LastRollSummary))
                    {
                        ImGui.Spacing();
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("GroupLastRoll"));
                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                        ImGui.TextColored(ImGuiColors.ParsedGold, member.LastRollSummary);
                    }
                }
            }

            ImGui.PopID();
        }
    }
}
