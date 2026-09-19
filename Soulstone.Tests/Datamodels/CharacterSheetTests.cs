using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Soulstone.Datamodels;
using Attribute = Soulstone.Datamodels.Attribute;

namespace Soulstone.Tests.Datamodels
{
    [Collection("NonParallel")]
    public class CharacterSheetTests : IDisposable
    {
        private readonly string tempDirectory;

        public CharacterSheetTests()
        {
            TestHelper.EnsureMockServices();
            tempDirectory = Path.Combine(Path.GetTempPath(), "SoulstoneCharTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            Plugin.dataLocation = tempDirectory;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Fact]
        public void DefaultConstructor_InitializesEmptyCollections()
        {
            // Arrange & Act
            var sheet = new CharacterSheet();

            // Assert
            sheet.CharacterFamily.Should().NotBeNull().And.BeEmpty();
            sheet.CharacterFriends.Should().NotBeNull().And.BeEmpty();
            sheet.CharacterEnnemies.Should().NotBeNull().And.BeEmpty();
            sheet.CharacterAttributes.Should().NotBeNull().And.BeEmpty();
            sheet.CharacterSkills.Should().NotBeNull().And.BeEmpty();
            sheet.CharacterAbilities.Should().NotBeNull().And.BeEmpty();
            sheet.CharacterInventory.Should().NotBeNull().And.BeEmpty();
            sheet.CustomItemTypes.Should().NotBeNull().And.BeEmpty();
            sheet.CharacterPictureUrl.Should().BeEmpty();
            sheet.CustomInventoryCapacity.Should().Be(0);

            sheet.characterFamily.Should().NotBeNull();
            sheet.characterFriends.Should().NotBeNull();
            sheet.characterEnnemies.Should().NotBeNull();
            sheet.characterAttributes.Should().NotBeNull();
            sheet.characterSkills.Should().NotBeNull();
            sheet.characterAbilities.Should().NotBeNull();
            sheet.characterInventory.Should().NotBeNull();
            sheet.customItemTypes.Should().NotBeNull();
            sheet.characterPictureUrl.Should().BeEmpty();
            sheet.customInventoryCapacity.Should().Be(0);
        }

        [Fact]
        public void Properties_SetAndGet_UpdatesCorrectly()
        {
            // Arrange
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Taelina Vael",
                CharacterNickName = "Tae",
                CharacterRace = "Elezen",
                CharacterSubRace = "Duskwight",
                CharacterJob = "Red Mage",
                CharacterSex = "Female",
                CharacterGender = "Woman",
                CharacterPronouns = "She/Her",
                CharacterAge = "28",
                CharacterHeight = "185 cm",
                CharacterWeight = "68 kg",
                CharacterBuild = "Slender",
                CharacterEyeColor = "Violet",
                CharacterHairColor = "Silver",
                CharacterSkinTone = "Pale",
                CharacterScars = "Small scar across bridge of nose",
                CharacterTattoos = "Runic ink on right forearm",
                CharacterHomeland = "Gridania",
                CharacterOrigin = "Black Shroud",
                CharacterAffiliation = "Scions",
                CharacterOccupation = "Arcanist / Spellsword",
                CharacterBackground = "A wandering scholar with a penchant for lost artifacts.",
                CharacterNotes = "Prefers tea over coffee.",
                CharacterInfo = "Available for adventure RP.",
                PlayerAvailability = "Evenings UTC",
                PlayerTimezone = "UTC+1",
                PlayerNotes = "Discord: @taelina",
                CharacterLevel = 90,
                CharacterClass = "RDM",
                CharacterExperiencePoints = 15000,
                CharacterHealthPoints = 500,
                CharacterMaxHealthPoints = 500,
                CharacterManaPoints = 10000,
                CharacterMaxManaPoints = 10000,
                CharacterPictureUrl = "https://example.com/portrait.png",
                CustomInventoryCapacity = 50
            };

            // Assert
            sheet.CharacterFullName.Should().Be("Taelina Vael");
            sheet.characterFullName.Should().Be("Taelina Vael");
            sheet.CharacterNickName.Should().Be("Tae");
            sheet.CharacterRace.Should().Be("Elezen");
            sheet.CharacterSubRace.Should().Be("Duskwight");
            sheet.CharacterJob.Should().Be("Red Mage");
            sheet.CharacterSex.Should().Be("Female");
            sheet.CharacterGender.Should().Be("Woman");
            sheet.CharacterPronouns.Should().Be("She/Her");
            sheet.CharacterAge.Should().Be("28");
            sheet.CharacterHeight.Should().Be("185 cm");
            sheet.CharacterWeight.Should().Be("68 kg");
            sheet.CharacterBuild.Should().Be("Slender");
            sheet.CharacterEyeColor.Should().Be("Violet");
            sheet.CharacterHairColor.Should().Be("Silver");
            sheet.CharacterSkinTone.Should().Be("Pale");
            sheet.CharacterScars.Should().Be("Small scar across bridge of nose");
            sheet.CharacterTattoos.Should().Be("Runic ink on right forearm");
            sheet.CharacterHomeland.Should().Be("Gridania");
            sheet.CharacterOrigin.Should().Be("Black Shroud");
            sheet.CharacterAffiliation.Should().Be("Scions");
            sheet.CharacterOccupation.Should().Be("Arcanist / Spellsword");
            sheet.CharacterBackground.Should().Be("A wandering scholar with a penchant for lost artifacts.");
            sheet.CharacterNotes.Should().Be("Prefers tea over coffee.");
            sheet.CharacterInfo.Should().Be("Available for adventure RP.");
            sheet.PlayerAvailability.Should().Be("Evenings UTC");
            sheet.PlayerTimezone.Should().Be("UTC+1");
            sheet.PlayerNotes.Should().Be("Discord: @taelina");
            sheet.CharacterLevel.Should().Be(90);
            sheet.CharacterClass.Should().Be("RDM");
            sheet.CharacterExperiencePoints.Should().Be(15000);
            sheet.CharacterHealthPoints.Should().Be(500);
            sheet.CharacterMaxHealthPoints.Should().Be(500);
            sheet.CharacterManaPoints.Should().Be(10000);
            sheet.CharacterMaxManaPoints.Should().Be(10000);
            sheet.CharacterPictureUrl.Should().Be("https://example.com/portrait.png");
            sheet.characterPictureUrl.Should().Be("https://example.com/portrait.png");
            sheet.CustomInventoryCapacity.Should().Be(50);
            sheet.customInventoryCapacity.Should().Be(50);
        }

        [Fact]
        public void DynamicCollections_CanAddAndRetrieveItems()
        {
            // Arrange
            var sheet = new CharacterSheet();

            // Act
            sheet.CharacterFamily.Add("Father", "Eolande Vael");
            sheet.CharacterFriends.Add("Best Friend", "Alisaie Leveilleur");
            sheet.CharacterEnnemies.Add("Rival", "Zenos yae Galvus");

            var attr = new Attribute("Strength", 12);
            sheet.CharacterAttributes.Add("STR", attr);

            var skill = new Skill { Id = 1, SkillName = "Athletics", SkillModifier = 3, LinkedAttribute = "STR" };
            sheet.CharacterSkills.Add("Athletics", skill);

            var ability = new Ability { Id = 1, AbilityName = "Corps-a-corps", AbilityModifier = 2, LinkedSkill = skill };
            sheet.CharacterAbilities.Add("Corps-a-corps", ability);

            // Assert
            sheet.CharacterFamily["Father"].Should().Be("Eolande Vael");
            sheet.CharacterFriends["Best Friend"].Should().Be("Alisaie Leveilleur");
            sheet.CharacterEnnemies["Rival"].Should().Be("Zenos yae Galvus");
            sheet.CharacterAttributes["STR"].Should().BeSameAs(attr);
            sheet.CharacterSkills["Athletics"].Should().BeSameAs(skill);
            sheet.CharacterAbilities["Corps-a-corps"].Should().BeSameAs(ability);
        }

        [Fact]
        public void JsonSerialization_PreservesNestedCollectionsAndFields()
        {
            // Arrange
            var original = new CharacterSheet
            {
                CharacterFullName = "Alphinaud Leveilleur",
                CharacterLevel = 90
            };
            original.CharacterFamily.Add("Sister", "Alisaie");
            original.CharacterAttributes.Add("INT", new Attribute("Intelligence", 20) { TempBonus = 2, EpicBonus = 1 });
            original.CharacterSkills.Add("Diplomacy", new Skill { Id = 5, SkillName = "Diplomacy", SkillModifier = 8 });

            // Act
            string json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = true });
            var deserialized = JsonSerializer.Deserialize<CharacterSheet>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.CharacterFullName.Should().Be("Alphinaud Leveilleur");
            deserialized.CharacterLevel.Should().Be(90);
            deserialized.CharacterFamily.Should().ContainKey("Sister").WhoseValue.Should().Be("Alisaie");
            deserialized.CharacterAttributes.Should().ContainKey("INT");
            deserialized.CharacterAttributes["INT"].Value.Should().Be(20);
            deserialized.CharacterAttributes["INT"].TempBonus.Should().Be(2);
            deserialized.CharacterAttributes["INT"].EpicBonus.Should().Be(1);
            deserialized.CharacterSkills.Should().ContainKey("Diplomacy");
            deserialized.CharacterSkills["Diplomacy"].SkillModifier.Should().Be(8);
        }

        [Fact]
        public void SaveSheet_And_LoadSheet_WithFullPath_ShouldPersistAndRetrieve()
        {
            // Arrange
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Urianger Augurelt",
                CharacterJob = "Astrologian",
                CharacterLevel = 90
            };
            string customPath = Path.Combine(tempDirectory, "urianger.json");
            File.WriteAllText(customPath, JsonSerializer.Serialize(sheet));

            // Act
            var loaded = CharacterSheet.LoadSheet(customPath, isFullPath: true);

            // Assert
            loaded.Should().NotBeNull();
            loaded.CharacterFullName.Should().Be("Urianger Augurelt");
            loaded.CharacterJob.Should().Be("Astrologian");
            loaded.CharacterLevel.Should().Be(90);
        }

        [Fact]
        public void SaveSheet_ShouldCreateSheetsDirectoryAndSaveFormattedJson()
        {
            // Arrange
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Y'shtola Rhul",
                CharacterJob = "Black Mage"
            };

            // Act
            CharacterSheet.SaveSheet(sheet);

            // Assert
            string expectedPath = Path.Combine(tempDirectory, "sheets", "y'shtola_rhul.json");
            File.Exists(expectedPath).Should().BeTrue();

            var loaded = CharacterSheet.LoadSheet("Y'shtola Rhul", isFullPath: false);
            loaded.Should().NotBeNull();
            loaded.CharacterFullName.Should().Be("Y'shtola Rhul");
            loaded.CharacterJob.Should().Be("Black Mage");
        }

        [Fact]
        public void LoadSheet_WhenFileDoesNotExist_CreatesAndSavesNewSheet()
        {
            // Act
            var sheet = CharacterSheet.LoadSheet("New Adventurer", isFullPath: false);

            // Assert
            sheet.Should().NotBeNull();
            sheet.CharacterFullName.Should().Be("New Adventurer");
            string expectedPath = Path.Combine(tempDirectory, "sheets", "new_adventurer.json");
            File.Exists(expectedPath).Should().BeTrue();
        }

        [Fact]
        public void GetEffectiveSkillTotal_WithDynamicSkillAttributeLinking_DoesNotIncludeStaticLinkedAttribute()
        {
            var sheet = new CharacterSheet();
            sheet.CharacterAttributes["Dexterity"] = new Attribute("Dexterity", 5);
            sheet.CharacterSkills["Stealth"] = new Skill
            {
                SkillName = "Stealth",
                SkillModifier = 3,
                LinkedAttribute = "Dexterity"
            };

            var standardDiceSys = new DiceSystem
            {
                DynamicSkillAttributeLinking = false,
                SkillLinkedToOneAttribute = true
            };

            var dynamicDiceSys = new DiceSystem
            {
                DynamicSkillAttributeLinking = true,
                SkillLinkedToOneAttribute = true
            };

            // Standard: 3 (Skill) + 5 (Dex) = 8
            sheet.GetEffectiveSkillTotal("Stealth", standardDiceSys).Should().Be(8);

            // Dynamic: 3 (Skill only, attribute chosen at roll time) = 3
            sheet.GetEffectiveSkillTotal("Stealth", dynamicDiceSys).Should().Be(3);
        }

        [Fact]
        public void GetEffectiveStats_WhenDiceSystemDefinesStats_FiltersOutOtherStats()
        {
            var sheet = new CharacterSheet();
            sheet.CharacterAttributes["Strength"] = new Attribute("Strength", 10);
            sheet.CharacterAttributes["Dexterity"] = new Attribute("Dexterity", 12);
            sheet.CharacterAttributes["CustomOldAttr"] = new Attribute("CustomOldAttr", 5);

            sheet.CharacterSkills["Athletics"] = new Skill { SkillName = "Athletics", SkillModifier = 2 };
            sheet.CharacterSkills["OldUnusedSkill"] = new Skill { SkillName = "OldUnusedSkill", SkillModifier = 1 };

            sheet.CharacterAbilities["Rage"] = new Ability { AbilityName = "Rage", AbilityModifier = 3 };
            sheet.CharacterAbilities["OldAbility"] = new Ability { AbilityName = "OldAbility", AbilityModifier = 1 };

            var system = new DiceSystem();
            system.SystemAttributes["Strength"] = new Attribute("Strength", 10);
            system.SystemAttributes["Dexterity"] = new Attribute("Dexterity", 10);
            system.SystemSkills["Athletics"] = new Skill { SkillName = "Athletics", SkillModifier = 0 };
            system.SystemAbilities["Rage"] = new Ability { AbilityName = "Rage", AbilityModifier = 0 };

            var attrs = sheet.GetEffectiveAttributes(system);
            attrs.Should().ContainKey("Strength");
            attrs.Should().ContainKey("Dexterity");
            attrs.Should().NotContainKey("CustomOldAttr");

            var skills = sheet.GetEffectiveSkills(system);
            skills.Should().ContainKey("Athletics");
            skills.Should().NotContainKey("OldUnusedSkill");

            var abs = sheet.GetEffectiveAbilities(system);
            abs.Should().ContainKey("Rage");
            abs.Should().NotContainKey("OldAbility");

            // When system has no defined stats, falls back to all character stats
            var emptySystem = new DiceSystem();
            sheet.GetEffectiveAttributes(emptySystem).Should().ContainKey("CustomOldAttr");
            sheet.GetEffectiveSkills(emptySystem).Should().ContainKey("OldUnusedSkill");
            sheet.GetEffectiveAbilities(emptySystem).Should().ContainKey("OldAbility");
        }

        [Fact]
        public void FieldVisibility_IsHiddenAndToggle_WorksCorrectly()
        {
            var sheet = new CharacterSheet();

            // Default: no hidden fields
            sheet.IsFieldHidden("CharacterAge").Should().BeFalse();
            sheet.IsFieldHidden("PlayerNotes").Should().BeFalse();

            // Hide fields
            sheet.SetFieldHidden("CharacterAge", true);
            sheet.IsFieldHidden("CharacterAge").Should().BeTrue();
            sheet.IsFieldHidden("characterage").Should().BeTrue(); // Case insensitive

            // Toggle
            sheet.ToggleFieldHidden("PlayerNotes");
            sheet.IsFieldHidden("PlayerNotes").Should().BeTrue();
            sheet.ToggleFieldHidden("PlayerNotes");
            sheet.IsFieldHidden("PlayerNotes").Should().BeFalse();

            // Unhide
            sheet.SetFieldHidden("CharacterAge", false);
            sheet.IsFieldHidden("CharacterAge").Should().BeFalse();
        }

        [Fact]
        public void FieldVisibility_SerializesAndDeserializesHiddenFields()
        {
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Alphinaud Leveilleur",
                CharacterAge = "16",
                CharacterSex = "Male"
            };
            sheet.SetFieldHidden("CharacterAge", true);
            sheet.SetFieldHidden("CharacterSex", true);

            string json = JsonSerializer.Serialize(sheet);
            var deserialized = JsonSerializer.Deserialize<CharacterSheet>(json);

            deserialized.Should().NotBeNull();
            deserialized!.IsFieldHidden("CharacterAge").Should().BeTrue();
            deserialized.IsFieldHidden("CharacterSex").Should().BeTrue();
            deserialized.IsFieldHidden("CharacterFullName").Should().BeFalse();
        }

        [Fact]
        public void QuickLooksAndDistinctiveFeatures_PropertiesAndSerialization_PreservesAllValues()
        {
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Y'shtola Rhul",
                CharacterDistinctiveFeatures = "Sightless white eyes, glowing focus",
                CharacterReputation = "Master of the arcane",
                CharacterQuickLook1 = "Wears dark sorceress robes",
                CharacterQuickLook2 = "Aura of immense aether",
                CharacterQuickLook3 = "Carries Nightseeker staff",
                CharacterQuickLook4 = "Always reads ancient tomes",
                CharacterQuickLook5 = "Calm and composed demeanor"
            };

            string json = JsonSerializer.Serialize(sheet, new JsonSerializerOptions { WriteIndented = false });
            var deserialized = JsonSerializer.Deserialize<CharacterSheet>(json);

            deserialized.Should().NotBeNull();
            deserialized!.CharacterFullName.Should().Be("Y'shtola Rhul");
            deserialized.CharacterDistinctiveFeatures.Should().Be("Sightless white eyes, glowing focus");
            deserialized.CharacterReputation.Should().Be("Master of the arcane");
            deserialized.CharacterQuickLook1.Should().Be("Wears dark sorceress robes");
            deserialized.CharacterQuickLook2.Should().Be("Aura of immense aether");
            deserialized.CharacterQuickLook3.Should().Be("Carries Nightseeker staff");
            deserialized.CharacterQuickLook4.Should().Be("Always reads ancient tomes");
            deserialized.CharacterQuickLook5.Should().Be("Calm and composed demeanor");
            deserialized.characterQuickLook1.Should().Be("Wears dark sorceress robes");
            deserialized.characterQuickLook5.Should().Be("Calm and composed demeanor");
        }

        [Fact]
        public void SaveAndLoadSheet_PreservesQuickLooksAndResources()
        {
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Estinien Varline",
                CharacterQuickLook1 = "Dragon lance on back",
                CharacterQuickLook2 = "Drachen mail armor",
                CharacterDistinctiveFeatures = "Piercing gaze, silver hair"
            };
            sheet.CharacterResources["Blood of the Dragon"] = new CharacterResource("Blood of the Dragon", 30, 30, resourceType: ResourceType.Counter);

            CharacterSheet.SaveSheet(sheet);

            var loaded = CharacterSheet.LoadSheet("Estinien Varline");
            loaded.Should().NotBeNull();
            loaded!.CharacterFullName.Should().Be("Estinien Varline");
            loaded.CharacterQuickLook1.Should().Be("Dragon lance on back");
            loaded.CharacterQuickLook2.Should().Be("Drachen mail armor");
            loaded.CharacterDistinctiveFeatures.Should().Be("Piercing gaze, silver hair");
            loaded.CharacterResources.Should().ContainKey("Blood of the Dragon");
            loaded.CharacterResources["Blood of the Dragon"].CurrentValue.Should().Be(30);
            loaded.CharacterResources["Blood of the Dragon"].ResourceType.Should().Be(ResourceType.Counter);
        }
    }
}
