using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Soulstone.Datamodels
{
    public class Buff
    {
        public string id = Guid.NewGuid().ToString();
        public string name = string.Empty;
        public string description = string.Empty;
        public int duration = 1;
        public int initialDuration = 1;
        public bool isDebuff = false;
        public Dictionary<string, int> statModifiers = new();

        public string Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string Description { get => description; set => description = value; }
        public int Duration { get => duration; set => duration = value; }
        public int InitialDuration { get => initialDuration; set => initialDuration = value; }
        public bool IsDebuff { get => isDebuff; set => isDebuff = value; }
        public Dictionary<string, int> StatModifiers { get => statModifiers; set => statModifiers = value; }

        public Buff()
        {
            id = Guid.NewGuid().ToString();
            statModifiers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public Buff(string name, int duration, string targetStat, int value, string description = "", bool isDebuff = false)
        {
            id = Guid.NewGuid().ToString();
            this.name = name;
            this.duration = duration;
            this.initialDuration = duration;
            this.description = description;
            this.isDebuff = isDebuff || value < 0;
            statModifiers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(targetStat))
            {
                statModifiers[targetStat] = value;
            }
        }

        public Buff(string name, int duration, Dictionary<string, int>? modifiers = null, string description = "", bool isDebuff = false)
        {
            id = Guid.NewGuid().ToString();
            this.name = name;
            this.duration = duration;
            this.initialDuration = duration;
            this.description = description;
            this.isDebuff = isDebuff || (modifiers != null && modifiers.Count > 0 && modifiers.Values.All(v => v < 0));
            this.statModifiers = modifiers != null
                ? new Dictionary<string, int>(modifiers, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public Buff Clone()
        {
            return new Buff
            {
                Id = Guid.NewGuid().ToString(),
                Name = this.Name,
                Description = this.Description,
                Duration = this.Duration,
                InitialDuration = this.InitialDuration,
                IsDebuff = this.IsDebuff,
                StatModifiers = new Dictionary<string, int>(this.StatModifiers, StringComparer.OrdinalIgnoreCase)
            };
        }

        public int GetStatModifier(string statName)
        {
            if (string.IsNullOrWhiteSpace(statName) || statModifiers == null) return 0;
            if (statModifiers.TryGetValue(statName, out int val))
            {
                return val;
            }
            return 0;
        }

        public void SetStatModifier(string statName, int value)
        {
            if (string.IsNullOrWhiteSpace(statName)) return;
            statModifiers ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            statModifiers[statName] = value;
            if (value < 0 && statModifiers.Values.All(v => v < 0))
            {
                isDebuff = true;
            }
        }

        public bool RemoveStatModifier(string statName)
        {
            if (string.IsNullOrWhiteSpace(statName) || statModifiers == null) return false;
            return statModifiers.Remove(statName);
        }

        public string GetFormattedModifiers()
        {
            if (statModifiers == null || statModifiers.Count == 0) return string.Empty;
            return string.Join(", ", statModifiers.Select(kv => $"{(kv.Value >= 0 ? "+" : "")}{kv.Value} {kv.Key}"));
        }

        public bool Tick(int turns = 1)
        {
            Duration -= turns;
            return Duration <= 0;
        }
    }
}
