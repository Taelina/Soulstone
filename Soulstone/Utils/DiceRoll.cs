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

        private SeString rollResultString;
        private SeString rollDetailedResultString;

        private List<int> individualRolls;

        public SeString RollResultString { get => rollResultString; set => rollResultString = value; }
        public SeString RollDetailedResultString { get => rollDetailedResultString; set => rollDetailedResultString = value; }

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
            if (addedValue != 0)
            {
                diceRoll.RollResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie} + {addedValue}:  Total: {total}";
                diceRoll.RollDetailedResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie} + {addedValue}: [{rollResults}] Total: {total}";
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
        public static DiceRoll RollDicePool(int numberOfDice, int sidesPerDie, int successThreshold, string rollName = "")
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
            string rollResults = string.Join(", ", rolls);
            diceRoll.rollResult = successes;
            diceRoll.RollResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie} (Success Threshold: {successThreshold}): Successes: {successes}";
            diceRoll.RollDetailedResultString = $"Rolled {rollName} {numberOfDice}d{sidesPerDie} (Success Threshold: {successThreshold}): [{rollResults}] Successes: {successes}";
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
        public static DiceRoll ParseDiceRollString(string input, bool advantage = false, bool disadvantage = false)
        {
            DiceRoll result = null;
            // Expected format: XdY where X is number of dice and Y is sides per die
            string[] bonus = input.ToLower().Split('+');
            string[] parts = bonus[0].ToLower().Split('d');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int numberOfDice) &&
                int.TryParse(parts[1], out int sidesPerDie) &&
                numberOfDice > 0 && sidesPerDie > 0)
            {
                if (bonus.Length == 2 && int.TryParse(bonus[1], out int addedValue))
                {
                    result = RollDiceRegular(numberOfDice, sidesPerDie, addedValue,"", advantage, disadvantage);
                }
                else
                {
                    result = RollDiceRegular(numberOfDice, sidesPerDie,0,"0",advantage,disadvantage);
                }
            }
            else
            {
                Plugin.Log.Information("Invalid dice roll format. Use XdY (e.g., 2d6 for two six-sided dice).");
            }
            return result;
        }

        public static void RollDice(int numberOfDice, int addedValue = 0, bool advantage = false, bool disadvantage = false, string rollName = "", bool detailedRoll = false, int target = 0)
        {
            DiceSystem currentDiceSystem = DiceSystemManager.Instance.CurrentDiceSystem;
            string diceType = Enum.GetName(typeof(DiceType), DiceSystemManager.Instance.CurrentDiceSystem.DiceType);
            string[] parsedType = diceType.Split('d');
            int parsedSides = Convert.ToInt32(parsedType[1]);
            DiceRoll roll = null;

            switch (currentDiceSystem.systemType)
            {
                case SystemType.DnDSystem:
                    Plugin.Log.Information($"Rolling 1d{parsedSides} + {addedValue}");
                    roll = DiceRoll.RollDiceRegular(1, parsedSides, addedValue, rollName, advantage, disadvantage);
                    break;
                case SystemType.DicePoolSystem:
                    Plugin.Log.Information($"Rolling {numberOfDice}d{parsedSides} against success threshold {currentDiceSystem.SuccessThreshold}");
                    roll = RollDicePool(numberOfDice, parsedSides, currentDiceSystem.SuccessThreshold, rollName);
                    break;
                case SystemType.PercentileSystem:
                    Plugin.Log.Information($"Rolling 1d100 against target {target}");
                    roll = RollDicePercentile(target, rollName, currentDiceSystem.successInterval);
                    break;
                default:
                    break;
            }
            
            if (!detailedRoll)
            {
                XivChatEntry rollMessage = new XivChatEntry
                {
                    Message = roll.RollResultString,
                    Type = XivChatType.Say
                };
                Messages.SendMessage(rollMessage);
            }
            else
            {
                XivChatEntry rollMessage = new XivChatEntry
                {
                    Message = roll.RollDetailedResultString,
                    Type = XivChatType.Say
                };
                Messages.SendMessage(rollMessage);
            }
        }
    }
}
