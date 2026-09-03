using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Soulstone.Datamodels;
using Soulstone.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Soulstone.Managers
{
    internal class PartySyncManager : IDisposable
    {
        public static PartySyncManager Instance { get; } = new();

        public ConcurrentDictionary<string, PartyMemberSyncData> ConnectedPartyMembers { get; } = new(StringComparer.OrdinalIgnoreCase);

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

        private bool isInitialized = false;
        private DateTime lastPresenceBroadcast = DateTime.MinValue;

        public void Init()
        {
            if (isInitialized) return;
            isInitialized = true;

            try
            {
                if (Plugin.ChatGui != null)
                {
                    Plugin.ChatGui.ChatMessage += OnChatMessage;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to hook ChatMessage in PartySyncManager");
            }

            RefreshPartyList();
        }

        public void Dispose()
        {
            if (!isInitialized) return;
            isInitialized = false;

            try
            {
                if (Plugin.ChatGui != null)
                {
                    Plugin.ChatGui.ChatMessage -= OnChatMessage;
                }
            }
            catch { }

            ConnectedPartyMembers.Clear();
        }

        public void OnChatMessage(IHandleableChatMessage chatMessage)
        {
            if (chatMessage == null) return;

            if (chatMessage.LogKind != XivChatType.Party && chatMessage.LogKind != XivChatType.Echo)
                return;

            string rawText = chatMessage.Message?.TextValue ?? string.Empty;
            if (!PartySyncPacket.TryDecodePacket(rawText, out var packet, out var cleanText) || packet == null)
                return;

            HandleIncomingPacket(packet, chatMessage.Sender?.TextValue ?? string.Empty);
        }

        public void HandleIncomingPacket(PartySyncPacket packet, string senderDisplayName = "")
        {
            if (packet == null) return;

            string senderName = !string.IsNullOrWhiteSpace(packet.SenderName)
                ? packet.SenderName
                : (!string.IsNullOrWhiteSpace(senderDisplayName) ? senderDisplayName : "Unknown");

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
                            string localRuleset = DiceSystemManager.Instance.CurrentDiceSystem?.systemName ?? string.Empty;
                            var memberData = ConnectedPartyMembers.GetOrAdd(senderName, name => new PartyMemberSyncData
                            {
                                CharacterName = name,
                                WorldName = packet.SenderWorld
                            });

                            memberData.ApplyPresence(presence, localRuleset);
                            UpdateLeaderStatus(memberData);
                            OnPartyMemberUpdated?.Invoke(memberData);
                            OnPartyRosterUpdated?.Invoke();
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
                    }
                    break;

                case SyncEventType.DiceRoll:
                    try
                    {
                        var roll = JsonSerializer.Deserialize<DiceRollPayload>(packet.PayloadJson);
                        if (roll != null)
                        {
                            if (ConnectedPartyMembers.TryGetValue(senderName, out var member))
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

                        memberData.IsPartyLeader = string.Equals(name, leaderName, StringComparison.OrdinalIgnoreCase);
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
                localData.IsPartyLeader = string.Equals(localPlayerName, leaderName, StringComparison.OrdinalIgnoreCase);
                PopulateLocalPlayerVitals(localData);
            }

            // Clean up members no longer in party (except local player)
            var toRemove = ConnectedPartyMembers.Keys.Where(k => !activeNames.Contains(k)).ToList();
            foreach (var key in toRemove)
            {
                ConnectedPartyMembers.TryRemove(key, out _);
            }

            OnPartyRosterUpdated?.Invoke();
        }

        private void UpdateLeaderStatus(PartyMemberSyncData member)
        {
            string leaderName = GetPartyLeaderName();
            member.IsPartyLeader = string.Equals(member.CharacterName, leaderName, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsLocalPlayerPartyLeader()
        {
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

                string tag = PartySyncPacket.EncodePacket(packet);
                string fullText = string.IsNullOrWhiteSpace(humanReadableEcho)
                    ? tag
                    : $"{humanReadableEcho} {tag}";

                bool inParty = Plugin.PartyList != null && Plugin.PartyList.Length > 1;
                XivChatType targetType = inParty ? XivChatType.Party : XivChatType.Echo;

                Messages.SendMessage(new XivChatEntry
                {
                    Message = fullText,
                    Type = targetType
                });
            }
            catch (Exception ex)
            {
                Plugin.Log?.Debug(ex, $"Failed to send party packet for event {eventType}");
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

        public void BroadcastDiceRoll(string rollName, int total, string details, bool isCritSuccess = false, bool isCritFailure = false, string echoText = "")
        {
            var payload = new DiceRollPayload
            {
                CharacterName = GetLocalPlayerName(),
                RollName = rollName,
                Total = total,
                Details = details,
                IsCriticalSuccess = isCritSuccess,
                IsCriticalFailure = isCritFailure,
                RulesetName = DiceSystemManager.Instance.CurrentDiceSystem?.systemName ?? string.Empty
            };

            SendPacket(SyncEventType.DiceRoll, payload, echoText);
        }

        public void BroadcastInitiativeSync(int round, int turnNumber, string? activeId, List<InitiativeParticipant> participants)
        {
            var payload = new InitiativeSyncPayload
            {
                Round = round,
                TurnNumber = turnNumber,
                ActiveParticipantId = activeId,
                Participants = participants != null ? new List<InitiativeParticipant>(participants) : new List<InitiativeParticipant>()
            };

            SendPacket(SyncEventType.InitiativeAddOrUpdate, payload);
        }

        public void BroadcastInitiativeTurn(int round, int turnNumber, string? activeId, string echoText = "")
        {
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
            SendPacket(SyncEventType.InitiativeReset, new object(), echoText);
        }

        public void BroadcastParticipantUpsert(InitiativeParticipant participant, string echoText = "")
        {
            if (participant == null) return;
            SendPacket(SyncEventType.InitiativeAddOrUpdate, participant, echoText);
        }

        public void BroadcastParticipantRemove(string participantId, string echoText = "")
        {
            if (string.IsNullOrWhiteSpace(participantId)) return;
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
            if (system == null) return;

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
        }
    }
}
