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
        public int characterHealthPoints = 0;
        public int characterMaxHealthPoints = 0;
        public int characterManaPoints = 0;
        public int characterMaxManaPoints = 0;

        //Character Generic Resources fields
        public Dictionary<string, CharacterResource> characterResources = new Dictionary<string, CharacterResource>();

        //Character Active Buffs / Debuffs
        public List<Buff> activeBuffs = new List<Buff>();

        //Character Feats
        public List<Feat> characterFeats = new List<Feat>();

        //Field Visibility (fields hidden from other players when viewing sheet)
        public List<string> hiddenFields = new List<string>();

        //Character Dynamic ability fields
        public Dictionary<string, Attribute> characterAttributes = new Dictionary<string, Attribute>();
        public Dictionary<string, Skill> characterSkills = new Dictionary<string, Skill>();
        public Dictionary<string, Ability> characterAbilities = new Dictionary<string, Ability>();
        public string linkedDiceSystem = string.Empty;

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
        public string CharacterReputation { get => characterReputation; set => characterReputation = value; }
        public string CharacterBackground { get => characterBackground; set => characterBackground = value; }
        public string CharacterDistinctiveFeatures { get => characterDistinctiveFeatures; set => characterDistinctiveFeatures = value; }
        public string CharacterQuickLook1 { get => characterQuickLook1; set => characterQuickLook1 = value; }
        public string CharacterQuickLook2 { get => characterQuickLook2; set => characterQuickLook2 = value; }
        public string CharacterQuickLook3 { get => characterQuickLook3; set => characterQuickLook3 = value; }
        public string CharacterQuickLook4 { get => characterQuickLook4; set => characterQuickLook4 = value; }
        public string CharacterQuickLook5 { get => characterQuickLook5; set => characterQuickLook5 = value; }
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
        public string LinkedDiceSystem { get => linkedDiceSystem; set => linkedDiceSystem = value; }
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
        public List<Feat> CharacterFeats { get => characterFeats; set => characterFeats = value; }
        public List<string> HiddenFields { get => hiddenFields; set => hiddenFields = value; }

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
            characterFeats = new List<Feat>();
            hiddenFields = new List<string>();
        }

        public bool IsFieldHidden(string fieldName)
        {
            if (hiddenFields == null || string.IsNullOrWhiteSpace(fieldName)) return false;
            return hiddenFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
        }

        public void SetFieldHidden(string fieldName, bool hidden)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) return;
            hiddenFields ??= new List<string>();
            if (hidden)
            {
                if (!IsFieldHidden(fieldName))
                {
                    hiddenFields.Add(fieldName);
                }
            }
            else
            {
                hiddenFields.RemoveAll(f => string.Equals(f, fieldName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public void ToggleFieldHidden(string fieldName)
        {
            SetFieldHidden(fieldName, !IsFieldHidden(fieldName));
        }

        public void SyncResourcesWithLegacyFields()
        {
            characterResources ??= new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase);

            if (characterResources.TryGetValue("Health", out var healthRes))
            {
                characterHealthPoints = healthRes.CurrentValue;
                characterMaxHealthPoints = healthRes.MaxValue;
            }
            else
            {
                characterHealthPoints = 0;
                characterMaxHealthPoints = 0;
            }

            if (characterResources.TryGetValue("Mana", out var manaRes))
            {
                characterManaPoints = manaRes.CurrentValue;
                characterMaxManaPoints = manaRes.MaxValue;
            }
            else
            {
                characterManaPoints = 0;
                characterMaxManaPoints = 0;
            }
        }

        public bool RemoveResource(string name)
        {
            if (characterResources == null) return false;
            var key = characterResources.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
            if (key == null) return false;

            bool removed = characterResources.Remove(key);
            if (removed)
            {
                if (string.Equals(name, "Health", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "HP", StringComparison.OrdinalIgnoreCase))
                {
                    characterHealthPoints = 0;
                    characterMaxHealthPoints = 0;
                }
                else if (string.Equals(name, "Mana", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "MP", StringComparison.OrdinalIgnoreCase))
                {
                    characterManaPoints = 0;
                    characterMaxManaPoints = 0;
                }
                PartySyncManager.Instance.BroadcastResourceUpdate();
            }
            return removed;
        }

        public bool MoveResource(string resourceName, int direction)
        {
            if (characterResources == null || characterResources.Count < 2) return false;
            var keys = characterResources.Keys.ToList();
            int idx = keys.FindIndex(k => string.Equals(k, resourceName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return false;

            int targetIdx = idx + direction;
            if (targetIdx < 0 || targetIdx >= keys.Count) return false;

            string targetKey = keys[targetIdx];
            keys[targetIdx] = keys[idx];
            keys[idx] = targetKey;

            var newDict = new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in keys)
            {
                newDict[k] = characterResources[k];
            }
            characterResources = newDict;
            PartySyncManager.Instance.BroadcastResourceUpdate();
            return true;
        }

        public bool MoveAttribute(string attributeKey, int direction)
        {
            if (characterAttributes == null || characterAttributes.Count < 2) return false;
            var keys = characterAttributes.Keys.ToList();
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
                newDict[k] = characterAttributes[k];
            }
            characterAttributes = newDict;
            return true;
        }

        public bool MoveSkill(string skillKey, int direction)
        {
            if (characterSkills == null || characterSkills.Count < 2) return false;
            var keys = characterSkills.Keys.ToList();
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
                newDict[k] = characterSkills[k];
            }
            characterSkills = newDict;
            return true;
        }

        public bool MoveAbility(string abilityKey, int direction)
        {
            if (characterAbilities == null || characterAbilities.Count < 2) return false;
            var keys = characterAbilities.Keys.ToList();
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
                newDict[k] = characterAbilities[k];
            }
            characterAbilities = newDict;
            return true;
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
                var result = new List<CharacterResource>();

                foreach (var def in definedResources)
                {
                    if (!characterResources.TryGetValue(def.Name, out var res))
                    {
                        int initMax = def.DefaultMax;
                        if (!string.IsNullOrWhiteSpace(def.Formula))
                        {
                            initMax = StatFormulaEvaluator.EvaluateToInt(def.Formula, this, diceSystem, defaultValue: def.DefaultMax);
                        }
                        int initCur = def.DefaultCurrent;
                        if (def.ResourceType == ResourceType.Counter)
                        {
                            initCur = (def.DefaultCurrent != 100) ? def.DefaultCurrent : 0;
                        }
                        else if (def.ResourceType == ResourceType.FlatNumber)
                        {
                            initCur = initMax;
                        }
                        else if (!string.IsNullOrWhiteSpace(def.Formula) && def.DefaultCurrent == def.DefaultMax)
                        {
                            initCur = initMax;
                        }
                        res = new CharacterResource(def.Name, initCur, initMax, formula: def.Formula, resourceType: def.ResourceType, isRollable: def.IsRollable, showInGroup: def.ShowInGroup);
                        characterResources[def.Name] = res;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(res.Formula) && !string.IsNullOrWhiteSpace(def.Formula))
                        {
                            res.Formula = def.Formula;
                        }
                        res.ResourceType = def.ResourceType;
                        res.IsRollable = def.IsRollable;
                        res.ShowInGroup = def.ShowInGroup;
                    }
                    result.Add(res);
                }

                return result;
            }

            return characterResources.Values.ToList();
        }

        public Dictionary<string, Attribute> GetEffectiveAttributes(DiceSystem? diceSystem = null)
        {
            characterAttributes ??= new Dictionary<string, Attribute>(StringComparer.OrdinalIgnoreCase);
            if (diceSystem != null && diceSystem.SystemAttributes != null && diceSystem.SystemAttributes.Count > 0)
            {
                var result = new Dictionary<string, Attribute>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in diceSystem.SystemAttributes)
                {
                    if (!characterAttributes.TryGetValue(kv.Key, out var attr))
                    {
                        attr = new Attribute(kv.Value.Name, kv.Value.Value, kv.Value.Description);
                        characterAttributes[kv.Key] = attr;
                    }
                    result[kv.Key] = attr;
                }
                return result;
            }
            return characterAttributes;
        }

        public Dictionary<string, Skill> GetEffectiveSkills(DiceSystem? diceSystem = null)
        {
            characterSkills ??= new Dictionary<string, Skill>(StringComparer.OrdinalIgnoreCase);
            if (diceSystem != null && diceSystem.SystemSkills != null && diceSystem.SystemSkills.Count > 0)
            {
                var result = new Dictionary<string, Skill>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in diceSystem.SystemSkills)
                {
                    if (!characterSkills.TryGetValue(kv.Key, out var sk))
                    {
                        sk = new Skill
                        {
                            skillName = kv.Value.skillName,
                            linkedAttribute = kv.Value.linkedAttribute,
                            skillModifier = kv.Value.skillModifier,
                            skillDescription = kv.Value.skillDescription
                        };
                        characterSkills[kv.Key] = sk;
                    }
                    result[kv.Key] = sk;
                }
                return result;
            }
            return characterSkills;
        }

        public Dictionary<string, Ability> GetEffectiveAbilities(DiceSystem? diceSystem = null)
        {
            characterAbilities ??= new Dictionary<string, Ability>(StringComparer.OrdinalIgnoreCase);
            if (diceSystem != null && diceSystem.SystemAbilities != null && diceSystem.SystemAbilities.Count > 0)
            {
                var result = new Dictionary<string, Ability>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in diceSystem.SystemAbilities)
                {
                    if (!characterAbilities.TryGetValue(kv.Key, out var ab))
                    {
                        ab = new Ability
                        {
                            abilityName = kv.Value.abilityName,
                            linkedAttribute = kv.Value.linkedAttribute,
                            linkedSkill = kv.Value.linkedSkill,
                            abilityModifier = kv.Value.abilityModifier,
                            abilityDescription = kv.Value.abilityDescription
                        };
                        characterAbilities[kv.Key] = ab;
                    }
                    result[kv.Key] = ab;
                }
                return result;
            }
            return characterAbilities;
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
            if (item is not GearItem gear || gear.IsAugmentation) return false;

            equippedGear ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var existingSlot = equippedGear.FirstOrDefault(kv => kv.Value == itemId).Key;
            if (existingSlot != null)
            {
                equippedGear.Remove(existingSlot);
            }

            equippedGear[slot] = itemId;
            return true;
        }

        public bool EquipGear(GearItem gear, string? slot = null)
        {
            if (gear == null || gear.IsAugmentation) return false;
            characterInventory ??= new List<Item>();
            if (!characterInventory.Any(i => i.Id == gear.Id))
            {
                characterInventory.Add(gear);
            }
            string targetSlot = !string.IsNullOrWhiteSpace(slot) ? slot : (!string.IsNullOrWhiteSpace(gear.Slot) ? gear.Slot : "Head");
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
            characterInventory ??= new List<Item>();
            var item = characterInventory.FirstOrDefault(i => i.Id == itemId);
            if (item is not GearItem aug || !aug.IsAugmentation) return false;

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
            if (item == null || !item.IsAugmentation) return false;
            characterInventory ??= new List<Item>();
            if (!characterInventory.Any(i => i.Id == item.Id))
            {
                characterInventory.Add(item);
            }
            string targetSlot = !string.IsNullOrWhiteSpace(slot) ? slot : (!string.IsNullOrWhiteSpace(item.Slot) ? item.Slot : "Neural");
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

        public void AddFeat(Feat feat)
        {
            if (feat == null) return;
            characterFeats ??= new List<Feat>();
            characterFeats.Add(feat);
        }

        public bool RemoveFeat(string featId)
        {
            if (string.IsNullOrWhiteSpace(featId) || characterFeats == null) return false;
            int removed = characterFeats.RemoveAll(f => string.Equals(f.Id, featId, StringComparison.OrdinalIgnoreCase));
            return removed > 0;
        }

        public Feat? GetFeat(string featId)
        {
            if (string.IsNullOrWhiteSpace(featId) || characterFeats == null) return null;
            return characterFeats.FirstOrDefault(f => string.Equals(f.Id, featId, StringComparison.OrdinalIgnoreCase));
        }

        public int GetFeatStatBonus(string statName)
        {
            if (string.IsNullOrWhiteSpace(statName) || characterFeats == null) return 0;
            int totalBonus = 0;
            foreach (var feat in characterFeats)
            {
                if (!feat.IsActive) continue;
                totalBonus += feat.GetStatModifier(statName);
                if (!string.Equals(statName, "All", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(statName, "Global", StringComparison.OrdinalIgnoreCase))
                {
                    totalBonus += feat.GetStatModifier("All") + feat.GetStatModifier("Global");
                }
            }
            return totalBonus;
        }

        public Dictionary<string, int> GetAllFeatStatBonuses()
        {
            var bonuses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (characterFeats == null) return bonuses;
            foreach (var feat in characterFeats)
            {
                if (!feat.IsActive || feat.StatModifiers == null) continue;
                foreach (var kv in feat.StatModifiers)
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
            return baseVal + GetGearStatBonus(attrName) + GetBuffStatBonus(attrName) + GetFeatStatBonus(attrName);
        }

        public int GetEffectiveSkillModifier(string skillName)
        {
            int baseMod = 0;
            if (characterSkills != null && characterSkills.TryGetValue(skillName, out var skill))
            {
                baseMod = skill.SkillModifier;
            }
            return baseMod + GetGearStatBonus(skillName) + GetBuffStatBonus(skillName) + GetFeatStatBonus(skillName);
        }

        public int GetEffectiveSkillTotal(string skillName, DiceSystem? diceSystem = null)
        {
            if (characterSkills == null || !characterSkills.TryGetValue(skillName, out var skill)) return 0;
            int skillGearBonus = GetGearStatBonus(skillName);
            int skillBuffBonus = GetBuffStatBonus(skillName);
            int skillFeatBonus = GetFeatStatBonus(skillName);
            int total = skill.skillModifier + skillGearBonus + skillBuffBonus + skillFeatBonus;
            if (diceSystem?.dynamicSkillAttributeLinking != true && diceSystem?.skillLinkedToOneAttribute != false && !string.IsNullOrEmpty(skill.linkedAttribute))
            {
                total += GetEffectiveAttributeValue(skill.linkedAttribute);
            }
            return total;
        }

        public int GetInitiativeModifier(DiceSystem? diceSystem)
        {
            int initBuff = GetBuffStatBonus("Initiative");
            int initFeat = GetFeatStatBonus("Initiative");
            int extraMod = initBuff + initFeat;
            if (diceSystem == null) return extraMod;
            if (diceSystem.InitiativeStatType == InitiativeStatType.Formula && !string.IsNullOrWhiteSpace(diceSystem.InitiativeFormula))
            {
                return StatFormulaEvaluator.EvaluateToInt(diceSystem.InitiativeFormula, this, diceSystem, defaultValue: 0) + extraMod;
            }
            if (diceSystem.InitiativeStatType == InitiativeStatType.Attribute && !string.IsNullOrEmpty(diceSystem.InitiativeStatName))
            {
                return GetEffectiveAttributeValue(diceSystem.InitiativeStatName) + extraMod;
            }
            if (diceSystem.InitiativeStatType == InitiativeStatType.Skill && !string.IsNullOrEmpty(diceSystem.InitiativeStatName))
            {
                return GetEffectiveSkillTotal(diceSystem.InitiativeStatName, diceSystem) + extraMod;
            }
            return extraMod;
        }

        public DiceRoll RollInitiative(DiceSystem? diceSystem, bool advantage = false, bool disadvantage = false, bool detailedRoll = false)
        {
            int modifier = GetInitiativeModifier(diceSystem);
            string statInfo = "Initiative";
            if (diceSystem != null)
            {
                if (diceSystem.InitiativeStatType == InitiativeStatType.Formula && !string.IsNullOrEmpty(diceSystem.InitiativeFormula))
                {
                    statInfo = $"Initiative ({diceSystem.InitiativeFormula})";
                }
                else if (diceSystem.InitiativeStatType != InitiativeStatType.None && !string.IsNullOrEmpty(diceSystem.InitiativeStatName))
                {
                    statInfo = $"Initiative ({diceSystem.InitiativeStatName})";
                }
            }

            int sides = DiceRoll.GetSystemSides(diceSystem);
            DiceRoll roll = DiceRoll.RollStatWithSystem(diceSystem, statInfo, modifier, advantage, disadvantage)
                ?? DiceRoll.RollDiceRegular(1, sides, modifier, statInfo, advantage, disadvantage);

            try
            {
                string actor = !string.IsNullOrWhiteSpace(CharacterFullName) ? CharacterFullName : "Character";
                string rollValue = detailedRoll ? roll.RollDetailedResultString.TextValue : roll.RollResultString.TextValue;
                string echo = LocalizationManager.Instance.GetLocalizedString("InitiativeRollEchoFormat", actor, rollValue);
                PartySyncManager.Instance.BroadcastDiceRoll(
                    "Initiative",
                    roll.RollResult,
                    string.Join(", ", roll.IndividualRolls),
                    echoText: echo,
                    characterName: actor
                );
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
            int abilityFeatBonus = GetFeatStatBonus(ability.abilityName);
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
            return baseMod + abilityBonus + abilityBuffBonus + abilityFeatBonus + attrBonus + skillBonus;
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
                    baseMax = characterMaxHealthPoints;
                }
                else if (string.Equals(resourceName, "Mana", StringComparison.OrdinalIgnoreCase))
                {
                    baseMax = characterMaxManaPoints;
                }
                else if (diceSystem != null)
                {
                    var def = diceSystem.GetEffectiveResources().FirstOrDefault(r => string.Equals(r.Name, resourceName, StringComparison.OrdinalIgnoreCase));
                    if (def != null) baseMax = def.DefaultMax;
                }
            }

            int gearBonus = GetGearStatBonus(resourceName) + GetGearStatBonus($"Max {resourceName}") + GetGearStatBonus($"Max{resourceName}");
            int buffBonus = GetBuffStatBonus(resourceName) + GetBuffStatBonus($"Max {resourceName}") + GetBuffStatBonus($"Max{resourceName}");
            int featBonus = GetFeatStatBonus(resourceName) + GetFeatStatBonus($"Max {resourceName}") + GetFeatStatBonus($"Max{resourceName}");
            return baseMax + gearBonus + buffBonus + featBonus;
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

        public DiceRoll? RollResource(string resourceName, DiceSystem? diceSystem = null, bool advantage = false, bool disadvantage = false, bool detailedRoll = false)
        {
            var sys = diceSystem ?? DiceSystemManager.Instance.CurrentDiceSystem;
            int effectiveVal = GetEffectiveResourceMax(resourceName, sys);
            if (characterResources != null && characterResources.TryGetValue(resourceName, out var res))
            {
                if (res.ResourceType == ResourceType.Counter || res.ResourceType == ResourceType.Bar)
                {
                    effectiveVal = res.CurrentValue;
                }
            }

            int sides = DiceRoll.GetSystemSides(sys);
            DiceRoll roll = DiceRoll.RollStatWithSystem(sys, resourceName, effectiveVal, advantage, disadvantage)
                ?? DiceRoll.RollDiceRegular(1, sides, effectiveVal, resourceName, advantage, disadvantage);

            try
            {
                string actor = !string.IsNullOrWhiteSpace(CharacterFullName) ? CharacterFullName : "Character";
                string displayMsg = detailedRoll ? roll.RollDetailedResultString.TextValue : roll.RollResultString.TextValue;
                PartySyncManager.Instance.BroadcastDiceRoll(
                    resourceName,
                    roll.RollResult,
                    string.Join(", ", roll.IndividualRolls),
                    echoText: displayMsg,
                    characterName: actor
                );
            }
            catch
            {
                // Ignored in test environment
            }

            return roll;
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

        public void ApplyRulesetTemplate(DiceSystem? system)
        {
            if (system == null) return;

            linkedDiceSystem = system.systemName;

            characterAttributes ??= new Dictionary<string, Attribute>();
            if (system.SystemAttributes != null)
            {
                foreach (var kv in system.SystemAttributes)
                {
                    if (!characterAttributes.ContainsKey(kv.Key))
                    {
                        characterAttributes[kv.Key] = new Attribute(kv.Value.Name, kv.Value.Value, kv.Value.Description);
                    }
                }
            }

            characterSkills ??= new Dictionary<string, Skill>();
            if (system.SystemSkills != null)
            {
                foreach (var kv in system.SystemSkills)
                {
                    if (!characterSkills.ContainsKey(kv.Key))
                    {
                        characterSkills[kv.Key] = new Skill
                        {
                            skillName = kv.Value.skillName,
                            linkedAttribute = kv.Value.linkedAttribute,
                            skillModifier = kv.Value.skillModifier,
                            skillDescription = kv.Value.skillDescription
                        };
                    }
                }
            }

            characterAbilities ??= new Dictionary<string, Ability>();
            if (system.SystemAbilities != null)
            {
                foreach (var kv in system.SystemAbilities)
                {
                    if (!characterAbilities.ContainsKey(kv.Key))
                    {
                        characterAbilities[kv.Key] = new Ability
                        {
                            abilityName = kv.Value.abilityName,
                            linkedAttribute = kv.Value.linkedAttribute,
                            linkedSkill = kv.Value.linkedSkill,
                            abilityModifier = kv.Value.abilityModifier,
                            abilityDescription = kv.Value.abilityDescription
                        };
                    }
                }
            }

            characterResources ??= new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase);
            foreach (var resDef in system.GetEffectiveResources())
            {
                if (!characterResources.ContainsKey(resDef.Name))
                {
                    int initCur = resDef.DefaultCurrent;
                    if (resDef.ResourceType == ResourceType.Counter && resDef.DefaultCurrent == 100)
                    {
                        initCur = 0;
                    }
                    characterResources[resDef.Name] = new CharacterResource(resDef.Name, initCur, resDef.DefaultMax, 0, resDef.Formula, resDef.ResourceType);
                }
                else
                {
                    characterResources[resDef.Name].ResourceType = resDef.ResourceType;
                }
            }

            if (system.CustomEquipmentSlots != null && system.CustomEquipmentSlots.Count > 0)
            {
                equippedGear ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var slot in system.CustomEquipmentSlots)
                {
                    if (!equippedGear.ContainsKey(slot))
                    {
                        equippedGear[slot] = string.Empty;
                    }
                }
            }

            if (system.CustomAugmentationSlots != null && system.CustomAugmentationSlots.Count > 0)
            {
                equippedAugmentations ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var slot in system.CustomAugmentationSlots)
                {
                    if (!equippedAugmentations.ContainsKey(slot))
                    {
                        equippedAugmentations[slot] = string.Empty;
                    }
                }
            }

            if (system.SystemHasInventoryLimit && system.InventoryMaxSlots > 0 && customInventoryCapacity == 0)
            {
                customInventoryCapacity = system.InventoryMaxSlots;
            }

            RecalculateAllResourceMaxes(system);
        }

        public static void CreateNewSheet(string characterName)
        {
            CharacterSheet newsheet = new CharacterSheet();
            newsheet.CharacterFullName = characterName;

            var activeSys = DiceSystemManager.Instance.CurrentDiceSystem;
            if (activeSys != null)
            {
                newsheet.ApplyRulesetTemplate(activeSys);
            }

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
                    var activeSys = DiceSystemManager.Instance.CurrentDiceSystem;
                    if (activeSys != null)
                    {
                        newsheet.ApplyRulesetTemplate(activeSys);
                    }
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

                    if (loadedSheet.characterFeats == null)
                    {
                        loadedSheet.characterFeats = new List<Feat>();
                    }

                    if (loadedSheet.hiddenFields == null)
                    {
                        loadedSheet.hiddenFields = new List<string>();
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

                    if (PartySyncManager.Instance.Configuration != null && !string.IsNullOrWhiteSpace(PartySyncManager.Instance.Configuration.SyncServerUrl))
                    {
                        _ = PartySyncManager.Instance.PublishCharacterSheetAsync(sheet);
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
