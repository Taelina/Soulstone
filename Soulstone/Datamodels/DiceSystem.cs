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

    public enum SystemType
    {
        DnDSystem = 0,
        DicePoolSystem = 1,
        PercentileSystem = 2
    }

    public enum InitiativeStatType
    {
        None = 0,
        Attribute = 1,
        Skill = 2
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
        public bool systemHasManaOrResourcePoints = false;
        public bool systemHasClasses = false;
        public bool systemHasBonusTemp = false;
        public bool systemHasBonusPerm = false;
        public bool systemHasEpicAttributes = false;
        public bool systemHasInventoryLimit = false;
        public int inventoryMaxSlots = 30;

        public bool systemHasAugmentations = false;
        public string augmentationTitle = "Cyberware & Implants";
        public List<string> customAugmentationSlots = new();

        public List<ResourceDefinition> systemResources = new();
        public List<string> customEquipmentSlots = new();

        public InitiativeStatType initiativeStatType = InitiativeStatType.None;
        public string initiativeStatName = string.Empty;

        public DiceType diceType = DiceType.d20;
        public SystemType systemType = SystemType.DnDSystem;

        public int successThreshold = 0;
        public int successInterval = 0;

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
        public SystemType SystemType { get => systemType; set => systemType = value; }
        public int SuccessInterval { get => successInterval; set => successInterval = value; }
        public bool SystemHasManaOrResourcePoints { get => systemHasManaOrResourcePoints; set => systemHasManaOrResourcePoints = value; }
        public bool SystemHasClasses { get => systemHasClasses; set => systemHasClasses = value; }
        public bool SystemHasBonusTemp { get => systemHasBonusTemp; set => systemHasBonusTemp = value; }
        public bool SystemHasBonusPerm { get => systemHasBonusPerm; set => systemHasBonusPerm = value; }
        public bool SystemHasEpicAttributes { get => systemHasEpicAttributes; set => systemHasEpicAttributes = value; }
        public bool SystemHasInventoryLimit { get => systemHasInventoryLimit; set => systemHasInventoryLimit = value; }
        public int InventoryMaxSlots { get => inventoryMaxSlots; set => inventoryMaxSlots = value; }
        public bool SystemHasAugmentations { get => systemHasAugmentations; set => systemHasAugmentations = value; }
        public string AugmentationTitle { get => augmentationTitle; set => augmentationTitle = value; }
        public List<string> CustomAugmentationSlots { get => customAugmentationSlots; set => customAugmentationSlots = value; }
        public List<ResourceDefinition> SystemResources { get => systemResources; set => systemResources = value; }
        public List<string> CustomEquipmentSlots { get => customEquipmentSlots; set => customEquipmentSlots = value; }
        public InitiativeStatType InitiativeStatType { get => initiativeStatType; set => initiativeStatType = value; }
        public string InitiativeStatName { get => initiativeStatName; set => initiativeStatName = value; }

        public List<ResourceDefinition> GetEffectiveResources()
        {
            if (systemResources != null && systemResources.Count > 0)
            {
                return systemResources;
            }

            var defaults = new List<ResourceDefinition>
            {
                new ResourceDefinition("Health", 100, 100, "#2ecc71", "Health Points", isRequired: true)
            };

            if (systemHasManaOrResourcePoints)
            {
                defaults.Add(new ResourceDefinition("Mana", 100, 100, "#3498db", "Mana Points"));
            }

            return defaults;
        }

        public void AddResource(ResourceDefinition resource)
        {
            systemResources ??= new List<ResourceDefinition>();
            var existing = systemResources.FirstOrDefault(r => string.Equals(r.Name, resource.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                systemResources.Remove(existing);
            }
            systemResources.Add(resource);
        }

        public bool RemoveResource(string resourceName)
        {
            if (systemResources == null) return false;
            var existing = systemResources.FirstOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                return systemResources.Remove(existing);
            }
            return false;
        }

        public List<string> GetEffectiveEquipmentSlots()
        {
            if (customEquipmentSlots != null && customEquipmentSlots.Count > 0)
            {
                return customEquipmentSlots;
            }
            return GearItem.StandardSlots.ToList();
        }

        public List<string> GetEffectiveAugmentationSlots()
        {
            if (customAugmentationSlots != null && customAugmentationSlots.Count > 0)
            {
                return customAugmentationSlots;
            }
            return GearItem.StandardAugmentationSlots.ToList();
        }

        public static DiceSystem? LoadDiceSystem(string systemName, bool isFullPath = false)
        {
            string path = isFullPath ? systemName : $"{Plugin.dataLocation}/diceSystem/{systemName}.json";
            if (File.Exists(path))
            {
                Plugin.Log.Information($"Loading existing dice system from {path}");
                return JsonSerializer.Deserialize<DiceSystem>(File.ReadAllText(path));
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
            File.WriteAllText($"{Plugin.dataLocation}/diceSystem/{systemName}.json", JsonSerializer.Serialize(system, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
