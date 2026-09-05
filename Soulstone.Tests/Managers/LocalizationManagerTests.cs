using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Soulstone.Localizations;
using Soulstone.Managers;

namespace Soulstone.Tests.Managers
{
    [Collection("NonParallel")]
    public class LocalizationManagerTests
    {
        private readonly Soulstone.Configuration configuration;
        private readonly Plugin plugin;

        public LocalizationManagerTests()
        {
            TestHelper.EnsureMockServices();

            configuration = new Soulstone.Configuration
            {
                Language = Language.English
            };

            plugin = (Plugin)RuntimeHelpers.GetUninitializedObject(typeof(Plugin));
            typeof(Plugin).GetProperty(nameof(Plugin.Configuration))?.SetValue(plugin, configuration);

            LocalizationManager.Instance.LocalizedLanguages.Clear();
            LocalizationManager.Instance.InitLoc(plugin);
        }

        [Fact]
        public void Instance_ShouldReturnSingletonInstance()
        {
            // Act
            var instance1 = LocalizationManager.Instance;
            var instance2 = LocalizationManager.Instance;

            // Assert
            instance1.Should().NotBeNull();
            instance1.Should().BeSameAs(instance2);
        }

        [Fact]
        public void InitLoc_ShouldLoadBothFrenchAndEnglishFromEmbeddedResources()
        {
            // Assert
            LocalizationManager.Instance.LocalizedLanguages.Should().ContainKey(Language.Français);
            LocalizationManager.Instance.LocalizedLanguages.Should().ContainKey(Language.English);

            var french = LocalizationManager.Instance.LocalizedLanguages[Language.Français];
            var english = LocalizationManager.Instance.LocalizedLanguages[Language.English];

            french.LocalizedStrings.Should().NotBeEmpty();
            english.LocalizedStrings.Should().NotBeEmpty();
            french.LocalizedStrings.Count.Should().BeGreaterThan(100);
            english.LocalizedStrings.Count.Should().BeGreaterThan(100);
        }

        [Fact]
        public void LocalizationDictionnaries_ShouldHaveMatchingKeys()
        {
            // Arrange
            var frenchKeys = LocalizationManager.Instance.LocalizedLanguages[Language.Français].LocalizedStrings.Keys;
            var englishKeys = LocalizationManager.Instance.LocalizedLanguages[Language.English].LocalizedStrings.Keys;

            // Assert
            frenchKeys.Should().BeEquivalentTo(englishKeys, "both languages should provide translations for the same keys");
        }

        [Theory]
        [InlineData("AddButton", "+")]
        [InlineData("DiceRollTab", "Dice Thrower")]
        [InlineData("EditStatCheckbox", "Edit stats")]
        [InlineData("StatTempHeader", "Temp (+Dice)")]
        [InlineData("StatEpicHeader", "Epic (+Succ)")]
        [InlineData("EpicAttributesCheckbox", "System handles epic attributes")]
        [InlineData("CancelButton", "Cancel")]
        [InlineData("CloseButton", "Close")]
        [InlineData("DeleteButton", "Delete")]
        [InlineData("RemoveTooltip", "Remove")]
        [InlineData("NoneText", "None")]
        [InlineData("NoneOption", "(None)")]
        [InlineData("UnnamedCharacter", "Unnamed Character")]
        [InlineData("ConfigWindowTitle", "Soulstone Settings")]
        [InlineData("ItemTypeGeneral", "General")]
        [InlineData("ItemTypeConsumable", "Consumable")]
        [InlineData("RarityLegendary", "Legendary")]
        [InlineData("FileBrowserTitle", "Select File")]
        [InlineData("GearTab", "Gear")]
        [InlineData("EquipButton", "Equip")]
        [InlineData("UnequipButton", "Unequip")]
        [InlineData("EquippedBadge", "EQUIPPED")]
        [InlineData("DiceSysResourcesHeader", "Configurable Resources")]
        [InlineData("InitiativeTrackerTitle", "Initiative Tracker")]
        [InlineData("InitiativeNextTurn", "Next Turn")]
        [InlineData("InitiativeReset", "Reset Turns")]
        public void GetLocalizedString_InEnglish_ReturnsEnglishTranslation(string key, string expectedValue)
        {
            // Arrange
            configuration.Language = Language.English;

            // Act
            string result = LocalizationManager.Instance.GetLocalizedString(key);

            // Assert
            result.Should().Be(expectedValue);
        }

        [Theory]
        [InlineData("AddButton", "+")]
        [InlineData("DiceRollTab", "Lanceur de dés")]
        [InlineData("EditStatCheckbox", "Editer les stats du personnage")]
        [InlineData("StatTempHeader", "Temp (+Dés)")]
        [InlineData("StatEpicHeader", "Épique (+Succès)")]
        [InlineData("EpicAttributesCheckbox", "Le système gère les attributs épiques")]
        [InlineData("CancelButton", "Annuler")]
        [InlineData("CloseButton", "Fermer")]
        [InlineData("DeleteButton", "Supprimer")]
        [InlineData("RemoveTooltip", "Supprimer")]
        [InlineData("NoneText", "Aucun")]
        [InlineData("NoneOption", "(Aucun)")]
        [InlineData("UnnamedCharacter", "Personnage sans nom")]
        [InlineData("ConfigWindowTitle", "Soulstone - Paramètres")]
        [InlineData("ItemTypeGeneral", "Général")]
        [InlineData("ItemTypeConsumable", "Consommable")]
        [InlineData("RarityLegendary", "Légendaire")]
        [InlineData("FileBrowserTitle", "Sélectionner un fichier")]
        [InlineData("GearTab", "Équipement")]
        [InlineData("EquipButton", "Équiper")]
        [InlineData("UnequipButton", "Déséquiper")]
        [InlineData("EquippedBadge", "ÉQUIPÉ")]
        [InlineData("DiceSysResourcesHeader", "Ressources Configurables")]
        [InlineData("InitiativeTrackerTitle", "Suivi d'Initiative")]
        [InlineData("InitiativeNextTurn", "Tour suivant")]
        [InlineData("InitiativeReset", "Réinitialiser les tours")]
        public void GetLocalizedString_InFrench_ReturnsFrenchTranslation(string key, string expectedValue)
        {
            // Arrange
            configuration.Language = Language.Français;

            // Act
            string result = LocalizationManager.Instance.GetLocalizedString(key);

            // Assert
            result.Should().Be(expectedValue);
        }

        [Fact]
        public void GetLocalizedString_WithFormattingArgs_FormatsProperly()
        {
            // Arrange
            configuration.Language = Language.English;

            // Act
            string resultEn = LocalizationManager.Instance.GetLocalizedString("InitiativeRound", 3);
            configuration.Language = Language.Français;
            string resultFr = LocalizationManager.Instance.GetLocalizedString("InitiativeRound", 3);

            // Assert
            resultEn.Should().Be("Round: 3");
            resultFr.Should().Be("Tour de table : 3");
        }

        [Fact]
        public void GetLocalizedString_WhenKeyDoesNotExist_ReturnsKeyNameAsFallback()
        {
            // Arrange
            configuration.Language = Language.English;

            // Act
            string result = LocalizationManager.Instance.GetLocalizedString("NonExistentKey");

            // Assert
            result.Should().Be("NonExistentKey");
        }

        [Fact]
        public void GetLocalizedString_WhenKeyMissingInFrench_FallsBackToEnglish()
        {
            // Arrange
            configuration.Language = Language.Français;
            var frenchLoc = LocalizationManager.Instance.LocalizedLanguages[Language.Français];
            var englishLoc = LocalizationManager.Instance.LocalizedLanguages[Language.English];

            englishLoc.LocalizedStrings["EnglishOnlyKey"] = "English Fallback Value";
            frenchLoc.LocalizedStrings.Remove("EnglishOnlyKey");

            // Act
            string result = LocalizationManager.Instance.GetLocalizedString("EnglishOnlyKey");

            // Assert
            result.Should().Be("English Fallback Value");
        }

        [Fact]
        public void LoadFromDirectory_ShouldMergeAndOverrideTranslations()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), "SoulstoneLocTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var customEn = new Dictionary<string, string>
                {
                    { "CustomOverrideKey", "Custom English Value" },
                    { "AddButton", "Custom Plus" }
                };
                File.WriteAllText(Path.Combine(tempDir, "en.json"), JsonSerializer.Serialize(customEn));

                // Act
                LocalizationManager.Instance.LoadFromDirectory(tempDir);
                configuration.Language = Language.English;

                // Assert
                LocalizationManager.Instance.GetLocalizedString("CustomOverrideKey").Should().Be("Custom English Value");
                LocalizationManager.Instance.GetLocalizedString("AddButton").Should().Be("Custom Plus");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                LocalizationManager.Instance.Reload();
            }
        }

        [Theory]
        [InlineData(Language.Français, "fr")]
        [InlineData(Language.English, "en")]
        public void LanguageExtensions_GetCode_ReturnsCorrectCode(Language lang, string expectedCode)
        {
            lang.GetCode().Should().Be(expectedCode);
        }

        [Theory]
        [InlineData("fr", Language.Français)]
        [InlineData("FR", Language.Français)]
        [InlineData("french", Language.Français)]
        [InlineData("en", Language.English)]
        [InlineData("EN", Language.English)]
        [InlineData("english", Language.English)]
        [InlineData("unknown", Language.English)]
        public void LanguageExtensions_FromCode_ParsesCorrectly(string code, Language expectedLang)
        {
            LanguageExtensions.FromCode(code).Should().Be(expectedLang);
        }
    }
}
