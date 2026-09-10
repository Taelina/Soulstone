using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Soulstone.Datamodels;
using Soulstone.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Soulstone.Utils
{
    internal class DiceRoll
    {
        private int rollResult;

        private SeString rollResultString = string.Empty;
        private SeString rollDetailedResultString = string.Empty;

        private List<int> individualRolls = new();

        public int RollResult { get => rollResult; set => rollResult = value; }
        public SeString RollResultString { get => rollResultString; set => rollResultString = value; }
        public SeString RollDetailedResultString { get => rollDetailedResultString; set => rollDetailedResultString = value; }
        public List<int> IndividualRolls { get => individualRolls; set => individualRolls = value; }

        //To be called for normal, dnd style dice rolls
        public static DiceRoll RollDiceRegular(int numberOfDice, int sidesPerDie, int addedValue = 0, string rollName = "", bool advantage = false, bool disadvantage = false)
        {
            DiceRoll diceRoll = new DiceRoll();
            Random rand = new Random();
            List<int> rolls = new List<int>();
            int total = 0;
            for (int i = 0; i < numberOfDice; i++)
            {
                int roll, roll1, roll2;
                if (!advantage && !disadvantage)
                {
                    roll = rand.Next(1, sidesPerDie + 1);
                    rolls.Add(roll);
                    total += roll;
                }
                else if(advantage)
                {
                    roll1 = rand.Next(1, sidesPerDie + 1);
                    roll2 = rand.Next(1, sidesPerDie + 1);
                    roll = Math.Max(roll1, roll2);
                    rolls.Add(roll);
                    total += roll;
                }
                else if(disadvantage)
                {
                    roll1 = rand.Next(1, sidesPerDie + 1);
                    roll2 = rand.Next(1, sidesPerDie + 1);
                    roll = Math.Min(roll1, roll2);
                    rolls.Add(roll);
                    total += roll;
                }
            }
            total += addedValue;
            string rollResults = string.Join(", ", rolls);
            diceRoll.rollResult = total;
            if (addedValue > 0)
            {
                diceRoll.RollResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie} + {addedValue}:  Total: {total}";
                diceRoll.RollDetailedResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie} + {addedValue}: [{rollResults}] Total: {total}";
            }
            else if (addedValue < 0)
            {
                diceRoll.RollResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie} - {Math.Abs(addedValue)}:  Total: {total}";
                diceRoll.RollDetailedResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie} - {Math.Abs(addedValue)}: [{rollResults}] Total: {total}";
            }
            else
            {
                diceRoll.RollResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie}: Total: {total}";
                diceRoll.RollDetailedResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie}: [{rollResults}] Total: {total}";
            }
            diceRoll.individualRolls = rolls;
            return diceRoll;
        }

        // To be called for dice pool style rolls where each die that meets or exceeds a threshold counts as a success
        public static DiceRoll RollDicePool(int numberOfDice, int sidesPerDie, int successThreshold, string rollName = "", int rawSuccesses = 0)
        {
            DiceRoll diceRoll = new DiceRoll();
            Random rand = new Random();
            List<int> rolls = new List<int>();
            int successes = 0;
            for (int i = 0; i < numberOfDice; i++)
            {
                int roll = rand.Next(1, sidesPerDie + 1);
                rolls.Add(roll);
                if (roll >= successThreshold)
                {
                    successes++;
                }
            }
            int totalSuccesses = successes + rawSuccesses;
            string rollResults = string.Join(", ", rolls);
            diceRoll.rollResult = totalSuccesses;
            if (rawSuccesses > 0)
            {
                diceRoll.RollResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie} (Success Threshold: {successThreshold}) + {rawSuccesses} epic bonus: Successes: {totalSuccesses}";
                diceRoll.RollDetailedResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie} (Success Threshold: {successThreshold}) + {rawSuccesses} epic bonus: [{rollResults}] Successes: {totalSuccesses}";
            }
            else
            {
                diceRoll.RollResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie} (Success Threshold: {successThreshold}): Successes: {totalSuccesses}";
                diceRoll.RollDetailedResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie} (Success Threshold: {successThreshold}): [{rollResults}] Successes: {totalSuccesses}";
            }
            diceRoll.individualRolls = rolls;
            return diceRoll;
        }

        public static DiceRoll RollDicePercentile(int targetValue, string rollName = "", int successInterval = 0)
        {
            try
            {
                DiceRoll roll = new DiceRoll();
                Random rand = new Random();
                List<int> rolls = new List<int>();
                int rollResult = rand.Next(1, 101);
                bool success = rollResult <= targetValue;
                int interval = successInterval > 0 ? successInterval : 10;
                int successOrFailureBy = Math.Abs(targetValue - rollResult) / interval;
                string successOrFailureString = success ? $"Sucess by : {successOrFailureBy}" : $"Failure by : {successOrFailureBy}";
                roll.rollResult = rollResult;
                roll.rollResultString = $"Rolled {rollName} target : {targetValue} \n Roll : {rollResult} \n {successOrFailureString} ";
                roll.rollDetailedResultString = roll.rollResultString;
                rolls.Add(rollResult);
                roll.individualRolls = rolls;
                return roll;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed in RollDicePercentile for target {targetValue}");
                return new DiceRoll { RollResult = 0, RollResultString = "Percentile roll error" };
            }
        }

        // To be called when parsing a generic chat like dice roll string like "2d6", "3d8+2" or "1d20-1"
        public static DiceRoll? ParseDiceRollString(string input, bool advantage = false, bool disadvantage = false)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            try
            {
                // Expected format: XdY with an optional signed modifier, e.g. 2d6, 3d8+2, 1d20-1
                var match = Regex.Match(input.Replace(" ", string.Empty), @"^(\d+)d(\d+)([+-]\d+)?$", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    Plugin.Log?.Warning("Invalid dice roll format. Use XdY(+/-Z) (e.g., 2d6 for two six-sided dice).");
                    return null;
                }

                if (!int.TryParse(match.Groups[1].Value, out int numberOfDice) ||
                    !int.TryParse(match.Groups[2].Value, out int sidesPerDie) ||
                    numberOfDice <= 0 || sidesPerDie <= 0)
                {
                    Plugin.Log?.Warning("Invalid dice roll format. Dice count and sides must both be positive.");
                    return null;
                }

                int addedValue = 0;
                if (match.Groups[3].Success && !int.TryParse(match.Groups[3].Value, out addedValue))
                {
                    Plugin.Log?.Warning("Invalid bonus format in dice roll string. Bonus must be an integer.");
                    return null;
                }

                return RollDiceRegular(numberOfDice, sidesPerDie, addedValue, "", advantage, disadvantage);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to parse dice roll string: '{input}'");
                return null;
            }
        }

        // Resolves the number of sides configured on a dice system (d4 through d100).
        public static int GetSystemSides(DiceSystem? diceSystem)
        {
            string diceType = diceSystem != null ? (Enum.GetName<DiceType>(diceSystem.DiceType) ?? "d20") : "d20";
            string[] parsedType = diceType.Split('d');
            return parsedType.Length > 1 && int.TryParse(parsedType[1], out int sides) && sides > 0 ? sides : 20;
        }

        // Rolls according to the rules of a given dice system rather than a hardcoded d20.
        // As in CharStatsWindow, callers pass the same effective stat value as numberOfDice,
        // addedValue and target: each system type only consumes the one that applies to it.
        public static DiceRoll? RollWithSystem(DiceSystem? diceSystem, int numberOfDice, int addedValue = 0, bool advantage = false, bool disadvantage = false, string rollName = "", int target = 0, int rawSuccesses = 0)
        {
            int parsedSides = GetSystemSides(diceSystem);
            SystemType sysType = diceSystem?.systemType ?? SystemType.DnDSystem;

            switch (sysType)
            {
                case SystemType.DicePoolSystem:
                    int threshold = diceSystem?.SuccessThreshold ?? 8;
                    int poolSize = Math.Max(1, numberOfDice);
                    Plugin.Log?.Information($"Rolling {poolSize}d{parsedSides} against success threshold {threshold} with {rawSuccesses} epic bonus");
                    return RollDicePool(poolSize, parsedSides, threshold, rollName, rawSuccesses);
                case SystemType.PercentileSystem:
                    int interval = diceSystem?.successInterval ?? 10;
                    Plugin.Log?.Information($"Rolling 1d100 against target {target}");
                    return RollDicePercentile(target, rollName, interval);
                case SystemType.DnDSystem:
                default:
                    Plugin.Log?.Information($"Rolling 1d{parsedSides} + {addedValue}");
                    return RollDiceRegular(1, parsedSides, addedValue, rollName, advantage, disadvantage);
            }
        }

        // Rolls a single stat value under the active system: the value acts as a modifier in
        // d20-like systems, as the pool size in dice pool systems and as the percentile target.
        public static DiceRoll? RollStatWithSystem(DiceSystem? diceSystem, string rollName, int statValue, bool advantage = false, bool disadvantage = false, int rawSuccesses = 0)
        {
            return RollWithSystem(diceSystem, statValue, statValue, advantage, disadvantage, rollName, statValue, rawSuccesses);
        }

        // Human readable formula for a dice system, used for UI defaults and roll request labels.
        public static string DescribeSystemRoll(DiceSystem? diceSystem, int statValue = 0)
        {
            int sides = GetSystemSides(diceSystem);
            SystemType sysType = diceSystem?.systemType ?? SystemType.DnDSystem;

            switch (sysType)
            {
                case SystemType.DicePoolSystem:
                    return $"{Math.Max(1, statValue)}d{sides} >= {diceSystem?.SuccessThreshold ?? 8}";
                case SystemType.PercentileSystem:
                    return $"1d100 <= {statValue}";
                case SystemType.DnDSystem:
                default:
                    if (statValue > 0) return $"1d{sides}+{statValue}";
                    if (statValue < 0) return $"1d{sides}{statValue}";
                    return $"1d{sides}";
            }
        }

        public static void RollDice(int numberOfDice, int addedValue = 0, bool advantage = false, bool disadvantage = false, string rollName = "", bool detailedRoll = false, int target = 0, int rawSuccesses = 0)
        {
            try
            {
                DiceSystem? currentDiceSystem = DiceSystemManager.Instance.CurrentDiceSystem;
                DiceRoll? roll = RollWithSystem(currentDiceSystem, numberOfDice, addedValue, advantage, disadvantage, rollName, target, rawSuccesses);

                if (roll != null)
                {
                    string displayMsg = detailedRoll ? roll.RollDetailedResultString.TextValue : roll.RollResultString.TextValue;
                    PartySyncManager.Instance.BroadcastDiceRoll(
                        rollName,
                        roll.RollResult,
                        string.Join(", ", roll.IndividualRolls),
                        false,
                        false,
                        displayMsg
                    );
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to execute RollDice for '{rollName}'");
            }
        }
    }
}
