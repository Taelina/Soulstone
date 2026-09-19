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
        Skill = 2,
        Formula = 3
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

        public Dictionary<string, Attribute> systemAttributes = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Skill> systemSkills = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Ability> systemAbilities = new(StringComparer.OrdinalIgnoreCase);

        public InitiativeStatType initiativeStatType = InitiativeStatType.None;
        public string initiativeStatName = string.Empty;
        public string initiativeFormula = string.Empty;
        public bool dynamicSkillAttributeLinking = false;

        public DiceType diceType = DiceType.d20;
        public SystemType systemType = SystemType.DnDSystem;

        public int successThreshold = 0;
        public int successInterval = 0;
        public int dicePoolMaxSuccessCount = 1;

        public DiceSystem()
        {
        }

        public string SystemName { get => systemName; set => systemName = value; }
        public bool DicePoolSystemEnabled { get => dicePoolSystemEnabled; set => dicePoolSystemEnabled = value; }
        public bool RegularDiceSystemEnabled { get => regularDiceSystemEnabled; set => regularDiceSystemEnabled = value; }
        public DiceType DiceType { get => diceType; set => diceType = value; }
        public int SuccessThreshold { get => successThreshold; set => successThreshold = value; }
        public int DicePoolMaxSuccessCount { get => dicePoolMaxSuccessCount; set => dicePoolMaxSuccessCount = value; }
        public bool DndStyleAttributes { get => dndStyleAttributes; set => dndStyleAttributes = value; }
        public bool SkillLinkedToOneAttribute { get => skillLinkedToOneAttribute; set => skillLinkedToOneAttribute = value; }
        public bool AbilityLinkedToOneAttribute { get => abilityLinkedToOneAttribute; set => abilityLinkedToOneAttribute = value; }
        public bool AbilityLinkedToOneSkill { get => abilityLinkedToOneSkill; set => abilityLinkedToOneSkill = value; }
        public bool SystemHasSaves { get => systemHasSaves; set => systemHasSaves = value; }
        public bool SystemHasAdvantageDisadvantage { get => systemHasAdvantageDisadvantage; set => systemHasAdvantageDisadvantage = value; }
        public SystemType SystemType { get => systemType; set => systemType = value; }
        public int SuccessInterval { get => successInterval; set => successInterval = value; }
        public bool SystemHasManaOrResourcePoints
        {
            get => systemHasManaOrResourcePoints;
            set => systemHasManaOrResourcePoints = value;
        }
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
        public Dictionary<string, Attribute> SystemAttributes { get => systemAttributes; set => systemAttributes = value; }
        public Dictionary<string, Skill> SystemSkills { get => systemSkills; set => systemSkills = value; }
        public Dictionary<string, Ability> SystemAbilities { get => systemAbilities; set => systemAbilities = value; }
        public InitiativeStatType InitiativeStatType { get => initiativeStatType; set => initiativeStatType = value; }
        public string InitiativeStatName { get => initiativeStatName; set => initiativeStatName = value; }
        public string InitiativeFormula { get => initiativeFormula; set => initiativeFormula = value; }
        public bool DynamicSkillAttributeLinking { get => dynamicSkillAttributeLinking; set => dynamicSkillAttributeLinking = value; }

        public void CaptureTemplateFromSheet(CharacterSheet sheet)
        {
            if (sheet == null) return;

            if (sheet.characterAttributes != null && sheet.characterAttributes.Count > 0)
            {
                systemAttributes = new Dictionary<string, Attribute>(sheet.characterAttributes, StringComparer.OrdinalIgnoreCase);
            }

            if (sheet.characterSkills != null && sheet.characterSkills.Count > 0)
            {
                systemSkills = new Dictionary<string, Skill>(sheet.characterSkills, StringComparer.OrdinalIgnoreCase);
            }

            if (sheet.characterAbilities != null && sheet.characterAbilities.Count > 0)
            {
                systemAbilities = new Dictionary<string, Ability>(sheet.characterAbilities, StringComparer.OrdinalIgnoreCase);
            }

            if (sheet.characterResources != null && sheet.characterResources.Count > 0)
            {
                systemResources ??= new List<ResourceDefinition>();
                foreach (var kv in sheet.characterResources)
                {
                    var res = kv.Value;
                    var existing = systemResources.FirstOrDefault(r => string.Equals(r.Name, res.Name, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        existing.DefaultMax = res.MaxValue;
                        existing.DefaultCurrent = res.CurrentValue;
                        existing.Formula = res.Formula;
                        existing.ResourceType = res.ResourceType;
                        existing.IsRollable = res.IsRollable;
                        existing.ShowInGroup = res.ShowInGroup;
                    }
                    else
                    {
                        systemResources.Add(new ResourceDefinition(res.Name, res.MaxValue, res.CurrentValue, formula: res.Formula, resourceType: res.ResourceType, isRollable: res.IsRollable, showInGroup: res.ShowInGroup));
                    }
                }
            }

            if (sheet.equippedGear != null && sheet.equippedGear.Count > 0)
            {
                customEquipmentSlots ??= new List<string>();
                foreach (var slot in sheet.equippedGear.Keys)
                {
                    if (!customEquipmentSlots.Contains(slot))
                    {
                        customEquipmentSlots.Add(slot);
                    }
                }
            }

            if (sheet.equippedAugmentations != null && sheet.equippedAugmentations.Count > 0)
            {
                customAugmentationSlots ??= new List<string>();
                foreach (var slot in sheet.equippedAugmentations.Keys)
                {
                    if (!customAugmentationSlots.Contains(slot))
                    {
                        customAugmentationSlots.Add(slot);
                    }
                }
            }

            if (sheet.customInventoryCapacity > 0)
            {
                inventoryMaxSlots = sheet.customInventoryCapacity;
                systemHasInventoryLimit = true;
            }

            sheet.linkedDiceSystem = this.systemName;
        }

        public List<ResourceDefinition> GetEffectiveResources()
        {
            systemResources ??= new List<ResourceDefinition>();
            return systemResources;
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

        public bool MoveResource(string resourceName, int direction)
        {
            if (systemResources == null || systemResources.Count < 2) return false;
            int idx = systemResources.FindIndex(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return false;

            int targetIdx = idx + direction;
            if (targetIdx < 0 || targetIdx >= systemResources.Count) return false;

            var item = systemResources[idx];
            systemResources.RemoveAt(idx);
            systemResources.Insert(targetIdx, item);
            return true;
        }

        public bool MoveAttribute(string attributeKey, int direction)
        {
            if (systemAttributes == null || systemAttributes.Count < 2) return false;
            var keys = systemAttributes.Keys.ToList();
            int idx = keys.FindIndex(k => string.Equals(k, attributeKey, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return false;

            int targetIdx = idx + direction;
            if (targetIdx < 0 || targetIdx >= keys.Count) return false;

            string targetKey = keys[targetIdx];
            keys[targetIdx] = keys[idx];
            keys[idx] = targetKey;

            var newDict = new Dictionary<string, Attribute>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in keys)
            {
                newDict[k] = systemAttributes[k];
            }
            systemAttributes = newDict;
            return true;
        }

        public bool MoveSkill(string skillKey, int direction)
        {
            if (systemSkills == null || systemSkills.Count < 2) return false;
            var keys = systemSkills.Keys.ToList();
            int idx = keys.FindIndex(k => string.Equals(k, skillKey, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return false;

            int targetIdx = idx + direction;
            if (targetIdx < 0 || targetIdx >= keys.Count) return false;

            string targetKey = keys[targetIdx];
            keys[targetIdx] = keys[idx];
            keys[idx] = targetKey;

            var newDict = new Dictionary<string, Skill>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in keys)
            {
                newDict[k] = systemSkills[k];
            }
            systemSkills = newDict;
            return true;
        }

        public bool MoveAbility(string abilityKey, int direction)
        {
            if (systemAbilities == null || systemAbilities.Count < 2) return false;
            var keys = systemAbilities.Keys.ToList();
            int idx = keys.FindIndex(k => string.Equals(k, abilityKey, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return false;

            int targetIdx = idx + direction;
            if (targetIdx < 0 || targetIdx >= keys.Count) return false;

            string targetKey = keys[targetIdx];
            keys[targetIdx] = keys[idx];
            keys[idx] = targetKey;

            var newDict = new Dictionary<string, Ability>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in keys)
            {
                newDict[k] = systemAbilities[k];
            }
            systemAbilities = newDict;
            return true;
        }

        public bool MoveEquipmentSlot(string slotName, int direction)
        {
            if (customEquipmentSlots == null || customEquipmentSlots.Count == 0)
            {
                customEquipmentSlots = GearItem.StandardSlots.ToList();
            }
            if (customEquipmentSlots.Count < 2) return false;
            int idx = customEquipmentSlots.FindIndex(s => string.Equals(s, slotName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return false;

            int targetIdx = idx + direction;
            if (targetIdx < 0 || targetIdx >= customEquipmentSlots.Count) return false;

            var item = customEquipmentSlots[idx];
            customEquipmentSlots.RemoveAt(idx);
            customEquipmentSlots.Insert(targetIdx, item);
            return true;
        }

        public bool MoveAugmentationSlot(string slotName, int direction)
        {
            if (customAugmentationSlots == null || customAugmentationSlots.Count == 0)
            {
                customAugmentationSlots = GearItem.StandardAugmentationSlots.ToList();
            }
            if (customAugmentationSlots.Count < 2) return false;
            int idx = customAugmentationSlots.FindIndex(s => string.Equals(s, slotName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return false;

            int targetIdx = idx + direction;
            if (targetIdx < 0 || targetIdx >= customAugmentationSlots.Count) return false;

            var item = customAugmentationSlots[idx];
            customAugmentationSlots.RemoveAt(idx);
            customAugmentationSlots.Insert(targetIdx, item);
            return true;
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
            string sanitized = systemName.Replace(" ", "_").ToLower();
            string path = isFullPath ? systemName : $"{Plugin.dataLocation}/diceSystem/{sanitized}.json";
            if (!isFullPath && !File.Exists(path) && File.Exists($"{Plugin.dataLocation}/diceSystem/{systemName}.json"))
            {
                path = $"{Plugin.dataLocation}/diceSystem/{systemName}.json";
            }

            try
            {
                if (File.Exists(path))
                {
                    Plugin.Log?.Information($"Loading existing dice system from {path}");
                    return JsonSerializer.Deserialize<DiceSystem>(File.ReadAllText(path));
                }
                else
                {
                    Plugin.Log?.Information("No existing dice system found, creating a new one.");
                    DiceSystem newSystem = new DiceSystem();
                    SaveDiceSystem(newSystem);
                    return newSystem;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to load dice system '{systemName}' from path '{path}'");
                return null;
            }
        }

        public static void SaveDiceSystem(DiceSystem system)
        {
            if (system == null) return;
            try
            {
                if (!Directory.Exists($"{Plugin.dataLocation}/diceSystem"))
                {
                    Directory.CreateDirectory($"{Plugin.dataLocation}/diceSystem");
                }
                string systemName = (system.SystemName ?? "dice_system").Replace(" ", "_").ToLower();
                var path = $"{Plugin.dataLocation}/diceSystem/{systemName}.json";
                Plugin.Log?.Information($"Saving dice system '{system.SystemName}' to {path}");
                File.WriteAllText(path, JsonSerializer.Serialize(system, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to save dice system '{system.SystemName}'");
            }
        }
    }
}
