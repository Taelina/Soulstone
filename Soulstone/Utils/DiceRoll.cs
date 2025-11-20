using Dalamud.Game.Text.SeStringHandling;
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

        public static DiceRoll RollDice(int numberOfDice, int sidesPerDie, int addedValue = 0)
        {
            DiceRoll diceRoll = new DiceRoll();
            Random rand = new Random();
            List<int> rolls = new List<int>();
            int total = 0;
            for (int i = 0; i < numberOfDice; i++)
            {
                int roll = rand.Next(1, sidesPerDie + 1);
                rolls.Add(roll);
                total += roll;
            }
            total += addedValue;
            string rollResults = string.Join(", ", rolls);
            Plugin.Log.Information($"Rolled {numberOfDice}d{sidesPerDie}: Total: {total}");
            diceRoll.rollResult = total;
            if (addedValue != 0)
            {
                diceRoll.RollResultString = $"Rolled {numberOfDice}d{sidesPerDie} + {addedValue}:  Total: {total}";
                diceRoll.RollDetailedResultString = $"Rolled {numberOfDice}d{sidesPerDie} + {addedValue}: [{rollResults}] Total: {total}";
            }
            else
            {
                diceRoll.RollResultString = $"Rolled {numberOfDice}d{sidesPerDie}: Total: {total}";
                diceRoll.RollDetailedResultString = $"Rolled {numberOfDice}d{sidesPerDie}: [{rollResults}] Total: {total}";
            }
            diceRoll.individualRolls = rolls;
            return diceRoll;
        }

        public static DiceRoll ParseDiceRollString(string input)
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
                    result = RollDice(numberOfDice, sidesPerDie, addedValue);
                }
                else
                {
                    result = RollDice(numberOfDice, sidesPerDie);
                }
            }
            else
            {
                Plugin.Log.Information("Invalid dice roll format. Use XdY (e.g., 2d6 for two six-sided dice).");
            }
            return result;
        }
    }
}
