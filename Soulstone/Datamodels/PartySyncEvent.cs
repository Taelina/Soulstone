using System;
using System.Collections.Generic;
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
        InitiativeRemove = 9,
        RollRequest = 10,
        PrivateStats = 11,
        InitiativeSync = 12
    }

    public class PartySyncPacket
    {
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
        public string RolledBy { get; set; } = string.Empty;
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

    public class RollRequestPayload
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
        public string RequestedBy { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string RollName { get; set; } = string.Empty;
        public string Formula { get; set; } = "1d20";
        public bool Advantage { get; set; }
        public bool Disadvantage { get; set; }
    }

    public class PrivateStatsPayload
    {
        public string CharacterName { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public int Level { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public Dictionary<string, int> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> Skills { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> Abilities { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
