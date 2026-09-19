using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Sync;
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
        private string rollName = "Check";
        private readonly Dictionary<string, string> memberRollFormulas = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> memberRollNames = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> memberRollStatValues = new(StringComparer.OrdinalIgnoreCase);

        // Rolls follow the active dice system unless the DM opts into a raw formula
        private bool useSystemDice = true;
        private bool rollAdvantage = false;
        private bool rollDisadvantage = false;
        private bool rollPrivate = false;

        // Expanded Sections
        private readonly HashSet<string> expandedStatsMembers = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> expandedRollDrawers = new(StringComparer.OrdinalIgnoreCase);
        private bool showSessionInfo = false;
        private int connectionTab = 0; // 0: Join, 1: Host

        // Batch Roll Modal State
        private bool showBatchRollModal = false;
        private string batchRollName = "Group Check";
        private string batchRollFormula = "1d20";
        private int batchRollStatValue = 0;

        // Toast / Feedback timer
        private DateTime inviteCopiedTime = DateTime.MinValue;

        public GroupWindow(Plugin plugin)
            : base("Group Management###SoulstoneGroupManagement", ImGuiWindowFlags.None)
        {
            this.plugin = plugin;
            serverUrl = string.IsNullOrWhiteSpace(plugin.Configuration.SyncServerUrl)
                ? Configuration.DefaultSyncServerUrl
                : plugin.Configuration.SyncServerUrl;

            Size = new Vector2(860, 620);
            SizeCondition = ImGuiCond.FirstUseEver;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(580, 400),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public void Dispose() { }

        internal static string WithStableId(string label, string id) => $"{label}##{id}";

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
            var scale = ImGuiHelpers.GlobalScale;
            var pos = ImGui.GetCursorScreenPos();
            var availWidth = ImGui.GetContentRegionAvail().X;
            var bannerHeight = 58.0f * scale;
            var drawList = ImGui.GetWindowDrawList();

            // Background card with metallic green accent
            var bgCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.12f, 0.14f, 0.96f));
            var borderCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.65f, 0.35f, 0.75f));
            var accentCol = ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGreen);

            drawList.AddRectFilled(pos, pos + new Vector2(availWidth, bannerHeight), bgCol, 8.0f * scale);
            drawList.AddRect(pos, pos + new Vector2(availWidth, bannerHeight), borderCol, 8.0f * scale, ImDrawFlags.None, 1.5f);

            // Left accent stripe
            drawList.AddRectFilled(
                pos + new Vector2(2.5f * scale, 5.0f * scale),
                pos + new Vector2(6.0f * scale, bannerHeight - 5.0f * scale),
                accentCol,
                2.0f * scale);

            // Framed Wifi Icon Emblem
            var emblemSize = 38.0f * scale;
            var emblemPos = pos + new Vector2(12.0f * scale, (bannerHeight - emblemSize) * 0.5f);
            drawList.AddRectFilled(emblemPos, emblemPos + new Vector2(emblemSize, emblemSize), ImGui.ColorConvertFloat4ToU32(new Vector4(0.14f, 0.22f, 0.16f, 0.95f)), 6.0f * scale);
            drawList.AddRect(emblemPos, emblemPos + new Vector2(emblemSize, emblemSize), accentCol, 6.0f * scale, ImDrawFlags.None, 1.2f);

            ImGui.PushFont(UiBuilder.IconFont);
            var iconStr = FontAwesomeIcon.Wifi.ToIconString();
            var iconSize = ImGui.CalcTextSize(iconStr);
            drawList.AddText(emblemPos + new Vector2((emblemSize - iconSize.X) * 0.5f, (emblemSize - iconSize.Y) * 0.5f), accentCol, iconStr);
            ImGui.PopFont();

            // Status details
            ImGui.SetCursorScreenPos(pos + new Vector2(emblemSize + 22.0f * scale, 8.0f * scale));
            ImGui.BeginGroup();
            {
                ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("GroupRelayStatus"));
                ImGui.SameLine(0, 8.0f * scale);

                int memberCount = sync.ConnectedPartyMembers.Count;
                string memberBadgeText = string.Format(LocalizationManager.Instance.GetLocalizedString("GroupConnectedMembers"), memberCount);
                UiUtils.PillBadge(memberBadgeText, new Vector4(0.12f, 0.35f, 0.22f, 0.9f), ImGuiColors.ParsedGreen, FontAwesomeIcon.Users);

                if (sync.IsSessionHost)
                {
                    ImGui.SameLine(0, 6.0f * scale);
                    UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("GroupBadgeLeader"), new Vector4(0.35f, 0.28f, 0.10f, 0.9f), ImGuiColors.ParsedGold, FontAwesomeIcon.Crown);
                }
                else if (!string.IsNullOrWhiteSpace(plugin.Configuration.SyncHostName))
                {
                    ImGui.SameLine(0, 6.0f * scale);
                    string hostInfo = $"{LocalizationManager.Instance.GetLocalizedString("GroupHostLabel")} {plugin.Configuration.SyncHostName}";
                    UiUtils.PillBadge(hostInfo, new Vector4(0.20f, 0.25f, 0.38f, 0.9f), ImGuiColors.ParsedBlue, FontAwesomeIcon.UserShield);
                }

                if (DiceSystemManager.Instance.IsSessionRulesetActive)
                {
                    ImGui.SameLine(0, 6.0f * scale);
                    UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("GroupSyncedFromDM"), new Vector4(0.15f, 0.35f, 0.40f, 0.9f), ImGuiColors.ParsedBlue, FontAwesomeIcon.DiceD20);
                }
            }
            ImGui.EndGroup();

            // Right-aligned buttons inside banner
            float rightButtonsWidth = 0f;
            if (sync.IsSessionHost && !string.IsNullOrWhiteSpace(sync.InviteCode))
            {
                rightButtonsWidth += 120.0f * scale;
            }
            rightButtonsWidth += 150.0f * scale;

            float remainingSpace = availWidth - (emblemSize + 24.0f * scale);
            if (remainingSpace > rightButtonsWidth)
            {
                ImGui.SetCursorScreenPos(new Vector2(pos.X + availWidth - rightButtonsWidth - 10.0f * scale, pos.Y + 14.0f * scale));
            }
            else
            {
                ImGui.SameLine(0, 8.0f * scale);
            }

            ImGui.BeginGroup();
            {
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
                    ImGui.SameLine(0, 4.0f * scale);
                }

                if (UiUtils.IconButton("SessionInfoToggle", showSessionInfo ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.InfoCircle, LocalizationManager.Instance.GetLocalizedString("GroupSessionInfo")))
                {
                    showSessionInfo = !showSessionInfo;
                }

                ImGui.SameLine(0, 4.0f * scale);
                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.45f, 0.18f, 0.18f, 0.8f)))
                using (ImRaii.PushColor(ImGuiCol.ButtonHovered, new Vector4(0.60f, 0.22f, 0.22f, 0.9f)))
                {
                    if (UiUtils.IconButton("LeaveSessionBtn", FontAwesomeIcon.SignOutAlt, LocalizationManager.Instance.GetLocalizedString("GroupLeaveSession")))
                    {
                        _ = sync.DisconnectAsync(true);
                    }
                }
            }
            ImGui.EndGroup();

            ImGui.SetCursorScreenPos(pos);
            ImGui.Dummy(new Vector2(availWidth, bannerHeight));

            if (showSessionInfo)
            {
                ImGui.Spacing();
                using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.08f, 0.09f, 0.11f, 0.8f)))
                using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 4.0f * scale))
                using (var infoChild = ImRaii.Child("##SessionInfoDetailsBox", new Vector2(0, 26.0f * scale), true))
                {
                    if (infoChild.Success)
                    {
                        ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("GroupRelayUrl")}: {plugin.Configuration.SyncServerUrl}");
                        if (!string.IsNullOrWhiteSpace(plugin.Configuration.SyncSessionId))
                        {
                            ImGui.SameLine(0, 12.0f * scale);
                            ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("GroupSessionIdLabel")}: {plugin.Configuration.SyncSessionId}");
                        }
                    }
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
            var scale = ImGuiHelpers.GlobalScale;
            var pos = ImGui.GetCursorScreenPos();
            var availWidth = ImGui.GetContentRegionAvail().X;
            var setupHeight = 56.0f * scale;
            var drawList = ImGui.GetWindowDrawList();

            // Background card with subtle amber border
            var bgCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.11f, 0.13f, 0.95f));
            var borderCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.50f, 0.35f, 0.15f, 0.70f));
            var accentCol = ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudOrange);

            drawList.AddRectFilled(pos, pos + new Vector2(availWidth, setupHeight), bgCol, 8.0f * scale);
            drawList.AddRect(pos, pos + new Vector2(availWidth, setupHeight), borderCol, 8.0f * scale, ImDrawFlags.None, 1.2f);

            // Left accent stripe
            drawList.AddRectFilled(
                pos + new Vector2(2.5f * scale, 5.0f * scale),
                pos + new Vector2(6.0f * scale, setupHeight - 5.0f * scale),
                accentCol,
                2.0f * scale);

            // Framed Plug Icon Emblem
            var emblemSize = 36.0f * scale;
            var emblemPos = pos + new Vector2(10.0f * scale, (setupHeight - emblemSize) * 0.5f);
            drawList.AddRectFilled(emblemPos, emblemPos + new Vector2(emblemSize, emblemSize), ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.16f, 0.12f, 0.95f)), 6.0f * scale);
            drawList.AddRect(emblemPos, emblemPos + new Vector2(emblemSize, emblemSize), accentCol, 6.0f * scale, ImDrawFlags.None, 1.2f);

            ImGui.PushFont(UiBuilder.IconFont);
            var iconStr = FontAwesomeIcon.Plug.ToIconString();
            var iconSize = ImGui.CalcTextSize(iconStr);
            drawList.AddText(emblemPos + new Vector2((emblemSize - iconSize.X) * 0.5f, (emblemSize - iconSize.Y) * 0.5f), accentCol, iconStr);
            ImGui.PopFont();

            ImGui.SetCursorScreenPos(pos + new Vector2(emblemSize + 18.0f * scale, 12.0f * scale));

            ImGui.BeginGroup();
            {
                ImGui.TextColored(ImGuiColors.DalamudOrange, LocalizationManager.Instance.GetLocalizedString("GroupRelayStatus"));
                ImGui.SameLine(0, 4.0f * scale);
                ImGui.TextDisabled($"({sync.ConnectionStatus})");

                // Quick Reconnect Bar if session exists
                if (!string.IsNullOrWhiteSpace(plugin.Configuration.SyncSessionId))
                {
                    ImGui.SameLine(0, 10.0f * scale);
                    using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.18f, 0.40f, 0.25f, 0.85f)))
                    {
                        if (UiUtils.IconButton("QuickReconnectBtn", FontAwesomeIcon.Sync, LocalizationManager.Instance.GetLocalizedString("GroupReconnect")))
                        {
                            _ = ReconnectAsync();
                        }
                    }
                    ImGui.SameLine(0, 4.0f * scale);

                    if (UiUtils.IconButton("ForgetSessionBtn", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("GroupForgetSession")))
                    {
                        _ = sync.DisconnectAsync(true);
                    }
                }

                // Mode Selector Tabs (Join vs Host)
                ImGui.SameLine(0, 10.0f * scale);
                using (ImRaii.PushColor(ImGuiCol.Button, connectionTab == 0 ? new Vector4(0.20f, 0.45f, 0.70f, 0.95f) : new Vector4(0.18f, 0.20f, 0.24f, 0.75f)))
                {
                    if (UiUtils.IconTextButton("JoinSessionTab", FontAwesomeIcon.SignInAlt, LocalizationManager.Instance.GetLocalizedString("GroupJoinTab")))
                    {
                        connectionTab = 0;
                    }
                }
                ImGui.SameLine(0, 4.0f * scale);
                using (ImRaii.PushColor(ImGuiCol.Button, connectionTab == 1 ? new Vector4(0.50f, 0.38f, 0.15f, 0.95f) : new Vector4(0.18f, 0.20f, 0.24f, 0.75f)))
                {
                    if (UiUtils.IconTextButton("HostSessionTab", FontAwesomeIcon.PlusCircle, LocalizationManager.Instance.GetLocalizedString("GroupHostTab")))
                    {
                        connectionTab = 1;
                    }
                }

                ImGui.SameLine(0, 6.0f * scale);

                if (connectionTab == 0) // Join Session
                {
                    float inputWidth = Math.Clamp(ImGui.GetContentRegionAvail().X - 80.0f * scale, 160.0f * scale, 280.0f * scale);
                    UiUtils.StyledInputText("RelayInvite", ref inviteCode, 4096, width: inputWidth / scale, hint: LocalizationManager.Instance.GetLocalizedString("GroupInviteCode"));
                    ImGui.SameLine(0, 4.0f * scale);

                    using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.18f, 0.45f, 0.70f, 0.9f)))
                    {
                        if (UiUtils.IconTextButton("JoinSessionButton", FontAwesomeIcon.SignInAlt, LocalizationManager.Instance.GetLocalizedString("GroupJoinSession")))
                        {
                            _ = JoinSessionAsync();
                        }
                    }
                }
                else // Host Session
                {
                    float inputWidth = Math.Clamp(ImGui.GetContentRegionAvail().X - 130.0f * scale, 160.0f * scale, 280.0f * scale);
                    if (UiUtils.StyledInputText("RelayUrl", ref serverUrl, 512, width: inputWidth / scale, hint: LocalizationManager.Instance.GetLocalizedString("GroupRelayUrl")))
                    {
                        plugin.Configuration.SyncServerUrl = serverUrl;
                        plugin.Configuration.Save();
                    }
                    ImGui.SameLine(0, 4.0f * scale);

                    using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.40f, 0.32f, 0.15f, 0.9f)))
                    {
                        if (UiUtils.IconTextButton("CreateSessionButton", FontAwesomeIcon.PlusCircle, LocalizationManager.Instance.GetLocalizedString("GroupCreateSession")))
                        {
                            _ = CreateSessionAsync();
                        }
                    }
                }
            }
            ImGui.EndGroup();

            ImGui.SetCursorScreenPos(pos);
            ImGui.Dummy(new Vector2(availWidth, setupHeight));

            if (!string.IsNullOrWhiteSpace(connectionMessage))
            {
                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.ParsedGold, connectionMessage);
            }
        }

        private async Task CreateSessionAsync()
        {
            try
            {
                connectionMessage = LocalizationManager.Instance.GetLocalizedString("GroupConnecting");
                serverUrl = RelayCrypto.NormalizeServerUrl(serverUrl);
                plugin.Configuration.SyncServerUrl = serverUrl;
                plugin.Configuration.Save();
                PartySyncManager.Instance.Init(plugin.Configuration);
                bool success = await PartySyncManager.Instance.CreateSessionAsync(serverUrl);
                connectionMessage = LocalizationManager.Instance.GetLocalizedString(success ? "GroupSessionCreated" : "GroupConnectionFailed");
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to handle the create session action");
                connectionMessage = LocalizationManager.Instance.GetLocalizedString("GroupConnectionFailed");
            }
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

            var scale = ImGuiHelpers.GlobalScale;

            foreach (var request in requests)
            {
                using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.18f, 0.14f, 0.08f, 0.95f)))
                using (ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedGold))
                using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 8.0f * scale))
                using (var requestPanel = ImRaii.Child($"##RollRequest_{request.RequestId}", new Vector2(0, 50.0f * scale), true))
                {
                    if (!requestPanel.Success) continue;

                    var drawList = ImGui.GetWindowDrawList();
                    var panelPos = ImGui.GetWindowPos();
                    var panelSize = ImGui.GetWindowSize();

                    // Left gold accent bar
                    drawList.AddRectFilled(
                        panelPos + new Vector2(2.5f * scale, 5.0f * scale),
                        panelPos + new Vector2(6.0f * scale, panelSize.Y - 5.0f * scale),
                        ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGold),
                        2.0f * scale);

                    // Framed Dice Icon Box
                    var iconBoxSize = 34.0f * scale;
                    var iconBoxPos = panelPos + new Vector2(10.0f * scale, (panelSize.Y - iconBoxSize) * 0.5f);
                    drawList.AddRectFilled(iconBoxPos, iconBoxPos + new Vector2(iconBoxSize, iconBoxSize), ImGui.ColorConvertFloat4ToU32(new Vector4(0.24f, 0.18f, 0.10f, 0.95f)), 6.0f * scale);
                    drawList.AddRect(iconBoxPos, iconBoxPos + new Vector2(iconBoxSize, iconBoxSize), ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGold), 6.0f * scale, ImDrawFlags.None, 1.2f);

                    ImGui.PushFont(UiBuilder.IconFont);
                    var iconStr = FontAwesomeIcon.DiceD20.ToIconString();
                    var iconSize = ImGui.CalcTextSize(iconStr);
                    drawList.AddText(iconBoxPos + new Vector2((iconBoxSize - iconSize.X) * 0.5f, (iconBoxSize - iconSize.Y) * 0.5f), ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGold), iconStr);
                    ImGui.PopFont();

                    ImGui.SetCursorScreenPos(panelPos + new Vector2(iconBoxSize + 18.0f * scale, 12.0f * scale));

                    ImGui.BeginGroup();
                    {
                        ImGui.TextColored(ImGuiColors.ParsedGold, $"{request.RequestedBy}:");
                        ImGui.SameLine(0, 6.0f * scale);
                        ImGui.TextColored(ImGuiColors.DalamudWhite, request.RollName);
                        ImGui.SameLine(0, 8.0f * scale);
                        UiUtils.PillBadge(request.Formula, new Vector4(0.28f, 0.22f, 0.10f, 0.9f), ImGuiColors.ParsedGold, FontAwesomeIcon.Dice);
                    }
                    ImGui.EndGroup();

                    float reqButtonsWidth = 220.0f * scale;
                    float avail = ImGui.GetContentRegionAvail().X;
                    if (avail > reqButtonsWidth)
                    {
                        ImGui.SameLine(panelPos.X + panelSize.X - reqButtonsWidth - 10.0f * scale);
                    }

                    using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.20f, 0.55f, 0.28f, 0.9f)))
                    {
                        if (UiUtils.IconTextButton($"RollReqNow_{request.RequestId}", FontAwesomeIcon.DiceD20, LocalizationManager.Instance.GetLocalizedString("GroupRollNow")))
                        {
                            PartySyncManager.Instance.ExecuteRollRequest(request.RequestId);
                        }
                    }

                    ImGui.SameLine(0, 6.0f * scale);
                    if (UiUtils.IconTextButton($"RollReqDismiss_{request.RequestId}", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("GroupDismissRoll")))
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
                        batchRollFormula = DiceRoll.DescribeSystemRoll(DiceSystemManager.Instance.CurrentDiceSystem);
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

                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                using (ImRaii.PushColor(ImGuiCol.Button, plugin.Configuration.ShowGroupResources ? new Vector4(0.20f, 0.40f, 0.30f, 0.9f) : new Vector4(0.18f, 0.18f, 0.22f, 0.7f)))
                {
                    if (UiUtils.IconButton("ToggleGroupResourcesBtn", FontAwesomeIcon.Heart, LocalizationManager.Instance.GetLocalizedString("GroupToggleResourcesTooltip")))
                    {
                        plugin.Configuration.ShowGroupResources = !plugin.Configuration.ShowGroupResources;
                        plugin.Configuration.Save();
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
            UiUtils.StyledInputText("GroupSearch", ref searchQuery, 64, width: 220.0f, hint: LocalizationManager.Instance.GetLocalizedString("GroupSearchHint"), icon: FontAwesomeIcon.Search);

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

            var (roleBg, roleTextCol) = GetJobBadgeColors(member.JobName);
            var accentColor = isLeader ? ImGuiColors.ParsedGold : (isLocal ? ImGuiColors.ParsedBlue : roleTextCol);

            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.11f, 0.14f, 0.95f)))
            using (ImRaii.PushColor(ImGuiCol.Border, isLeader ? new Vector4(0.85f, 0.70f, 0.25f, 0.85f) : (isLocal ? new Vector4(0.30f, 0.55f, 0.85f, 0.75f) : new Vector4(0.24f, 0.26f, 0.32f, 0.65f))))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 8.0f * ImGuiHelpers.GlobalScale))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(12.0f, 10.0f) * ImGuiHelpers.GlobalScale))
            using (var cardChild = ImRaii.Child($"MemberCardFrame_{member.CharacterName}", new Vector2(0, 0), true, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
            {
                if (cardChild.Success)
                {
                    var drawList = ImGui.GetWindowDrawList();
                    var cardPos = ImGui.GetWindowPos();
                    var cardSize = ImGui.GetWindowSize();
                    drawList.AddRectFilled(
                        cardPos + new Vector2(2.5f * ImGuiHelpers.GlobalScale, 6.0f * ImGuiHelpers.GlobalScale),
                        cardPos + new Vector2(6.0f * ImGuiHelpers.GlobalScale, cardSize.Y - 6.0f * ImGuiHelpers.GlobalScale),
                        ImGui.ColorConvertFloat4ToU32(accentColor),
                        2.0f * ImGuiHelpers.GlobalScale);

                    DrawCardHeader(member, isLeader, isLocal);
                    ImGui.Spacing();
                    UiUtils.DrawOrnamentalDivider(accentColor: accentColor);
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
            }

            ImGui.PopID();
        }

        private void DrawCardHeader(PartyMemberSyncData member, bool isLeader, bool isLocal)
        {
            var (jobBg, jobCol) = GetJobBadgeColors(member.JobName);
            var roleIcon = isLeader ? FontAwesomeIcon.Crown : GetJobRoleIcon(member.JobName);
            var iconColor = isLeader ? ImGuiColors.ParsedGold : jobCol;

            var scale = ImGuiHelpers.GlobalScale;
            var emblemSize = 34.0f * scale;
            var pos = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();

            drawList.AddRectFilled(pos, pos + new Vector2(emblemSize, emblemSize), ImGui.ColorConvertFloat4ToU32(new Vector4(0.16f, 0.17f, 0.22f, 0.95f)), 6.0f * scale);
            drawList.AddRect(pos, pos + new Vector2(emblemSize, emblemSize), ImGui.ColorConvertFloat4ToU32(iconColor), 6.0f * scale, ImDrawFlags.None, 1.2f);

            ImGui.PushFont(UiBuilder.IconFont);
            var iconStr = roleIcon.ToIconString();
            var iconSize = ImGui.CalcTextSize(iconStr);
            var iconCenter = pos + new Vector2((emblemSize - iconSize.X) * 0.5f, (emblemSize - iconSize.Y) * 0.5f);
            drawList.AddText(iconCenter, ImGui.ColorConvertFloat4ToU32(iconColor), iconStr);
            ImGui.PopFont();

            ImGui.SetCursorScreenPos(pos + new Vector2(emblemSize + 10.0f * scale, 0));

            ImGui.BeginGroup();
            {
                // Name & World
                ImGui.TextColored(ImGuiColors.DalamudWhite, member.CharacterName);

                if (!string.IsNullOrWhiteSpace(member.WorldName))
                {
                    ImGui.SameLine(0, 6.0f * scale);
                    ImGui.TextDisabled($"({member.WorldName})");
                }

                // Badges row
                if (!string.IsNullOrWhiteSpace(member.JobName))
                {
                    UiUtils.PillBadge(member.JobName, jobBg, jobCol);
                    ImGui.SameLine(0, 6.0f * scale);
                }

                if (isLeader)
                {
                    UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("GroupBadgeLeader"), new Vector4(0.38f, 0.30f, 0.12f, 0.9f), ImGuiColors.ParsedGold, FontAwesomeIcon.Crown);
                    ImGui.SameLine(0, 6.0f * scale);
                }

                if (member.HasSoulstone)
                {
                    UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("GroupStatusConnected"), new Vector4(0.14f, 0.38f, 0.20f, 0.9f), ImGuiColors.ParsedGreen, FontAwesomeIcon.CheckCircle);
                }
                else
                {
                    UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("GroupStatusNoSoulstone"), new Vector4(0.25f, 0.25f, 0.25f, 0.85f), ImGuiColors.DalamudGrey, FontAwesomeIcon.TimesCircle);
                }

                if (!string.IsNullOrWhiteSpace(member.ActiveRulesetName))
                {
                    ImGui.SameLine(0, 6.0f * scale);
                    string rulesetBadge = $"{member.ActiveRulesetName}";
                    var rulesetBg = member.IsRulesetInSync ? new Vector4(0.15f, 0.30f, 0.45f, 0.9f) : new Vector4(0.50f, 0.25f, 0.10f, 0.9f);
                    var rulesetCol = member.IsRulesetInSync ? ImGuiColors.ParsedBlue : ImGuiColors.ParsedOrange;
                    UiUtils.PillBadge(rulesetBadge, rulesetBg, rulesetCol, FontAwesomeIcon.DiceD20);
                }
            }
            ImGui.EndGroup();

            // Right-aligned quick toggles
            if (PartySyncManager.Instance.IsSessionHost && member.HasSoulstone)
            {
                float actionsWidth = 190.0f * scale;
                var currentX = ImGui.GetCursorPosX();
                var availWidth = ImGui.GetContentRegionAvail().X;
                if (availWidth > actionsWidth)
                {
                    ImGui.SameLine(0, 0);
                    ImGui.SetCursorPosX(currentX + availWidth - actionsWidth);
                }
                else
                {
                    ImGui.SameLine(0, 8.0f * scale);
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
                    ImGui.SameLine(0, 6.0f * scale);
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
            if (!plugin.Configuration.ShowGroupResources) return;

            // Sheet / Custom Resources
            if (member.CustomResources != null && member.CustomResources.Count > 0)
            {
                var diceSys = DiceSystemManager.Instance.CurrentDiceSystem;
                bool first = true;
                foreach (var kv in member.CustomResources)
                {
                    if (member.CustomResourceShowInGroup != null &&
                        member.CustomResourceShowInGroup.TryGetValue(kv.Key, out bool show) && !show)
                    {
                        continue;
                    }

                    if (first)
                    {
                        ImGui.Spacing();
                        first = false;
                    }

                    var def = diceSys?.SystemResources.FirstOrDefault(d => string.Equals(d.Name, kv.Key, StringComparison.OrdinalIgnoreCase));
                    var resCol = UiUtils.GetResourceColor(kv.Key, def?.ColorHex);
                    int resType = member.CustomResourceTypes.TryGetValue(kv.Key, out int tVal) ? tVal : (int)(def?.ResourceType ?? ResourceType.Bar);
                    int resMax = member.CustomResourceMaxes.TryGetValue(kv.Key, out int mVal) && mVal > 0 ? mVal : (def?.DefaultMax ?? 100);

                    if (resType == (int)ResourceType.FlatNumber)
                    {
                        string flatLabel = $"{kv.Key}: {kv.Value}";
                        UiUtils.PillBadge(flatLabel, new Vector4(resCol.X * 0.35f, resCol.Y * 0.35f, resCol.Z * 0.35f, 0.85f), resCol, FontAwesomeIcon.Bolt);
                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    }
                    else if (resType == (int)ResourceType.Counter)
                    {
                        string overlay = resMax > 0
                            ? $"{kv.Key}: {kv.Value} / {resMax}"
                            : $"{kv.Key}: {kv.Value}";
                        UiUtils.DrawSectionedBar(
                            kv.Value,
                            resMax > 0 ? resMax : 1,
                            overlay,
                            new Vector2(-1.0f, 16.0f * ImGuiHelpers.GlobalScale),
                            activeColor: resCol);
                        ImGui.Spacing();
                    }
                    else
                    {
                        float fraction = resMax > 0 ? Math.Clamp((float)kv.Value / resMax, 0.0f, 1.0f) : 1.0f;
                        string overlay = $"{kv.Key}: {kv.Value} / {resMax} ({(int)(fraction * 100)}%)";
                        UiUtils.DrawProgressBar(kv.Value, resMax, overlay, new Vector2(-1.0f, 16.0f * ImGuiHelpers.GlobalScale), resCol);
                        ImGui.Spacing();
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

                UiUtils.PillBadge(buffLabel, buffBg, buffCol, buff.IsDebuff ? FontAwesomeIcon.ExclamationCircle : FontAwesomeIcon.Bolt);

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
            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.08f, 0.09f, 0.12f, 0.9f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 4.0f * ImGuiHelpers.GlobalScale))
            using (var rollBox = ImRaii.Child($"##LastRollBox", new Vector2(0, 28.0f * ImGuiHelpers.GlobalScale), true))
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

            var diceSystem = DiceSystemManager.Instance.CurrentDiceSystem;
            int curStatValue = memberRollStatValues.TryGetValue(member.CharacterName, out var sVal) ? sVal : 0;

            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.08f, 0.09f, 0.11f, 0.85f)))
            using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(0.35f, 0.30f, 0.15f, 0.75f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * ImGuiHelpers.GlobalScale))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 8.0f) * ImGuiHelpers.GlobalScale))
            using (var drawer = ImRaii.Child($"DmRollDrawer_{member.CharacterName}", new Vector2(0, 0), true, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
            {
                if (!drawer.Success) return;

                ImGui.TextColored(ImGuiColors.ParsedGold, $"{LocalizationManager.Instance.GetLocalizedString("GroupQuickRoll")}: {member.CharacterName}");
                ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                UiUtils.PillBadge(DiceRoll.DescribeSystemRoll(diceSystem, curStatValue), new Vector4(0.24f, 0.20f, 0.12f, 0.85f), ImGuiColors.ParsedGold, FontAwesomeIcon.DiceD20);
                ImGui.Spacing();

                // Rolls honour the active dice system by default; unchecking allows a raw formula.
                ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("GroupUseSystemDice")}##UseSystem_{member.CharacterName}", ref useSystemDice);
                if (ImGui.IsItemHovered())
                {
                    string systemLabel = diceSystem?.systemName ?? LocalizationManager.Instance.GetLocalizedString("GroupSystemRoll");
                    ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("GroupUseSystemDiceTooltip", systemLabel));
                }

                if (diceSystem?.systemHasAdvantageDisadvantage == true && (diceSystem?.systemType ?? SystemType.DnDSystem) == SystemType.DnDSystem)
                {
                    ImGui.SameLine(0, 12.0f * ImGuiHelpers.GlobalScale);
                    if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("AdvantageCheckbox")}##RollAdv_{member.CharacterName}", ref rollAdvantage))
                    {
                        if (rollAdvantage) rollDisadvantage = false;
                    }
                    ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                    if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("DisadvantageCheckbox")}##RollDisadv_{member.CharacterName}", ref rollDisadvantage))
                    {
                        if (rollDisadvantage) rollAdvantage = false;
                    }
                }

                ImGui.SameLine(0, 12.0f * ImGuiHelpers.GlobalScale);
                ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("GroupPrivateRollCheck")}##RollPriv_{member.CharacterName}", ref rollPrivate);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("GroupPrivateRollTooltip"));
                }

                ImGui.Spacing();

                string curName = memberRollNames.TryGetValue(member.CharacterName, out var nVal) ? nVal : rollName;
                string curFormula = memberRollFormulas.TryGetValue(member.CharacterName, out var fVal) ? fVal : $"1d{DiceRoll.GetSystemSides(diceSystem)}";

                if (UiUtils.StyledInputText($"RollName_{member.CharacterName}", ref curName, 128, width: 140.0f, hint: LocalizationManager.Instance.GetLocalizedString("GroupRollName")))
                {
                    memberRollNames[member.CharacterName] = curName;
                }

                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                if (useSystemDice)
                {
                    // The single value means modifier, pool size or target depending on the system.
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextDisabled(GetSystemStatLabel(diceSystem));
                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    if (UiUtils.StyledInputInt($"RollStat_{member.CharacterName}", ref curStatValue, step: 1, width: 60.0f))
                    {
                        memberRollStatValues[member.CharacterName] = curStatValue;
                    }
                }
                else
                {
                    if (UiUtils.StyledInputText($"RollFormula_{member.CharacterName}", ref curFormula, 128, width: 120.0f, hint: LocalizationManager.Instance.GetLocalizedString("GroupRollFormula")))
                    {
                        memberRollFormulas[member.CharacterName] = curFormula;
                    }
                }

                ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.20f, 0.45f, 0.70f, 0.9f)))
                {
                    if (UiUtils.IconTextButton($"ReqRoll_{member.CharacterName}", FontAwesomeIcon.Bullhorn, LocalizationManager.Instance.GetLocalizedString("GroupRequestRoll")))
                    {
                        if (useSystemDice)
                        {
                            PartySyncManager.Instance.RequestRollWithSystem(member.CharacterName, curName, curStatValue, rollAdvantage, rollDisadvantage, rollPrivate);
                        }
                        else
                        {
                            PartySyncManager.Instance.RequestRoll(member.CharacterName, curFormula, curName, rollAdvantage, rollDisadvantage, rollPrivate);
                        }
                    }
                }

                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.40f, 0.32f, 0.12f, 0.9f)))
                {
                    if (UiUtils.IconTextButton($"RollFor_{member.CharacterName}", FontAwesomeIcon.DiceD20, LocalizationManager.Instance.GetLocalizedString("GroupRollForMember")))
                    {
                        if (useSystemDice)
                        {
                            PartySyncManager.Instance.RollForMemberWithSystem(member.CharacterName, curName, curStatValue, rollAdvantage, rollDisadvantage, isPrivate: rollPrivate);
                        }
                        else
                        {
                            PartySyncManager.Instance.RollForMember(member.CharacterName, curFormula, curName, rollAdvantage, rollDisadvantage, rollPrivate);
                        }
                    }
                }
            }
        }

        // Label for the single numeric input, which the active system reads as a modifier,
        // a dice pool size or a percentile target.
        private static string GetSystemStatLabel(DiceSystem? diceSystem)
        {
            return (diceSystem?.systemType ?? SystemType.DnDSystem) switch
            {
                SystemType.DicePoolSystem => LocalizationManager.Instance.GetLocalizedString("GroupRollPoolSize"),
                SystemType.PercentileSystem => LocalizationManager.Instance.GetLocalizedString("GroupRollTarget"),
                _ => LocalizationManager.Instance.GetLocalizedString("GroupRollModifier"),
            };
        }

        private void DrawDmPrivateStats(PartyMemberSyncData member)
        {
            if (!expandedStatsMembers.Contains(member.CharacterName)) return;

            ImGui.Spacing();
            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.08f, 0.09f, 0.11f, 0.85f)))
            using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(0.20f, 0.35f, 0.55f, 0.75f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * ImGuiHelpers.GlobalScale))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 8.0f) * ImGuiHelpers.GlobalScale))
            using (var statsGroup = ImRaii.Child($"DmStatsDrawer_{member.CharacterName}", new Vector2(0, 0), true, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
            {
                if (!statsGroup.Success) return;

                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors.ParsedBlue, FontAwesomeIcon.Scroll.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                ImGui.TextColored(ImGuiColors.ParsedBlue, $"{LocalizationManager.Instance.GetLocalizedString("GroupStatsSummary")}: {member.CharacterName}");
                ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);

                string levelClassText = $"{LocalizationManager.Instance.GetLocalizedString("LevelLabel")} {member.Level} | {member.ClassName}";
                UiUtils.PillBadge(levelClassText, new Vector4(0.20f, 0.30f, 0.45f, 0.85f), ImGuiColors.ParsedBlue, FontAwesomeIcon.Medal);

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
                {
                    if (UiUtils.IconTextButton($"Stat_{memberName}_{kv.Key}", FontAwesomeIcon.DiceD20, chipText))
                    {
                        PartySyncManager.Instance.RollForMemberWithSystem(memberName, $"{kv.Key} Check", kv.Value, rollAdvantage, rollDisadvantage);
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
            int columns = 5;
            if (ImGui.BeginTable("##GroupTacticalGrid", columns, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("GroupRoleOther"), ImGuiTableColumnFlags.WidthFixed, 50.0f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("NameLabel"), ImGuiTableColumnFlags.WidthStretch, 2.0f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("GroupResources"), ImGuiTableColumnFlags.WidthStretch, 3.5f);
                ImGui.TableSetupColumn(LocalizationManager.Instance.GetLocalizedString("GroupLastRoll"), ImGuiTableColumnFlags.WidthStretch, 2.0f);
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

                    // Col 2: Resources
                    ImGui.TableSetColumnIndex(2);
                    if (plugin.Configuration.ShowGroupResources && member.CustomResources != null && member.CustomResources.Count > 0)
                    {
                        var visibleResources = member.CustomResources
                            .Where(kv => member.CustomResourceShowInGroup == null ||
                                         !member.CustomResourceShowInGroup.TryGetValue(kv.Key, out bool show) || show)
                            .ToList();

                        if (visibleResources.Count > 0)
                        {
                            var diceSys = DiceSystemManager.Instance.CurrentDiceSystem;
                            bool first = true;
                            foreach (var kv in visibleResources)
                            {
                                var def = diceSys?.SystemResources.FirstOrDefault(d => string.Equals(d.Name, kv.Key, StringComparison.OrdinalIgnoreCase));
                                var resCol = UiUtils.GetResourceColor(kv.Key, def?.ColorHex);
                                int resType = member.CustomResourceTypes.TryGetValue(kv.Key, out int tVal) ? tVal : (int)(def?.ResourceType ?? ResourceType.Bar);
                                int resMax = member.CustomResourceMaxes.TryGetValue(kv.Key, out int mVal) && mVal > 0 ? mVal : (def?.DefaultMax ?? 100);

                                if (resType == (int)ResourceType.FlatNumber)
                                {
                                    UiUtils.PillBadge($"{kv.Key}: {kv.Value}", new Vector4(resCol.X * 0.35f, resCol.Y * 0.35f, resCol.Z * 0.35f, 0.85f), resCol, FontAwesomeIcon.Bolt);
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                }
                                else if (resType == (int)ResourceType.Counter)
                                {
                                    if (!first)
                                    {
                                        ImGui.Spacing();
                                    }
                                    first = false;
                                    string overlay = $"{kv.Key}: {kv.Value}/{resMax}";
                                    UiUtils.DrawSectionedBar(
                                        kv.Value,
                                        resMax > 0 ? resMax : 1,
                                        overlay,
                                        new Vector2(-1.0f, 16.0f * ImGuiHelpers.GlobalScale),
                                        activeColor: resCol);
                                }
                                else
                                {
                                    if (!first)
                                    {
                                        ImGui.Spacing();
                                    }
                                    first = false;
                                    UiUtils.DrawProgressBar(kv.Value, resMax, $"{kv.Key}: {kv.Value}/{resMax}", new Vector2(-1.0f, 16.0f * ImGuiHelpers.GlobalScale), resCol);
                                }
                            }
                        }
                        else
                        {
                            ImGui.TextDisabled("—");
                        }
                    }
                    else
                    {
                        ImGui.TextDisabled("—");
                    }

                    // Col 3: Last Roll
                    ImGui.TableSetColumnIndex(3);
                    if (!string.IsNullOrWhiteSpace(member.LastRollSummary))
                    {
                        ImGui.TextColored(ImGuiColors.ParsedGold, member.LastRollSummary);
                    }
                    else
                    {
                        ImGui.TextDisabled("—");
                    }

                    // Col 4: Actions
                    ImGui.TableSetColumnIndex(4);
                    if (PartySyncManager.Instance.IsSessionHost && member.HasSoulstone)
                    {
                        if (UiUtils.IconButton($"GridRoll_{member.CharacterName}", FontAwesomeIcon.DiceD20, LocalizationManager.Instance.GetLocalizedString("GroupRollForMember")))
                        {
                            PartySyncManager.Instance.RollForMemberWithSystem(member.CharacterName, rollName);
                        }
                        ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                        if (UiUtils.IconButton($"GridReq_{member.CharacterName}", FontAwesomeIcon.Bullhorn, LocalizationManager.Instance.GetLocalizedString("GroupRequestRoll")))
                        {
                            PartySyncManager.Instance.RequestRollWithSystem(member.CharacterName, rollName);
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
                var diceSystem = DiceSystemManager.Instance.CurrentDiceSystem;
                string systemFormula = DiceRoll.DescribeSystemRoll(diceSystem);

                ImGui.Spacing();
                ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("GroupBatchRollTitle"));
                ImGui.Spacing();

                // Quick presets, expressed with the active dice system instead of a fixed d20
                if (UiUtils.IconTextButton("BatchPresetPerc", FontAwesomeIcon.Eye, $"Perception {systemFormula}")) { batchRollName = "Perception Check"; batchRollFormula = systemFormula; }
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("BatchPresetInit", FontAwesomeIcon.Stopwatch, $"Initiative {systemFormula}")) { batchRollName = "Initiative"; batchRollFormula = systemFormula; }
                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("BatchPresetSave", FontAwesomeIcon.ShieldAlt, $"Save {systemFormula}")) { batchRollName = "Saving Throw"; batchRollFormula = systemFormula; }

                ImGui.Spacing();
                UiUtils.StyledInputText("BatchRollName", ref batchRollName, 128, width: 260.0f, hint: LocalizationManager.Instance.GetLocalizedString("GroupRollName"));

                if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("GroupUseSystemDice")}##BatchUseSystem", ref useSystemDice))
                {
                    if (useSystemDice) batchRollFormula = systemFormula;
                }
                if (ImGui.IsItemHovered())
                {
                    string systemLabel = diceSystem?.systemName ?? LocalizationManager.Instance.GetLocalizedString("GroupSystemRoll");
                    ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("GroupUseSystemDiceTooltip", systemLabel));
                }

                if (useSystemDice)
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextDisabled(GetSystemStatLabel(diceSystem));
                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    UiUtils.StyledInputInt("BatchRollStat", ref batchRollStatValue, step: 1, width: 100.0f);
                }
                else
                {
                    UiUtils.StyledInputText("BatchRollFormula", ref batchRollFormula, 128, width: 260.0f, hint: LocalizationManager.Instance.GetLocalizedString("GroupRollFormula"));
                }

                ImGui.Spacing();
                ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("GroupPrivateRollCheck")}##BatchPrivateRoll", ref rollPrivate);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(LocalizationManager.Instance.GetLocalizedString("GroupPrivateRollTooltip"));
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.20f, 0.50f, 0.30f, 0.9f)))
                {
                    if (UiUtils.IconTextButton("BatchRollSendBtn", FontAwesomeIcon.PaperPlane, LocalizationManager.Instance.GetLocalizedString("GroupBatchRollSend"), size: new Vector2(160.0f * ImGuiHelpers.GlobalScale, 0)))
                    {
                        foreach (var m in PartySyncManager.Instance.ConnectedPartyMembers.Values)
                        {
                            if (m.HasSoulstone)
                            {
                                if (useSystemDice)
                                {
                                    PartySyncManager.Instance.RequestRollWithSystem(m.CharacterName, batchRollName, batchRollStatValue, isPrivate: rollPrivate);
                                }
                                else
                                {
                                    PartySyncManager.Instance.RequestRoll(m.CharacterName, batchRollFormula, batchRollName, isPrivate: rollPrivate);
                                }
                            }
                        }
                        showBatchRollModal = false;
                    }
                }

                ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                if (UiUtils.IconTextButton("BatchRollCancelBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(100.0f * ImGuiHelpers.GlobalScale, 0)))
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
