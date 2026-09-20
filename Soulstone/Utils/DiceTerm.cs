using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Soulstone.Utils
{
    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual
    }

    public class DiceCondition
    {
        public ComparisonOperator Operator { get; set; } = ComparisonOperator.Equal;
        public int TargetValue { get; set; }

        public bool Matches(int value)
        {
            return Operator switch
            {
                ComparisonOperator.Equal => value == TargetValue,
                ComparisonOperator.NotEqual => value != TargetValue,
                ComparisonOperator.GreaterThan => value > TargetValue,
                ComparisonOperator.GreaterThanOrEqual => value >= TargetValue,
                ComparisonOperator.LessThan => value < TargetValue,
                ComparisonOperator.LessThanOrEqual => value <= TargetValue,
                _ => false
            };
        }

        public string Format()
        {
            string opStr = Operator switch
            {
                ComparisonOperator.Equal => "=",
                ComparisonOperator.NotEqual => "!=",
                ComparisonOperator.GreaterThan => ">",
                ComparisonOperator.GreaterThanOrEqual => ">=",
                ComparisonOperator.LessThan => "<",
                ComparisonOperator.LessThanOrEqual => "<=",
                _ => "="
            };
            return $"{opStr}{TargetValue}";
        }
    }

    public class SingleDieResult
    {
        public int Value { get; set; }
        public int InitialValue { get; set; }
        public bool IsDropped { get; set; }
        public bool IsRerolled { get; set; }
        public bool IsExploded { get; set; }
    }

    public class DiceTermResult
    {
        public string TermFormula { get; set; } = string.Empty;
        public int TotalValue { get; set; }
        public List<int> KeptRolls { get; set; } = new List<int>();
        public List<int> DroppedRolls { get; set; } = new List<int>();
        public List<int> AllRolls { get; set; } = new List<int>();
        public List<SingleDieResult> DetailedDice { get; set; } = new List<SingleDieResult>();
        public string DetailedBreakdown { get; set; } = string.Empty;
        public bool IsSuccessCount { get; set; }
    }

    public class DiceFormulaResult
    {
        public bool Success { get; set; }
        public double Total { get; set; }
        public int TotalInt => (int)Math.Round(Total);
        public List<int> IndividualRolls { get; set; } = new List<int>();
        public List<DiceTermResult> TermResults { get; set; } = new List<DiceTermResult>();
        public string DetailedBreakdown { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
    }

    public class DiceTerm
    {
        public int Count { get; set; } = 1;
        public int Sides { get; set; } = 20;
        public bool IsFudge { get; set; }
        public bool IsPercentile { get; set; }

        // Keep / Drop modifiers
        public int? KeepHighest { get; set; }
        public int? KeepLowest { get; set; }
        public int? DropHighest { get; set; }
        public int? DropLowest { get; set; }

        // Reroll modifiers
        public DiceCondition? RerollCondition { get; set; }
        public bool RerollOnce { get; set; }

        // Exploding modifiers
        public DiceCondition? ExplodeCondition { get; set; }
        public bool ExplodeOnce { get; set; }
        public bool ExplodeCompound { get; set; }

        // Clamping modifiers (min/max per die)
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }

        // Target Success / Failure counting modifiers
        public DiceCondition? SuccessCondition { get; set; }
        public DiceCondition? FailureCondition { get; set; }

        public string RawFormula { get; set; } = string.Empty;

        private static readonly Regex DiceTermPattern = new Regex(
            @"^(?<count>\d+)?d(?<sides>\d+|%|f)(?<mods>.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ModifierPattern = new Regex(
            @"(?<kh>kh(?<kh_count>\d+)?)|(?<kl>kl(?<kl_count>\d+)?)|(?<dh>dh(?<dh_count>\d+)?)|(?<dl>dl(?<dl_count>\d+)?)|(?<k>k(?<k_count>\d+)?)|(?<d>d(?<d_count>\d+)?)|(?<ro>ro(?<ro_op><=|>=|<>|!=|<|>|=)?(?<ro_val>\d+)?)|(?<r>r(?<r_op><=|>=|<>|!=|<|>|=)?(?<r_val>\d+)?)|(?<compound>!!(?<compound_op><=|>=|<>|!=|<|>|=)?(?<compound_val>\d+)?)|(?<explode_once>!o(?<explode_once_op><=|>=|<>|!=|<|>|=)?(?<explode_once_val>\d+)?)|(?<explode>!(?<explode_op><=|>=|<>|!=|<|>|=)?(?<explode_val>\d+)?)|(?<min>min(?<min_val>\d+))|(?<max>max(?<max_val>\d+))|(?<cs>cs(?<cs_op><=|>=|<>|!=|<|>|=)?(?<cs_val>\d+))|(?<cf>cf(?<cf_op><=|>=|<>|!=|<|>|=)?(?<cf_val>\d+))|(?<direct_cmp>(?<cmp_op><=|>=|<>|!=|<|>|=)(?<cmp_val>\d+))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool TryParse(string input, out DiceTerm? term)
        {
            term = null;
            if (string.IsNullOrWhiteSpace(input)) return false;

            var match = DiceTermPattern.Match(input.Trim());
            if (!match.Success) return false;

            int count = 1;
            if (match.Groups["count"].Success && !string.IsNullOrEmpty(match.Groups["count"].Value))
            {
                if (!int.TryParse(match.Groups["count"].Value, out count) || count <= 0)
                    return false;
            }

            string sidesStr = match.Groups["sides"].Value;
            bool isPercentile = false;
            bool isFudge = false;
            int sides = 20;

            if (sidesStr.Equals("%", StringComparison.OrdinalIgnoreCase))
            {
                isPercentile = true;
                sides = 100;
            }
            else if (sidesStr.Equals("f", StringComparison.OrdinalIgnoreCase))
            {
                isFudge = true;
                sides = 3;
            }
            else
            {
                if (!int.TryParse(sidesStr, out sides) || sides <= 0)
                    return false;
            }

            var result = new DiceTerm
            {
                Count = count,
                Sides = sides,
                IsPercentile = isPercentile,
                IsFudge = isFudge,
                RawFormula = input.Trim()
            };

            string modsStr = match.Groups["mods"].Value;
            if (!string.IsNullOrEmpty(modsStr))
            {
                int processedLength = 0;
                var modMatches = ModifierPattern.Matches(modsStr);
                foreach (Match m in modMatches)
                {
                    if (m.Index != processedLength)
                    {
                        return false; // Unknown characters in modifiers
                    }
                    processedLength += m.Length;

                    if (m.Groups["kh"].Success)
                    {
                        int kCount = m.Groups["kh_count"].Success && int.TryParse(m.Groups["kh_count"].Value, out int kc) ? kc : 1;
                        result.KeepHighest = kCount;
                    }
                    else if (m.Groups["k"].Success)
                    {
                        int kCount = m.Groups["k_count"].Success && int.TryParse(m.Groups["k_count"].Value, out int kc) ? kc : 1;
                        result.KeepHighest = kCount;
                    }
                    else if (m.Groups["kl"].Success)
                    {
                        int kCount = m.Groups["kl_count"].Success && int.TryParse(m.Groups["kl_count"].Value, out int kc) ? kc : 1;
                        result.KeepLowest = kCount;
                    }
                    else if (m.Groups["dh"].Success)
                    {
                        int dCount = m.Groups["dh_count"].Success && int.TryParse(m.Groups["dh_count"].Value, out int dc) ? dc : 1;
                        result.DropHighest = dCount;
                    }
                    else if (m.Groups["dl"].Success)
                    {
                        int dCount = m.Groups["dl_count"].Success && int.TryParse(m.Groups["dl_count"].Value, out int dc) ? dc : 1;
                        result.DropLowest = dCount;
                    }
                    else if (m.Groups["d"].Success)
                    {
                        int dCount = m.Groups["d_count"].Success && int.TryParse(m.Groups["d_count"].Value, out int dc) ? dc : 1;
                        result.DropLowest = dCount;
                    }
                    else if (m.Groups["ro"].Success)
                    {
                        result.RerollOnce = true;
                        var op = ParseOperator(m.Groups["ro_op"].Value, ComparisonOperator.Equal);
                        int val = m.Groups["ro_val"].Success && int.TryParse(m.Groups["ro_val"].Value, out int rv) ? rv : (isFudge ? -1 : 1);
                        result.RerollCondition = new DiceCondition { Operator = op, TargetValue = val };
                    }
                    else if (m.Groups["r"].Success)
                    {
                        result.RerollOnce = false;
                        var op = ParseOperator(m.Groups["r_op"].Value, ComparisonOperator.Equal);
                        int val = m.Groups["r_val"].Success && int.TryParse(m.Groups["r_val"].Value, out int rv) ? rv : (isFudge ? -1 : 1);
                        result.RerollCondition = new DiceCondition { Operator = op, TargetValue = val };
                    }
                    else if (m.Groups["compound"].Success)
                    {
                        result.ExplodeCompound = true;
                        var op = ParseOperator(m.Groups["compound_op"].Value, ComparisonOperator.Equal);
                        int val = m.Groups["compound_val"].Success && int.TryParse(m.Groups["compound_val"].Value, out int cv) ? cv : sides;
                        result.ExplodeCondition = new DiceCondition { Operator = op, TargetValue = val };
                    }
                    else if (m.Groups["explode_once"].Success)
                    {
                        result.ExplodeOnce = true;
                        var op = ParseOperator(m.Groups["explode_once_op"].Value, ComparisonOperator.Equal);
                        int val = m.Groups["explode_once_val"].Success && int.TryParse(m.Groups["explode_once_val"].Value, out int ev) ? ev : sides;
                        result.ExplodeCondition = new DiceCondition { Operator = op, TargetValue = val };
                    }
                    else if (m.Groups["explode"].Success)
                    {
                        var op = ParseOperator(m.Groups["explode_op"].Value, ComparisonOperator.Equal);
                        int val = m.Groups["explode_val"].Success && int.TryParse(m.Groups["explode_val"].Value, out int ev) ? ev : sides;
                        result.ExplodeCondition = new DiceCondition { Operator = op, TargetValue = val };
                    }
                    else if (m.Groups["min"].Success)
                    {
                        if (int.TryParse(m.Groups["min_val"].Value, out int minV))
                            result.MinValue = minV;
                    }
                    else if (m.Groups["max"].Success)
                    {
                        if (int.TryParse(m.Groups["max_val"].Value, out int maxV))
                            result.MaxValue = maxV;
                    }
                    else if (m.Groups["cs"].Success)
                    {
                        var op = ParseOperator(m.Groups["cs_op"].Value, ComparisonOperator.GreaterThanOrEqual);
                        if (int.TryParse(m.Groups["cs_val"].Value, out int csV))
                            result.SuccessCondition = new DiceCondition { Operator = op, TargetValue = csV };
                    }
                    else if (m.Groups["cf"].Success)
                    {
                        var op = ParseOperator(m.Groups["cf_op"].Value, ComparisonOperator.LessThanOrEqual);
                        if (int.TryParse(m.Groups["cf_val"].Value, out int cfV))
                            result.FailureCondition = new DiceCondition { Operator = op, TargetValue = cfV };
                    }
                    else if (m.Groups["direct_cmp"].Success)
                    {
                        var op = ParseOperator(m.Groups["cmp_op"].Value, ComparisonOperator.GreaterThanOrEqual);
                        if (int.TryParse(m.Groups["cmp_val"].Value, out int cmpV))
                            result.SuccessCondition = new DiceCondition { Operator = op, TargetValue = cmpV };
                    }
                }

                if (processedLength != modsStr.Length)
                {
                    return false;
                }
            }

            term = result;
            return true;
        }

        private static ComparisonOperator ParseOperator(string opStr, ComparisonOperator defaultOp)
        {
            return opStr switch
            {
                ">=" => ComparisonOperator.GreaterThanOrEqual,
                "<=" => ComparisonOperator.LessThanOrEqual,
                ">" => ComparisonOperator.GreaterThan,
                "<" => ComparisonOperator.LessThan,
                "==" or "=" => ComparisonOperator.Equal,
                "!=" or "<>" => ComparisonOperator.NotEqual,
                _ => defaultOp
            };
        }

        public DiceTermResult Roll(Random? rand = null, bool advantage = false, bool disadvantage = false)
        {
            rand ??= new Random();
            var termResult = new DiceTermResult
            {
                TermFormula = RawFormula
            };

            int effectiveCount = Count;
            int? effectiveKeepHighest = KeepHighest;
            int? effectiveKeepLowest = KeepLowest;

            if (KeepHighest == null && KeepLowest == null && DropHighest == null && DropLowest == null)
            {
                if (advantage && !disadvantage && Count == 1)
                {
                    effectiveCount = 2;
                    effectiveKeepHighest = 1;
                }
                else if (disadvantage && !advantage && Count == 1)
                {
                    effectiveCount = 2;
                    effectiveKeepLowest = 1;
                }
            }

            var dicePool = new List<SingleDieResult>();

            int RollSingleDie()
            {
                return IsFudge ? rand.Next(-1, 2) : rand.Next(1, Sides + 1);
            }

            for (int i = 0; i < effectiveCount; i++)
            {
                int r = RollSingleDie();
                termResult.AllRolls.Add(r);

                // Handle reroll
                if (RerollCondition != null && RerollCondition.Matches(r))
                {
                    if (RerollOnce)
                    {
                        r = RollSingleDie();
                        termResult.AllRolls.Add(r);
                    }
                    else
                    {
                        int rerolls = 0;
                        while (RerollCondition.Matches(r) && rerolls < 100)
                        {
                            rerolls++;
                            r = RollSingleDie();
                            termResult.AllRolls.Add(r);
                        }
                    }
                }

                int finalVal = r;
                if (MinValue.HasValue && finalVal < MinValue.Value) finalVal = MinValue.Value;
                if (MaxValue.HasValue && finalVal > MaxValue.Value) finalVal = MaxValue.Value;

                // Handle exploding
                if (ExplodeCompound && ExplodeCondition != null && ExplodeCondition.Matches(r))
                {
                    int compoundTotal = finalVal;
                    int explodes = 0;
                    int currRoll = r;
                    while (ExplodeCondition.Matches(currRoll) && explodes < 100)
                    {
                        explodes++;
                        currRoll = RollSingleDie();
                        termResult.AllRolls.Add(currRoll);
                        int applied = currRoll;
                        if (MinValue.HasValue && applied < MinValue.Value) applied = MinValue.Value;
                        if (MaxValue.HasValue && applied > MaxValue.Value) applied = MaxValue.Value;
                        compoundTotal += applied;
                        if (ExplodeOnce) break;
                    }
                    dicePool.Add(new SingleDieResult
                    {
                        Value = compoundTotal,
                        InitialValue = r,
                        IsExploded = explodes > 0
                    });
                }
                else
                {
                    dicePool.Add(new SingleDieResult
                    {
                        Value = finalVal,
                        InitialValue = r,
                        IsExploded = false
                    });

                    if (ExplodeCondition != null && ExplodeCondition.Matches(r) && !IsFudge)
                    {
                        int explodes = 0;
                        int currRoll = r;
                        while (ExplodeCondition.Matches(currRoll) && explodes < 100)
                        {
                            explodes++;
                            currRoll = RollSingleDie();
                            termResult.AllRolls.Add(currRoll);
                            int extraVal = currRoll;
                            if (MinValue.HasValue && extraVal < MinValue.Value) extraVal = MinValue.Value;
                            if (MaxValue.HasValue && extraVal > MaxValue.Value) extraVal = MaxValue.Value;

                            dicePool.Add(new SingleDieResult
                            {
                                Value = extraVal,
                                InitialValue = currRoll,
                                IsExploded = true
                            });

                            if (ExplodeOnce) break;
                        }
                    }
                }
            }

            // Handle Keep / Drop
            if (effectiveKeepHighest.HasValue)
            {
                int k = Math.Clamp(effectiveKeepHighest.Value, 0, dicePool.Count);
                var ordered = dicePool.Select((d, idx) => new { Die = d, Index = idx })
                                      .OrderByDescending(x => x.Die.Value)
                                      .ToList();
                for (int i = k; i < ordered.Count; i++)
                {
                    ordered[i].Die.IsDropped = true;
                }
            }
            else if (effectiveKeepLowest.HasValue)
            {
                int k = Math.Clamp(effectiveKeepLowest.Value, 0, dicePool.Count);
                var ordered = dicePool.Select((d, idx) => new { Die = d, Index = idx })
                                      .OrderBy(x => x.Die.Value)
                                      .ToList();
                for (int i = k; i < ordered.Count; i++)
                {
                    ordered[i].Die.IsDropped = true;
                }
            }
            else if (DropHighest.HasValue)
            {
                int d = Math.Clamp(DropHighest.Value, 0, dicePool.Count);
                var ordered = dicePool.Select((d, idx) => new { Die = d, Index = idx })
                                      .OrderByDescending(x => x.Die.Value)
                                      .ToList();
                for (int i = 0; i < d; i++)
                {
                    ordered[i].Die.IsDropped = true;
                }
            }
            else if (DropLowest.HasValue)
            {
                int d = Math.Clamp(DropLowest.Value, 0, dicePool.Count);
                var ordered = dicePool.Select((d, idx) => new { Die = d, Index = idx })
                                      .OrderBy(x => x.Die.Value)
                                      .ToList();
                for (int i = 0; i < d; i++)
                {
                    ordered[i].Die.IsDropped = true;
                }
            }

            termResult.DetailedDice = dicePool;
            termResult.KeptRolls = dicePool.Where(d => !d.IsDropped).Select(d => d.Value).ToList();
            termResult.DroppedRolls = dicePool.Where(d => d.IsDropped).Select(d => d.Value).ToList();

            // Calculate total
            if (SuccessCondition != null)
            {
                termResult.IsSuccessCount = true;
                int successes = 0;
                foreach (var die in dicePool.Where(d => !d.IsDropped))
                {
                    if (SuccessCondition.Matches(die.Value)) successes++;
                    if (FailureCondition != null && FailureCondition.Matches(die.Value)) successes--;
                }
                termResult.TotalValue = successes;
            }
            else
            {
                termResult.IsSuccessCount = false;
                termResult.TotalValue = dicePool.Where(d => !d.IsDropped).Sum(d => d.Value);
            }

            // Format breakdown
            var formattedDice = new List<string>();
            foreach (var die in dicePool)
            {
                string valStr = die.Value.ToString();
                if (IsFudge)
                {
                    valStr = die.Value switch
                    {
                        1 => "+",
                        -1 => "-",
                        _ => "0"
                    };
                }

                if (die.IsDropped)
                {
                    formattedDice.Add($"~{valStr}~");
                }
                else if (SuccessCondition != null && SuccessCondition.Matches(die.Value))
                {
                    formattedDice.Add($"{valStr}*");
                }
                else
                {
                    formattedDice.Add(valStr);
                }
            }

            string diceListStr = string.Join(", ", formattedDice);
            if (SuccessCondition != null)
            {
                termResult.DetailedBreakdown = $"{RawFormula} ({diceListStr}) = {termResult.TotalValue} successes";
            }
            else if (dicePool.Count > 1 || dicePool.Any(d => d.IsDropped || d.IsExploded))
            {
                termResult.DetailedBreakdown = $"{RawFormula} ({diceListStr})";
            }
            else
            {
                termResult.DetailedBreakdown = $"{RawFormula} ({diceListStr})";
            }

            return termResult;
        }
    }
}
