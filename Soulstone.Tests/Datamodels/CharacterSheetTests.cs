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
    }
}
