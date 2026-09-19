using Soulstone.Datamodels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using System.Collections;
using System.Reflection;

namespace Soulstone.Utils
{
    internal static class StatFormulaEvaluator
    {
        private static readonly Dictionary<string, string> AttributeAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "STR", "Strength" },
            { "DEX", "Dexterity" },
            { "CON", "Constitution" },
            { "INT", "Intelligence" },
            { "WIS", "Wisdom" },
            { "CHA", "Charisma" },
            { "AGI", "Agility" },
            { "VIT", "Vitality" },
            { "PER", "Perception" },
            { "WIL", "Willpower" },
            { "END", "Endurance" },
            { "HP", "Health" },
            { "MP", "Mana" },
            { "SP", "Stamina" }
        };

        private static readonly HashSet<string> KnownFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "min", "max", "clamp", "floor", "ceil", "ceiling", "round", "abs", "sqrt", "mod",
            "log", "ln", "log10", "exp", "pow", "sign", "trunc", "truncate", "dndmod", "statmod",
            "if", "cond", "choose"
        };

        [ThreadStatic]
        private static Stack<string>? evaluationStack;

        [ThreadStatic]
        private static HashSet<string>? resolvingStats;

        private const int MaxRecursionDepth = 32;

        public static double Evaluate(
            string formula,
            CharacterSheet? sheet = null,
            DiceSystem? diceSystem = null,
            IDictionary<string, double>? customVariables = null)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return 0;

            evaluationStack ??= new Stack<string>();
            resolvingStats ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (evaluationStack.Count >= MaxRecursionDepth)
            {
                string stackChain = string.Join(" -> ", evaluationStack.Reverse());
                Plugin.Log?.Error($"[StatFormulaEvaluator] Recursion depth limit ({MaxRecursionDepth}) exceeded in formula solver for '{formula}'. Call stack: {stackChain}");
                throw new InvalidOperationException($"Recursion limit exceeded ({MaxRecursionDepth}) in formula evaluator for '{formula}'. Call chain: {stackChain}");
            }

            evaluationStack.Push(formula);
            try
            {
                var tokens = Tokenize(formula);
                if (tokens.Count == 0)
                    return 0;

                var parser = new Parser(tokens, sheet, diceSystem, customVariables);
                return parser.Parse();
            }
            finally
            {
                if (evaluationStack.Count > 0)
                {
                    evaluationStack.Pop();
                }
            }
        }

        public static int EvaluateToInt(
            string formula,
            CharacterSheet? sheet = null,
            DiceSystem? diceSystem = null,
            int defaultValue = 0,
            IDictionary<string, double>? customVariables = null)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return defaultValue;

            try
            {
                double result = Evaluate(formula, sheet, diceSystem, customVariables);
                if (double.IsNaN(result) || double.IsInfinity(result))
                    return defaultValue;

                return (int)Math.Round(result);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"[StatFormulaEvaluator] Failed to evaluate formula '{formula}'. Returning default value {defaultValue}.");
                return defaultValue;
            }
        }

        public static bool TryEvaluate(
            string formula,
            CharacterSheet? sheet,
            DiceSystem? diceSystem,
            out double result,
            out string? errorMessage,
            IDictionary<string, double>? customVariables = null)
        {
            result = 0;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(formula))
            {
                errorMessage = "Formula is empty.";
                return false;
            }

            try
            {
                var tokens = Tokenize(formula);
                var parser = new Parser(tokens, sheet, diceSystem, customVariables);
                result = parser.Parse();

                if (double.IsNaN(result) || double.IsInfinity(result))
                {
                    errorMessage = "Formula evaluated to an invalid numeric value (NaN or Infinity).";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static List<string> ExtractVariables(string formula)
        {
            var vars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(formula))
                return vars.ToList();

            try
            {
                var tokens = Tokenize(formula);
                for (int i = 0; i < tokens.Count; i++)
                {
                    var t = tokens[i];
                    if (t.Type == TokenType.Identifier && !KnownFunctions.Contains(t.Text))
                    {
                        // Check if it's followed by '(' -> function call
                        if (i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.LParen)
                            continue;

                        vars.Add(t.Text);
                    }
                }
            }
            catch
            {
                // In case of tokenization issues, fallback regex
                var matches = Regex.Matches(formula, @"[@\b][a-zA-Z_][a-zA-Z0-9_\.]*\b");
                foreach (Match m in matches)
                {
                    string val = m.Value.TrimStart('@');
                    if (!KnownFunctions.Contains(val) && !Regex.IsMatch(val, @"^\d*d\d+$", RegexOptions.IgnoreCase))
                    {
                        vars.Add(val);
                    }
                }
            }

            return vars.ToList();
        }

        public static double ResolveStatValue(
            string statName,
            CharacterSheet? sheet,
            DiceSystem? diceSystem = null,
            IDictionary<string, double>? customVariables = null)
        {
            if (string.IsNullOrWhiteSpace(statName))
                return 0;

            string cleanName = statName.Trim(' ', '[', ']', '{', '}', '\'', '"');
            if (cleanName.StartsWith('@'))
            {
                cleanName = cleanName.Substring(1).Trim();
            }

            if (customVariables != null && customVariables.TryGetValue(cleanName, out double customVal))
            {
                return customVal;
            }

            if (sheet == null)
                return 0;

            resolvingStats ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (resolvingStats.Contains(cleanName))
            {
                string chain = string.Join(" -> ", evaluationStack != null ? evaluationStack.Reverse() : Array.Empty<string>());
                Plugin.Log?.Error($"[StatFormulaEvaluator] Circular dependency detected in formula solver for '{cleanName}'. Active chain: {chain} -> '{cleanName}'");
                throw new InvalidOperationException($"Circular dependency detected in formula solver for '{cleanName}'. Active chain: {chain} -> '{cleanName}'");
            }

            resolvingStats.Add(cleanName);
            try
            {
                return ResolveStatValueInternal(cleanName, sheet, diceSystem, customVariables);
            }
            finally
            {
                resolvingStats.Remove(cleanName);
            }
        }

        private static double ResolveStatValueInternal(
            string cleanName,
            CharacterSheet sheet,
            DiceSystem? diceSystem,
            IDictionary<string, double>? customVariables)
        {

            // Handle Dot Notation (e.g. Strength.Mod, Health.Current, Health.Max, Gear.Strength, Buff.Strength)
            if (cleanName.Contains('.') || cleanName.Contains(':'))
            {
                char sep = cleanName.Contains('.') ? '.' : ':';
                var parts = cleanName.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    string target = parts[0].Trim();
                    string prop = parts[1].Trim();

                    // Gear / Buff / Feat prefix
                    if (target.Equals("Gear", StringComparison.OrdinalIgnoreCase) ||
                        target.Equals("GearBonus", StringComparison.OrdinalIgnoreCase) ||
                        target.Equals("Equipment", StringComparison.OrdinalIgnoreCase))
                    {
                        return sheet.GetGearStatBonus(prop);
                    }
                    if (target.Equals("Buff", StringComparison.OrdinalIgnoreCase) ||
                        target.Equals("BuffBonus", StringComparison.OrdinalIgnoreCase) ||
                        target.Equals("Buffs", StringComparison.OrdinalIgnoreCase) ||
                        target.Equals("Debuff", StringComparison.OrdinalIgnoreCase))
                    {
                        return sheet.GetBuffStatBonus(prop);
                    }
                    if (target.Equals("Feat", StringComparison.OrdinalIgnoreCase) ||
                        target.Equals("Feats", StringComparison.OrdinalIgnoreCase) ||
                        target.Equals("FeatBonus", StringComparison.OrdinalIgnoreCase))
                    {
                        return sheet.GetFeatStatBonus(prop);
                    }
                    if (target.Equals("Base", StringComparison.OrdinalIgnoreCase))
                    {
                        if (sheet.CharacterAttributes != null)
                        {
                            var aKey = FindMatchingKey(sheet.CharacterAttributes.Keys, prop);
                            if (aKey != null) return sheet.CharacterAttributes[aKey].Value;
                        }
                        if (sheet.CharacterSkills != null)
                        {
                            var sKey = FindMatchingKey(sheet.CharacterSkills.Keys, prop);
                            if (sKey != null) return sheet.CharacterSkills[sKey].SkillModifier;
                        }
                        if (sheet.CharacterAbilities != null)
                        {
                            var abKey = FindMatchingKey(sheet.CharacterAbilities.Keys, prop);
                            if (abKey != null) return sheet.CharacterAbilities[abKey].AbilityModifier;
                        }
                        if (sheet.CharacterResources != null)
                        {
                            var rKey = FindMatchingKey(sheet.CharacterResources.Keys, prop);
                            if (rKey != null) return sheet.CharacterResources[rKey].MaxValue;
                        }
                    }

                    // Attribute property resolution
                    if (sheet.CharacterAttributes != null)
                    {
                        string attrLookup = target;
                        if (AttributeAliases.TryGetValue(target, out var aliasCanonical))
                        {
                            attrLookup = aliasCanonical;
                        }

                        var aKey = FindMatchingKey(sheet.CharacterAttributes.Keys, attrLookup);
                        if (aKey != null)
                        {
                            var attr = sheet.CharacterAttributes[aKey];
                            if (prop.Equals("Mod", StringComparison.OrdinalIgnoreCase) || prop.Equals("Modifier", StringComparison.OrdinalIgnoreCase))
                            {
                                int eff = sheet.GetEffectiveAttributeValue(aKey);
                                return (int)Math.Floor((eff - 10.0) / 2.0);
                            }
                            if (prop.Equals("Base", StringComparison.OrdinalIgnoreCase) || prop.Equals("Value", StringComparison.OrdinalIgnoreCase))
                                return attr.Value;
                            if (prop.Equals("Temp", StringComparison.OrdinalIgnoreCase) || prop.Equals("TempBonus", StringComparison.OrdinalIgnoreCase))
                                return attr.TempBonus;
                            if (prop.Equals("Perm", StringComparison.OrdinalIgnoreCase) || prop.Equals("PermBonus", StringComparison.OrdinalIgnoreCase))
                                return attr.PermBonus;
                            if (prop.Equals("Epic", StringComparison.OrdinalIgnoreCase) || prop.Equals("EpicBonus", StringComparison.OrdinalIgnoreCase))
                                return attr.EpicBonus;
                            if (prop.Equals("Total", StringComparison.OrdinalIgnoreCase))
                                return attr.TotalValue;
                            if (prop.Equals("Gear", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetGearStatBonus(aKey);
                            if (prop.Equals("Buff", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetBuffStatBonus(aKey);
                            if (prop.Equals("Feat", StringComparison.OrdinalIgnoreCase) || prop.Equals("FeatBonus", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetFeatStatBonus(aKey);
                            if (prop.Equals("Effective", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetEffectiveAttributeValue(aKey);
                        }
                    }

                    // Resource property resolution
                    if (sheet.CharacterResources != null)
                    {
                        var rKey = FindMatchingKey(sheet.CharacterResources.Keys, target);
                        if (rKey != null)
                        {
                            var res = sheet.CharacterResources[rKey];
                            if (prop.Equals("Current", StringComparison.OrdinalIgnoreCase) || prop.Equals("Cur", StringComparison.OrdinalIgnoreCase) || prop.Equals("Val", StringComparison.OrdinalIgnoreCase) || prop.Equals("Value", StringComparison.OrdinalIgnoreCase))
                                return res.CurrentValue;
                            if (prop.Equals("Max", StringComparison.OrdinalIgnoreCase) || prop.Equals("BaseMax", StringComparison.OrdinalIgnoreCase))
                                return res.MaxValue;
                            if (prop.Equals("Temp", StringComparison.OrdinalIgnoreCase) || prop.Equals("TempBonus", StringComparison.OrdinalIgnoreCase))
                                return res.TempBonus;
                            if (prop.Equals("Effective", StringComparison.OrdinalIgnoreCase) || prop.Equals("EffectiveMax", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetEffectiveResourceMax(rKey, diceSystem);
                            if (prop.Equals("Fraction", StringComparison.OrdinalIgnoreCase) || prop.Equals("Percent", StringComparison.OrdinalIgnoreCase) || prop.Equals("Pct", StringComparison.OrdinalIgnoreCase))
                            {
                                int effMax = sheet.GetEffectiveResourceMax(rKey, diceSystem);
                                if (effMax <= 0) return 1.0;
                                double frac = (double)res.CurrentValue / effMax;
                                return prop.Equals("Fraction", StringComparison.OrdinalIgnoreCase) ? frac : (frac * 100.0);
                            }
                        }
                    }

                    // Skill property resolution
                    if (sheet.CharacterSkills != null)
                    {
                        var sKey = FindMatchingKey(sheet.CharacterSkills.Keys, target);
                        if (sKey != null)
                        {
                            var sk = sheet.CharacterSkills[sKey];
                            if (prop.Equals("Base", StringComparison.OrdinalIgnoreCase) || prop.Equals("Modifier", StringComparison.OrdinalIgnoreCase) || prop.Equals("Mod", StringComparison.OrdinalIgnoreCase))
                                return sk.SkillModifier;
                            if (prop.Equals("Gear", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetGearStatBonus(sKey);
                            if (prop.Equals("Buff", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetBuffStatBonus(sKey);
                            if (prop.Equals("Feat", StringComparison.OrdinalIgnoreCase) || prop.Equals("FeatBonus", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetFeatStatBonus(sKey);
                            if (prop.Equals("Total", StringComparison.OrdinalIgnoreCase) || prop.Equals("Effective", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetEffectiveSkillTotal(sKey, diceSystem);
                        }
                    }

                    // Ability property resolution
                    if (sheet.CharacterAbilities != null)
                    {
                        var abKey = FindMatchingKey(sheet.CharacterAbilities.Keys, target);
                        if (abKey != null)
                        {
                            var ab = sheet.CharacterAbilities[abKey];
                            if (prop.Equals("Base", StringComparison.OrdinalIgnoreCase) || prop.Equals("Modifier", StringComparison.OrdinalIgnoreCase) || prop.Equals("Mod", StringComparison.OrdinalIgnoreCase))
                                return ab.AbilityModifier;
                            if (prop.Equals("Gear", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetGearStatBonus(abKey);
                            if (prop.Equals("Buff", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetBuffStatBonus(abKey);
                            if (prop.Equals("Feat", StringComparison.OrdinalIgnoreCase) || prop.Equals("FeatBonus", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetFeatStatBonus(abKey);
                            if (prop.Equals("Total", StringComparison.OrdinalIgnoreCase) || prop.Equals("Effective", StringComparison.OrdinalIgnoreCase))
                                return sheet.GetEffectiveAbilityModifier(abKey);
                        }
                    }
                }
            }

            // Core Level properties
            if (cleanName.Equals("Level", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CharacterLevel", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("Lvl", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("LV", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CharLevel", StringComparison.OrdinalIgnoreCase))
            {
                return sheet.CharacterLevel;
            }

            // Experience properties
            if (cleanName.Equals("Experience", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CharacterExperience", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CharacterExperiencePoints", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("Exp", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("XP", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("ExperiencePoints", StringComparison.OrdinalIgnoreCase))
            {
                return sheet.CharacterExperiencePoints;
            }

            // Health properties
            if (cleanName.Equals("Health", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("HP", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("HealthPoints", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CharacterHealthPoints", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CurrentHP", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CurrentHealth", StringComparison.OrdinalIgnoreCase))
            {
                if (sheet.CharacterResources != null && sheet.CharacterResources.TryGetValue("Health", out var hpRes))
                    return hpRes.CurrentValue;
                return sheet.CharacterHealthPoints;
            }
            if (cleanName.Equals("MaxHealth", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("MaxHP", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("MaxHealthPoints", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CharacterMaxHealthPoints", StringComparison.OrdinalIgnoreCase))
            {
                if (sheet.CharacterResources != null && sheet.CharacterResources.TryGetValue("Health", out var hpRes))
                    return sheet.GetEffectiveResourceMax("Health", diceSystem);
                return sheet.CharacterMaxHealthPoints;
            }

            // Mana properties
            if (cleanName.Equals("Mana", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("MP", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("ManaPoints", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CharacterManaPoints", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CurrentMP", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CurrentMana", StringComparison.OrdinalIgnoreCase))
            {
                if (sheet.CharacterResources != null && sheet.CharacterResources.TryGetValue("Mana", out var mpRes))
                    return mpRes.CurrentValue;
                return sheet.CharacterManaPoints;
            }
            if (cleanName.Equals("MaxMana", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("MaxMP", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("MaxManaPoints", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CharacterMaxManaPoints", StringComparison.OrdinalIgnoreCase))
            {
                if (sheet.CharacterResources != null && sheet.CharacterResources.TryGetValue("Mana", out var mpRes))
                    return sheet.GetEffectiveResourceMax("Mana", diceSystem);
                return sheet.CharacterMaxManaPoints;
            }

            // Initiative
            if (cleanName.Equals("Initiative", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("Init", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CombatInitiative", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("EffectiveInitiative", StringComparison.OrdinalIgnoreCase))
            {
                return sheet.GetInitiativeModifier(diceSystem);
            }

            // Inventory & Weight
            if (cleanName.Equals("InventoryCapacity", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("InventoryMaxSlots", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("MaxInventory", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CustomInventoryCapacity", StringComparison.OrdinalIgnoreCase))
            {
                return sheet.GetEffectiveInventoryCapacity(diceSystem);
            }
            if (cleanName.Equals("InventoryCount", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("ItemCount", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("InventoryItemCount", StringComparison.OrdinalIgnoreCase))
            {
                return sheet.characterInventory?.Count ?? 0;
            }
            if (cleanName.Equals("InventoryWeight", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("TotalWeight", StringComparison.OrdinalIgnoreCase))
            {
                if (sheet.characterInventory == null || sheet.characterInventory.Count == 0) return 0;
                double totalWeight = 0;
                foreach (var item in sheet.characterInventory)
                {
                    totalWeight += item.Weight * Math.Max(1, item.Quantity);
                }
                return totalWeight;
            }

            // Buffs count
            if (cleanName.Equals("BuffCount", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("BuffsCount", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("ActiveBuffsCount", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("DebuffCount", StringComparison.OrdinalIgnoreCase))
            {
                return sheet.ActiveBuffs?.Count ?? 0;
            }

            // Feats count
            if (cleanName.Equals("FeatsCount", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("FeatCount", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("ActiveFeatsCount", StringComparison.OrdinalIgnoreCase))
            {
                return sheet.CharacterFeats?.Count(f => f.IsActive) ?? 0;
            }

            // Augmentations count
            if (cleanName.Equals("AugmentationsCount", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("AugmentationCount", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("EquippedAugmentationsCount", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("CyberwareCount", StringComparison.OrdinalIgnoreCase))
            {
                return sheet.EquippedAugmentations?.Count(kv => !string.IsNullOrWhiteSpace(kv.Value)) ?? 0;
            }

            // Gear count
            if (cleanName.Equals("EquippedGearCount", StringComparison.OrdinalIgnoreCase) ||
                cleanName.Equals("GearCount", StringComparison.OrdinalIgnoreCase))
            {
                return sheet.EquippedGear?.Count(kv => !string.IsNullOrWhiteSpace(kv.Value)) ?? 0;
            }

            // Attribute Modifier aliases (e.g. STR_Mod, STRMOD, StrengthMod, StrengthModifier)
            if (cleanName.EndsWith("_Mod", StringComparison.OrdinalIgnoreCase) ||
                cleanName.EndsWith("Mod", StringComparison.OrdinalIgnoreCase) ||
                cleanName.EndsWith("_Modifier", StringComparison.OrdinalIgnoreCase) ||
                cleanName.EndsWith("Modifier", StringComparison.OrdinalIgnoreCase))
            {
                string stem = cleanName;
                if (stem.EndsWith("_Modifier", StringComparison.OrdinalIgnoreCase)) stem = stem.Substring(0, stem.Length - 9);
                else if (stem.EndsWith("Modifier", StringComparison.OrdinalIgnoreCase)) stem = stem.Substring(0, stem.Length - 8);
                else if (stem.EndsWith("_Mod", StringComparison.OrdinalIgnoreCase)) stem = stem.Substring(0, stem.Length - 4);
                else if (stem.EndsWith("Mod", StringComparison.OrdinalIgnoreCase)) stem = stem.Substring(0, stem.Length - 3);

                if (AttributeAliases.TryGetValue(stem, out var canonicalStem))
                {
                    stem = canonicalStem;
                }

                if (sheet.CharacterAttributes != null)
                {
                    var aKey = FindMatchingKey(sheet.CharacterAttributes.Keys, stem);
                    if (aKey != null)
                    {
                        int eff = sheet.GetEffectiveAttributeValue(aKey);
                        return (int)Math.Floor((eff - 10.0) / 2.0);
                    }
                }
            }

            // Generic Resource check (e.g. MaxHealth, MaxStamina, CurrentHealth, CurrentStamina)
            if (sheet.CharacterResources != null)
            {
                if (cleanName.StartsWith("Max", StringComparison.OrdinalIgnoreCase) && cleanName.Length > 3)
                {
                    string resStem = cleanName.Substring(3).TrimStart('_');
                    var resKey = FindMatchingKey(sheet.CharacterResources.Keys, resStem);
                    if (resKey != null)
                    {
                        return sheet.GetEffectiveResourceMax(resKey, diceSystem);
                    }
                }
                if ((cleanName.StartsWith("Current", StringComparison.OrdinalIgnoreCase) && cleanName.Length > 7) ||
                    (cleanName.StartsWith("Cur", StringComparison.OrdinalIgnoreCase) && cleanName.Length > 3))
                {
                    string resStem = cleanName.StartsWith("Current", StringComparison.OrdinalIgnoreCase)
                        ? cleanName.Substring(7).TrimStart('_')
                        : cleanName.Substring(3).TrimStart('_');
                    var resKey = FindMatchingKey(sheet.CharacterResources.Keys, resStem);
                    if (resKey != null)
                    {
                        return sheet.CharacterResources[resKey].CurrentValue;
                    }
                }

                var rDirectKey = FindMatchingKey(sheet.CharacterResources.Keys, cleanName);
                if (rDirectKey != null)
                {
                    var res = sheet.CharacterResources[rDirectKey];
                    return res.MaxValue > 0 ? sheet.GetEffectiveResourceMax(rDirectKey, diceSystem) : res.CurrentValue;
                }
            }

            // Character Attributes
            if (sheet.CharacterAttributes != null)
            {
                var attrKey = FindMatchingKey(sheet.CharacterAttributes.Keys, cleanName);
                if (attrKey != null)
                {
                    return sheet.GetEffectiveAttributeValue(attrKey);
                }

                // Check by Attribute.Name
                var attrEntry = sheet.CharacterAttributes.FirstOrDefault(kv =>
                    string.Equals(kv.Value.Name, cleanName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Value.Name.Replace(" ", ""), cleanName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(attrEntry.Key))
                {
                    return sheet.GetEffectiveAttributeValue(attrEntry.Key);
                }
            }

            // Character Skills
            if (sheet.CharacterSkills != null)
            {
                var skillKey = FindMatchingKey(sheet.CharacterSkills.Keys, cleanName);
                if (skillKey != null)
                {
                    return sheet.GetEffectiveSkillTotal(skillKey, diceSystem);
                }

                var skillEntry = sheet.CharacterSkills.FirstOrDefault(kv =>
                    string.Equals(kv.Value.SkillName, cleanName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Value.SkillName.Replace(" ", ""), cleanName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(skillEntry.Key))
                {
                    return sheet.GetEffectiveSkillTotal(skillEntry.Key, diceSystem);
                }
            }

            // Character Abilities
            if (sheet.CharacterAbilities != null)
            {
                var abilityKey = FindMatchingKey(sheet.CharacterAbilities.Keys, cleanName);
                if (abilityKey != null)
                {
                    return sheet.GetEffectiveAbilityModifier(abilityKey);
                }

                var abilityEntry = sheet.CharacterAbilities.FirstOrDefault(kv =>
                    string.Equals(kv.Value.AbilityName, cleanName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Value.AbilityName.Replace(" ", ""), cleanName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(abilityEntry.Key))
                {
                    return sheet.GetEffectiveAbilityModifier(abilityEntry.Key);
                }
            }

            // Attribute Aliases (STR -> Strength, etc.)
            if (AttributeAliases.TryGetValue(cleanName, out var canonicalName))
            {
                return ResolveStatValue(canonicalName, sheet, diceSystem, customVariables);
            }

            // Reflection fallback: resolve ANY property or field on CharacterSheet
            var propInfo = typeof(CharacterSheet).GetProperty(cleanName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (propInfo != null)
            {
                var val = propInfo.GetValue(sheet);
                if (val != null)
                {
                    if (val is int iVal) return iVal;
                    if (val is double dVal) return dVal;
                    if (val is float fVal) return fVal;
                    if (val is long lVal) return lVal;
                    if (val is short sVal) return sVal;
                    if (val is byte bVal) return bVal;
                    if (val is bool boolVal) return boolVal ? 1 : 0;
                    if (val is ICollection col) return col.Count;
                    if (val is string strVal && double.TryParse(strVal, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedStr))
                        return parsedStr;
                }
            }

            var fieldInfo = typeof(CharacterSheet).GetField(cleanName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (fieldInfo != null)
            {
                var val = fieldInfo.GetValue(sheet);
                if (val != null)
                {
                    if (val is int iVal) return iVal;
                    if (val is double dVal) return dVal;
                    if (val is float fVal) return fVal;
                    if (val is long lVal) return lVal;
                    if (val is short sVal) return sVal;
                    if (val is byte bVal) return bVal;
                    if (val is bool boolVal) return boolVal ? 1 : 0;
                    if (val is ICollection col) return col.Count;
                    if (val is string strVal && double.TryParse(strVal, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedStr))
                        return parsedStr;
                }
            }

            return 0;
        }

        private static string? FindMatchingKey(IEnumerable<string> keys, string target)
        {
            foreach (var k in keys)
            {
                if (string.Equals(k, target, StringComparison.OrdinalIgnoreCase))
                    return k;
            }

            string targetNoSpaces = target.Replace(" ", "").Replace("_", "");
            foreach (var k in keys)
            {
                string keyNoSpaces = k.Replace(" ", "").Replace("_", "");
                if (string.Equals(keyNoSpaces, targetNoSpaces, StringComparison.OrdinalIgnoreCase))
                    return k;
            }

            return null;
        }

        private enum TokenType
        {
            Number,
            Identifier,
            Dice,
            Plus,
            Minus,
            Multiply,
            Divide,
            Modulo,
            Power,
            LessThan,
            GreaterThan,
            LessThanOrEqual,
            GreaterThanOrEqual,
            Equal,
            NotEqual,
            LParen,
            RParen,
            Comma,
            End
        }

        private class Token
        {
            public TokenType Type { get; set; }
            public string Text { get; set; } = string.Empty;
            public double NumberValue { get; set; }
            public int DiceCount { get; set; }
            public int DiceSides { get; set; }

            public override string ToString() => $"{Type}: {Text}";
        }

        private static List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            int i = 0;
            int len = input.Length;

            while (i < len)
            {
                char c = input[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (c == '+') { tokens.Add(new Token { Type = TokenType.Plus, Text = "+" }); i++; }
                else if (c == '-') { tokens.Add(new Token { Type = TokenType.Minus, Text = "-" }); i++; }
                else if (c == '*') { tokens.Add(new Token { Type = TokenType.Multiply, Text = "*" }); i++; }
                else if (c == '/') { tokens.Add(new Token { Type = TokenType.Divide, Text = "/" }); i++; }
                else if (c == '%') { tokens.Add(new Token { Type = TokenType.Modulo, Text = "%" }); i++; }
                else if (c == '^') { tokens.Add(new Token { Type = TokenType.Power, Text = "^" }); i++; }
                else if (c == '<')
                {
                    if (i + 1 < len && input[i + 1] == '=')
                    {
                        tokens.Add(new Token { Type = TokenType.LessThanOrEqual, Text = "<=" });
                        i += 2;
                    }
                    else if (i + 1 < len && input[i + 1] == '>')
                    {
                        tokens.Add(new Token { Type = TokenType.NotEqual, Text = "<>" });
                        i += 2;
                    }
                    else
                    {
                        tokens.Add(new Token { Type = TokenType.LessThan, Text = "<" });
                        i++;
                    }
                }
                else if (c == '>')
                {
                    if (i + 1 < len && input[i + 1] == '=')
                    {
                        tokens.Add(new Token { Type = TokenType.GreaterThanOrEqual, Text = ">=" });
                        i += 2;
                    }
                    else
                    {
                        tokens.Add(new Token { Type = TokenType.GreaterThan, Text = ">" });
                        i++;
                    }
                }
                else if (c == '=')
                {
                    if (i + 1 < len && input[i + 1] == '=')
                    {
                        tokens.Add(new Token { Type = TokenType.Equal, Text = "==" });
                        i += 2;
                    }
                    else
                    {
                        tokens.Add(new Token { Type = TokenType.Equal, Text = "=" });
                        i++;
                    }
                }
                else if (c == '!')
                {
                    if (i + 1 < len && input[i + 1] == '=')
                    {
                        tokens.Add(new Token { Type = TokenType.NotEqual, Text = "!=" });
                        i += 2;
                    }
                    else
                    {
                        throw new FormatException($"Unexpected character '!' at position {i} in formula: {input}");
                    }
                }
                else if (c == '(') { tokens.Add(new Token { Type = TokenType.LParen, Text = "(" }); i++; }
                else if (c == ')') { tokens.Add(new Token { Type = TokenType.RParen, Text = ")" }); i++; }
                else if (c == ',') { tokens.Add(new Token { Type = TokenType.Comma, Text = "," }); i++; }
                else if (c == '[' || c == '{' || c == '\'' || c == '"')
                {
                    char closeChar = c == '[' ? ']' : (c == '{' ? '}' : c);
                    int start = ++i;
                    while (i < len && input[i] != closeChar) i++;
                    string id = input.Substring(start, i - start);
                    if (i < len) i++; // skip close char
                    tokens.Add(new Token { Type = TokenType.Identifier, Text = id });
                }
                else if (char.IsDigit(c) || (c == '.' && i + 1 < len && char.IsDigit(input[i + 1])))
                {
                    int start = i;
                    bool hasDot = c == '.';
                    i++;
                    while (i < len && (char.IsDigit(input[i]) || (!hasDot && input[i] == '.')))
                    {
                        if (input[i] == '.') hasDot = true;
                        i++;
                    }

                    // Check if it's a dice roll like 2d6 or 1d20
                    if (i < len && (input[i] == 'd' || input[i] == 'D') && i + 1 < len && char.IsDigit(input[i + 1]))
                    {
                        string countStr = input.Substring(start, i - start);
                        int count = int.TryParse(countStr, out int cnt) ? cnt : 1;
                        i++; // skip 'd'
                        int sideStart = i;
                        while (i < len && char.IsDigit(input[i])) i++;
                        string sidesStr = input.Substring(sideStart, i - sideStart);
                        int sides = int.TryParse(sidesStr, out int s) ? s : 6;

                        tokens.Add(new Token
                        {
                            Type = TokenType.Dice,
                            Text = $"{count}d{sides}",
                            DiceCount = count,
                            DiceSides = sides
                        });
                    }
                    else
                    {
                        string numStr = input.Substring(start, i - start);
                        double val = double.Parse(numStr, CultureInfo.InvariantCulture);
                        tokens.Add(new Token { Type = TokenType.Number, Text = numStr, NumberValue = val });
                    }
                }
                else if (c == 'd' || c == 'D')
                {
                    // Check if standalone d20, d6 etc.
                    if (i + 1 < len && char.IsDigit(input[i + 1]) && (tokens.Count == 0 || tokens[^1].Type == TokenType.Plus || tokens[^1].Type == TokenType.Minus || tokens[^1].Type == TokenType.Multiply || tokens[^1].Type == TokenType.Divide || tokens[^1].Type == TokenType.Modulo || tokens[^1].Type == TokenType.Power || tokens[^1].Type == TokenType.LParen || tokens[^1].Type == TokenType.Comma))
                    {
                        i++; // skip 'd'
                        int sideStart = i;
                        while (i < len && char.IsDigit(input[i])) i++;
                        string sidesStr = input.Substring(sideStart, i - sideStart);
                        int sides = int.TryParse(sidesStr, out int s) ? s : 6;

                        tokens.Add(new Token
                        {
                            Type = TokenType.Dice,
                            Text = $"1d{sides}",
                            DiceCount = 1,
                            DiceSides = sides
                        });
                    }
                    else
                    {
                        int start = i;
                        while (i < len && (char.IsLetterOrDigit(input[i]) || input[i] == '_' || input[i] == '.' || input[i] == ':')) i++;
                        string id = input.Substring(start, i - start);
                        tokens.Add(new Token { Type = TokenType.Identifier, Text = id });
                    }
                }
                else if (c == '@' || char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    if (c == '@') i++;
                    while (i < len && (char.IsLetterOrDigit(input[i]) || input[i] == '_' || input[i] == '.' || input[i] == ':')) i++;
                    string id = input.Substring(start, i - start);
                    tokens.Add(new Token { Type = TokenType.Identifier, Text = id });
                }
                else
                {
                    throw new FormatException($"Unexpected character '{c}' at position {i} in formula: {input}");
                }
            }

            tokens.Add(new Token { Type = TokenType.End, Text = string.Empty });
            return tokens;
        }

        private class Parser
        {
            private readonly List<Token> tokens;
            private readonly CharacterSheet? sheet;
            private readonly DiceSystem? diceSystem;
            private readonly IDictionary<string, double>? customVariables;
            private int pos = 0;
            private static readonly Random Rng = new();

            public Parser(
                List<Token> tokens,
                CharacterSheet? sheet,
                DiceSystem? diceSystem,
                IDictionary<string, double>? customVariables)
            {
                this.tokens = tokens;
                this.sheet = sheet;
                this.diceSystem = diceSystem;
                this.customVariables = customVariables;
            }

            private Token Current => pos < tokens.Count ? tokens[pos] : tokens[^1];

            private Token Consume(TokenType expected)
            {
                var token = Current;
                if (token.Type != expected)
                {
                    throw new FormatException($"Expected token '{expected}' but found '{token.Type}' ('{token.Text}') at position {pos}.");
                }
                pos++;
                return token;
            }

            public double Parse()
            {
                double result = ParseEquality();
                if (Current.Type != TokenType.End)
                {
                    throw new FormatException($"Unexpected token '{Current.Text}' after expression end.");
                }
                return result;
            }

            private double ParseEquality()
            {
                double left = ParseComparison();

                while (Current.Type == TokenType.Equal || Current.Type == TokenType.NotEqual)
                {
                    var op = Current.Type;
                    pos++;
                    double right = ParseComparison();

                    if (op == TokenType.Equal)
                        left = Math.Abs(left - right) < 0.0000001 ? 1.0 : 0.0;
                    else
                        left = Math.Abs(left - right) >= 0.0000001 ? 1.0 : 0.0;
                }

                return left;
            }

            private double ParseComparison()
            {
                double left = ParseAdditive();

                while (Current.Type == TokenType.LessThan || Current.Type == TokenType.GreaterThan ||
                       Current.Type == TokenType.LessThanOrEqual || Current.Type == TokenType.GreaterThanOrEqual)
                {
                    var op = Current.Type;
                    pos++;
                    double right = ParseAdditive();

                    if (op == TokenType.LessThan)
                        left = left < right ? 1.0 : 0.0;
                    else if (op == TokenType.GreaterThan)
                        left = left > right ? 1.0 : 0.0;
                    else if (op == TokenType.LessThanOrEqual)
                        left = left <= right ? 1.0 : 0.0;
                    else if (op == TokenType.GreaterThanOrEqual)
                        left = left >= right ? 1.0 : 0.0;
                }

                return left;
            }

            private double ParseAdditive()
            {
                double left = ParseMultiplicative();

                while (Current.Type == TokenType.Plus || Current.Type == TokenType.Minus)
                {
                    var op = Current.Type;
                    pos++;
                    double right = ParseMultiplicative();

                    if (op == TokenType.Plus)
                        left += right;
                    else
                        left -= right;
                }

                return left;
            }

            private double ParseMultiplicative()
            {
                double left = ParsePower();

                while (Current.Type == TokenType.Multiply || Current.Type == TokenType.Divide || Current.Type == TokenType.Modulo)
                {
                    var op = Current.Type;
                    pos++;
                    double right = ParsePower();

                    if (op == TokenType.Multiply)
                    {
                        left *= right;
                    }
                    else if (op == TokenType.Divide)
                    {
                        if (Math.Abs(right) < double.Epsilon)
                            throw new DivideByZeroException("Division by zero in formula evaluation.");
                        left /= right;
                    }
                    else
                    {
                        if (Math.Abs(right) < double.Epsilon)
                            throw new DivideByZeroException("Modulo by zero in formula evaluation.");
                        left %= right;
                    }
                }

                return left;
            }

            private double ParsePower()
            {
                double left = ParseUnary();

                if (Current.Type == TokenType.Power)
                {
                    pos++;
                    double right = ParsePower(); // right-associative
                    return Math.Pow(left, right);
                }

                return left;
            }

            private double ParseUnary()
            {
                if (Current.Type == TokenType.Plus)
                {
                    pos++;
                    return ParseUnary();
                }

                if (Current.Type == TokenType.Minus)
                {
                    pos++;
                    return -ParseUnary();
                }

                return ParsePrimary();
            }

            private double ParsePrimary()
            {
                var token = Current;

                if (token.Type == TokenType.Number)
                {
                    pos++;
                    return token.NumberValue;
                }

                if (token.Type == TokenType.Dice)
                {
                    pos++;
                    int sum = 0;
                    for (int i = 0; i < token.DiceCount; i++)
                    {
                        sum += Rng.Next(1, Math.Max(1, token.DiceSides) + 1);
                    }
                    return sum;
                }

                if (token.Type == TokenType.LParen)
                {
                    pos++;
                    double value = ParseEquality();
                    Consume(TokenType.RParen);
                    return value;
                }

                if (token.Type == TokenType.Identifier)
                {
                    string id = token.Text;
                    pos++;

                    // Check if function call
                    if (Current.Type == TokenType.LParen)
                    {
                        pos++;
                        var args = new List<double>();
                        if (Current.Type != TokenType.RParen)
                        {
                            args.Add(ParseEquality());
                            while (Current.Type == TokenType.Comma)
                            {
                                pos++;
                                args.Add(ParseEquality());
                            }
                        }
                        Consume(TokenType.RParen);
                        return EvaluateFunction(id, args);
                    }

                    // Stat or variable resolution
                    return ResolveStatValue(id, sheet, diceSystem, customVariables);
                }

                throw new FormatException($"Unexpected token '{token.Text}' of type '{token.Type}' in formula.");
            }

            private double EvaluateFunction(string funcName, List<double> args)
            {
                string name = funcName.TrimStart('@').ToLowerInvariant();
                switch (name)
                {
                    case "min":
                        if (args.Count == 0) throw new ArgumentException("min() requires at least one argument.");
                        return args.Min();

                    case "max":
                        if (args.Count == 0) throw new ArgumentException("max() requires at least one argument.");
                        return args.Max();

                    case "clamp":
                        if (args.Count != 3) throw new ArgumentException("clamp() requires 3 arguments: clamp(value, min, max).");
                        return Math.Clamp(args[0], args[1], args[2]);

                    case "floor":
                        if (args.Count != 1) throw new ArgumentException("floor() requires 1 argument: floor(value).");
                        return Math.Floor(args[0]);

                    case "ceil":
                    case "ceiling":
                        if (args.Count != 1) throw new ArgumentException("ceil() requires 1 argument: ceil(value).");
                        return Math.Ceiling(args[0]);

                    case "round":
                        if (args.Count == 1) return Math.Round(args[0]);
                        if (args.Count == 2) return Math.Round(args[0], (int)args[1]);
                        throw new ArgumentException("round() requires 1 or 2 arguments.");

                    case "trunc":
                    case "truncate":
                        if (args.Count != 1) throw new ArgumentException("trunc() requires 1 argument: trunc(value).");
                        return Math.Truncate(args[0]);

                    case "abs":
                        if (args.Count != 1) throw new ArgumentException("abs() requires 1 argument: abs(value).");
                        return Math.Abs(args[0]);

                    case "sqrt":
                        if (args.Count != 1) throw new ArgumentException("sqrt() requires 1 argument: sqrt(value).");
                        if (args[0] < 0) throw new ArgumentException("sqrt() argument cannot be negative.");
                        return Math.Sqrt(args[0]);

                    case "mod":
                        if (args.Count != 2) throw new ArgumentException("mod() requires 2 arguments: mod(a, b).");
                        if (Math.Abs(args[1]) < double.Epsilon) throw new DivideByZeroException("mod() divisor cannot be zero.");
                        return args[0] % args[1];

                    case "pow":
                        if (args.Count != 2) throw new ArgumentException("pow() requires 2 arguments: pow(base, exponent).");
                        return Math.Pow(args[0], args[1]);

                    case "exp":
                        if (args.Count != 1) throw new ArgumentException("exp() requires 1 argument: exp(x).");
                        return Math.Exp(args[0]);

                    case "log":
                    case "ln":
                        if (args.Count == 1)
                        {
                            if (args[0] <= 0) throw new ArgumentException("log() argument must be greater than zero.");
                            return Math.Log(args[0]);
                        }
                        if (args.Count == 2)
                        {
                            if (args[0] <= 0 || args[1] <= 0 || Math.Abs(args[1] - 1.0) < double.Epsilon) throw new ArgumentException("log(val, newBase) arguments must be valid.");
                            return Math.Log(args[0], args[1]);
                        }
                        throw new ArgumentException("log() requires 1 or 2 arguments.");

                    case "log10":
                        if (args.Count != 1) throw new ArgumentException("log10() requires 1 argument: log10(x).");
                        if (args[0] <= 0) throw new ArgumentException("log10() argument must be greater than zero.");
                        return Math.Log10(args[0]);

                    case "sign":
                        if (args.Count != 1) throw new ArgumentException("sign() requires 1 argument: sign(x).");
                        return Math.Sign(args[0]);

                    case "dndmod":
                    case "statmod":
                        if (args.Count != 1) throw new ArgumentException("dndmod() requires 1 argument: dndmod(score).");
                        return Math.Floor((args[0] - 10.0) / 2.0);

                    case "if":
                    case "cond":
                    case "choose":
                        if (args.Count != 3) throw new ArgumentException("if() requires 3 arguments: if(condition, trueValue, falseValue).");
                        return Math.Abs(args[0]) > double.Epsilon ? args[1] : args[2];

                    default:
                        throw new NotSupportedException($"Unknown function '{funcName}()'.");
                }
            }
        }
    }
}
