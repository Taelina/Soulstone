using System;
using System.Collections.Generic;
using System.Linq;
using Soulstone.Managers;
using Soulstone.Utils;

namespace Soulstone.Datamodels
{
    public class InitiativeParticipant
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public int InitiativeValue { get; set; } = 0;
        public int BonusModifier { get; set; } = 0;
        public bool IsCurrentCharacter { get; set; } = false;
        public bool IsNpc { get; set; } = false;
        public string Notes { get; set; } = string.Empty;
        public List<Buff> Buffs { get; set; } = new();
        internal CharacterSheet? CharacterSheet { get; set; } = null;
        public string? SheetFilePath { get; set; } = null;

        public InitiativeParticipant() { }

        public InitiativeParticipant(string name, int initiativeValue, int bonusModifier = 0, bool isCurrentCharacter = false, string notes = "", List<Buff>? buffs = null, bool isNpc = false)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            InitiativeValue = initiativeValue;
            BonusModifier = bonusModifier;
            IsCurrentCharacter = isCurrentCharacter;
            IsNpc = isNpc;
            Notes = notes;
            Buffs = buffs != null ? new List<Buff>(buffs) : new List<Buff>();
        }

        internal InitiativeParticipant(string name, int initiativeValue, int bonusModifier, bool isCurrentCharacter, string notes, List<Buff>? buffs, CharacterSheet? characterSheet, string? sheetFilePath = null, bool isNpc = false)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            InitiativeValue = initiativeValue;
            BonusModifier = bonusModifier;
            IsCurrentCharacter = isCurrentCharacter;
            IsNpc = isNpc;
            Notes = notes;
            Buffs = buffs != null ? new List<Buff>(buffs) : new List<Buff>();
            CharacterSheet = characterSheet;
            SheetFilePath = sheetFilePath;
        }

        internal int GetEffectiveInitiativeModifier(DiceSystem? diceSystem)
        {
            if (CharacterSheet != null)
            {
                return CharacterSheet.GetInitiativeModifier(diceSystem);
            }
            return BonusModifier + GetBuffStatBonus("Initiative");
        }

        internal DiceRoll RollInitiative(DiceSystem? diceSystem, bool advantage = false, bool disadvantage = false, bool detailedRoll = false)
        {
            if (CharacterSheet != null)
            {
                var roll = CharacterSheet.RollInitiative(diceSystem, advantage, disadvantage, detailedRoll);
                InitiativeValue = roll.RollResult;
                BonusModifier = CharacterSheet.GetInitiativeModifier(diceSystem);
                return roll;
            }
            else
            {
                int modifier = GetEffectiveInitiativeModifier(diceSystem);
                string statInfo = "Initiative";
                if (diceSystem != null)
                {
                    if (diceSystem.InitiativeStatType == InitiativeStatType.Formula && !string.IsNullOrEmpty(diceSystem.InitiativeFormula))
                    {
                        statInfo = $"Initiative ({diceSystem.InitiativeFormula})";
                    }
                    else if (diceSystem.InitiativeStatType != InitiativeStatType.None && !string.IsNullOrEmpty(diceSystem.InitiativeStatName))
                    {
                        statInfo = $"Initiative ({diceSystem.InitiativeStatName})";
                    }
                }

                int sides = DiceRoll.GetSystemSides(diceSystem);
                DiceRoll roll = DiceRoll.RollStatWithSystem(diceSystem, statInfo, modifier, advantage, disadvantage)
                    ?? DiceRoll.RollDiceRegular(1, sides, modifier, statInfo, advantage, disadvantage);

                InitiativeValue = roll.RollResult;

                try
                {
                    string actor = !string.IsNullOrWhiteSpace(Name) ? Name : "Combatant";
                    string rollValue = detailedRoll ? roll.RollDetailedResultString.TextValue : roll.RollResultString.TextValue;
                    string echo = LocalizationManager.Instance.GetLocalizedString("InitiativeRollEchoFormat", actor, rollValue);
                    PartySyncManager.Instance.BroadcastDiceRoll(
                        "Initiative",
                        roll.RollResult,
                        string.Join(", ", roll.IndividualRolls),
                        echoText: echo,
                        characterName: actor
                    );
                }
                catch
                {
                    // Ignored in test environment
                }

                return roll;
            }
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
