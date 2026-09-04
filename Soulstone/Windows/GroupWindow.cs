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
using System.Threading.Tasks;

namespace Soulstone.Windows
{
    public class GroupWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private string serverUrl;
        private string inviteCode = string.Empty;
        private string connectionMessage = string.Empty;

        // Search & Filters
        private string searchQuery = string.Empty;
        private int activeFilterIndex = 0; // 0: All, 1: Soulstone Only, 2: Leaders, 3: Needs Sync
        private bool isGridView = false; // false = Cards View, true = Tactical Grid View

        // Roll Controls & Presets
        private string rollFormula = "1d20";
        private string rollName = "Check";
        private readonly Dictionary<string, string> memberRollFormulas = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> memberRollNames = new(StringComparer.OrdinalIgnoreCase);

        // Expanded Sections
        private readonly HashSet<string> expandedStatsMembers = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> expandedRollDrawers = new(StringComparer.OrdinalIgnoreCase);
        private bool showSessionInfo = false;
        private int connectionTab = 0; // 0: Join, 1: Host

        // Batch Roll Modal State
        private bool showBatchRollModal = false;
        private string batchRollName = "Group Check";
        private string batchRollFormula = "1d20";

        // Toast / Feedback timer
        private DateTime inviteCopiedTime = DateTime.MinValue;

        public GroupWindow(Plugin plugin)
            : base("Group Management###SoulstoneGroupManagement", ImGuiWindowFlags.None)
        {
            this.plugin = plugin;
            serverUrl = plugin.Configuration.SyncServerUrl;

            Size = new Vector2(860, 620);
            SizeCondition = ImGuiCond.FirstUseEver;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(580, 400),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public void Dispose() { }

        public override void Draw()
        {
            DrawConnectionHeader();
            ImGui.Spacing();

            DrawPendingRollRequests();

            DrawToolbarAndFilters();
            ImGui.Separator();
            ImGui.Spacing();

            DrawRosterContent();
            DrawBatchRollModal();
        }

        #region 1. Connection Header & Session Panel

        private void DrawConnectionHeader()
        {
            var sync = PartySyncManager.Instance;
            bool isConnected = sync.IsConnected;

            using (var group = ImRaii.Group())
            {
                if (isConnected)
                {
                    DrawConnectedSessionBanner(sync);
                }
                else
                {
                    DrawDisconnectedSessionSetup(sync);
                }
            }
        }

        private void DrawConnectedSessionBanner(PartySyncManager sync)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ImGuiColors.ParsedGreen, FontAwesomeIcon.Wifi.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

            ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("GroupRelayStatus"));
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

            int memberCount = sync.ConnectedPartyMembers.Count;
            string memberBadgeText = string.Format(LocalizationManager.Instance.GetLocalizedString("GroupConnectedMembers"), memberCount);
            UiUtils.Badge(memberBadgeText, new Vector4(0.12f, 0.35f, 0.22f, 0.9f), ImGuiColors.ParsedGreen);

            if (sync.IsSessionHost)
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("GroupBadgeLeader"), new Vector4(0.35f, 0.28f, 0.10f, 0.9f), ImGuiColors.ParsedGold);
            }
            else if (!string.IsNullOrWhiteSpace(plugin.Configuration.SyncHostName))
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                string hostInfo = $"{LocalizationManager.Instance.GetLocalizedString("GroupHostLabel")} {plugin.Configuration.SyncHostName}";
                UiUtils.Badge(hostInfo, new Vector4(0.20f, 0.25f, 0.38f, 0.9f), ImGuiColors.ParsedBlue);
            }

            if (DiceSystemManager.Instance.IsSessionRulesetActive)
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("GroupSyncedFromDM"), new Vector4(0.15f, 0.35f, 0.40f, 0.9f), ImGuiColors.ParsedBlue);
            }

            // Right-aligned buttons
            float rightButtonsWidth = 0f;
            if (sync.IsSessionHost && !string.IsNullOrWhiteSpace(sync.InviteCode))
            {
                rightButtonsWidth += 120.0f * ImGuiHelpers.GlobalScale;
            }
            rightButtonsWidth += 140.0f * ImGuiHelpers.GlobalScale;

            float avail = ImGui.GetContentRegionAvail().X;
            if (avail > rightButtonsWidth)
            {
                ImGui.SameLine(ImGui.GetCursorPosX() + avail - rightButtonsWidth);
            }
            else
            {
                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
            }

            if (sync.IsSessionHost && !string.IsNullOrWhiteSpace(sync.InviteCode))
            {
                bool justCopied = (DateTime.UtcNow - inviteCopiedTime).TotalSeconds < 3.0;
                var copyIcon = justCopied ? FontAwesomeIcon.Check : FontAwesomeIcon.Copy;
                var copyText = justCopied
                    ? LocalizationManager.Instance.GetLocalizedString("GroupCopied")
                    : LocalizationManager.Instance.GetLocalizedString("GroupCopyInvite");

                if (UiUtils.IconButton("CopyInviteBtn", copyIcon, copyText))
                {
                    ImGui.SetClipboardText(sync.InviteCode);
                    inviteCopiedTime = DateTime.UtcNow;
                    connectionMessage = LocalizationManager.Instance.GetLocalizedString("GroupInviteCopied");
                }
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            }

            if (UiUtils.IconButton("SessionInfoToggle", showSessionInfo ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.InfoCircle, LocalizationManager.Instance.GetLocalizedString("GroupSessionInfo")))
            {
                showSessionInfo = !showSessionInfo;
            }

            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.45f, 0.18f, 0.18f, 0.8f)))
            using (ImRaii.PushColor(ImGuiCol.ButtonHovered, new Vector4(0.60f, 0.22f, 0.22f, 0.9f)))
            {
                if (UiUtils.IconButton("LeaveSessionBtn", FontAwesomeIcon.SignOutAlt, LocalizationManager.Instance.GetLocalizedString("GroupLeaveSession")))
                {
                    _ = sync.DisconnectAsync(true);
                }
            }

            if (showSessionInfo)
            {
                ImGui.Spacing();
                ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("GroupRelayUrl")}: {plugin.Configuration.SyncServerUrl}");
                if (!string.IsNullOrWhiteSpace(plugin.Configuration.SyncSessionId))
                {
                    ImGui.SameLine(0, 12.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("GroupSessionIdLabel")}: {plugin.Configuration.SyncSessionId}");
                }
            }

            if (!string.IsNullOrWhiteSpace(connectionMessage))
            {
                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.ParsedGold, connectionMessage);
            }
        }

        private void DrawDisconnectedSessionSetup(PartySyncManager sync)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ImGuiColors.DalamudOrange, FontAwesomeIcon.Plug.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

            ImGui.TextColored(ImGuiColors.DalamudOrange, LocalizationManager.Instance.GetLocalizedString("GroupRelayStatus"));
            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            ImGui.TextDisabled($"({sync.ConnectionStatus})");

            // Quick Reconnect Bar if session exists
            if (!string.IsNullOrWhiteSpace(plugin.Configuration.SyncSessionId))
            {
                ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.18f, 0.40f, 0.25f, 0.85f)))
                {
                    if (UiUtils.IconButton("QuickReconnectBtn", FontAwesomeIcon.Sync, LocalizationManager.Instance.GetLocalizedString("GroupReconnect")))
                    {
                        _ = ReconnectAsync();
                    }
                }
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);

                if (UiUtils.IconButton("ForgetSessionBtn", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("GroupForgetSession")))
                {
                    _ = sync.DisconnectAsync(true);
                }
            }

            // Mode Selector Tabs (Join vs Host)
            ImGui.SameLine(0, 12.0f * ImGuiHelpers.GlobalScale);
            using (ImRaii.PushColor(ImGuiCol.Button, connectionTab == 0 ? new Vector4(0.20f, 0.45f, 0.70f, 0.95f) : new Vector4(0.18f, 0.20f, 0.24f, 0.75f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 4.0f * ImGuiHelpers.GlobalScale))
            {
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("GroupJoinTab")))
                {
                    connectionTab = 0;
                }
            }
            ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            using (ImRaii.PushColor(ImGuiCol.Button, connectionTab == 1 ? new Vector4(0.50f, 0.38f, 0.15f, 0.95f) : new Vector4(0.18f, 0.20f, 0.24f, 0.75f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 4.0f * ImGuiHelpers.GlobalScale))
            {
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("GroupHostTab")))
                {
                    connectionTab = 1;
                }
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

            if (connectionTab == 0) // Join Session
            {
                float inputWidth = Math.Clamp(ImGui.GetContentRegionAvail().X - 80.0f * ImGuiHelpers.GlobalScale, 160.0f * ImGuiHelpers.GlobalScale, 280.0f * ImGuiHelpers.GlobalScale);
                ImGui.SetNextItemWidth(inputWidth);
                ImGui.InputTextWithHint("##RelayInvite", LocalizationManager.Instance.GetLocalizedString("GroupInviteCode"), ref inviteCode, 4096);
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);

                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.18f, 0.45f, 0.70f, 0.9f)))
                {
                    if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("GroupJoinSession")))
                    {
                        _ = JoinSessionAsync();
                    }
                }
            }
            else // Host Session
            {
                float inputWidth = Math.Clamp(ImGui.GetContentRegionAvail().X - 130.0f * ImGuiHelpers.GlobalScale, 160.0f * ImGuiHelpers.GlobalScale, 280.0f * ImGuiHelpers.GlobalScale);
                ImGui.SetNextItemWidth(inputWidth);
                ImGui.InputTextWithHint("##RelayUrl", LocalizationManager.Instance.GetLocalizedString("GroupRelayUrl"), ref serverUrl, 512);
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);

                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.40f, 0.32f, 0.15f, 0.9f)))
                {
                    if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("GroupCreateSession")))
                    {
                        _ = CreateSessionAsync();
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(connectionMessage))
            {
                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.ParsedGold, connectionMessage);
            }
        }

        private async Task CreateSessionAsync()
        {
            connectionMessage = LocalizationManager.Instance.GetLocalizedString("GroupConnecting");
            bool success = await PartySyncManager.Instance.CreateSessionAsync(serverUrl);
            connectionMessage = LocalizationManager.Instance.GetLocalizedString(success ? "GroupSessionCreated" : "GroupConnectionFailed");
        }

        private async Task JoinSessionAsync()
        {
            connectionMessage = LocalizationManager.Instance.GetLocalizedString("GroupConnecting");
            bool success = await PartySyncManager.Instance.JoinSessionAsync(inviteCode);
            connectionMessage = LocalizationManager.Instance.GetLocalizedString(success ? "GroupSessionJoined" : "GroupInvalidInvite");
            if (success) inviteCode = string.Empty;
        }

        private async Task ReconnectAsync()
        {
            connectionMessage = LocalizationManager.Instance.GetLocalizedString("GroupConnecting");
            bool success = await PartySyncManager.Instance.ReconnectAsync();
            connectionMessage = LocalizationManager.Instance.GetLocalizedString(success ? "GroupSessionJoined" : "GroupConnectionFailed");
        }

        #endregion

        #region 2. Pending Roll Requests Banner

        private void DrawPendingRollRequests()
        {
            var requests = PartySyncManager.Instance.PendingRollRequests.Values.ToList();
            if (requests.Count == 0) return;

            foreach (var request in requests)
            {
                using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.30f, 0.22f, 0.08f, 0.92f)))
                using (ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedGold))
                using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * ImGuiHelpers.GlobalScale))
                using (var requestPanel = ImRaii.Child($"##RollRequest_{request.RequestId}", new Vector2(0, 48.0f * ImGuiHelpers.GlobalScale), true))
                {
                    if (!requestPanel.Success) continue;

                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.DiceD20.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

                    ImGui.TextColored(ImGuiColors.ParsedGold, $"{request.RequestedBy}:");
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TextColored(ImGuiColors.DalamudWhite, $"{request.RollName} ({request.Formula})");

                    float reqButtonsWidth = 200.0f * ImGuiHelpers.GlobalScale;
                    if (ImGui.GetContentRegionAvail().X > reqButtonsWidth)
                    {
                        ImGui.SameLine(ImGui.GetWindowWidth() - reqButtonsWidth - 16.0f * ImGuiHelpers.GlobalScale);
                    }

                    using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.20f, 0.55f, 0.28f, 0.9f)))
                    {
                        if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("GroupRollNow")}##{request.RequestId}"))
                        {
                            PartySyncManager.Instance.ExecuteRollRequest(request.RequestId);
                        }
                    }

                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("GroupDismissRoll")}##{request.RequestId}"))
                    {
                        PartySyncManager.Instance.DismissRollRequest(request.RequestId);
                    }
                }
                ImGui.Spacing();
            }
        }

        #endregion

        #region 3. Action Toolbar, Search, and Filters

        private void DrawToolbarAndFilters()
        {
            bool isHost = PartySyncManager.Instance.IsSessionHost;

            // Row 1: Actions Toolbar
            using (var group = ImRaii.Group())
            {
                if (isHost)
                {
                    if (UiUtils.IconButton("GroupBatchRollBtn", FontAwesomeIcon.Bullhorn, LocalizationManager.Instance.GetLocalizedString("GroupBatchRoll")))
                    {
                        showBatchRollModal = true;
                    }

                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
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

                // View Mode Toggle on right
                float viewToggleWidth = 80.0f * ImGuiHelpers.GlobalScale;
                float avail = ImGui.GetContentRegionAvail().X;
                if (avail > viewToggleWidth)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - viewToggleWidth - 20.0f * ImGuiHelpers.GlobalScale);
                }

                using (ImRaii.PushColor(ImGuiCol.Button, !isGridView ? new Vector4(0.25f, 0.35f, 0.50f, 0.9f) : new Vector4(0.18f, 0.18f, 0.22f, 0.7f)))
                {
                    if (UiUtils.IconButton("ViewCardsToggle", FontAwesomeIcon.ThLarge, LocalizationManager.Instance.GetLocalizedString("GroupViewCards")))
                    {
                        isGridView = false;
                    }
                }
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                using (ImRaii.PushColor(ImGuiCol.Button, isGridView ? new Vector4(0.25f, 0.35f, 0.50f, 0.9f) : new Vector4(0.18f, 0.18f, 0.22f, 0.7f)))
                {
                    if (UiUtils.IconButton("ViewGridToggle", FontAwesomeIcon.ThList, LocalizationManager.Instance.GetLocalizedString("GroupViewGrid")))
                    {
                        isGridView = true;
                    }
                }
            }

            ImGui.Spacing();

            // Row 2: Search & Filter Chips
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ImGuiColors.DalamudGrey, FontAwesomeIcon.Search.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

            ImGui.SetNextItemWidth(220.0f * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint("##GroupSearch", LocalizationManager.Instance.GetLocalizedString("GroupSearchHint"), ref searchQuery, 64);

            ImGui.SameLine(0, 12.0f * ImGuiHelpers.GlobalScale);

            var membersList = PartySyncManager.Instance.ConnectedPartyMembers.Values.ToList();
            int totalCount = membersList.Count;
            int soulstoneCount = membersList.Count(m => m.HasSoulstone);
            int leaderCount = membersList.Count(m => m.IsPartyLeader);
            int outOfSyncCount = membersList.Count(m => m.HasSoulstone && !m.IsRulesetInSync);

            DrawFilterChip(0, $"{LocalizationManager.Instance.GetLocalizedString("GroupFilterAll")} ({totalCount})");
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            DrawFilterChip(1, $"{LocalizationManager.Instance.GetLocalizedString("GroupFilterSoulstone")} ({soulstoneCount})");
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            DrawFilterChip(2, $"{LocalizationManager.Instance.GetLocalizedString("GroupFilterLeader")} ({leaderCount})");

            if (outOfSyncCount > 0)
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                DrawFilterChip(3, $"{LocalizationManager.Instance.GetLocalizedString("GroupFilterOutOfSync")} ({outOfSyncCount})", true);
            }
        }

        private void DrawFilterChip(int index, string label, bool isWarning = false)
        {
            bool isSelected = activeFilterIndex == index;
            Vector4 bgCol;
            Vector4 textCol;

            if (isSelected)
            {
                bgCol = isWarning ? new Vector4(0.55f, 0.30f, 0.10f, 0.95f) : new Vector4(0.20f, 0.45f, 0.70f, 0.95f);
                textCol = ImGuiColors.DalamudWhite;
            }
            else
            {
                bgCol = new Vector4(0.18f, 0.20f, 0.24f, 0.75f);
                textCol = isWarning ? ImGuiColors.ParsedOrange : ImGuiColors.DalamudGrey;
            }

            using (ImRaii.PushColor(ImGuiCol.Button, bgCol))
            using (ImRaii.PushColor(ImGuiCol.Text, textCol))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12.0f * ImGuiHelpers.GlobalScale))
            {
                if (ImGui.Button(label))
                {
                    activeFilterIndex = index;
                }
            }
        }

        #endregion

        #region 4. Roster Presentation (Cards & Tactical Grid)

        private void DrawRosterContent()
        {
            var allMembers = PartySyncManager.Instance.ConnectedPartyMembers.Values
                .OrderByDescending(m => m.IsPartyLeader)
                .ThenBy(m => m.CharacterName)
                .ToList();

            if (allMembers.Count == 0)
            {
                ImGui.Spacing();
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("GroupNoMembers"));
                return;
            }

            var filteredMembers = allMembers.Where(m =>
            {
                // Search filter
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    bool matchName = m.CharacterName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchWorld = !string.IsNullOrWhiteSpace(m.WorldName) && m.WorldName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                    bool matchJob = !string.IsNullOrWhiteSpace(m.JobName) && m.JobName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                    if (!matchName && !matchWorld && !matchJob) return false;
                }

                // Category filter
                if (activeFilterIndex == 1 && !m.HasSoulstone) return false;
                if (activeFilterIndex == 2 && !m.IsPartyLeader) return false;
                if (activeFilterIndex == 3 && (!m.HasSoulstone || m.IsRulesetInSync)) return false;

                return true;
            }).ToList();

            if (filteredMembers.Count == 0)
            {
                ImGui.Spacing();
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("GroupNoMatches"));
                return;
            }

            using var scroll = ImRaii.Child("##GroupRosterScrollView", new Vector2(0, 0), false);
            if (!scroll.Success) return;

            if (isGridView)
            {
                DrawTacticalGrid(filteredMembers);
            }
            else
            {
                foreach (var member in filteredMembers)
                {
                    DrawMemberCard(member);
                    ImGui.Spacing();
                }
            }
        }

        #endregion

        #region 5. Detailed Member Card

        private void DrawMemberCard(PartyMemberSyncData member)
        {
            ImGui.PushID($"MemberCard_{member.CharacterName}");

            bool isLeader = member.IsPartyLeader;
            bool isLocal = string.Equals(member.CharacterName, PartySyncManager.Instance.GetLocalPlayerName(), StringComparison.OrdinalIgnoreCase);

            using (var card = ImRaii.Group())
            {
                DrawCardHeader(member, isLeader, isLocal);
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                DrawCardVitals(member);

                if (member.ActiveBuffs != null && member.ActiveBuffs.Count > 0)
                {
                    DrawCardBuffs(member.ActiveBuffs);
                }

                if (!string.IsNullOrWhiteSpace(member.LastRollSummary))
                {
                    DrawCardLastRoll(member.LastRollSummary);
                }

                // DM Roll Drawer
                if (PartySyncManager.Instance.IsSessionHost && member.HasSoulstone)
                {
                    DrawDmRollDrawer(member);
                }

                // Decrypted Private Stats (DM View)
                if (PartySyncManager.Instance.IsSessionHost && member.HasPrivateStats)
                {
                    DrawDmPrivateStats(member);
                }
            }

            ImGui.PopID();
        }

        private void DrawCardHeader(PartyMemberSyncData member, bool isLeader, bool isLocal)
        {
            // Icon & Role
            ImGui.PushFont(UiBuilder.IconFont);
            var roleIcon = isLeader ? FontAwesomeIcon.Crown : GetJobRoleIcon(member.JobName);
            var iconColor = isLeader ? ImGuiColors.ParsedGold : ImGuiColors.DalamudWhite;
            ImGui.TextColored(iconColor, roleIcon.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

            // Name & World
            ImGui.TextColored(ImGuiColors.DalamudWhite, member.CharacterName);

            if (!string.IsNullOrWhiteSpace(member.WorldName))
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                ImGui.TextDisabled($"({member.WorldName})");
            }

            // Job Badge
            if (!string.IsNullOrWhiteSpace(member.JobName))
            {
                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                var (jobBg, jobCol) = GetJobBadgeColors(member.JobName);
                UiUtils.Badge(member.JobName, jobBg, jobCol);
            }

            // Badges
            if (isLeader)
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("GroupBadgeLeader"), new Vector4(0.38f, 0.30f, 0.12f, 0.9f), ImGuiColors.ParsedGold);
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (member.HasSoulstone)
            {
                UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("GroupStatusConnected"), new Vector4(0.14f, 0.38f, 0.20f, 0.9f), ImGuiColors.ParsedGreen);
            }
            else
            {
                UiUtils.Badge(LocalizationManager.Instance.GetLocalizedString("GroupStatusNoSoulstone"), new Vector4(0.25f, 0.25f, 0.25f, 0.85f), ImGuiColors.DalamudGrey);
            }

            if (!string.IsNullOrWhiteSpace(member.ActiveRulesetName))
            {
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                string rulesetBadge = $"Ruleset: {member.ActiveRulesetName}";
                var rulesetBg = member.IsRulesetInSync ? new Vector4(0.15f, 0.30f, 0.45f, 0.9f) : new Vector4(0.50f, 0.25f, 0.10f, 0.9f);
                var rulesetCol = member.IsRulesetInSync ? ImGuiColors.ParsedBlue : ImGuiColors.ParsedOrange;
                UiUtils.Badge(rulesetBadge, rulesetBg, rulesetCol);
            }

            // Right-aligned quick toggles
            if (PartySyncManager.Instance.IsSessionHost && member.HasSoulstone)
            {
                float actionsWidth = 140.0f * ImGuiHelpers.GlobalScale;
                if (ImGui.GetContentRegionAvail().X > actionsWidth)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - actionsWidth - 16.0f * ImGuiHelpers.GlobalScale);
                }

                bool isRollExpanded = expandedRollDrawers.Contains(member.CharacterName);
                using (ImRaii.PushColor(ImGuiCol.Button, isRollExpanded ? new Vector4(0.35f, 0.28f, 0.12f, 0.9f) : new Vector4(0.20f, 0.22f, 0.28f, 0.8f)))
                {
                    if (UiUtils.IconButton($"ToggleRoll_{member.CharacterName}", FontAwesomeIcon.DiceD20, LocalizationManager.Instance.GetLocalizedString("GroupQuickRoll")))
                    {
                        if (isRollExpanded) expandedRollDrawers.Remove(member.CharacterName);
                        else expandedRollDrawers.Add(member.CharacterName);
                    }
                }

                if (member.HasPrivateStats)
                {
                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    bool isStatsExpanded = expandedStatsMembers.Contains(member.CharacterName);
                    using (ImRaii.PushColor(ImGuiCol.Button, isStatsExpanded ? new Vector4(0.20f, 0.40f, 0.60f, 0.9f) : new Vector4(0.20f, 0.22f, 0.28f, 0.8f)))
                    {
                        if (UiUtils.IconButton($"ToggleStats_{member.CharacterName}", FontAwesomeIcon.Scroll, LocalizationManager.Instance.GetLocalizedString("GroupPrivateStats")))
                        {
                            if (isStatsExpanded) expandedStatsMembers.Remove(member.CharacterName);
                            else expandedStatsMembers.Add(member.CharacterName);
                        }
                    }
                }
            }
        }

        private void DrawCardVitals(PartyMemberSyncData member)
        {
            // HP Bar
            int maxHp = member.MaxHp > 0 ? member.MaxHp : 100;
            float hpFraction = Math.Clamp((float)member.CurrentHp / maxHp, 0.0f, 1.0f);
            string hpOverlay = $"{LocalizationManager.Instance.GetLocalizedString("GroupHealth")}: {member.CurrentHp} / {maxHp} ({(int)(hpFraction * 100)}%)";

            Vector4 hpColor = GetHpBarColor(hpFraction);
            using (ImRaii.PushColor(ImGuiCol.PlotHistogram, hpColor))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 4.0f * ImGuiHelpers.GlobalScale))
            {
                ImGui.ProgressBar(hpFraction, new Vector2(-1.0f, 18.0f * ImGuiHelpers.GlobalScale), hpOverlay);
            }

            // Mana Bar
            int maxMana = member.MaxMana > 0 ? member.MaxMana : 100;
            float manaFraction = Math.Clamp((float)member.CurrentMana / maxMana, 0.0f, 1.0f);
            string manaOverlay = $"{LocalizationManager.Instance.GetLocalizedString("GroupMana")}: {member.CurrentMana} / {maxMana} ({(int)(manaFraction * 100)}%)";

            using (ImRaii.PushColor(ImGuiCol.PlotHistogram, new Vector4(0.20f, 0.50f, 0.85f, 0.9f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 4.0f * ImGuiHelpers.GlobalScale))
            {
                ImGui.ProgressBar(manaFraction, new Vector2(-1.0f, 15.0f * ImGuiHelpers.GlobalScale), manaOverlay);
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
                    using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 4.0f * ImGuiHelpers.GlobalScale))
                    {
                        ImGui.ProgressBar(fraction, new Vector2(-1.0f, 14.0f * ImGuiHelpers.GlobalScale), overlay);
                    }
                }
            }
        }

        private void DrawCardBuffs(List<Buff> buffs)
        {
            ImGui.Spacing();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.Magic.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

            ImGui.TextColored(ImGuiColors.DalamudWhite, LocalizationManager.Instance.GetLocalizedString("GroupActiveBuffs"));
            ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);

            foreach (var buff in buffs)
            {
                string buffLabel = buff.Duration >= 0 ? $"{buff.Name} ({buff.Duration}t)" : buff.Name;
                var buffBg = buff.IsDebuff ? new Vector4(0.48f, 0.16f, 0.16f, 0.9f) : new Vector4(0.16f, 0.40f, 0.22f, 0.9f);
                var buffCol = buff.IsDebuff ? ImGuiColors.DalamudRed : ImGuiColors.ParsedGreen;

                UiUtils.Badge(buffLabel, buffBg, buffCol);

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.TextColored(buffCol, buff.Name);
                    if (!string.IsNullOrWhiteSpace(buff.Description))
                    {
                        ImGui.TextUnformatted(buff.Description);
                    }
                    string mods = buff.GetFormattedModifiers();
                    if (!string.IsNullOrWhiteSpace(mods))
                    {
                        ImGui.TextColored(ImGuiColors.ParsedGold, mods);
                    }
                    ImGui.EndTooltip();
                }

                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
            }
            ImGui.NewLine();
        }

        private void DrawCardLastRoll(string rollSummary)
        {
            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.12f, 0.15f, 0.8f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 4.0f * ImGuiHelpers.GlobalScale))
            using (var rollBox = ImRaii.Child($"##LastRollBox", new Vector2(0, 26.0f * ImGuiHelpers.GlobalScale), true))
            {
                if (rollBox.Success)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.DiceD20.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                    ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("GroupLastRoll"));
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TextColored(ImGuiColors.ParsedGold, rollSummary);
                }
            }
        }

        private void DrawDmRollDrawer(PartyMemberSyncData member)
        {
            if (!expandedRollDrawers.Contains(member.CharacterName)) return;

            ImGui.Spacing();
            using (var drawer = ImRaii.Group())
            {
                ImGui.TextColored(ImGuiColors.ParsedGold, $"{LocalizationManager.Instance.GetLocalizedString("GroupQuickRoll")}: {member.CharacterName}");
                ImGui.Spacing();

                // Presets
                if (ImGui.SmallButton("d20##Preset")) { SetMemberFormula(member.CharacterName, "1d20", "Check"); }
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.SmallButton("d100##Preset")) { SetMemberFormula(member.CharacterName, "1d100", "Check"); }
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.SmallButton("Advantage##Preset")) { SetMemberFormula(member.CharacterName, "2d20kh1", "Advantage Check"); }
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.SmallButton("Disadvantage##Preset")) { SetMemberFormula(member.CharacterName, "2d20kl1", "Disadvantage Check"); }

                ImGui.Spacing();

                string curName = memberRollNames.TryGetValue(member.CharacterName, out var nVal) ? nVal : rollName;
                string curFormula = memberRollFormulas.TryGetValue(member.CharacterName, out var fVal) ? fVal : rollFormula;

                ImGui.SetNextItemWidth(140.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputTextWithHint($"##RollName_{member.CharacterName}", LocalizationManager.Instance.GetLocalizedString("GroupRollName"), ref curName, 128))
                {
                    memberRollNames[member.CharacterName] = curName;
                }

                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                ImGui.SetNextItemWidth(120.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputTextWithHint($"##RollFormula_{member.CharacterName}", LocalizationManager.Instance.GetLocalizedString("GroupRollFormula"), ref curFormula, 128))
                {
                    memberRollFormulas[member.CharacterName] = curFormula;
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.20f, 0.45f, 0.70f, 0.9f)))
                {
                    if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("GroupRequestRoll")}##{member.CharacterName}"))
                    {
                        PartySyncManager.Instance.RequestRoll(member.CharacterName, curFormula, curName);
                    }
                }

                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.40f, 0.32f, 0.12f, 0.9f)))
                {
                    if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("GroupRollForMember")}##{member.CharacterName}"))
                    {
                        PartySyncManager.Instance.RollForMember(member.CharacterName, curFormula, curName);
                    }
                }
            }
        }

        private void SetMemberFormula(string memberName, string formula, string name)
        {
            memberRollFormulas[memberName] = formula;
            memberRollNames[memberName] = name;
        }

        private void DrawDmPrivateStats(PartyMemberSyncData member)
        {
            if (!expandedStatsMembers.Contains(member.CharacterName)) return;

            ImGui.Spacing();
            using (var statsGroup = ImRaii.Group())
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors.ParsedBlue, FontAwesomeIcon.Scroll.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                ImGui.TextColored(ImGuiColors.ParsedBlue, $"{LocalizationManager.Instance.GetLocalizedString("GroupStatsSummary")}: {member.CharacterName}");
                ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);

                string levelClassText = $"{LocalizationManager.Instance.GetLocalizedString("LevelLabel")} {member.Level} | {member.ClassName}";
                UiUtils.Badge(levelClassText, new Vector4(0.20f, 0.30f, 0.45f, 0.85f), ImGuiColors.ParsedBlue);

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Attributes Grid with Direct Roll buttons
                if (member.Attributes.Count > 0)
                {
                    DrawStatChipsGrid(LocalizationManager.Instance.GetLocalizedString("AttributeLabel"), member.Attributes, member.CharacterName);
                    ImGui.Spacing();
                }

                // Skills Grid with Direct Roll buttons
                if (member.Skills.Count > 0)
                {
                    DrawStatChipsGrid(LocalizationManager.Instance.GetLocalizedString("SkillLabel"), member.Skills, member.CharacterName);
                    ImGui.Spacing();
                }

                // Abilities Grid
                if (member.Abilities.Count > 0)
                {
                    DrawStatChipsGrid(LocalizationManager.Instance.GetLocalizedString("AbilityLabel"), member.Abilities, member.CharacterName);
                }
            }
        }

        private void DrawStatChipsGrid(string categoryTitle, Dictionary<string, int> stats, string memberName)
        {
            ImGui.TextDisabled(categoryTitle);
            ImGui.Spacing();

            foreach (var kv in stats.OrderBy(s => s.Key))
            {
                string chipText = $"{kv.Key}: {kv.Value}";
                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.18f, 0.22f, 0.28f, 0.85f)))
                using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 4.0f * ImGuiHelpers.GlobalScale))
                {
                    if (ImGui.SmallButton($"{chipText} 🎲##Stat_{memberName}_{kv.Key}"))
                    {
                        string formula = kv.Value >= 0 ? $"1d20+{kv.Value}" : $"1d20{kv.Value}";
                        PartySyncManager.Instance.RollForMember(memberName, formula, $"{kv.Key} Check");
                    }
                    if (ImGui.IsItemHovered())
                    {
                        string tooltip = string.Format(LocalizationManager.Instance.GetLocalizedString("GroupDirectRollTooltip"), $"{kv.Key} ({kv.Value})");
                        ImGui.SetTooltip(tooltip);
                    }
                }
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            }
            ImGui.NewLine();
        }

        #endregion

        #region 6. Tactical Grid View

        private void DrawTacticalGrid(List<PartyMemberSyncData> members)
        {
            int columns = 6;
            if (ImGui.BeginTable("##GroupTacticalGrid", columns, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("GroupRoleOther"), ImGuiTableColumnFlags.WidthFixed, 50.0f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("NameLabel"), ImGuiTableColumnFlags.WidthStretch, 2.0f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("GroupHealth"), ImGuiTableColumnFlags.WidthStretch, 2.5f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("GroupMana"), ImGuiTableColumnFlags.WidthStretch, 2.0f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("GroupLastRoll"), ImGuiTableColumnFlags.WidthStretch, 2.5f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("GroupPlayerControls"), ImGuiTableColumnFlags.WidthFixed, 100.0f * ImGuiHelpers.GlobalScale);
                ImGui.TableHeadersRow();

                foreach (var member in members)
                {
                    ImGui.TableNextRow();

                    // Col 0: Role / Job Icon
                    ImGui.TableSetColumnIndex(0);
                    ImGui.PushFont(UiBuilder.IconFont);
                    var icon = member.IsPartyLeader ? FontAwesomeIcon.Crown : GetJobRoleIcon(member.JobName);
                    var iconCol = member.IsPartyLeader ? ImGuiColors.ParsedGold : ImGuiColors.DalamudWhite;
                    ImGui.TextColored(iconCol, icon.ToIconString());
                    ImGui.PopFont();

                    // Col 1: Name & World
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextColored(ImGuiColors.DalamudWhite, member.CharacterName);
                    if (!string.IsNullOrWhiteSpace(member.JobName))
                    {
                        ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                        ImGui.TextDisabled($"[{member.JobName}]");
                    }

                    // Col 2: Health
                    ImGui.TableSetColumnIndex(2);
                    int maxHp = member.MaxHp > 0 ? member.MaxHp : 100;
                    float hpFraction = Math.Clamp((float)member.CurrentHp / maxHp, 0.0f, 1.0f);
                    using (ImRaii.PushColor(ImGuiCol.PlotHistogram, GetHpBarColor(hpFraction)))
                    {
                        ImGui.ProgressBar(hpFraction, new Vector2(-1.0f, 16.0f * ImGuiHelpers.GlobalScale), $"{member.CurrentHp}/{maxHp}");
                    }

                    // Col 3: Mana
                    ImGui.TableSetColumnIndex(3);
                    int maxMana = member.MaxMana > 0 ? member.MaxMana : 100;
                    float manaFraction = Math.Clamp((float)member.CurrentMana / maxMana, 0.0f, 1.0f);
                    using (ImRaii.PushColor(ImGuiCol.PlotHistogram, new Vector4(0.20f, 0.50f, 0.85f, 0.9f)))
                    {
                        ImGui.ProgressBar(manaFraction, new Vector2(-1.0f, 16.0f * ImGuiHelpers.GlobalScale), $"{member.CurrentMana}/{maxMana}");
                    }

                    // Col 4: Last Roll
                    ImGui.TableSetColumnIndex(4);
                    if (!string.IsNullOrWhiteSpace(member.LastRollSummary))
                    {
                        ImGui.TextColored(ImGuiColors.ParsedGold, member.LastRollSummary);
                    }
                    else
                    {
                        ImGui.TextDisabled("—");
                    }

                    // Col 5: Actions
                    ImGui.TableSetColumnIndex(5);
                    if (PartySyncManager.Instance.IsSessionHost && member.HasSoulstone)
                    {
                        if (UiUtils.IconButton($"GridRoll_{member.CharacterName}", FontAwesomeIcon.DiceD20, LocalizationManager.Instance.GetLocalizedString("GroupRollForMember")))
                        {
                            PartySyncManager.Instance.RollForMember(member.CharacterName, "1d20", "Check");
                        }
                        ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                        if (UiUtils.IconButton($"GridReq_{member.CharacterName}", FontAwesomeIcon.Bullhorn, LocalizationManager.Instance.GetLocalizedString("GroupRequestRoll")))
                        {
                            PartySyncManager.Instance.RequestRoll(member.CharacterName, "1d20", "Check");
                        }
                    }

                    if (member.ActiveBuffs != null && member.ActiveBuffs.Count > 0)
                    {
                        ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(ImGuiColors.ParsedGreen, FontAwesomeIcon.Magic.ToIconString());
                        ImGui.PopFont();
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            foreach (var b in member.ActiveBuffs)
                            {
                                ImGui.TextUnformatted($"{b.Name} ({b.Duration}t)");
                            }
                            ImGui.EndTooltip();
                        }
                    }
                }

                ImGui.EndTable();
            }
        }

        #endregion

        #region 7. Batch Roll Modal

        private void DrawBatchRollModal()
        {
            if (!showBatchRollModal) return;

            ImGui.OpenPopup("##BatchRollModalPopup");
            var center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(420, 240) * ImGuiHelpers.GlobalScale);

            if (ImGui.BeginPopupModal(LocalizationManager.Instance.GetLocalizedString("GroupBatchRollTitle"), ref showBatchRollModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Spacing();
                ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("GroupBatchRollTitle"));
                ImGui.Spacing();

                // Quick Presets
                if (ImGui.SmallButton("Perception 1d20")) { batchRollName = "Perception Check"; batchRollFormula = "1d20"; }
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.SmallButton("Initiative 1d20")) { batchRollName = "Initiative"; batchRollFormula = "1d20"; }
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.SmallButton("Save 1d20")) { batchRollName = "Saving Throw"; batchRollFormula = "1d20"; }

                ImGui.Spacing();
                ImGui.SetNextItemWidth(260.0f * ImGuiHelpers.GlobalScale);
                ImGui.InputTextWithHint("##BatchRollName", LocalizationManager.Instance.GetLocalizedString("GroupRollName"), ref batchRollName, 128);

                ImGui.SetNextItemWidth(260.0f * ImGuiHelpers.GlobalScale);
                ImGui.InputTextWithHint("##BatchRollFormula", LocalizationManager.Instance.GetLocalizedString("GroupRollFormula"), ref batchRollFormula, 128);

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.20f, 0.50f, 0.30f, 0.9f)))
                {
                    if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("GroupBatchRollSend"), new Vector2(160.0f * ImGuiHelpers.GlobalScale, 0)))
                    {
                        foreach (var m in PartySyncManager.Instance.ConnectedPartyMembers.Values)
                        {
                            if (m.HasSoulstone)
                            {
                                PartySyncManager.Instance.RequestRoll(m.CharacterName, batchRollFormula, batchRollName);
                            }
                        }
                        showBatchRollModal = false;
                    }
                }

                ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("CancelButton"), new Vector2(100.0f * ImGuiHelpers.GlobalScale, 0)))
                {
                    showBatchRollModal = false;
                }

                ImGui.EndPopup();
            }
        }

        #endregion

        #region 8. Helpers & Styling Utilities

        private static Vector4 GetHpBarColor(float fraction)
        {
            if (fraction <= 0.0f)
                return new Vector4(0.35f, 0.35f, 0.35f, 0.9f); // Incapacitated
            if (fraction <= 0.25f)
                return new Vector4(0.85f, 0.20f, 0.20f, 0.95f); // Crimson / Critical
            if (fraction <= 0.50f)
                return new Vector4(0.95f, 0.65f, 0.15f, 0.95f); // Amber / Injured
            return new Vector4(0.22f, 0.75f, 0.35f, 0.95f); // Vibrant Emerald / Healthy
        }

        private static FontAwesomeIcon GetJobRoleIcon(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName)) return FontAwesomeIcon.User;

            string j = jobName.ToUpperInvariant();
            if (j is "PLD" or "WAR" or "DRK" or "GNB" or "GLA" or "MRD") return FontAwesomeIcon.ShieldAlt;
            if (j is "WHM" or "SCH" or "AST" or "SGE" or "CNJ") return FontAwesomeIcon.Heartbeat;
            if (j is "BLM" or "SMN" or "RDM" or "PCT" or "BLU" or "THM" or "ACN") return FontAwesomeIcon.Magic;
            if (j is "BRD" or "MCH" or "DNC" or "ARC") return FontAwesomeIcon.Crosshairs;
            return FontAwesomeIcon.User;
        }

        private static (Vector4 bg, Vector4 text) GetJobBadgeColors(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName)) return (new Vector4(0.2f, 0.25f, 0.35f, 0.8f), ImGuiColors.ParsedBlue);

            string j = jobName.ToUpperInvariant();
            if (j is "PLD" or "WAR" or "DRK" or "GNB" or "GLA" or "MRD") // Tank
                return (new Vector4(0.15f, 0.25f, 0.50f, 0.9f), ImGuiColors.ParsedBlue);
            if (j is "WHM" or "SCH" or "AST" or "SGE" or "CNJ") // Healer
                return (new Vector4(0.15f, 0.40f, 0.22f, 0.9f), ImGuiColors.ParsedGreen);
            if (j is "MNK" or "DRG" or "NIN" or "SAM" or "RPR" or "VPR" or "PGL" or "LNC" or "ROG" or
                     "BRD" or "MCH" or "DNC" or "ARC" or
                     "BLM" or "SMN" or "RDM" or "PCT" or "BLU" or "THM" or "ACN") // DPS
                return (new Vector4(0.50f, 0.18f, 0.18f, 0.9f), ImGuiColors.DalamudRed);

            return (new Vector4(0.25f, 0.25f, 0.35f, 0.85f), ImGuiColors.DalamudWhite);
        }

        #endregion
    }
}
