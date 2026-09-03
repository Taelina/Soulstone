using Soulstone.Datamodels;
using Soulstone.Managers;
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
        public void PartySyncPacket_EncodeAndDecode_WorksCorrectly()
        {
            var packet = new PartySyncPacket
            {
                ProtocolVersion = 1,
                EventType = SyncEventType.Presence,
                SenderName = "Alphinaud Leveilleur",
                SenderWorld = "Balmung",
                PayloadJson = "{\"test\":123}"
            };

            string encoded = PartySyncPacket.EncodePacket(packet);
            Assert.StartsWith("[SS:v1:", encoded);
            Assert.EndsWith("]", encoded);

            string fullMessage = $"Hey everyone! {encoded}";
            bool success = PartySyncPacket.TryDecodePacket(fullMessage, out var decoded, out var cleanText);

            Assert.True(success);
            Assert.NotNull(decoded);
            Assert.Equal(1, decoded!.ProtocolVersion);
            Assert.Equal(SyncEventType.Presence, decoded.EventType);
            Assert.Equal("Alphinaud Leveilleur", decoded.SenderName);
            Assert.Equal("Balmung", decoded.SenderWorld);
            Assert.Equal("{\"test\":123}", decoded.PayloadJson);
            Assert.Equal("Hey everyone!", cleanText);
        }


        [Fact]
        public void PartySyncPacket_MalformedMessage_ReturnsFalseWithoutThrowing()
        {
            Assert.False(PartySyncPacket.TryDecodePacket(null!, out var packet1, out _));
            Assert.False(PartySyncPacket.TryDecodePacket("", out var packet2, out _));
            Assert.False(PartySyncPacket.TryDecodePacket("Just a normal party message", out var packet3, out _));
            Assert.False(PartySyncPacket.TryDecodePacket("[SS:v1:not-valid-base64-json!]", out var packet4, out _));
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
    }
}
