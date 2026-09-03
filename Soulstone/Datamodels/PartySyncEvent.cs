using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soulstone.Datamodels
{
    public enum SyncEventType
    {
        Presence = 0,
        DiceRoll = 1,
        InitiativeAddOrUpdate = 2,
        InitiativeTurnAdvance = 3,
        InitiativeReset = 4,
        ResourceUpdate = 5,
        RulesetBroadcast = 6,
        BuffUpdate = 7,
        SyncRequest = 8,
        InitiativeRemove = 9
    }

    public class PartySyncPacket
    {
        public const string PacketPrefix = "[SS:v1:";
        public const string PacketSuffix = "]";

        [JsonPropertyName("v")]
        public int ProtocolVersion { get; set; } = 1;

        [JsonPropertyName("t")]
        public SyncEventType EventType { get; set; }

        [JsonPropertyName("s")]
        public string SenderName { get; set; } = string.Empty;

        [JsonPropertyName("w")]
        public string SenderWorld { get; set; } = string.Empty;

        [JsonPropertyName("p")]
        public string PayloadJson { get; set; } = string.Empty;

        public static string EncodePacket(PartySyncPacket packet)
        {
            try
            {
                string json = JsonSerializer.Serialize(packet);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                string base64 = Convert.ToBase64String(bytes);
                return $"{PacketPrefix}{base64}{PacketSuffix}";
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool TryDecodePacket(string message, out PartySyncPacket? packet, out string cleanText)
        {
            packet = null;
            cleanText = message ?? string.Empty;

            if (string.IsNullOrWhiteSpace(message)) return false;

            int prefixIndex = message.IndexOf(PacketPrefix, StringComparison.Ordinal);
            if (prefixIndex < 0) return false;

            int suffixIndex = message.IndexOf(PacketSuffix, prefixIndex + PacketPrefix.Length, StringComparison.Ordinal);
            if (suffixIndex < 0) return false;

            string base64Content = message.Substring(prefixIndex + PacketPrefix.Length, suffixIndex - (prefixIndex + PacketPrefix.Length));

            // Remove the packet tag from the message for clean display
            string before = message.Substring(0, prefixIndex);
            string after = message.Substring(suffixIndex + PacketSuffix.Length);
            cleanText = (before + after).Trim();

            try
            {
                byte[] bytes = Convert.FromBase64String(base64Content);
                string json = Encoding.UTF8.GetString(bytes);
                packet = JsonSerializer.Deserialize<PartySyncPacket>(json);
                return packet != null;
            }
            catch
            {
                return false;
            }
        }
    }

    public class PresencePayload
    {
        public string CharacterName { get; set; } = string.Empty;
        public string WorldName { get; set; } = string.Empty;
        public string RulesetName { get; set; } = string.Empty;
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public int CurrentMana { get; set; }
        public int MaxMana { get; set; }
        public Dictionary<string, int> CustomResources { get; set; } = new();
        public Dictionary<string, int> CustomResourceMaxes { get; set; } = new();
        public List<Buff> ActiveBuffs { get; set; } = new();
        public string LastRollSummary { get; set; } = string.Empty;
    }

    public class DiceRollPayload
    {
        public string CharacterName { get; set; } = string.Empty;
        public string RollName { get; set; } = string.Empty;
        public int Total { get; set; }
        public string Details { get; set; } = string.Empty;
        public bool IsCriticalSuccess { get; set; }
        public bool IsCriticalFailure { get; set; }
        public string RulesetName { get; set; } = string.Empty;
    }

    public class InitiativeSyncPayload
    {
        public int Round { get; set; } = 1;
        public int TurnNumber { get; set; } = 1;
        public string? ActiveParticipantId { get; set; }
        public List<InitiativeParticipant> Participants { get; set; } = new();
    }

    public class InitiativeTurnPayload
    {
        public int Round { get; set; } = 1;
        public int TurnNumber { get; set; } = 1;
        public string? ActiveParticipantId { get; set; }
    }

    public class ResourceUpdatePayload
    {
        public string CharacterName { get; set; } = string.Empty;
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public int CurrentMana { get; set; }
        public int MaxMana { get; set; }
        public Dictionary<string, int> CustomResources { get; set; } = new();
        public Dictionary<string, int> CustomResourceMaxes { get; set; } = new();
    }

    public class RulesetBroadcastPayload
    {
        public string SenderName { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public string RulesetJson { get; set; } = string.Empty;
    }

    public class BuffUpdatePayload
    {
        public string CharacterName { get; set; } = string.Empty;
        public List<Buff> ActiveBuffs { get; set; } = new();
    }
}
