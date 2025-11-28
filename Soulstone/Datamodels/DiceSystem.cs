using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Soulstone.Datamodels
{
    public enum DiceType
    {
        d4 = 0,
        d6 = 1,
        d8 = 2,
        d10 = 3,
        d12 = 4,
        d20 = 5,
        d100 = 6
    }
    internal class DiceSystem
    {
        public string systemName = "Standard Dice System";

        public bool dicePoolSystemEnabled = false;
        public bool regularDiceSystemEnabled = true;
        public bool dndStyleAttributes = true;
        public bool skillLinkedToOneAttribute = true;
        public bool abilityLinkedToOneAttribute = true; //This one and the following are not mutually exclusive
        public bool abilityLinkedToOneSkill = true;
        public bool systemHasSaves = true;
        public bool systemHasAdvantageDisadvantage = true;

        public DiceType diceType = DiceType.d20;

        public int successThreshold = 0;

        public string SystemName { get => systemName; set => systemName = value; }
        public bool DicePoolSystemEnabled { get => dicePoolSystemEnabled; set => dicePoolSystemEnabled = value; }
        public bool RegularDiceSystemEnabled { get => regularDiceSystemEnabled; set => regularDiceSystemEnabled = value; }
        public DiceType DiceType { get => diceType; set => diceType = value; }
        public int SuccessThreshold { get => successThreshold; set => successThreshold = value; }
        public bool DndStyleAttributes { get => dndStyleAttributes; set => dndStyleAttributes = value; }
        public bool SkillLinkedToOneAttribute { get => skillLinkedToOneAttribute; set => skillLinkedToOneAttribute = value; }
        public bool AbilityLinkedToOneAttribute { get => abilityLinkedToOneAttribute; set => abilityLinkedToOneAttribute = value; }
        public bool AbilityLinkedToOneSkill { get => abilityLinkedToOneSkill; set => abilityLinkedToOneSkill = value; }
        public bool SystemHasSaves { get => systemHasSaves; set => systemHasSaves = value; }
        public bool SystemHasAdvantageDisadvantage { get => systemHasAdvantageDisadvantage; set => systemHasAdvantageDisadvantage = value; }

        public static DiceSystem LoadDiceSystem(string systemName)
        {
            if (File.Exists($"{Plugin.dataLocation}/diceSystem/{systemName}.json"))
            {
                return JsonSerializer.Deserialize<DiceSystem>(File.ReadAllText($"{Plugin.dataLocation}/diceSystem/{systemName}.json"));
            }
            else
            {
                Plugin.Log.Information("No existing dice system found, creating a new one.");
                DiceSystem newSystem = new DiceSystem();
                SaveDiceSystem(newSystem);
                return newSystem;
            }
        }

        public static void SaveDiceSystem(DiceSystem system)
        {
            if (!Directory.Exists($"{Plugin.dataLocation}/diceSystem"))
            {
                Directory.CreateDirectory($"{Plugin.dataLocation}/diceSystem");
            }
            string systemName = system.SystemName.Replace(" ", "_").ToLower();
            File.WriteAllText($"{Plugin.dataLocation}/diceSystem/{systemName}.json", JsonSerializer.Serialize(system));
        }
    }
}
