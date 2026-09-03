using System;
using System.Collections.Generic;

namespace Soulstone.Datamodels
{
    public class PartyMemberSyncData
    {
        public string CharacterName { get; set; } = string.Empty;
        public string WorldName { get; set; } = string.Empty;
        public bool HasSoulstone { get; set; } = false;
        public bool IsPartyLeader { get; set; } = false;
        public string JobName { get; set; } = string.Empty;
        public string ActiveRulesetName { get; set; } = string.Empty;
        public bool IsRulesetInSync { get; set; } = false;
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public int CurrentMana { get; set; }
        public int MaxMana { get; set; }
        public Dictionary<string, int> CustomResources { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> CustomResourceMaxes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<Buff> ActiveBuffs { get; set; } = new();
        public string LastRollSummary { get; set; } = string.Empty;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        public void ApplyPresence(PresencePayload payload, string? localRulesetName = null)
        {
            if (payload == null) return;

            HasSoulstone = true;
            LastSeen = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(payload.CharacterName))
                CharacterName = payload.CharacterName;
            if (!string.IsNullOrWhiteSpace(payload.WorldName))
                WorldName = payload.WorldName;
            ActiveRulesetName = payload.RulesetName ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(localRulesetName) && !string.IsNullOrWhiteSpace(ActiveRulesetName))
            {
                IsRulesetInSync = string.Equals(ActiveRulesetName, localRulesetName, StringComparison.OrdinalIgnoreCase);
            }

            CurrentHp = payload.CurrentHp;
            MaxHp = payload.MaxHp;
            CurrentMana = payload.CurrentMana;
            MaxMana = payload.MaxMana;

            if (payload.CustomResources != null)
            {
                CustomResources = new Dictionary<string, int>(payload.CustomResources, StringComparer.OrdinalIgnoreCase);
            }
            if (payload.CustomResourceMaxes != null)
            {
                CustomResourceMaxes = new Dictionary<string, int>(payload.CustomResourceMaxes, StringComparer.OrdinalIgnoreCase);
            }
            if (payload.ActiveBuffs != null)
            {
                ActiveBuffs = new List<Buff>(payload.ActiveBuffs);
            }
            if (!string.IsNullOrWhiteSpace(payload.LastRollSummary))
            {
                LastRollSummary = payload.LastRollSummary;
            }
        }

        public void ApplyResourceUpdate(ResourceUpdatePayload payload)
        {
            if (payload == null) return;

            HasSoulstone = true;
            LastSeen = DateTime.UtcNow;
            CurrentHp = payload.CurrentHp;
            MaxHp = payload.MaxHp;
            CurrentMana = payload.CurrentMana;
            MaxMana = payload.MaxMana;

            if (payload.CustomResources != null)
            {
                CustomResources = new Dictionary<string, int>(payload.CustomResources, StringComparer.OrdinalIgnoreCase);
            }
            if (payload.CustomResourceMaxes != null)
            {
                CustomResourceMaxes = new Dictionary<string, int>(payload.CustomResourceMaxes, StringComparer.OrdinalIgnoreCase);
            }
        }

        public void ApplyBuffUpdate(BuffUpdatePayload payload)
        {
            if (payload == null) return;

            HasSoulstone = true;
            LastSeen = DateTime.UtcNow;
            if (payload.ActiveBuffs != null)
            {
                ActiveBuffs = new List<Buff>(payload.ActiveBuffs);
            }
        }
    }
}
