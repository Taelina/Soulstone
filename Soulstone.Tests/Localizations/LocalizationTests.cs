using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using Soulstone.Localizations;

namespace Soulstone.Tests.Localizations
{
    public class LocalizationTests
    {
        [Theory]
        [InlineData("NewFamilyMemberModalTitle", "Add Family Member", "Ajouter un membre de la famille")]
        [InlineData("NewFriendModalTitle", "Add Friend", "Ajouter un ami")]
        [InlineData("NewEnemyModalTitle", "Add Enemy", "Ajouter un ennemi")]
        [InlineData("NewAttributeModalTitle", "Add Attribute", "Ajouter un attribut")]
        [InlineData("NewSkillModalTitle", "Add Skill", "Ajouter une compétence")]
        [InlineData("NewAbilityModalTitle", "Create / Edit Ability", "Créer / Modifier une capacité")]
        [InlineData("DiceSysUpdateTitle", "Update Dice System", "Mettre à jour le système de dés")]
        public void ModalTitles_AreAvailableInBothEmbeddedLanguages(string key, string english, string french)
        {
            var manager = new Soulstone.Managers.LocalizationManager();

            manager.LocalizedLanguages[Language.English].LocalizedStrings
                .Should().ContainKey(key).WhoseValue.Should().Be(english);
            manager.LocalizedLanguages[Language.Français].LocalizedStrings
                .Should().ContainKey(key).WhoseValue.Should().Be(french);
        }

        [Theory]
        [InlineData(Language.Français, 0)]
        [InlineData(Language.English, 1)]
        public void LanguageEnum_MatchesExpectedIntegerValues(Language language, int expectedValue)
        {
            ((int)language).Should().Be(expectedValue);
        }

        [Fact]
        public void Localization_Properties_SetAndGetCorrectly()
        {
            // Arrange
            var localization = new Localization();
            var dict = new Dictionary<string, string>
            {
                { "TestKey", "TestValue" }
            };

            // Act
            localization.Language = Language.English;
            localization.LocalizedStrings = dict;

            // Assert
            localization.Language.Should().Be(Language.English);
            localization.LocalizedStrings.Should().BeSameAs(dict);
            localization.LocalizedStrings.Should().ContainKey("TestKey").WhoseValue.Should().Be("TestValue");
        }
    }
}
