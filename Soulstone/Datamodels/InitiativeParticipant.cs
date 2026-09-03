using System;
using System.Collections.Generic;
using System.Linq;

namespace Soulstone.Datamodels
{
    public class InitiativeParticipant
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public int InitiativeValue { get; set; } = 0;
        public int BonusModifier { get; set; } = 0;
        public bool IsCurrentCharacter { get; set; } = false;
        public string Notes { get; set; } = string.Empty;
        public List<Buff> Buffs { get; set; } = new();

        public InitiativeParticipant() { }

        public InitiativeParticipant(string name, int initiativeValue, int bonusModifier = 0, bool isCurrentCharacter = false, string notes = "", List<Buff>? buffs = null)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            InitiativeValue = initiativeValue;
            BonusModifier = bonusModifier;
            IsCurrentCharacter = isCurrentCharacter;
            Notes = notes;
            Buffs = buffs != null ? new List<Buff>(buffs) : new List<Buff>();
        }

        public void AddBuff(Buff buff)
        {
            if (buff == null) return;
            Buffs ??= new List<Buff>();
            Buffs.Add(buff);
        }

        public bool RemoveBuff(string buffId)
        {
            if (Buffs == null) return false;
            int index = Buffs.FindIndex(b => b.Id == buffId);
            if (index < 0) return false;
            Buffs.RemoveAt(index);
            return true;
        }

        public int GetBuffStatBonus(string statName)
        {
            if (string.IsNullOrWhiteSpace(statName) || Buffs == null) return 0;
            int total = 0;
            foreach (var buff in Buffs)
            {
                total += buff.GetStatModifier(statName);
                if (!string.Equals(statName, "All", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(statName, "Global", StringComparison.OrdinalIgnoreCase))
                {
                    total += buff.GetStatModifier("All") + buff.GetStatModifier("Global");
                }
            }
            return total;
        }

        public Dictionary<string, int> GetAllBuffStatBonuses()
        {
            var bonuses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (Buffs == null) return bonuses;
            foreach (var buff in Buffs)
            {
                if (buff.StatModifiers == null) continue;
                foreach (var kv in buff.StatModifiers)
                {
                    if (bonuses.ContainsKey(kv.Key))
                        bonuses[kv.Key] += kv.Value;
                    else
                        bonuses[kv.Key] = kv.Value;
                }
            }
            return bonuses;
        }

        public List<Buff> TickBuffs(int turns = 1)
        {
            var expired = new List<Buff>();
            if (Buffs == null || Buffs.Count == 0) return expired;

            for (int i = Buffs.Count - 1; i >= 0; i--)
            {
                var buff = Buffs[i];
                if (buff.Tick(turns))
                {
                    expired.Add(buff);
                    Buffs.RemoveAt(i);
                }
            }
            return expired;
        }
    }
}
