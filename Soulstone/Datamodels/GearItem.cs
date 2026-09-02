using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Soulstone.Datamodels
{
    public class GearItem : Item
    {
        public static readonly string[] StandardSlots = new[]
        {
            "MainHand",
            "OffHand",
            "Head",
            "Body",
            "Hands",
            "Legs",
            "Feet",
            "Earrings",
            "Neck",
            "Wrists",
            "Ring1",
            "Ring2"
        };

        public static readonly string[] StandardAugmentationSlots = new[]
        {
            "Neural",
            "Optics",
            "Cranial",
            "Torso",
            "Arms",
            "Legs",
            "Subdermal",
            "Internal"
        };

        public string slot = "Head";
        public bool isAugmentation = false;
        public Dictionary<string, int> statModifiers = new();
        public int durability = 100;
        public int maxDurability = 100;

        public string Slot { get => slot; set => slot = value; }
        public bool IsAugmentation { get => isAugmentation; set => isAugmentation = value; }
        public Dictionary<string, int> StatModifiers { get => statModifiers; set => statModifiers = value; }
        public int Durability { get => durability; set => durability = value; }
        public int MaxDurability { get => maxDurability; set => maxDurability = value; }

        public GearItem() : base()
        {
            itemType = "Equipment";
            maxStack = 1;
            statModifiers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public GearItem(
            string name,
            string slot = "Head",
            string description = "",
            string rarity = "Common",
            Dictionary<string, int>? modifiers = null,
            string effect = "",
            float weight = 1.0f,
            string imageUrl = "",
            bool isAugmentation = false)
            : base(name, description, effect, isAugmentation ? "Augmentation" : "Equipment", 1, imageUrl, false, "")
        {
            this.slot = string.IsNullOrWhiteSpace(slot) ? (isAugmentation ? "Neural" : "Head") : slot;
            this.isAugmentation = isAugmentation;
            this.rarity = string.IsNullOrWhiteSpace(rarity) ? "Common" : rarity;
            this.weight = weight;
            this.maxStack = 1;
            this.statModifiers = modifiers != null
                ? new Dictionary<string, int>(modifiers, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public override Item Clone()
        {
            var clone = new GearItem
            {
                Id = Guid.NewGuid().ToString(),
                Name = this.Name,
                Description = this.Description,
                Effect = this.Effect,
                ItemType = this.ItemType,
                Quantity = this.Quantity,
                MaxStack = this.MaxStack,
                ImageUrl = this.ImageUrl,
                Weight = this.Weight,
                Rarity = this.Rarity,
                IsUsable = this.IsUsable,
                UseFormula = this.UseFormula,
                Slot = this.Slot,
                IsAugmentation = this.IsAugmentation,
                Durability = this.Durability,
                MaxDurability = this.MaxDurability,
                StatModifiers = new Dictionary<string, int>(this.StatModifiers, StringComparer.OrdinalIgnoreCase),
                CustomProperties = new Dictionary<string, string>(this.CustomProperties)
            };
            return clone;
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
    }
}
