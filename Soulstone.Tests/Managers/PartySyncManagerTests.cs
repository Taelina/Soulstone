using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Sync;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Soulstone.Tests.Managers
{
    [Collection("NonParallelCollection")]
    public class PartySyncManagerTests
    {
        [Fact]
        public void RelayInvite_EncodeAndDecode_PreservesConnectionAndCryptoData()
        {
            var keys = RelayCrypto.CreateHostKeyPair();
            var invite = new RelayInvite
            {
                ServerUrl = "https://relay.soulstone.example",
                SessionId = "session-id",
                MemberToken = "member-token",
                RoomKey = RelayCrypto.CreateRoomKey(),
                HostPublicKey = keys.PublicKey,
                HostName = "Dungeon Master",
                HostWorld = "Balmung"
            };

            string code = RelayCrypto.EncodeInvite(invite);

            Assert.True(RelayCrypto.TryDecodeInvite(code, out var decoded));
            Assert.NotNull(decoded);
            Assert.Equal(invite.ServerUrl, decoded!.ServerUrl);
            Assert.Equal(invite.SessionId, decoded.SessionId);
            Assert.Equal(invite.MemberToken, decoded.MemberToken);
            Assert.Equal(invite.RoomKey, decoded.RoomKey);
            Assert.Equal(invite.HostPublicKey, decoded.HostPublicKey);
            Assert.Equal(invite.HostName, decoded.HostName);
        }

        [Fact]
        public void ShortInviteLink_EncryptsAndRecoversInviteWithoutExposingSecrets()
        {
            var keys = RelayCrypto.CreateHostKeyPair();
            var invite = new RelayInvite
            {
                ServerUrl = "https://relay.soulstone.example",
                SessionId = "session-id",
                MemberToken = "member-token",
                RoomKey = RelayCrypto.CreateRoomKey(),
                HostPublicKey = keys.PublicKey,
                HostName = "Dungeon Master",
                HostWorld = "Balmung"
            };

            string code = RelayCrypto.CreateShortInviteCode();
            string link = RelayCrypto.CreateShortInviteLink(invite.ServerUrl, code);
            string payload = RelayCrypto.EncryptInvite(invite, code);

            Assert.True(code.Length <= 20);
            Assert.Equal($"https://relay.soulstone.example/join/{code}", link);
            Assert.DoesNotContain(invite.MemberToken, payload);
            Assert.DoesNotContain(invite.RoomKey, payload);
            Assert.True(RelayCrypto.TryParseShortInviteLink(link, out var serverUrl, out var parsedCode));
            Assert.Equal(invite.ServerUrl, serverUrl);
            Assert.Equal(code, parsedCode);
            Assert.True(RelayCrypto.TryDecryptInvite(payload, parsedCode, out var decoded));
            Assert.Equal(invite.SessionId, decoded!.SessionId);
            Assert.Equal(invite.MemberToken, decoded.MemberToken);
            Assert.Equal(invite.RoomKey, decoded.RoomKey);
        }

        [Fact]
        public void ShortInvite_WithWrongCodeOrModifiedPayload_IsRejected()
        {
            var invite = new RelayInvite
            {
                ServerUrl = "https://relay.soulstone.example",
                SessionId = "session-id",
                MemberToken = "member-token",
                RoomKey = RelayCrypto.CreateRoomKey(),
                HostPublicKey = RelayCrypto.CreateHostKeyPair().PublicKey,
                HostName = "Dungeon Master"
            };
            string code = RelayCrypto.CreateShortInviteCode();
            string payload = RelayCrypto.EncryptInvite(invite, code);

            Assert.False(RelayCrypto.TryDecryptInvite(payload, RelayCrypto.CreateShortInviteCode(), out _));
            Assert.False(RelayCrypto.TryDecryptInvite(payload + "A", code, out _));
        }

        [Fact]
        public void RelayGroupMessage_EncryptDecryptAndTamperDetection_Work()
        {
            string roomKey = RelayCrypto.CreateRoomKey();
            var packet = new PartySyncPacket
            {
                EventType = SyncEventType.Presence,
                SenderName = "Alphinaud Leveilleur",
                SenderWorld = "Balmung",
                PayloadJson = "{\"hp\":100}"
            };

            RelayEnvelope envelope = RelayCrypto.EncryptGroupMessage(packet, roomKey);

            Assert.True(RelayCrypto.TryDecryptMessage(envelope, roomKey, null, out var decoded));
            Assert.Equal(packet.PayloadJson, decoded!.PayloadJson);
            envelope.Ciphertext = Convert.ToBase64String(new byte[] { 1, 2, 3 });
            Assert.False(RelayCrypto.TryDecryptMessage(envelope, roomKey, null, out _));
        }

        [Fact]
        public void RelayPrivateStats_RequireHostPrivateKey()
        {
            var keys = RelayCrypto.CreateHostKeyPair();
            var packet = new PartySyncPacket
            {
                EventType = SyncEventType.PrivateStats,
                SenderName = "Alisaie Leveilleur",
                PayloadJson = "{\"Strength\":12}"
            };

            RelayEnvelope envelope = RelayCrypto.EncryptPrivateMessage(packet, keys.PublicKey);

            Assert.Equal("host", envelope.Destination);
            Assert.False(RelayCrypto.TryDecryptMessage(envelope, RelayCrypto.CreateRoomKey(), null, out _));
            Assert.True(RelayCrypto.TryDecryptMessage(envelope, RelayCrypto.CreateRoomKey(), keys.PrivateKey, out var decoded));
            Assert.Equal(packet.PayloadJson, decoded!.PayloadJson);
        }

        [Fact]
        public void RelayHostSignature_DetectsModifiedDmCommand()
        {
            var keys = RelayCrypto.CreateHostKeyPair();
            var envelope = RelayCrypto.EncryptGroupMessage(new PartySyncPacket
            {
                EventType = SyncEventType.RollRequest,
                SenderName = "Dungeon Master",
                PayloadJson = "{}"
            }, RelayCrypto.CreateRoomKey());

            RelayCrypto.SignEnvelope(envelope, keys.PrivateKey);

            Assert.True(RelayCrypto.VerifyHostSignature(envelope, keys.PublicKey));
            envelope.SenderName = "Impostor";
            Assert.False(RelayCrypto.VerifyHostSignature(envelope, keys.PublicKey));
        }

        [Fact]
        public void HandleIncomingPacket_InitiativeSync_RaisesFullSyncEvent()
        {
            var syncMgr = PartySyncManager.Instance;
            var participant = new InitiativeParticipant("Lyse", 20, 3);
            var payload = new InitiativeSyncPayload
            {
                Round = 2,
                TurnNumber = 4,
                ActiveParticipantId = participant.Id,
                Participants = new List<InitiativeParticipant> { participant }
            };
            var packet = new PartySyncPacket
            {
                EventType = SyncEventType.InitiativeSync,
                PayloadJson = JsonSerializer.Serialize(payload)
            };
            InitiativeSyncPayload? receivedPayload = null;
            void HandleSync(InitiativeSyncPayload received) => receivedPayload = received;
            syncMgr.OnInitiativeSyncReceived += HandleSync;

            try
            {
                syncMgr.HandleIncomingPacket(packet, "Dungeon Master");
            }
            finally
            {
                syncMgr.OnInitiativeSyncReceived -= HandleSync;
            }

            Assert.NotNull(receivedPayload);
            Assert.Equal(2, receivedPayload!.Round);
            Assert.Equal(4, receivedPayload.TurnNumber);
            Assert.Equal(participant.Id, receivedPayload.ActiveParticipantId);
            Assert.Single(receivedPayload.Participants);
        }

        [Fact]
        public void HandleIncomingPacket_Presence_UpdatesConnectedPartyMember()
        {
            var syncMgr = PartySyncManager.Instance;
            syncMgr.ConnectedPartyMembers.Clear();

            var presence = new PresencePayload
            {
                CharacterName = "Alisaie Leveilleur",
                WorldName = "Balmung",
                RulesetName = "Custom D20",
                CurrentHp = 80,
                MaxHp = 100,
                CurrentMana = 50,
                MaxMana = 100,
                CustomResources = new Dictionary<string, int> { { "Resolve", 4 } },
                CustomResourceMaxes = new Dictionary<string, int> { { "Resolve", 5 } },
                ActiveBuffs = new List<Buff> { new Buff("Haste", 3, new Dictionary<string, int> { { "Agility", 2 } }) },
                LastRollSummary = "Athletics: 18"
            };

            var packet = new PartySyncPacket
            {
                ProtocolVersion = 1,
                EventType = SyncEventType.Presence,
                SenderName = "Alisaie Leveilleur",
                SenderWorld = "Balmung",
                PayloadJson = JsonSerializer.Serialize(presence)
            };

            bool eventFired = false;
            PartyMemberSyncData? updatedMember = null;
            syncMgr.OnPartyMemberUpdated += member =>
            {
                if (member.CharacterName == "Alisaie Leveilleur")
                {
                    eventFired = true;
                    updatedMember = member;
                }
            };

            syncMgr.HandleIncomingPacket(packet, "Alisaie Leveilleur");

            Assert.True(eventFired);
            Assert.NotNull(updatedMember);
            Assert.True(updatedMember!.HasSoulstone);
            Assert.Equal(80, updatedMember.CurrentHp);
            Assert.Equal(100, updatedMember.MaxHp);
            Assert.Equal(50, updatedMember.CurrentMana);
            Assert.Equal(100, updatedMember.MaxMana);
            Assert.Equal("Custom D20", updatedMember.ActiveRulesetName);
            Assert.Equal(4, updatedMember.CustomResources["Resolve"]);
            Assert.Equal(5, updatedMember.CustomResourceMaxes["Resolve"]);
            Assert.Single(updatedMember.ActiveBuffs);
            Assert.Equal("Haste", updatedMember.ActiveBuffs[0].Name);
            Assert.Equal("Athletics: 18", updatedMember.LastRollSummary);
        }

        [Fact]
        public void HandleIncomingPacket_ResourceUpdate_UpdatesVitals()
        {
            var syncMgr = PartySyncManager.Instance;
            syncMgr.ConnectedPartyMembers.Clear();

            var memberData = new PartyMemberSyncData
            {
                CharacterName = "Urianger Augurelt",
                HasSoulstone = true,
                CurrentHp = 100,
                MaxHp = 100
            };
            syncMgr.ConnectedPartyMembers["Urianger Augurelt"] = memberData;

            var resUpdate = new ResourceUpdatePayload
            {
                CharacterName = "Urianger Augurelt",
                CurrentHp = 65,
                MaxHp = 100,
                CurrentMana = 20,
                MaxMana = 100,
                CustomResources = new Dictionary<string, int> { { "Cards", 2 } },
                CustomResourceMaxes = new Dictionary<string, int> { { "Cards", 3 } }
            };

            var packet = new PartySyncPacket
            {
                ProtocolVersion = 1,
                EventType = SyncEventType.ResourceUpdate,
                SenderName = "Urianger Augurelt",
                PayloadJson = JsonSerializer.Serialize(resUpdate)
            };

            syncMgr.HandleIncomingPacket(packet, "Urianger Augurelt");

            Assert.Equal(65, memberData.CurrentHp);
            Assert.Equal(20, memberData.CurrentMana);
            Assert.Equal(2, memberData.CustomResources["Cards"]);
        }

        [Fact]
        public void HandleIncomingPacket_DiceRoll_FiresEventAndSetsLastRoll()
        {
            var syncMgr = PartySyncManager.Instance;
            syncMgr.ConnectedPartyMembers.Clear();

            var memberData = new PartyMemberSyncData
            {
                CharacterName = "Thancred Waters",
                HasSoulstone = true
            };
            syncMgr.ConnectedPartyMembers["Thancred Waters"] = memberData;

            var diceRoll = new DiceRollPayload
            {
                CharacterName = "Thancred Waters",
                RollName = "Stealth Check",
                Total = 23,
                Details = "1d20+7 = 23",
                IsCriticalSuccess = false,
                IsCriticalFailure = false
            };

            var packet = new PartySyncPacket
            {
                ProtocolVersion = 1,
                EventType = SyncEventType.DiceRoll,
                SenderName = "Thancred Waters",
                PayloadJson = JsonSerializer.Serialize(diceRoll)
            };

            DiceRollPayload? receivedRoll = null;
            syncMgr.OnRemoteDiceRolled += roll => receivedRoll = roll;

            syncMgr.HandleIncomingPacket(packet, "Thancred Waters");

            Assert.NotNull(receivedRoll);
            Assert.Equal("Stealth Check", receivedRoll!.RollName);
            Assert.Equal(23, receivedRoll.Total);
            Assert.Contains("Stealth Check: 23", memberData.LastRollSummary);
        }

        [Fact]
        public void HandleIncomingPacket_RulesetBroadcast_FiresRulesetOfferedEvent()
        {
            var syncMgr = PartySyncManager.Instance;
            var rulesetPayload = new RulesetBroadcastPayload
            {
                SenderName = "Y'shtola Rhul",
                SystemName = "Ancient Magic D20",
                RulesetJson = "{\"systemName\":\"Ancient Magic D20\"}"
            };

            var packet = new PartySyncPacket
            {
                ProtocolVersion = 1,
                EventType = SyncEventType.RulesetBroadcast,
                SenderName = "Y'shtola Rhul",
                PayloadJson = JsonSerializer.Serialize(rulesetPayload)
            };

            RulesetBroadcastPayload? receivedPayload = null;
            syncMgr.OnRulesetOffered += payload => receivedPayload = payload;

            syncMgr.HandleIncomingPacket(packet, "Y'shtola Rhul");

            Assert.NotNull(receivedPayload);
            Assert.Equal("Ancient Magic D20", receivedPayload!.SystemName);
            Assert.Equal("Y'shtola Rhul", receivedPayload.SenderName);
        }

        [Fact]
        public void HandleIncomingPacket_PrivateStats_PopulatesMemberStatsForDM()
        {
            var syncMgr = PartySyncManager.Instance;
            syncMgr.ConnectedPartyMembers.Clear();

            var cfg = new Soulstone.Configuration
            {
                SyncHostToken = "host-secret-token",
                SyncHostName = "Dungeon Master"
            };
            syncMgr.Init(cfg);

            var statsPayload = new PrivateStatsPayload
            {
                CharacterName = "Estinien Varlineau",
                TargetName = "Dungeon Master",
                Level = 90,
                ClassName = "Dragoon",
                Attributes = new Dictionary<string, int> { { "Strength", 24 }, { "Dexterity", 18 } },
                Skills = new Dictionary<string, int> { { "Jump", 15 } },
                Abilities = new Dictionary<string, int> { { "Stargazer", 5 } }
            };

            var packet = new PartySyncPacket
            {
                ProtocolVersion = 1,
                EventType = SyncEventType.PrivateStats,
                SenderName = "Estinien Varlineau",
                SenderWorld = "Balmung",
                PayloadJson = JsonSerializer.Serialize(statsPayload)
            };

            PrivateStatsPayload? receivedStats = null;
            syncMgr.OnPrivateStatsUpdated += stats => receivedStats = stats;

            syncMgr.HandleIncomingPacket(packet, "Estinien Varlineau");

            Assert.NotNull(receivedStats);
            Assert.Equal("Estinien Varlineau", receivedStats!.CharacterName);
            Assert.True(syncMgr.ConnectedPartyMembers.TryGetValue("Estinien Varlineau", out var member));
            Assert.NotNull(member);
            Assert.True(member!.HasPrivateStats);
            Assert.Equal(90, member.Level);
            Assert.Equal("Dragoon", member.ClassName);
            Assert.Equal(24, member.Attributes["Strength"]);
            Assert.Equal(15, member.Skills["Jump"]);
            Assert.Equal(5, member.Abilities["Stargazer"]);
        }

        [Fact]
        public void HandleIncomingPacket_RollRequest_QueuesInPendingRollRequests()
        {
            var syncMgr = PartySyncManager.Instance;
            syncMgr.PendingRollRequests.Clear();

            var request = new RollRequestPayload
            {
                RequestId = "req-12345",
                RequestedBy = "Dungeon Master",
                TargetName = syncMgr.GetLocalPlayerName(),
                RollName = "Perception Check",
                Formula = "1d20+3"
            };

            var packet = new PartySyncPacket
            {
                ProtocolVersion = 1,
                EventType = SyncEventType.RollRequest,
                SenderName = "Dungeon Master",
                PayloadJson = JsonSerializer.Serialize(request)
            };

            syncMgr.HandleIncomingPacket(packet, "Dungeon Master");

            Assert.True(syncMgr.PendingRollRequests.TryGetValue("req-12345", out var queued));
            Assert.NotNull(queued);
            Assert.Equal("Perception Check", queued!.RollName);
            Assert.Equal("1d20+3", queued.Formula);

            // Execute request
            bool executed = syncMgr.ExecuteRollRequest("req-12345");
            Assert.True(executed);
            Assert.False(syncMgr.PendingRollRequests.ContainsKey("req-12345"));
        }

        [Fact]
        public void ShortInviteLink_SupportsRemotePlainHttpUrlsWithoutSsl()
        {
            var keys = RelayCrypto.CreateHostKeyPair();
            var invite = new RelayInvite
            {
                ServerUrl = "http://192.168.1.50:5077",
                SessionId = "session-id",
                MemberToken = "member-token",
                RoomKey = RelayCrypto.CreateRoomKey(),
                HostPublicKey = keys.PublicKey,
                HostName = "Dungeon Master",
                HostWorld = "Balmung"
            };

            string code = RelayCrypto.CreateShortInviteCode();
            string link = RelayCrypto.CreateShortInviteLink(invite.ServerUrl, code);
            string payload = RelayCrypto.EncryptInvite(invite, code);

            Assert.StartsWith("http://192.168.1.50:5077/join/", link);
            Assert.True(RelayCrypto.TryParseShortInviteLink(link, out var serverUrl, out var parsedCode));
            Assert.Equal("http://192.168.1.50:5077", serverUrl);
            Assert.Equal(code, parsedCode);
            Assert.True(RelayCrypto.TryDecryptInvite(payload, parsedCode, out var decoded));
            Assert.Equal(invite.ServerUrl, decoded!.ServerUrl);
        }

        [Fact]
        public void CharacterSheet_ApplyRulesetTemplate_PopulatesAttributesSkillsAndResources()
        {
            var sheet = new CharacterSheet { CharacterFullName = "Alphinaud Leveilleur" };
            var system = new DiceSystem { SystemName = "Custom Tactical D20" };
            system.SystemAttributes["Strength"] = new Soulstone.Datamodels.Attribute("Strength", 14);
            system.SystemAttributes["Intelligence"] = new Soulstone.Datamodels.Attribute("Intelligence", 18);
            system.SystemSkills["Arcana"] = new Skill { skillName = "Arcana", linkedAttribute = "Intelligence", skillModifier = 5 };
            system.SystemAbilities["Aetheric Ray"] = new Ability { abilityName = "Aetheric Ray", linkedAttribute = "Intelligence", abilityModifier = 3 };
            system.AddResource(new ResourceDefinition("Aether", defaultMax: 100, defaultCurrent: 50, colorHex: "#3498db", description: "Aether charge"));
            system.CustomEquipmentSlots.Add("Grimoire");
            system.CustomAugmentationSlots.Add("Neural Link");

            sheet.ApplyRulesetTemplate(system);

            Assert.Equal("Custom Tactical D20", sheet.LinkedDiceSystem);
            Assert.True(sheet.characterAttributes.ContainsKey("Strength"));
            Assert.Equal(14, sheet.characterAttributes["Strength"].Value);
            Assert.True(sheet.characterAttributes.ContainsKey("Intelligence"));
            Assert.Equal(18, sheet.characterAttributes["Intelligence"].Value);
            Assert.True(sheet.characterSkills.ContainsKey("Arcana"));
            Assert.Equal("Intelligence", sheet.characterSkills["Arcana"].linkedAttribute);
            Assert.Equal(5, sheet.characterSkills["Arcana"].skillModifier);
            Assert.True(sheet.characterAbilities.ContainsKey("Aetheric Ray"));
            Assert.Equal(3, sheet.characterAbilities["Aetheric Ray"].abilityModifier);
            Assert.True(sheet.characterResources.ContainsKey("Aether"));
            Assert.Equal(100, sheet.characterResources["Aether"].MaxValue);
            Assert.Equal(50, sheet.characterResources["Aether"].CurrentValue);
            Assert.True(sheet.equippedGear.ContainsKey("Grimoire"));
            Assert.True(sheet.equippedAugmentations.ContainsKey("Neural Link"));
        }

        [Fact]
        public void DiceSystem_CaptureTemplateFromSheet_CapturesAllSheetElements()
        {
            var sheet = new CharacterSheet { CharacterFullName = "Alisaie Leveilleur" };
            sheet.characterAttributes["Dexterity"] = new Soulstone.Datamodels.Attribute("Dexterity", 16);
            sheet.characterSkills["Fencing"] = new Skill { skillName = "Fencing", linkedAttribute = "Dexterity", skillModifier = 4 };
            sheet.characterAbilities["Red Doublet"] = new Ability { abilityName = "Red Doublet", linkedAttribute = "Dexterity", abilityModifier = 2 };
            sheet.characterResources["Mana"] = new CharacterResource("Mana", 80, 80, formula: "10 + 2 * Dexterity");
            sheet.equippedGear["Rapier"] = "Mythrite Rapier";
            sheet.equippedAugmentations["Aether Accelerator"] = "Model 1";

            var system = new DiceSystem { SystemName = "Red Mage System" };
            system.CaptureTemplateFromSheet(sheet);

            Assert.Equal("Red Mage System", sheet.LinkedDiceSystem);
            Assert.True(system.SystemAttributes.ContainsKey("Dexterity"));
            Assert.Equal(16, system.SystemAttributes["Dexterity"].Value);
            Assert.True(system.SystemSkills.ContainsKey("Fencing"));
            Assert.Equal(4, system.SystemSkills["Fencing"].skillModifier);
            Assert.True(system.SystemAbilities.ContainsKey("Red Doublet"));
            Assert.Contains(system.SystemResources, r => r.Name == "Mana" && r.Formula == "10 + 2 * Dexterity");
            Assert.Contains("Rapier", system.CustomEquipmentSlots);
            Assert.Contains("Aether Accelerator", system.CustomAugmentationSlots);
        }

        [Fact]
        public void DiceSystemManager_SwitchDiceSystem_SavesPreviousSheetAndAppliesNewTemplate()
        {
            var dsm = DiceSystemManager.Instance;
            var initialSys = new DiceSystem { SystemName = "InitialSystem" };
            dsm.CurrentDiceSystem = initialSys;

            var sheet = new CharacterSheet { CharacterFullName = "Estinien Varlineau" };
            sheet.characterAttributes["Strength"] = new Soulstone.Datamodels.Attribute("Strength", 18);
            CharacterManager.Instance.CharacterSheet = sheet;

            var newSys = new DiceSystem { SystemName = "Dragoon Tactical" };
            newSys.SystemAttributes["Agility"] = new Soulstone.Datamodels.Attribute("Agility", 15);
            newSys.SystemSkills["Jump"] = new Skill { skillName = "Jump", linkedAttribute = "Agility", skillModifier = 6 };

            dsm.SwitchDiceSystem(newSys);

            Assert.Equal("Dragoon Tactical", dsm.CurrentDiceSystem?.SystemName);
            Assert.Equal("Dragoon Tactical", sheet.LinkedDiceSystem);
            Assert.True(sheet.characterAttributes.ContainsKey("Strength"));
            Assert.True(sheet.characterAttributes.ContainsKey("Agility"));
            Assert.True(sheet.characterSkills.ContainsKey("Jump"));
        }

        [Fact]
        public void RulesetOffered_SkippedIfMatchingActiveRulesetByName()
        {
            var dsm = DiceSystemManager.Instance;
            dsm.CurrentDiceSystem = new DiceSystem { SystemName = "Pathfinder2e" };

            var offeredSys = new DiceSystem { SystemName = "Pathfinder2e", SuccessThreshold = 15 };
            var payload = new RulesetBroadcastPayload
            {
                SenderName = "DM",
                SystemName = "Pathfinder2e",
                RulesetJson = JsonSerializer.Serialize(offeredSys)
            };

            dsm.OnRulesetOfferedFromParty(payload);

            Assert.False(dsm.IsSessionRulesetActive);
            Assert.Equal(0, dsm.CurrentDiceSystem.SuccessThreshold);
        }

        [Fact]
        public void RulesetOffered_AdoptsAndAppliesTemplateWhenNameDiffers()
        {
            var dsm = DiceSystemManager.Instance;
            dsm.CurrentDiceSystem = new DiceSystem { SystemName = "Standard_Dice_System" };

            var offeredSys = new DiceSystem { SystemName = "Cyberpunk RED", SuccessThreshold = 12 };
            var attr = new Dictionary<string, Soulstone.Datamodels.Attribute>
            {
                { "Reflexes", new Soulstone.Datamodels.Attribute("Reflexes", 8) }
            };
            var skills = new Dictionary<string, Skill>
            {
                { "Handgun", new Skill { skillName = "Handgun", linkedAttribute = "Reflexes", skillModifier = 4 } }
            };

            var payload = new RulesetBroadcastPayload
            {
                SenderName = "DM",
                SystemName = "Cyberpunk RED",
                RulesetJson = JsonSerializer.Serialize(offeredSys),
                Attributes = attr,
                Skills = skills
            };

            dsm.OnRulesetOfferedFromParty(payload);

            Assert.True(dsm.IsSessionRulesetActive);
            Assert.NotNull(dsm.CurrentDiceSystem);
            Assert.Equal("Cyberpunk RED", dsm.CurrentDiceSystem.SystemName);
            Assert.True(dsm.CurrentDiceSystem.SystemAttributes.ContainsKey("Reflexes"));
            Assert.True(dsm.CurrentDiceSystem.SystemSkills.ContainsKey("Handgun"));

            dsm.RevertToLocalRuleset();
        }

        [Fact]
        public void HandleIncomingPacket_DiceRoll_TriggersRemoteDiceRolledAndUpdatesMemberSummary()
        {
            var syncMgr = PartySyncManager.Instance;
            syncMgr.ConnectedPartyMembers.Clear();

            var member = syncMgr.ConnectedPartyMembers.GetOrAdd("Urianger Augurelt", n => new PartyMemberSyncData { CharacterName = n });

            var rollPayload = new DiceRollPayload
            {
                CharacterName = "Urianger Augurelt",
                RolledBy = "Urianger Augurelt",
                RollName = "Astrology",
                Total = 19,
                Details = "1d20+4 -> [15] + 4",
                EchoMessage = "[Soulstone] Urianger Augurelt rolled Astrology: 19 (1d20+4 -> [15] + 4)"
            };

            var packet = new PartySyncPacket
            {
                ProtocolVersion = 1,
                EventType = SyncEventType.DiceRoll,
                SenderName = "Urianger Augurelt",
                PayloadJson = JsonSerializer.Serialize(rollPayload),
                EchoMessage = rollPayload.EchoMessage
            };

            DiceRollPayload? observedRoll = null;
            syncMgr.OnRemoteDiceRolled += r => observedRoll = r;

            syncMgr.HandleIncomingPacket(packet, "Urianger Augurelt");

            Assert.NotNull(observedRoll);
            Assert.Equal("Astrology", observedRoll!.RollName);
            Assert.Equal(19, observedRoll.Total);
            Assert.Equal("Astrology: 19 (1d20+4 -> [15] + 4)", member.LastRollSummary);
        }
    }
}
