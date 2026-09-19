using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Soulstone.Managers;
using Soulstone.Utils;

namespace Soulstone.Datamodels
{
    public class Feat
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string RollFormula { get; set; } = string.Empty;
        public Dictionary<string, int> StatModifiers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public bool IsActive { get; set; } = true;

        public Feat()
        {
        }

        public Feat(
            string name,
            string description = "",
            string category = "General",
            string rollFormula = "",
            Dictionary<string, int>? statModifiers = null,
            bool isActive = true)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Description = description;
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category;
            RollFormula = rollFormula ?? string.Empty;
            StatModifiers = statModifiers != null
                ? new Dictionary<string, int>(statModifiers, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            IsActive = isActive;
        }

        public Feat Clone()
        {
            return new Feat
            {
                Id = Guid.NewGuid().ToString(),
                Name = Name,
                Description = Description,
                Category = Category,
                RollFormula = RollFormula,
                StatModifiers = new Dictionary<string, int>(StatModifiers, StringComparer.OrdinalIgnoreCase),
                IsActive = IsActive
            };
        }

        public int GetStatModifier(string statName)
        {
            if (!IsActive || string.IsNullOrWhiteSpace(statName) || StatModifiers == null)
                return 0;

            return StatModifiers.TryGetValue(statName, out int value) ? value : 0;
        }

        public void SetStatModifier(string statName, int value)
        {
            if (string.IsNullOrWhiteSpace(statName)) return;

            StatModifiers ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (value == 0)
            {
                StatModifiers.Remove(statName);
            }
            else
            {
                StatModifiers[statName] = value;
            }
        }

        public bool RemoveStatModifier(string statName)
        {
            if (string.IsNullOrWhiteSpace(statName) || StatModifiers == null) return false;
            return StatModifiers.Remove(statName);
        }

        public string GetFormattedModifiers()
        {
            if (StatModifiers == null || StatModifiers.Count == 0)
                return string.Empty;

            return string.Join(", ", StatModifiers.Select(kv => $"{(kv.Value >= 0 ? "+" : "")}{kv.Value} {kv.Key}"));
        }

        internal string ResolveFormulaPreview(CharacterSheet? sheet = null, DiceSystem? diceSystem = null)
        {
            if (string.IsNullOrWhiteSpace(RollFormula))
                return string.Empty;

            string formula = RollFormula.Trim();
            if (sheet == null) return formula;

            try
            {
                // Replace any identifiers / stat tokens (e.g., Strength, DEX_Mod, Athletics, Level) with resolved values
                var vars = StatFormulaEvaluator.ExtractVariables(formula);
                foreach (var varName in vars.OrderByDescending(v => v.Length))
                {
                    double val = StatFormulaEvaluator.ResolveStatValue(varName, sheet, diceSystem);
                    int intVal = (int)Math.Round(val);
                    string pattern = $@"\b{Regex.Escape(varName)}\b";
                    formula = Regex.Replace(formula, pattern, intVal.ToString(), RegexOptions.IgnoreCase);
                }
                return formula;
            }
            catch
            {
                return formula;
            }
        }

        internal DiceRoll? RollFeat(
            CharacterSheet? sheet = null,
            DiceSystem? diceSystem = null,
            bool advantage = false,
            bool disadvantage = false,
            bool detailedRoll = false,
            bool isPrivate = false)
        {
            if (string.IsNullOrWhiteSpace(RollFormula))
                return null;

            try
            {
                string resolvedFormula = ResolveFormulaPreview(sheet, diceSystem);
                string rollName = !string.IsNullOrWhiteSpace(Name) ? Name : "Feat Roll";

                // Check if the resolved formula is a simple dice notation XdY(+/-Z)
                var simpleDiceRoll = DiceRoll.ParseDiceRollString(resolvedFormula, advantage, disadvantage);
                if (simpleDiceRoll != null)
                {
                    simpleDiceRoll.RollResultString = $"[{rollName}] {simpleDiceRoll.RollResultString.TextValue}";
                    simpleDiceRoll.RollDetailedResultString = $"[{rollName}] {simpleDiceRoll.RollDetailedResultString.TextValue}";

                    var msg = detailedRoll ? simpleDiceRoll.RollDetailedResultString : simpleDiceRoll.RollResultString;

                    PartySyncManager.Instance.BroadcastDiceRoll(
                        rollName,
                        simpleDiceRoll.RollResult,
                        string.Join(", ", simpleDiceRoll.IndividualRolls),
                        echoText: LocalizationManager.Instance.GetLocalizedString("RollEchoResult", rollName, msg.TextValue),
                        isPrivate: isPrivate
                    );

                    return simpleDiceRoll;
                }

                // If compound formula containing dice (e.g. 1d20 + 2d6 + 3 or 1d8 + 2)
                var diceRegex = new Regex(@"(\d+)d(\d+)", RegexOptions.IgnoreCase);
                var matches = diceRegex.Matches(resolvedFormula);
                var rand = new Random();
                var individualRolls = new List<int>();
                string mathFormula = resolvedFormula;

                foreach (Match match in matches)
                {
                    if (int.TryParse(match.Groups[1].Value, out int count) &&
                        int.TryParse(match.Groups[2].Value, out int sides) &&
                        count > 0 && sides > 0)
                    {
                        int sum = 0;
                        var groupRolls = new List<int>();
                        for (int i = 0; i < count; i++)
                        {
                            int r = (!advantage && !disadvantage) ? rand.Next(1, sides + 1)
                                : advantage ? Math.Max(rand.Next(1, sides + 1), rand.Next(1, sides + 1))
                                : Math.Min(rand.Next(1, sides + 1), rand.Next(1, sides + 1));
                            groupRolls.Add(r);
                            sum += r;
                        }
                        individualRolls.AddRange(groupRolls);

                        // Replace the first occurrence of this dice match with sum
                        var regexFirst = new Regex(Regex.Escape(match.Value));
                        mathFormula = regexFirst.Replace(mathFormula, sum.ToString(), 1);
                    }
                }

                int totalResult = StatFormulaEvaluator.EvaluateToInt(mathFormula, sheet, diceSystem);

                var roll = new DiceRoll
                {
                    RollResult = totalResult,
                    IndividualRolls = individualRolls
                };

                string rollsSummary = individualRolls.Count > 0 ? $"({string.Join(", ", individualRolls)})" : "";
                string resultSummary = $"{RollFormula} = {totalResult}";
                string detailedSummary = $"{RollFormula} -> [{resolvedFormula}] {rollsSummary} = {totalResult}";

                roll.RollResultString = $"[{rollName}] {resultSummary}";
                roll.RollDetailedResultString = $"[{rollName}] {detailedSummary}";

                var finalMsg = detailedRoll ? roll.RollDetailedResultString : roll.RollResultString;

                PartySyncManager.Instance.BroadcastDiceRoll(
                    rollName,
                    roll.RollResult,
                    string.Join(", ", roll.IndividualRolls),
                    echoText: LocalizationManager.Instance.GetLocalizedString("RollEchoResult", rollName, finalMsg.TextValue),
                    isPrivate: isPrivate
                );

                return roll;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to roll feat '{Name}' with formula '{RollFormula}'");
                return null;
            }
        }
    }
}
