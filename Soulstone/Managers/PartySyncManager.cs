using Soulstone.Datamodels;
using Soulstone.Sync;
using Soulstone.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Soulstone.Managers
{
    internal class PartySyncManager : IDisposable
    {
        public static PartySyncManager Instance { get; } = new();

        public ConcurrentDictionary<string, PartyMemberSyncData> ConnectedPartyMembers { get; } = new(StringComparer.OrdinalIgnoreCase);
        public ConcurrentDictionary<string, RollRequestPayload> PendingRollRequests { get; } = new(StringComparer.OrdinalIgnoreCase);

        public event Action? OnPartyRosterUpdated;
        public event Action<PartyMemberSyncData>? OnPartyMemberUpdated;
        public event Action<DiceRollPayload>? OnRemoteDiceRolled;
        public event Action<InitiativeSyncPayload>? OnInitiativeSyncReceived;
        public event Action<InitiativeTurnPayload>? OnTurnAdvancedReceived;
        public event Action<InitiativeParticipant>? OnParticipantUpsertReceived;
        public event Action<string>? OnParticipantRemovedReceived;
        public event Action? OnInitiativeResetReceived;
        public event Action<ResourceUpdatePayload>? OnResourceUpdated;
        public event Action<RulesetBroadcastPayload>? OnRulesetOffered;
        public event Action<BuffUpdatePayload>? OnBuffUpdated;
        public event Action<RollRequestPayload>? OnRollRequested;
        public event Action<PrivateStatsPayload>? OnPrivateStatsUpdated;
        public event Action? OnConnectionChanged;

        private bool isInitialized = false;
        private DateTime lastPresenceBroadcast = DateTime.MinValue;
        private readonly RelaySyncClient relayClient = new();
        private Configuration? configuration;

        public string ConnectionStatus => relayClient.Status;
        public bool IsConnected => relayClient.IsConnected;
        public bool IsSessionHost => configuration != null && !string.IsNullOrWhiteSpace(configuration.SyncHostToken);
        public string InviteCode => configuration?.SyncInviteCode ?? string.Empty;

        public void Init(Configuration config)
        {
            configuration = config;
            if (isInitialized) return;
            isInitialized = true;
            relayClient.MessageReceived += OnRelayMessage;
            relayClient.StatusChanged += OnRelayStatusChanged;

            RefreshPartyList();
            if (config.SyncAutoConnect && !string.IsNullOrWhiteSpace(config.SyncSessionId))
            {
                _ = ReconnectAsync();
            }
        }

        public void Dispose()
        {
            if (!isInitialized)
            {
                configuration = null;
                return;
            }
            isInitialized = false;

            relayClient.MessageReceived -= OnRelayMessage;
            relayClient.StatusChanged -= OnRelayStatusChanged;
            relayClient.DisposeAsync().AsTask().GetAwaiter().GetResult();
            configuration = null;
            ConnectedPartyMembers.Clear();
            PendingRollRequests.Clear();
        }

        public async Task<bool> CreateSessionAsync(string serverUrl)
        {
            try
            {
                if (configuration == null)
                    throw new InvalidOperationException("Party synchronization has not been initialized.");

                RelaySessionResponse session = await relayClient.CreateSessionAsync(serverUrl).ConfigureAwait(false);
                var keys = RelayCrypto.CreateHostKeyPair();
                string shortCode = RelayCrypto.CreateShortInviteCode();
                string shortInviteLink = RelayCrypto.CreateShortInviteLink(serverUrl, shortCode);
                if (!RelayCrypto.TryParseShortInviteLink(shortInviteLink, out string normalizedServerUrl, out _))
                    throw new InvalidOperationException("Failed to create the short invite link.");
                string roomKey = RelayCrypto.CreateRoomKey();
                string hostName = GetLocalPlayerName();
                string hostWorld = GetLocalPlayerWorld();
                var invite = new RelayInvite
                {
                    ServerUrl = normalizedServerUrl,
                    SessionId = session.SessionId,
                    MemberToken = session.MemberToken,
                    RoomKey = roomKey,
                    HostPublicKey = keys.PublicKey,
                    HostName = hostName,
                    HostWorld = hostWorld
                };
                await relayClient.RegisterInviteAsync(
                    normalizedServerUrl,
                    session.SessionId,
                    session.HostToken,
                    RelayCrypto.CreateInviteId(shortCode),
                    RelayCrypto.EncryptInvite(invite, shortCode)).ConfigureAwait(false);

                configuration.SyncServerUrl = normalizedServerUrl;
                configuration.SyncSessionId = session.SessionId;
                configuration.SyncHostToken = session.HostToken;
                configuration.SyncMemberToken = session.MemberToken;
                configuration.SyncRoomKey = roomKey;
                configuration.SyncHostPublicKey = keys.PublicKey;
                configuration.SyncHostPrivateKey = keys.PrivateKey;
                configuration.SyncHostName = hostName;
                configuration.SyncHostWorld = hostWorld;
                configuration.SyncInviteCode = shortInviteLink;
                configuration.Save();
                await ConnectFromConfigurationAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to create Soulstone relay session");
                return false;
            }
        }

        public async Task<bool> JoinSessionAsync(string inviteCode)
        {
            try
            {
                if (configuration == null)
                    throw new InvalidOperationException("Party synchronization has not been initialized.");

                string trimmedInvite = inviteCode.Trim();
                RelayInvite? invite;
                if (!RelayCrypto.TryDecodeInvite(trimmedInvite, out invite))
                {
                    if (!RelayCrypto.TryParseShortInviteLink(trimmedInvite, out string serverUrl, out string shortCode))
                        return false;
                    string payload = await relayClient.ResolveInviteAsync(
                        serverUrl,
                        RelayCrypto.CreateInviteId(shortCode)).ConfigureAwait(false);
                    if (!RelayCrypto.TryDecryptInvite(payload, shortCode, out invite) || invite == null ||
                        !string.Equals(invite.ServerUrl.TrimEnd('/'), serverUrl, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                if (invite == null || !IsSenderInCurrentParty(invite.HostName))
                    return false;

                configuration.SyncServerUrl = invite.ServerUrl;
                configuration.SyncSessionId = invite.SessionId;
                configuration.SyncHostToken = string.Empty;
                configuration.SyncMemberToken = invite.MemberToken;
                configuration.SyncRoomKey = invite.RoomKey;
                configuration.SyncHostPublicKey = invite.HostPublicKey;
                configuration.SyncHostPrivateKey = string.Empty;
                configuration.SyncHostName = invite.HostName;
                configuration.SyncHostWorld = invite.HostWorld;
                configuration.SyncInviteCode = string.Empty;
                configuration.Save();
                await ConnectFromConfigurationAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to join Soulstone relay session");
                return false;
            }
        }

        public async Task DisconnectAsync(bool forgetSession = false)
        {
            await relayClient.DisconnectAsync().ConfigureAwait(false);
            if (forgetSession && configuration != null)
            {
                configuration.SyncSessionId = string.Empty;
                configuration.SyncHostToken = string.Empty;
                configuration.SyncMemberToken = string.Empty;
                configuration.SyncRoomKey = string.Empty;
                configuration.SyncHostPublicKey = string.Empty;
                configuration.SyncHostPrivateKey = string.Empty;
                configuration.SyncHostName = string.Empty;
                configuration.SyncHostWorld = string.Empty;
                configuration.SyncInviteCode = string.Empty;
                configuration.Save();
                ConnectedPartyMembers.Clear();
                PendingRollRequests.Clear();
            }
            OnConnectionChanged?.Invoke();
            OnPartyRosterUpdated?.Invoke();
        }

        public async Task<bool> ReconnectAsync()
        {
            try
            {
                await ConnectFromConfigurationAsync().ConfigureAwait(false);
                return relayClient.IsConnected;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warning(ex, "Failed to reconnect to the Soulstone relay");
                return false;
            }
        }

        private async Task ConnectFromConfigurationAsync()
        {
            if (configuration == null) return;
            string token = !string.IsNullOrWhiteSpace(configuration.SyncHostToken)
                ? configuration.SyncHostToken
                : configuration.SyncMemberToken;
            await relayClient.ConnectAsync(configuration.SyncServerUrl, configuration.SyncSessionId, token).ConfigureAwait(false);
            BroadcastPresence();
            BroadcastPrivateStats();
            OnConnectionChanged?.Invoke();
        }

        private void OnRelayStatusChanged(string status)
        {
            OnConnectionChanged?.Invoke();
        }

        private void OnRelayMessage(string json)
        {
            if (configuration == null) return;
            try
            {
                var envelope = JsonSerializer.Deserialize<RelayEnvelope>(json);
                if (envelope == null || envelope.Version != 1 || !IsSenderInCurrentParty(envelope.SenderName)) return;
                if (RequiresHostSignature(envelope.EventType) &&
                    !RelayCrypto.VerifyHostSignature(envelope, configuration.SyncHostPublicKey)) return;
                if (!RelayCrypto.TryDecryptMessage(envelope, configuration.SyncRoomKey, configuration.SyncHostPrivateKey, out var packet) || packet == null) return;
                if (!string.Equals(packet.SenderName, envelope.SenderName, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(packet.SenderWorld, envelope.SenderWorld, StringComparison.OrdinalIgnoreCase)) return;
                if (packet.EventType == SyncEventType.DiceRoll)
                {
                    var roll = JsonSerializer.Deserialize<DiceRollPayload>(packet.PayloadJson);
                    if (roll != null && !string.Equals(roll.CharacterName, envelope.SenderName, StringComparison.OrdinalIgnoreCase) &&
                        !RelayCrypto.VerifyHostSignature(envelope, configuration.SyncHostPublicKey)) return;
                }
                HandleIncomingPacket(packet, envelope.SenderName);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Debug(ex, "Ignored invalid Soulstone relay message");
            }
        }

        public void HandleIncomingPacket(PartySyncPacket packet, string senderDisplayName = "")
        {
            if (packet == null || packet.ProtocolVersion != 1) return;

            string senderName = !string.IsNullOrWhiteSpace(senderDisplayName)
                ? senderDisplayName
                : (!string.IsNullOrWhiteSpace(packet.SenderName) ? packet.SenderName : "Unknown");

            // Ignore echo of our own packets if needed, or process them to stay in sync
            string localName = GetLocalPlayerName();
            bool isFromSelf = string.Equals(senderName, localName, StringComparison.OrdinalIgnoreCase);

            switch (packet.EventType)
            {
                case SyncEventType.Presence:
                    try
                    {
                        var presence = JsonSerializer.Deserialize<PresencePayload>(packet.PayloadJson);
                        if (presence != null)
                        {
                            presence.CharacterName = senderName;
                            presence.WorldName = packet.SenderWorld;
                            string localRuleset = DiceSystemManager.Instance.CurrentDiceSystem?.systemName ?? string.Empty;
                            var memberData = ConnectedPartyMembers.GetOrAdd(senderName, name => new PartyMemberSyncData
                            {
                                CharacterName = name,
                                WorldName = packet.SenderWorld
                            });

                            bool firstContact = !memberData.HasSoulstone;
                            memberData.ApplyPresence(presence, localRuleset);
                            UpdateLeaderStatus(memberData);
                            OnPartyMemberUpdated?.Invoke(memberData);
                            OnPartyRosterUpdated?.Invoke();
                            if (firstContact && !isFromSelf)
                            {
                                BroadcastPresence();
                                BroadcastPrivateStats();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Debug(ex, "Error deserializing PresencePayload");
                    }
                    break;

                case SyncEventType.SyncRequest:
                    if (!isFromSelf)
                    {
                        BroadcastPresence();
                        BroadcastPrivateStats();
                    }
                    break;

                case SyncEventType.DiceRoll:
                    try
                    {
                        var roll = JsonSerializer.Deserialize<DiceRollPayload>(packet.PayloadJson);
                        if (roll != null)
                        {
                            if (string.IsNullOrWhiteSpace(roll.RolledBy)) roll.RolledBy = senderName;
                            if (!string.Equals(roll.RolledBy, senderName, StringComparison.OrdinalIgnoreCase) &&
                                !RequiresHostSignature(packet.EventType)) return;
                            if (ConnectedPartyMembers.TryGetValue(roll.CharacterName, out var member))
                            {
                                member.LastRollSummary = $"{roll.RollName}: {roll.Total} ({roll.Details})";
                                member.LastSeen = DateTime.UtcNow;
                                OnPartyMemberUpdated?.Invoke(member);
                            }
                            OnRemoteDiceRolled?.Invoke(roll);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Debug(ex, "Error deserializing DiceRollPayload");
                    }
                    break;

                case SyncEventType.InitiativeAddOrUpdate:
                    try
                    {
                        var participant = JsonSerializer.Deserialize<InitiativeParticipant>(packet.PayloadJson);
                        if (participant != null)
                        {
                            OnParticipantUpsertReceived?.Invoke(participant);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Debug(ex, "Error deserializing InitiativeParticipant");
                    }
                    break;

                case SyncEventType.InitiativeSync:
                    try
                    {
                        var initiative = JsonSerializer.Deserialize<InitiativeSyncPayload>(packet.PayloadJson);
                        if (initiative != null)
                        {
                            OnInitiativeSyncReceived?.Invoke(initiative);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Debug(ex, "Error deserializing InitiativeSyncPayload");
                    }
                    break;

                case SyncEventType.InitiativeRemove:
                    try
                    {
                        string participantId = packet.PayloadJson.Trim('"', ' ');
                        if (!string.IsNullOrEmpty(participantId))
                        {
                            OnParticipantRemovedReceived?.Invoke(participantId);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Debug(ex, "Error deserializing InitiativeRemove payload");
                    }
                    break;

                case SyncEventType.InitiativeTurnAdvance:
                    try
                    {
                        var turn = JsonSerializer.Deserialize<InitiativeTurnPayload>(packet.PayloadJson);
                        if (turn != null)
                        {
                            OnTurnAdvancedReceived?.Invoke(turn);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Debug(ex, "Error deserializing InitiativeTurnPayload");
                    }
                    break;

                case SyncEventType.InitiativeReset:
                    OnInitiativeResetReceived?.Invoke();
                    break;

                case SyncEventType.ResourceUpdate:
                    try
                    {
                        var res = JsonSerializer.Deserialize<ResourceUpdatePayload>(packet.PayloadJson);
                        if (res != null)
                        {
                            res.CharacterName = senderName;
                            if (ConnectedPartyMembers.TryGetValue(senderName, out var member))
                            {
                                member.ApplyResourceUpdate(res);
                                OnPartyMemberUpdated?.Invoke(member);
                            }
                            OnResourceUpdated?.Invoke(res);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Debug(ex, "Error deserializing ResourceUpdatePayload");
                    }
                    break;

                case SyncEventType.RulesetBroadcast:
                    try
                    {
                        var rulesetPayload = JsonSerializer.Deserialize<RulesetBroadcastPayload>(packet.PayloadJson);
                        if (rulesetPayload != null)
                        {
                            OnRulesetOffered?.Invoke(rulesetPayload);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Debug(ex, "Error deserializing RulesetBroadcastPayload");
                    }
                    break;

                case SyncEventType.BuffUpdate:
                    try
                    {
                        var buffs = JsonSerializer.Deserialize<BuffUpdatePayload>(packet.PayloadJson);
                        if (buffs != null)
                        {
                            buffs.CharacterName = senderName;
                            if (ConnectedPartyMembers.TryGetValue(senderName, out var member))
                            {
                                member.ApplyBuffUpdate(buffs);
                                OnPartyMemberUpdated?.Invoke(member);
                            }
                            OnBuffUpdated?.Invoke(buffs);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Debug(ex, "Error deserializing BuffUpdatePayload");
                    }
                    break;

                case SyncEventType.RollRequest:
                    try
                    {
                        var request = JsonSerializer.Deserialize<RollRequestPayload>(packet.PayloadJson);
                        if (request != null && IsAddressedToLocalPlayer(request.TargetName))
                        {
                            request.RequestedBy = senderName;
                            PendingRollRequests[request.RequestId] = request;
                            OnRollRequested?.Invoke(request);
                            OnPartyRosterUpdated?.Invoke();
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Debug(ex, "Error deserializing RollRequestPayload");
                    }
                    break;

                case SyncEventType.PrivateStats:
                    try
                    {
                        var stats = JsonSerializer.Deserialize<PrivateStatsPayload>(packet.PayloadJson);
                        if (stats != null && IsSessionHost && (string.IsNullOrWhiteSpace(stats.TargetName) || IsAddressedToLocalPlayer(stats.TargetName)))
                        {
                            stats.CharacterName = senderName;
                            var member = ConnectedPartyMembers.GetOrAdd(senderName, name => new PartyMemberSyncData { CharacterName = name });
                            member.ApplyPrivateStats(stats);
                            OnPrivateStatsUpdated?.Invoke(stats);
                            OnPartyMemberUpdated?.Invoke(member);
                            OnPartyRosterUpdated?.Invoke();
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Debug(ex, "Error deserializing PrivateStatsPayload");
                    }
                    break;
            }
        }

        public void RefreshPartyList()
        {
            var activeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string leaderName = GetPartyLeaderName();

            try
            {
                if (Plugin.PartyList != null && Plugin.PartyList.Length > 0)
                {
                    for (int i = 0; i < Plugin.PartyList.Length; i++)
                    {
                        var pm = Plugin.PartyList[i];
                        if (pm == null) continue;

                        string name = pm.Name.TextValue;
                        if (string.IsNullOrWhiteSpace(name)) continue;

                        activeNames.Add(name);

                        var memberData = ConnectedPartyMembers.GetOrAdd(name, n => new PartyMemberSyncData
                        {
                            CharacterName = n,
                            HasSoulstone = false
                        });

                        try
                        {
                            memberData.WorldName = pm.World.Value.Name.ToString();
                        }
                        catch { }

                        try
                        {
                            memberData.JobName = pm.ClassJob.Value.Abbreviation.ToString();
                        }
                        catch { }

                        // Fallback vitals from game party frame if no Soulstone presence yet
                        if (!memberData.HasSoulstone)
                        {
                            memberData.CurrentHp = (int)pm.CurrentHP;
                            memberData.MaxHp = (int)pm.MaxHP;
                            memberData.CurrentMana = (int)pm.CurrentMP;
                            memberData.MaxMana = (int)pm.MaxMP;
                        }

                        UpdateLeaderStatus(memberData);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Debug(ex, "Error refreshing party list from Dalamud");
            }

            // Always ensure local player is represented
            string localPlayerName = GetLocalPlayerName();
            if (!string.IsNullOrWhiteSpace(localPlayerName))
            {
                activeNames.Add(localPlayerName);
                var localData = ConnectedPartyMembers.GetOrAdd(localPlayerName, n => new PartyMemberSyncData
                {
                    CharacterName = n,
                    HasSoulstone = true
                });

                localData.HasSoulstone = true;
                UpdateLeaderStatus(localData);
                PopulateLocalPlayerVitals(localData);
            }

            // Clean up members: only remove members who are NOT in party list AND do NOT have an active Soulstone sync session presence
            var toRemove = ConnectedPartyMembers.Where(kvp =>
                !activeNames.Contains(kvp.Key) &&
                (!kvp.Value.HasSoulstone || (DateTime.UtcNow - kvp.Value.LastSeen).TotalMinutes > 60)
            ).Select(kvp => kvp.Key).ToList();

            foreach (var key in toRemove)
            {
                ConnectedPartyMembers.TryRemove(key, out _);
            }

            OnPartyRosterUpdated?.Invoke();
        }

        private void UpdateLeaderStatus(PartyMemberSyncData member)
        {
            string hostName = configuration?.SyncHostName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(hostName))
            {
                member.IsPartyLeader = string.Equals(member.CharacterName, hostName, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                string leaderName = GetPartyLeaderName();
                member.IsPartyLeader = string.Equals(member.CharacterName, leaderName, StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool IsLocalPlayerPartyLeader()
        {
            if (IsSessionHost) return true;
            if (IsConnected) return false;
            try
            {
                if (Plugin.PartyList == null || Plugin.PartyList.Length <= 1)
                    return true;

                int leaderIdx = (int)Plugin.PartyList.PartyLeaderIndex;
                if (leaderIdx >= 0 && leaderIdx < Plugin.PartyList.Length)
                {
                    var leaderMember = Plugin.PartyList[leaderIdx];
                    if (leaderMember != null)
                    {
                        string leaderName = leaderMember.Name.TextValue;
                        return string.Equals(leaderName, GetLocalPlayerName(), StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch { }

            return true;
        }

        public string GetPartyLeaderName()
        {
            if (!string.IsNullOrWhiteSpace(configuration?.SyncHostName))
            {
                return configuration.SyncHostName;
            }

            try
            {
                if (Plugin.PartyList != null && Plugin.PartyList.Length > 0)
                {
                    int leaderIdx = (int)Plugin.PartyList.PartyLeaderIndex;
                    if (leaderIdx >= 0 && leaderIdx < Plugin.PartyList.Length)
                    {
                        var leader = Plugin.PartyList[leaderIdx];
                        if (leader != null && !string.IsNullOrWhiteSpace(leader.Name.TextValue))
                        {
                            return leader.Name.TextValue;
                        }
                    }
                }
            }
            catch { }

            return GetLocalPlayerName();
        }

        public string GetLocalPlayerName()
        {
            try
            {
                if (Plugin.ObjectTable?.LocalPlayer != null)
                {
                    return Plugin.ObjectTable.LocalPlayer.Name.TextValue;
                }
            }
            catch { }

            var sheet = CharacterManager.Instance.CharacterSheet;
            if (sheet != null && !string.IsNullOrWhiteSpace(sheet.characterFullName))
            {
                return sheet.characterFullName;
            }

            return "Local Player";
        }

        public string GetLocalPlayerWorld()
        {
            try
            {
                if (Plugin.ObjectTable?.LocalPlayer != null)
                {
                    return Plugin.ObjectTable.LocalPlayer.HomeWorld.Value.Name.ToString();
                }
            }
            catch { }

            return string.Empty;
        }

        private bool IsSenderInCurrentParty(string senderName)
        {
            if (string.IsNullOrWhiteSpace(senderName)) return false;
            if (IsConnected) return true;
            if (string.Equals(senderName, GetLocalPlayerName(), StringComparison.OrdinalIgnoreCase)) return true;
            try
            {
                if (Plugin.PartyList == null || Plugin.PartyList.Length <= 1) return true;
                return Plugin.PartyList.Any(member => member != null &&
                    string.Equals(member.Name.TextValue, senderName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return true;
            }
        }

        private bool IsAddressedToLocalPlayer(string targetName)
        {
            return string.Equals(targetName, GetLocalPlayerName(), StringComparison.OrdinalIgnoreCase) ||
                   (!string.IsNullOrWhiteSpace(configuration?.SyncHostName) &&
                    string.Equals(targetName, configuration.SyncHostName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool RequiresHostSignature(SyncEventType eventType)
        {
            return eventType is SyncEventType.RulesetBroadcast
                or SyncEventType.InitiativeAddOrUpdate
                or SyncEventType.InitiativeTurnAdvance
                or SyncEventType.InitiativeReset
                or SyncEventType.InitiativeRemove
                or SyncEventType.RollRequest;
        }

        public void PopulateLocalPlayerVitals(PartyMemberSyncData data)
        {
            var sheet = CharacterManager.Instance.CharacterSheet;
            var diceSys = DiceSystemManager.Instance.CurrentDiceSystem;

            if (sheet != null)
            {
                data.CurrentHp = sheet.characterHealthPoints;
                data.MaxHp = sheet.characterMaxHealthPoints > 0 ? sheet.characterMaxHealthPoints : 100;
                data.CurrentMana = sheet.characterManaPoints;
                data.MaxMana = sheet.characterMaxManaPoints > 0 ? sheet.characterMaxManaPoints : 100;

                data.CustomResources.Clear();
                data.CustomResourceMaxes.Clear();
                if (sheet.characterResources != null)
                {
                    foreach (var kv in sheet.characterResources)
                    {
                        data.CustomResources[kv.Key] = kv.Value.CurrentValue;
                        data.CustomResourceMaxes[kv.Key] = kv.Value.MaxValue;
                    }
                }

                data.ActiveBuffs = sheet.activeBuffs != null ? new List<Buff>(sheet.activeBuffs) : new List<Buff>();
            }

            if (diceSys != null)
            {
                data.ActiveRulesetName = diceSys.systemName;
                data.IsRulesetInSync = true;
            }
        }

        public void SendPacket(SyncEventType eventType, object payload, string humanReadableEcho = "")
        {
            if (!string.IsNullOrWhiteSpace(humanReadableEcho)) Messages.PrintEcho(humanReadableEcho);
            if (!relayClient.IsConnected || configuration == null) return;
            _ = SendPacketCoreAsync(eventType, payload);
        }

        private async Task SendPacketCoreAsync(SyncEventType eventType, object payload)
        {
            if (configuration == null) return;
            try
            {
                var packet = new PartySyncPacket
                {
                    ProtocolVersion = 1,
                    EventType = eventType,
                    SenderName = GetLocalPlayerName(),
                    SenderWorld = GetLocalPlayerWorld(),
                    PayloadJson = JsonSerializer.Serialize(payload)
                };

                RelayEnvelope envelope = eventType == SyncEventType.PrivateStats
                    ? RelayCrypto.EncryptPrivateMessage(packet, configuration.SyncHostPublicKey)
                    : RelayCrypto.EncryptGroupMessage(packet, configuration.SyncRoomKey);
                if (IsSessionHost)
                {
                    RelayCrypto.SignEnvelope(envelope, configuration.SyncHostPrivateKey);
                }
                await relayClient.SendAsync(envelope).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warning(ex, $"Failed to send relay message for event {eventType}");
            }
        }

        public void BroadcastPresence()
        {
            if ((DateTime.UtcNow - lastPresenceBroadcast).TotalMilliseconds < 500)
                return;

            lastPresenceBroadcast = DateTime.UtcNow;

            var sheet = CharacterManager.Instance.CharacterSheet;
            var diceSys = DiceSystemManager.Instance.CurrentDiceSystem;

            var payload = new PresencePayload
            {
                CharacterName = GetLocalPlayerName(),
                WorldName = GetLocalPlayerWorld(),
                RulesetName = diceSys?.systemName ?? string.Empty,
                CurrentHp = sheet?.characterHealthPoints ?? 100,
                MaxHp = sheet?.characterMaxHealthPoints ?? 100,
                CurrentMana = sheet?.characterManaPoints ?? 100,
                MaxMana = sheet?.characterMaxManaPoints ?? 100,
                ActiveBuffs = sheet?.activeBuffs != null ? new List<Buff>(sheet.activeBuffs) : new List<Buff>()
            };

            if (sheet?.characterResources != null)
            {
                foreach (var kv in sheet.characterResources)
                {
                    payload.CustomResources[kv.Key] = kv.Value.CurrentValue;
                    payload.CustomResourceMaxes[kv.Key] = kv.Value.MaxValue;
                }
            }

            SendPacket(SyncEventType.Presence, payload);

            // Also update local record
            string localName = GetLocalPlayerName();
            var localData = ConnectedPartyMembers.GetOrAdd(localName, n => new PartyMemberSyncData { CharacterName = n });
            localData.ApplyPresence(payload, diceSys?.systemName);
            OnPartyMemberUpdated?.Invoke(localData);
        }

        public void BroadcastDiceRoll(string rollName, int total, string details, bool isCritSuccess = false, bool isCritFailure = false, string echoText = "", string? characterName = null)
        {
            string roller = GetLocalPlayerName();
            string actor = string.IsNullOrWhiteSpace(characterName) ? roller : characterName;
            var payload = new DiceRollPayload
            {
                CharacterName = actor,
                RolledBy = roller,
                RollName = rollName,
                Total = total,
                Details = details,
                IsCriticalSuccess = isCritSuccess,
                IsCriticalFailure = isCritFailure,
                RulesetName = DiceSystemManager.Instance.CurrentDiceSystem?.systemName ?? string.Empty
            };

            SendPacket(SyncEventType.DiceRoll, payload, echoText);

            var member = ConnectedPartyMembers.GetOrAdd(actor, name => new PartyMemberSyncData { CharacterName = name });
            member.LastRollSummary = $"{rollName}: {total} ({details})";
            member.LastSeen = DateTime.UtcNow;
            OnPartyMemberUpdated?.Invoke(member);
        }

        public bool RequestRoll(string targetName, string formula, string rollName, bool advantage = false, bool disadvantage = false)
        {
            if (!IsSessionHost || string.IsNullOrWhiteSpace(targetName) || string.IsNullOrWhiteSpace(formula)) return false;
            var request = new RollRequestPayload
            {
                RequestedBy = GetLocalPlayerName(),
                TargetName = targetName,
                RollName = string.IsNullOrWhiteSpace(rollName) ? formula : rollName.Trim(),
                Formula = formula.Replace(" ", string.Empty),
                Advantage = advantage,
                Disadvantage = disadvantage
            };
            SendPacket(SyncEventType.RollRequest, request, $"[Soulstone] Roll requested from {targetName}: {request.RollName} ({request.Formula})");
            return true;
        }

        public bool RollForMember(string targetName, string formula, string rollName, bool advantage = false, bool disadvantage = false)
        {
            if (!IsSessionHost || string.IsNullOrWhiteSpace(targetName)) return false;
            var roll = DiceRoll.ParseDiceRollString(formula.Replace(" ", string.Empty), advantage, disadvantage);
            if (roll == null) return false;
            string label = string.IsNullOrWhiteSpace(rollName) ? formula : rollName.Trim();
            BroadcastDiceRoll(label, roll.RollResult, string.Join(", ", roll.IndividualRolls), echoText: $"[Soulstone] Rolled for {targetName}: {roll.RollResultString.TextValue}", characterName: targetName);
            return true;
        }

        public bool ExecuteRollRequest(string requestId)
        {
            if (!PendingRollRequests.TryRemove(requestId, out var request)) return false;
            var roll = DiceRoll.ParseDiceRollString(request.Formula, request.Advantage, request.Disadvantage);
            if (roll == null) return false;
            BroadcastDiceRoll(request.RollName, roll.RollResult, string.Join(", ", roll.IndividualRolls), echoText: $"[Soulstone] {request.RollName}: {roll.RollResultString.TextValue}");
            OnPartyRosterUpdated?.Invoke();
            return true;
        }

        public void DismissRollRequest(string requestId)
        {
            PendingRollRequests.TryRemove(requestId, out _);
            OnPartyRosterUpdated?.Invoke();
        }

        public void BroadcastPrivateStats()
        {
            if (!relayClient.IsConnected || IsSessionHost) return;
            var sheet = CharacterManager.Instance.CharacterSheet;
            if (sheet == null) return;
            var stats = new PrivateStatsPayload
            {
                CharacterName = GetLocalPlayerName(),
                TargetName = configuration?.SyncHostName ?? GetPartyLeaderName(),
                Level = sheet.CharacterLevel,
                ClassName = sheet.CharacterClass
            };
            if (sheet.CharacterAttributes != null)
            {
                foreach (var attribute in sheet.CharacterAttributes)
                    stats.Attributes[attribute.Key] = sheet.GetEffectiveAttributeValue(attribute.Key);
            }
            if (sheet.CharacterSkills != null)
            {
                foreach (var skill in sheet.CharacterSkills)
                    stats.Skills[skill.Key] = sheet.GetEffectiveSkillTotal(skill.Key, DiceSystemManager.Instance.CurrentDiceSystem);
            }
            if (sheet.CharacterAbilities != null)
            {
                foreach (var ability in sheet.CharacterAbilities)
                    stats.Abilities[ability.Key] = sheet.GetEffectiveAbilityModifier(ability.Key);
            }
            SendPacket(SyncEventType.PrivateStats, stats);
        }

        public void BroadcastInitiativeSync(int round, int turnNumber, string? activeId, List<InitiativeParticipant> participants)
        {
            if (!IsSessionHost) return;
            var payload = new InitiativeSyncPayload
            {
                Round = round,
                TurnNumber = turnNumber,
                ActiveParticipantId = activeId,
                Participants = participants != null ? new List<InitiativeParticipant>(participants) : new List<InitiativeParticipant>()
            };

            SendPacket(SyncEventType.InitiativeSync, payload);
        }

        public void BroadcastInitiativeTurn(int round, int turnNumber, string? activeId, string echoText = "")
        {
            if (!IsSessionHost) return;
            var payload = new InitiativeTurnPayload
            {
                Round = round,
                TurnNumber = turnNumber,
                ActiveParticipantId = activeId
            };

            SendPacket(SyncEventType.InitiativeTurnAdvance, payload, echoText);
        }

        public void BroadcastInitiativeReset(string echoText = "")
        {
            if (!IsSessionHost) return;
            SendPacket(SyncEventType.InitiativeReset, new object(), echoText);
        }

        public void BroadcastParticipantUpsert(InitiativeParticipant participant, string echoText = "")
        {
            if (participant == null || !IsSessionHost) return;
            SendPacket(SyncEventType.InitiativeAddOrUpdate, participant, echoText);
        }

        public void BroadcastParticipantRemove(string participantId, string echoText = "")
        {
            if (string.IsNullOrWhiteSpace(participantId) || !IsSessionHost) return;
            SendPacket(SyncEventType.InitiativeRemove, participantId, echoText);
        }

        public void BroadcastResourceUpdate()
        {
            var sheet = CharacterManager.Instance.CharacterSheet;
            if (sheet == null) return;

            var payload = new ResourceUpdatePayload
            {
                CharacterName = GetLocalPlayerName(),
                CurrentHp = sheet.characterHealthPoints,
                MaxHp = sheet.characterMaxHealthPoints > 0 ? sheet.characterMaxHealthPoints : 100,
                CurrentMana = sheet.characterManaPoints,
                MaxMana = sheet.characterMaxManaPoints > 0 ? sheet.characterMaxManaPoints : 100
            };

            if (sheet.characterResources != null)
            {
                foreach (var kv in sheet.characterResources)
                {
                    payload.CustomResources[kv.Key] = kv.Value.CurrentValue;
                    payload.CustomResourceMaxes[kv.Key] = kv.Value.MaxValue;
                }
            }

            SendPacket(SyncEventType.ResourceUpdate, payload);

            // Update local member
            string localName = GetLocalPlayerName();
            if (ConnectedPartyMembers.TryGetValue(localName, out var member))
            {
                member.ApplyResourceUpdate(payload);
                OnPartyMemberUpdated?.Invoke(member);
            }
        }

        public void BroadcastRuleset(DiceSystem system)
        {
            if (system == null || !IsSessionHost) return;

            try
            {
                string json = JsonSerializer.Serialize(system);
                var payload = new RulesetBroadcastPayload
                {
                    SenderName = GetLocalPlayerName(),
                    SystemName = system.systemName,
                    RulesetJson = json
                };

                SendPacket(SyncEventType.RulesetBroadcast, payload, $"[Soulstone] Party Leader shared ruleset: {system.systemName}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to broadcast ruleset");
            }
        }

        public void BroadcastBuffUpdate()
        {
            var sheet = CharacterManager.Instance.CharacterSheet;
            if (sheet == null) return;

            var payload = new BuffUpdatePayload
            {
                CharacterName = GetLocalPlayerName(),
                ActiveBuffs = sheet.activeBuffs != null ? new List<Buff>(sheet.activeBuffs) : new List<Buff>()
            };

            SendPacket(SyncEventType.BuffUpdate, payload);

            string localName = GetLocalPlayerName();
            if (ConnectedPartyMembers.TryGetValue(localName, out var member))
            {
                member.ApplyBuffUpdate(payload);
                OnPartyMemberUpdated?.Invoke(member);
            }
        }

        public void RequestRosterRefresh()
        {
            RefreshPartyList();
            SendPacket(SyncEventType.SyncRequest, new object());
            BroadcastPrivateStats();
        }
    }
}
