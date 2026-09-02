using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Soulstone.Datamodels;
using Soulstone.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            DiceRoll roll = new DiceRoll();
            Random rand = new Random();
            List<int> rolls = new List<int>();
            int rollResult = rand.Next(1,101);
            bool success = rollResult <= targetValue;
            int sucessOrFailureBy = Math.Abs(targetValue - rollResult) / successInterval;
            string successOrFailureStirng = success ? $"Sucess by : {sucessOrFailureBy}" : $"Failure by : {sucessOrFailureBy}" ; 
            roll.rollResult = rollResult;
            roll.rollResultString = $"Rolled {rollName} target : {targetValue} \n Roll : {rollResult} \n {successOrFailureStirng} ";
            roll.rollDetailedResultString = roll.rollResultString;
            rolls.Add(rollResult);
            roll.individualRolls = rolls;
            return roll;
        }

        // To be called when parsing a generic chat like dice roll string like "2d6" or "3d8+2"
        public static DiceRoll? ParseDiceRollString(string input, bool advantage = false, bool disadvantage = false)
        {
            DiceRoll? result = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            // Expected format: XdY where X is number of dice and Y is sides per die
            string[] bonus = input.ToLower().Split('+');
            string[] parts = bonus[0].ToLower().Split('d');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int numberOfDice) &&
                int.TryParse(parts[1], out int sidesPerDie) &&
                numberOfDice > 0 && sidesPerDie > 0)
            {
                if (bonus.Length == 2)
                {
                    if (int.TryParse(bonus[1], out int addedValue))
                    {
                        result = RollDiceRegular(numberOfDice, sidesPerDie, addedValue, "", advantage, disadvantage);
                    }
                    else
                    {
                        Plugin.Log?.Information("Invalid bonus format. Bonus must be an integer.");
                    }
                }
                else if (bonus.Length == 1)
                {
                    result = RollDiceRegular(numberOfDice, sidesPerDie, 0, "", advantage, disadvantage);
                }
                else
                {
                    Plugin.Log?.Information("Invalid dice roll format. Too many '+' characters.");
                }
            }
            else
            {
                Plugin.Log?.Information("Invalid dice roll format. Use XdY (e.g., 2d6 for two six-sided dice).");
            }
            return result;
        }

        public static void RollDice(int numberOfDice, int addedValue = 0, bool advantage = false, bool disadvantage = false, string rollName = "", bool detailedRoll = false, int target = 0, int rawSuccesses = 0)
        {
            DiceSystem? currentDiceSystem = DiceSystemManager.Instance.CurrentDiceSystem;
            string diceType = currentDiceSystem != null ? (Enum.GetName<DiceType>(currentDiceSystem.DiceType) ?? "d20") : "d20";
            string[] parsedType = diceType.Split('d');
            int parsedSides = parsedType.Length > 1 && int.TryParse(parsedType[1], out int sides) ? sides : 20;
            DiceRoll? roll = null;

            SystemType sysType = currentDiceSystem?.systemType ?? SystemType.DnDSystem;
            switch (sysType)
            {
                case SystemType.DnDSystem:
                    Plugin.Log?.Information($"Rolling 1d{parsedSides} + {addedValue}");
                    roll = DiceRoll.RollDiceRegular(1, parsedSides, addedValue, rollName, advantage, disadvantage);
                    break;
                case SystemType.DicePoolSystem:
                    int threshold = currentDiceSystem?.SuccessThreshold ?? 8;
                    Plugin.Log?.Information($"Rolling {numberOfDice}d{parsedSides} against success threshold {threshold} with {rawSuccesses} epic bonus");
                    roll = RollDicePool(numberOfDice, parsedSides, threshold, rollName, rawSuccesses);
                    break;
                case SystemType.PercentileSystem:
                    int interval = currentDiceSystem?.successInterval ?? 10;
                    Plugin.Log?.Information($"Rolling 1d100 against target {target}");
                    roll = RollDicePercentile(target, rollName, interval);
                    break;
                default:
                    roll = DiceRoll.RollDiceRegular(1, parsedSides, addedValue, rollName, advantage, disadvantage);
                    break;
            }
            
            if (roll != null)
            {
                if (!detailedRoll)
                {
                    XivChatEntry rollMessage = new XivChatEntry
                    {
                        Message = roll.RollResultString,
                        Type = XivChatType.Echo
                    };
                    Messages.SendMessage(rollMessage);
                }
                else
                {
                    XivChatEntry rollMessage = new XivChatEntry
                    {
                        Message = roll.RollDetailedResultString,
                        Type = XivChatType.Echo
                    };
                    Messages.SendMessage(rollMessage);
                }
            }
        }
    }
}
