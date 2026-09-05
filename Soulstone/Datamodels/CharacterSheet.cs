using Soulstone.Managers;
using Soulstone.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Soulstone.Datamodels
{
    internal class CharacterSheet
    {
        //Character identity static fields
        public string characterFullName = string.Empty;
        public string characterNickName = string.Empty;
        public string characterRace = string.Empty;
        public string characterSubRace = string.Empty;
        public string characterJob = string.Empty;
        public string characterSex = string.Empty;
        public string characterGender = string.Empty;
        public string characterPronouns = string.Empty;
        public string characterAge = string.Empty;

        //Character physical description static fields
        public string characterHeight = string.Empty;
        public string characterWeight = string.Empty;
        public string characterBuild = string.Empty;
        public string characterEyeColor = string.Empty;
        public string characterHairColor = string.Empty;
        public string characterSkinTone = string.Empty;
        public string characterScars = string.Empty;
        public string characterTattoos = string.Empty;
        public string characterDistinctiveFeatures = string.Empty;
        //Character background static fields
        public string characterHomeland = string.Empty;
        public string characterOrigin = string.Empty;
        public string characterAffiliation = string.Empty;
        public string characterOccupation = string.Empty;
        public string characterReputation = string.Empty;
        public string characterBackground = string.Empty;

        //Character OOC fields
        public string characterNotes = string.Empty;
        public string characterInfo = string.Empty;
        public string playerAvailability = string.Empty;
        public string playerTimezone = string.Empty;
        public string playerNotes = string.Empty;

        //Character quick look fields
        public string characterQuickLook1 = string.Empty;
        public string characterQuickLook2 = string.Empty;
        public string characterQuickLook3 = string.Empty;
        public string characterQuickLook4 = string.Empty;
        public string characterQuickLook5 = string.Empty;

        //Character dynamic background fields
        public Dictionary<string, string> characterFamily = new Dictionary<string, string>();
        public Dictionary<string, string> characterFriends = new Dictionary<string, string>();
        public Dictionary<string, string> characterEnnemies = new Dictionary<string, string>();

        //Character dynamic inventory fields
        public string characterPictureUrl = string.Empty;
        public List<Item> characterInventory = new List<Item>();
        public List<string> customItemTypes = new List<string>();
        public int customInventoryCapacity = 0;
        public Dictionary<string, string> equippedGear = new Dictionary<string, string>();
        public Dictionary<string, string> equippedAugmentations = new Dictionary<string, string>();

        //Character static ability fields
        public int characterLevel;
        public string characterClass = string.Empty;
        public int characterExperiencePoints;
        public int characterHealthPoints;
        public int characterMaxHealthPoints;
        public int characterManaPoints;
        public int characterMaxManaPoints;

        //Character Generic Resources fields
        public Dictionary<string, CharacterResource> characterResources = new Dictionary<string, CharacterResource>();

        //Character Active Buffs / Debuffs
        public List<Buff> activeBuffs = new List<Buff>();

        //Character Dynamic ability fields
        public Dictionary<string, Attribute> characterAttributes = new Dictionary<string, Attribute>();
        public Dictionary<string, Skill> characterSkills = new Dictionary<string, Skill>();
        public Dictionary<string, Ability> characterAbilities = new Dictionary<string, Ability>();

        public string CharacterFullName { get => characterFullName; set => characterFullName = value;}
        public string CharacterNickName { get => characterNickName; set => characterNickName = value; }
        public string CharacterRace { get => characterRace; set => characterRace = value; }
        public string CharacterSubRace { get => characterSubRace; set => characterSubRace = value; }
        public string CharacterSex { get => characterSex; set => characterSex = value; }
        public string CharacterGender { get => characterGender; set => characterGender = value; }
        public string CharacterPronouns { get => characterPronouns; set => characterPronouns = value; }
        public string CharacterAge { get => characterAge; set => characterAge = value; }
        public string CharacterHeight { get => characterHeight; set => characterHeight = value; }
        public string CharacterWeight { get => characterWeight; set => characterWeight = value; }
        public string CharacterBuild { get => characterBuild; set => characterBuild = value; }
        public string CharacterEyeColor { get => characterEyeColor; set => characterEyeColor = value; }
        public string CharacterHairColor { get => characterHairColor; set => characterHairColor = value; }
        public string CharacterSkinTone { get => characterSkinTone; set => characterSkinTone = value; }
        public string CharacterScars { get => characterScars; set => characterScars = value; }
        public string CharacterTattoos { get => characterTattoos; set => characterTattoos = value; }
        public string CharacterHomeland { get => characterHomeland; set => characterHomeland = value; }
        public string CharacterOrigin { get => characterOrigin; set => characterOrigin = value; }
        public string CharacterAffiliation { get => characterAffiliation; set => characterAffiliation = value; }
        public string CharacterOccupation { get => characterOccupation; set => characterOccupation = value; }
        public string CharacterBackground { get => characterBackground; set => characterBackground = value; }
        public string CharacterNotes { get => characterNotes; set => characterNotes = value; }
        public string CharacterInfo { get => characterInfo; set => characterInfo = value; }
        public string PlayerAvailability { get => playerAvailability; set => playerAvailability = value; }
        public string PlayerTimezone { get => playerTimezone; set => playerTimezone = value; }
        public string PlayerNotes { get => playerNotes; set => playerNotes = value; }
        public Dictionary<string, string> CharacterFamily { get => characterFamily; set => characterFamily = value; }
        public Dictionary<string, string> CharacterFriends { get => characterFriends; set => characterFriends = value; }
        public Dictionary<string, string> CharacterEnnemies { get => characterEnnemies; set => characterEnnemies = value; }
        public Dictionary<string, Attribute> CharacterAttributes { get => characterAttributes; set => characterAttributes = value; }
        public Dictionary<string, Skill> CharacterSkills { get => characterSkills; set => characterSkills = value; }
        public Dictionary<string, Ability> CharacterAbilities { get => characterAbilities; set => characterAbilities = value; }
        public string CharacterJob { get => characterJob; set => characterJob = value; }
        public int CharacterLevel { get => characterLevel; set => characterLevel = value; }
        public string CharacterClass { get => characterClass; set => characterClass = value; }
        public int CharacterExperiencePoints { get => characterExperiencePoints; set => characterExperiencePoints = value; }
        public int CharacterHealthPoints { get => characterHealthPoints; set => characterHealthPoints = value; }
        public int CharacterMaxHealthPoints { get => characterMaxHealthPoints; set => characterMaxHealthPoints = value; }
        public int CharacterManaPoints { get => characterManaPoints; set => characterManaPoints = value; }
        public int CharacterMaxManaPoints { get => characterMaxManaPoints; set => characterMaxManaPoints = value; }
        public string CharacterPictureUrl { get => characterPictureUrl; set => characterPictureUrl = value; }
        public List<Item> CharacterInventory { get => characterInventory; set => characterInventory = value; }
        public List<string> CustomItemTypes { get => customItemTypes; set => customItemTypes = value; }
        public int CustomInventoryCapacity { get => customInventoryCapacity; set => customInventoryCapacity = value; }
        public Dictionary<string, string> EquippedGear { get => equippedGear; set => equippedGear = value; }
        public Dictionary<string, string> EquippedAugmentations { get => equippedAugmentations; set => equippedAugmentations = value; }
        public Dictionary<string, CharacterResource> CharacterResources { get => characterResources; set => characterResources = value; }
        public List<Buff> ActiveBuffs { get => activeBuffs; set => activeBuffs = value; }

        public CharacterSheet()
        {
            characterFamily = new Dictionary<string, string>();
            characterFriends = new Dictionary<string, string>();
            characterEnnemies = new Dictionary<string, string>();
            characterAttributes = new Dictionary<string, Attribute>();
            characterAbilities = new Dictionary<string, Ability>();
            characterSkills = new Dictionary<string, Skill>();
            characterInventory = new List<Item>();
            customItemTypes = new List<string>();
            equippedGear = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            equippedAugmentations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            characterResources = new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase);
            activeBuffs = new List<Buff>();
            SyncResourcesWithLegacyFields();
        }

        public void SyncResourcesWithLegacyFields()
        {
            characterResources ??= new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase);

            if (!characterResources.TryGetValue("Health", out var healthRes))
            {
                if (characterHealthPoints != 0 || characterMaxHealthPoints != 0)
                {
                    characterResources["Health"] = new CharacterResource("Health", characterHealthPoints, characterMaxHealthPoints);
                }
            }
            else
            {
                if (characterHealthPoints != 0 || characterMaxHealthPoints != 0)
                {
                    healthRes.CurrentValue = characterHealthPoints;
                    healthRes.MaxValue = characterMaxHealthPoints;
                }
                else
                {
                    characterHealthPoints = healthRes.CurrentValue;
                    characterMaxHealthPoints = healthRes.MaxValue;
                }
            }

            if (!characterResources.TryGetValue("Mana", out var manaRes))
            {
                if (characterManaPoints != 0 || characterMaxManaPoints != 0)
                {
                    characterResources["Mana"] = new CharacterResource("Mana", characterManaPoints, characterMaxManaPoints);
                }
            }
            else
            {
                if (characterManaPoints != 0 || characterMaxManaPoints != 0)
                {
                    manaRes.CurrentValue = characterManaPoints;
                    manaRes.MaxValue = characterMaxManaPoints;
                }
                else
                {
                    characterManaPoints = manaRes.CurrentValue;
                    characterMaxManaPoints = manaRes.MaxValue;
                }
            }
        }

        public void SetResourceCurrent(string name, int value)
        {
            characterResources ??= new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase);
            if (characterResources.TryGetValue(name, out var res))
            {
                res.CurrentValue = value;
            }
            else
            {
                characterResources[name] = new CharacterResource(name, value, value);
            }

            if (string.Equals(name, "Health", StringComparison.OrdinalIgnoreCase))
            {
                characterHealthPoints = value;
            }
            else if (string.Equals(name, "Mana", StringComparison.OrdinalIgnoreCase))
            {
                characterManaPoints = value;
            }

            PartySyncManager.Instance.BroadcastResourceUpdate();
        }

        public void SetResourceMax(string name, int value)
        {
            characterResources ??= new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase);
            if (characterResources.TryGetValue(name, out var res))
            {
                res.MaxValue = value;
            }
            else
            {
                characterResources[name] = new CharacterResource(name, value, value);
            }

            if (string.Equals(name, "Health", StringComparison.OrdinalIgnoreCase))
            {
                characterMaxHealthPoints = value;
            }
            else if (string.Equals(name, "Mana", StringComparison.OrdinalIgnoreCase))
            {
                characterMaxManaPoints = value;
            }

            PartySyncManager.Instance.BroadcastResourceUpdate();
        }

        public CharacterResource GetOrCreateResource(string name, int defaultCurrent = 100, int defaultMax = 100)
        {
            characterResources ??= new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase);
            if (!characterResources.TryGetValue(name, out var res))
            {
                res = new CharacterResource(name, defaultCurrent, defaultMax);
                characterResources[name] = res;
            }
            return res;
        }

        public List<CharacterResource> GetEffectiveResources(DiceSystem? diceSystem = null)
        {
            characterResources ??= new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase);
            SyncResourcesWithLegacyFields();

            if (diceSystem != null)
            {
                var definedResources = diceSystem.GetEffectiveResources();
                foreach (var def in definedResources)
                {
                    if (!characterResources.ContainsKey(def.Name))
                    {
                        int initMax = def.DefaultMax;
                        if (!string.IsNullOrWhiteSpace(def.Formula))
                        {
                            initMax = StatFormulaEvaluator.EvaluateToInt(def.Formula, this, diceSystem, defaultValue: def.DefaultMax);
                        }
                        int initCur = def.DefaultCurrent;
                        if (!string.IsNullOrWhiteSpace(def.Formula) && def.DefaultCurrent == def.DefaultMax)
                        {
                            initCur = initMax;
                        }
                        characterResources[def.Name] = new CharacterResource(def.Name, initCur, initMax, formula: def.Formula);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(characterResources[def.Name].Formula) && !string.IsNullOrWhiteSpace(def.Formula))
                        {
                            characterResources[def.Name].Formula = def.Formula;
                        }
                    }
                }
            }

            return characterResources.Values.ToList();
        }

        public void AddItem(Item item)
        {
            if (characterInventory == null)
            {
                characterInventory = new List<Item>();
            }
            characterInventory.Add(item);
        }

        public bool RemoveItem(string itemId)
        {
            if (characterInventory == null) return false;
            var item = characterInventory.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                UnequipItem(itemId);
                return characterInventory.Remove(item);
            }
            return false;
        }

        public bool EquipGear(string slot, string itemId)
        {
            if (string.IsNullOrWhiteSpace(slot) || string.IsNullOrWhiteSpace(itemId)) return false;
            characterInventory ??= new List<Item>();
            var item = characterInventory.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return false;

            equippedGear ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            equippedGear[slot] = itemId;
            return true;
        }

        public bool EquipGear(GearItem gear, string? slot = null)
        {
            if (gear == null) return false;
            characterInventory ??= new List<Item>();
            if (!characterInventory.Any(i => i.Id == gear.Id))
            {
                characterInventory.Add(gear);
            }
            string targetSlot = !string.IsNullOrWhiteSpace(slot) ? slot : gear.Slot;
            return EquipGear(targetSlot, gear.Id);
        }

        public bool UnequipGear(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot) || equippedGear == null) return false;
            return equippedGear.Remove(slot);
        }

        public bool EquipAugmentation(string slot, string itemId)
        {
            if (string.IsNullOrWhiteSpace(slot) || string.IsNullOrWhiteSpace(itemId)) return false;
            equippedAugmentations ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var existingSlot = equippedAugmentations.FirstOrDefault(kv => kv.Value == itemId).Key;
            if (existingSlot != null)
            {
                equippedAugmentations.Remove(existingSlot);
            }

            equippedAugmentations[slot] = itemId;
            return true;
        }

        public bool EquipAugmentation(GearItem item, string? slot = null)
        {
            if (item == null) return false;
            characterInventory ??= new List<Item>();
            if (!characterInventory.Any(i => i.Id == item.Id))
            {
                characterInventory.Add(item);
            }
            string targetSlot = !string.IsNullOrWhiteSpace(slot) ? slot : item.Slot;
            return EquipAugmentation(targetSlot, item.Id);
        }

        public bool UnequipAugmentation(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot) || equippedAugmentations == null) return false;
            return equippedAugmentations.Remove(slot);
        }

        public GearItem? GetEquippedAugmentation(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot) || equippedAugmentations == null || characterInventory == null) return null;
            if (equippedAugmentations.TryGetValue(slot, out var itemId))
            {
                return characterInventory.FirstOrDefault(i => i.Id == itemId) as GearItem;
            }
            return null;
        }

        public Dictionary<string, GearItem> GetEquippedAugmentationItems()
        {
            var dict = new Dictionary<string, GearItem>(StringComparer.OrdinalIgnoreCase);
            if (equippedAugmentations == null || characterInventory == null) return dict;

            foreach (var kv in equippedAugmentations)
            {
                var item = characterInventory.FirstOrDefault(i => i.Id == kv.Value) as GearItem;
                if (item != null)
                {
                    dict[kv.Key] = item;
                }
            }
            return dict;
        }

        public bool IsAugmentationEquipped(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || equippedAugmentations == null) return false;
            return equippedAugmentations.Values.Contains(itemId);
        }

        public string? GetEquippedAugmentationSlot(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || equippedAugmentations == null) return null;
            return equippedAugmentations.FirstOrDefault(kv => kv.Value == itemId).Key;
        }

        public bool UnequipItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return false;
            bool removed = false;
            if (equippedGear != null)
            {
                var keys = equippedGear.Where(kv => kv.Value == itemId).Select(kv => kv.Key).ToList();
                foreach (var k in keys)
                {
                    removed |= equippedGear.Remove(k);
                }
            }
            if (equippedAugmentations != null)
            {
                var keys = equippedAugmentations.Where(kv => kv.Value == itemId).Select(kv => kv.Key).ToList();
                foreach (var k in keys)
                {
                    removed |= equippedAugmentations.Remove(k);
                }
            }
            return removed;
        }

        public GearItem? GetEquippedGear(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot) || equippedGear == null || characterInventory == null) return null;
            if (equippedGear.TryGetValue(slot, out var itemId))
            {
                return characterInventory.FirstOrDefault(i => i.Id == itemId) as GearItem;
            }
            return null;
        }

        public Dictionary<string, GearItem> GetEquippedGearItems()
        {
            var dict = new Dictionary<string, GearItem>(StringComparer.OrdinalIgnoreCase);
            if (equippedGear == null || characterInventory == null) return dict;

            foreach (var kv in equippedGear)
            {
                var item = characterInventory.FirstOrDefault(i => i.Id == kv.Value) as GearItem;
                if (item != null)
                {
                    dict[kv.Key] = item;
                }
            }
            return dict;
        }

        public bool IsItemEquipped(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return false;
            bool inGear = equippedGear != null && equippedGear.Values.Contains(itemId);
            bool inAugs = equippedAugmentations != null && equippedAugmentations.Values.Contains(itemId);
            return inGear || inAugs;
        }

        public string? GetEquippedSlot(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return null;
            if (equippedGear != null && equippedGear.Any(kv => kv.Value == itemId))
            {
                return equippedGear.FirstOrDefault(kv => kv.Value == itemId).Key;
            }
            if (equippedAugmentations != null && equippedAugmentations.Any(kv => kv.Value == itemId))
            {
                return equippedAugmentations.FirstOrDefault(kv => kv.Value == itemId).Key;
            }
            return null;
        }

        public int GetGearStatBonus(string statName)
        {
            if (string.IsNullOrWhiteSpace(statName)) return 0;
            int totalBonus = 0;
            var equippedItems = GetEquippedGearItems();
            foreach (var gear in equippedItems.Values)
            {
                totalBonus += gear.GetStatModifier(statName);
            }
            var equippedAugs = GetEquippedAugmentationItems();
            foreach (var aug in equippedAugs.Values)
            {
                totalBonus += aug.GetStatModifier(statName);
            }
            return totalBonus;
        }

        public Dictionary<string, int> GetAllGearStatBonuses()
        {
            var bonuses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var allItems = GetEquippedGearItems().Values.Concat(GetEquippedAugmentationItems().Values);
            foreach (var gear in allItems)
            {
                if (gear.StatModifiers == null) continue;
                foreach (var kv in gear.StatModifiers)
                {
                    if (bonuses.ContainsKey(kv.Key))
                    {
                        bonuses[kv.Key] += kv.Value;
                    }
                    else
                    {
                        bonuses[kv.Key] = kv.Value;
                    }
                }
            }
            return bonuses;
        }

        public void AddBuff(Buff buff)
        {
            if (buff == null) return;
            activeBuffs ??= new List<Buff>();
            activeBuffs.Add(buff);
            SyncWithInitiativeTracker();
        }

        public bool RemoveBuff(string buffId)
        {
            if (activeBuffs == null) return false;
            int index = activeBuffs.FindIndex(b => b.Id == buffId);
            if (index < 0) return false;
            activeBuffs.RemoveAt(index);
            SyncWithInitiativeTracker();
            return true;
        }

        public void SyncWithInitiativeTracker()
        {
            try
            {
                var mgr = InitiativeTrackerManager.Instance;
                if (mgr?.Participants != null && mgr.Participants.Count > 0)
                {
                    var participant = mgr.Participants.FirstOrDefault(p =>
                        p.IsCurrentCharacter || (!string.IsNullOrWhiteSpace(CharacterFullName) && string.Equals(p.Name, CharacterFullName, StringComparison.OrdinalIgnoreCase)));
                    if (participant != null)
                    {
                        participant.IsCurrentCharacter = true;
                        participant.Buffs = new List<Buff>(activeBuffs ?? new List<Buff>());
                    }
                }
            }
            catch
            {
                // Ignored if tracker is not in use or during isolated tests
            }
        }

        public int GetBuffStatBonus(string statName)
        {
            if (string.IsNullOrWhiteSpace(statName) || activeBuffs == null) return 0;
            int totalBonus = 0;
            foreach (var buff in activeBuffs)
            {
                totalBonus += buff.GetStatModifier(statName);
                if (!string.Equals(statName, "All", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(statName, "Global", StringComparison.OrdinalIgnoreCase))
                {
                    totalBonus += buff.GetStatModifier("All") + buff.GetStatModifier("Global");
                }
            }
            return totalBonus;
        }

        public Dictionary<string, int> GetAllBuffStatBonuses()
        {
            var bonuses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (activeBuffs == null) return bonuses;
            foreach (var buff in activeBuffs)
            {
                if (buff.StatModifiers == null) continue;
                foreach (var kv in buff.StatModifiers)
                {
                    if (bonuses.ContainsKey(kv.Key))
                    {
                        bonuses[kv.Key] += kv.Value;
                    }
                    else
                    {
                        bonuses[kv.Key] = kv.Value;
                    }
                }
            }
            return bonuses;
        }

        public List<Buff> TickBuffs(int turns = 1)
        {
            var expired = new List<Buff>();
            if (activeBuffs == null || activeBuffs.Count == 0) return expired;

            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                if (buff.Tick(turns))
                {
                    expired.Add(buff);
                    activeBuffs.RemoveAt(i);
                }
            }
            SyncWithInitiativeTracker();
            return expired;
        }

        public int GetEffectiveAttributeValue(string attrName)
        {
            int baseVal = 0;
            if (characterAttributes != null && characterAttributes.TryGetValue(attrName, out var attr))
            {
                baseVal = attr.TotalValue;
            }
            return baseVal + GetGearStatBonus(attrName) + GetBuffStatBonus(attrName);
        }

        public int GetEffectiveSkillModifier(string skillName)
        {
            int baseMod = 0;
            if (characterSkills != null && characterSkills.TryGetValue(skillName, out var skill))
            {
                baseMod = skill.SkillModifier;
            }
            return baseMod + GetGearStatBonus(skillName) + GetBuffStatBonus(skillName);
        }

        public int GetEffectiveSkillTotal(string skillName, DiceSystem? diceSystem = null)
        {
            if (characterSkills == null || !characterSkills.TryGetValue(skillName, out var skill)) return 0;
            int skillGearBonus = GetGearStatBonus(skillName);
            int skillBuffBonus = GetBuffStatBonus(skillName);
            int total = skill.skillModifier + skillGearBonus + skillBuffBonus;
            if (diceSystem?.skillLinkedToOneAttribute != false && !string.IsNullOrEmpty(skill.linkedAttribute))
            {
                total += GetEffectiveAttributeValue(skill.linkedAttribute);
            }
            return total;
        }

        public int GetInitiativeModifier(DiceSystem? diceSystem)
        {
            int initBuff = GetBuffStatBonus("Initiative");
            if (diceSystem == null) return initBuff;
            if (diceSystem.InitiativeStatType == InitiativeStatType.Attribute && !string.IsNullOrEmpty(diceSystem.InitiativeStatName))
            {
                return GetEffectiveAttributeValue(diceSystem.InitiativeStatName) + initBuff;
            }
            if (diceSystem.InitiativeStatType == InitiativeStatType.Skill && !string.IsNullOrEmpty(diceSystem.InitiativeStatName))
            {
                return GetEffectiveSkillTotal(diceSystem.InitiativeStatName, diceSystem) + initBuff;
            }
            return initBuff;
        }

        public DiceRoll RollInitiative(DiceSystem? diceSystem, bool advantage = false, bool disadvantage = false, bool detailedRoll = false)
        {
            int modifier = GetInitiativeModifier(diceSystem);
            string statInfo = diceSystem != null && diceSystem.InitiativeStatType != InitiativeStatType.None && !string.IsNullOrEmpty(diceSystem.InitiativeStatName)
                ? $"Initiative ({diceSystem.InitiativeStatName})"
                : "Initiative";

            DiceType dType = diceSystem?.DiceType ?? DiceType.d20;
            string dTypeName = Enum.GetName<DiceType>(dType) ?? "d20";
            string[] parsedType = dTypeName.Split('d');
            int sides = parsedType.Length > 1 && int.TryParse(parsedType[1], out int parsedSides) ? parsedSides : 20;

            DiceRoll roll = DiceRoll.RollDiceRegular(1, sides, modifier, statInfo, advantage, disadvantage);

            try
            {
                var rollMessage = new Dalamud.Game.Text.XivChatEntry
                {
                    Message = detailedRoll ? roll.RollDetailedResultString : roll.RollResultString,
                    Type = Dalamud.Game.Text.XivChatType.Echo
                };
                Messages.SendMessage(rollMessage);
            }
            catch
            {
                // Ignored in test environment
            }

            return roll;
        }

        public int GetEffectiveAbilityModifier(string abilityName)
        {
            if (characterAbilities == null || !characterAbilities.TryGetValue(abilityName, out var ability)) return 0;
            int baseMod = ability.abilityModifier;
            int abilityBonus = GetGearStatBonus(ability.abilityName);
            int abilityBuffBonus = GetBuffStatBonus(ability.abilityName);
            int attrBonus = 0;
            if (!string.IsNullOrEmpty(ability.linkedAttribute))
            {
                attrBonus = GetEffectiveAttributeValue(ability.linkedAttribute);
            }
            int skillBonus = 0;
            if (ability.linkedSkill != null && !string.IsNullOrEmpty(ability.linkedSkill.skillName))
            {
                skillBonus = GetEffectiveSkillModifier(ability.linkedSkill.skillName);
            }
            return baseMod + abilityBonus + abilityBuffBonus + attrBonus + skillBonus;
        }

        public int GetEffectiveResourceMax(string resourceName, DiceSystem? diceSystem = null)
        {
            int baseMax = 0;
            CharacterResource? res = null;
            characterResources?.TryGetValue(resourceName, out res);

            string formula = string.Empty;
            if (res != null && !string.IsNullOrWhiteSpace(res.Formula))
            {
                formula = res.Formula;
            }
            else if (diceSystem != null)
            {
                var def = diceSystem.GetEffectiveResources().FirstOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));
                if (def != null && !string.IsNullOrWhiteSpace(def.Formula))
                {
                    formula = def.Formula;
                }
            }

            if (!string.IsNullOrWhiteSpace(formula))
            {
                int defaultVal = (res != null && res.MaxValue > 0) ? res.MaxValue : 100;
                baseMax = StatFormulaEvaluator.EvaluateToInt(formula, this, diceSystem, defaultValue: defaultVal);
                if (res != null)
                {
                    baseMax += res.TempBonus;
                }
            }
            else
            {
                if (res != null && res.MaxValue > 0)
                {
                    baseMax = res.TotalMaxValue;
                }
                else if (string.Equals(resourceName, "Health", StringComparison.OrdinalIgnoreCase))
                {
                    baseMax = characterMaxHealthPoints > 0 ? characterMaxHealthPoints : 100;
                }
                else if (string.Equals(resourceName, "Mana", StringComparison.OrdinalIgnoreCase))
                {
                    baseMax = characterMaxManaPoints > 0 ? characterMaxManaPoints : 100;
                }
                else if (diceSystem != null)
                {
                    var def = diceSystem.GetEffectiveResources().FirstOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));
                    if (def != null) baseMax = def.DefaultMax;
                }
            }

            int gearBonus = GetGearStatBonus(resourceName) + GetGearStatBonus($"Max {resourceName}") + GetGearStatBonus($"Max{resourceName}");
            int buffBonus = GetBuffStatBonus(resourceName) + GetBuffStatBonus($"Max {resourceName}") + GetBuffStatBonus($"Max{resourceName}");
            return baseMax + gearBonus + buffBonus;
        }

        public void RecalculateResourceMax(string resourceName, DiceSystem? diceSystem = null)
        {
            characterResources ??= new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase);
            if (!characterResources.TryGetValue(resourceName, out var res)) return;

            string formula = res.Formula;
            if (string.IsNullOrWhiteSpace(formula) && diceSystem != null)
            {
                var def = diceSystem.GetEffectiveResources().FirstOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));
                if (def != null && !string.IsNullOrWhiteSpace(def.Formula))
                {
                    formula = def.Formula;
                }
            }

            if (!string.IsNullOrWhiteSpace(formula))
            {
                int evaluated = StatFormulaEvaluator.EvaluateToInt(formula, this, diceSystem, defaultValue: res.MaxValue > 0 ? res.MaxValue : 100);
                res.MaxValue = evaluated;
                if (string.Equals(resourceName, "Health", StringComparison.OrdinalIgnoreCase))
                {
                    characterMaxHealthPoints = evaluated;
                }
                else if (string.Equals(resourceName, "Mana", StringComparison.OrdinalIgnoreCase))
                {
                    characterMaxManaPoints = evaluated;
                }
            }
        }

        public void RecalculateAllResourceMaxes(DiceSystem? diceSystem = null)
        {
            if (characterResources == null) return;
            foreach (var key in characterResources.Keys.ToList())
            {
                RecalculateResourceMax(key, diceSystem);
            }
        }

        public int GetEffectiveInventoryCapacity(DiceSystem? diceSystem = null)
        {
            if (customInventoryCapacity > 0)
            {
                return customInventoryCapacity;
            }
            if (diceSystem != null && diceSystem.SystemHasInventoryLimit)
            {
                return diceSystem.InventoryMaxSlots;
            }
            return 0; // 0 indicates unlimited
        }

        public static void CreateNewSheet(string characterName)
        {
            CharacterSheet newsheet = new CharacterSheet();
            newsheet.CharacterFullName = characterName;     
            SaveSheet(newsheet);
            CharacterManager.Instance.ForceLoadCharData(characterName);
        }

        public static CharacterSheet? LoadSheet(string characterName, bool isFullPath = false)
        {
            string path = isFullPath ? characterName : $"{Plugin.dataLocation}/sheets/{characterName.Replace(" ", "_").ToLower()}.json";
            CharacterSheet? loadedSheet = null;
            try
            {
                if (!File.Exists(path))
                {
                    Plugin.Log?.Information("No existing character sheet found, creating a new one.");
                    CharacterSheet newsheet = new CharacterSheet();
                    newsheet.CharacterFullName = characterName;
                    SaveSheet(newsheet);
                }

                Plugin.Log?.Information($"Loading existing character sheet from {path}");
                string loadedfile = File.ReadAllText(path);

                if (!string.IsNullOrEmpty(loadedfile))
                {
                    loadedSheet = JsonSerializer.Deserialize<CharacterSheet>(loadedfile);
                }

                if (loadedSheet != null)
                {
                    if (loadedSheet.characterFamily == null)
                    {
                        loadedSheet.characterFamily = new Dictionary<string, string>();
                    }

                    if (loadedSheet.characterFriends == null)
                    {
                        loadedSheet.characterFriends = new Dictionary<string, string>();
                    }

                    if (loadedSheet.characterEnnemies == null)
                    {
                        loadedSheet.characterEnnemies = new Dictionary<string, string>();
                    }

                    if (loadedSheet.characterAttributes == null)
                    {
                        loadedSheet.characterAttributes = new Dictionary<string, Attribute>();
                    }

                    if (loadedSheet.characterSkills == null)
                    {
                        loadedSheet.characterSkills = new Dictionary<string, Skill>();
                    }

                    if (loadedSheet.characterAbilities == null)
                    {
                        loadedSheet.characterAbilities = new Dictionary<string, Ability>();
                    }

                    if (loadedSheet.characterInventory == null)
                    {
                        loadedSheet.characterInventory = new List<Item>();
                    }

                    if (loadedSheet.customItemTypes == null)
                    {
                        loadedSheet.customItemTypes = new List<string>();
                    }

                    if (loadedSheet.equippedGear == null)
                    {
                        loadedSheet.equippedGear = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }

                    if (loadedSheet.equippedAugmentations == null)
                    {
                        loadedSheet.equippedAugmentations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }

                    if (loadedSheet.characterResources == null)
                    {
                        loadedSheet.characterResources = new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase);
                    }

                    if (loadedSheet.activeBuffs == null)
                    {
                        loadedSheet.activeBuffs = new List<Buff>();
                    }

                    loadedSheet.SyncResourcesWithLegacyFields();

                    return loadedSheet;
                }
                else
                {
                    Plugin.Log?.Warning("Failed to load character sheet.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to load character sheet for '{characterName}' from path '{path}'");
                return null;
            }
        }

        public static void SaveSheet(CharacterSheet sheet)
        {
            if (sheet == null) return;
            try
            {
                if (!Directory.Exists($"{Plugin.dataLocation}/sheets"))
                {
                    Directory.CreateDirectory($"{Plugin.dataLocation}/sheets");
                }
                var characterName = (sheet.CharacterFullName ?? "character").Replace(" ", "_").ToLower();
                var path = $"{Plugin.dataLocation}/sheets/{characterName}.json";
                Plugin.Log?.Information($"Saving character sheet for {sheet.CharacterFullName} to {path}");
                File.WriteAllText(path, JsonSerializer.Serialize(sheet, new JsonSerializerOptions { WriteIndented = true }));

                try
                {
                    if (PartySyncManager.Instance.IsConnected)
                    {
                        PartySyncManager.Instance.BroadcastResourceUpdate();
                        PartySyncManager.Instance.BroadcastPrivateStats();
                    }
                }
                catch (Exception syncEx)
                {
                    Plugin.Log?.Debug(syncEx, "Failed to broadcast sync update on character sheet save");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to save character sheet for '{sheet.CharacterFullName}'");
            }
        }
    }
}
