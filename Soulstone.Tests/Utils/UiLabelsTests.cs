using FluentAssertions;
using Soulstone.Datamodels;
using Soulstone.Localizations;
using Soulstone.Managers;
using Soulstone.Utils;
using Xunit;

namespace Soulstone.Tests.Utils;

[Collection("NonParallel")]
public class UiLabelsTests : IDisposable
{
    private readonly Soulstone.Configuration configuration = new() { Language = Language.Français };

    public UiLabelsTests()
    {
        TestHelper.EnsureMockServices();
        LocalizationManager.Instance.LoadEmbeddedLanguages();
        LocalizationManager.Instance.InitLoc(configuration);
    }

    public void Dispose() => LocalizationManager.Instance.InitLoc(new Soulstone.Configuration { Language = Language.English });

    [Theory]
    [InlineData("General", "Général")]
    [InlineData("magic", "Magie")]
    [InlineData("Class", "Classe")]
    [InlineData("Custom", "Personnalisé")]
    [InlineData("My custom category", "My custom category")]
    public void FeatCategories_LocalizeBuiltInsAndPreserveCustomNames(string value, string expected)
    {
        UiLabels.FeatCategory(value).Should().Be(expected);
    }

    [Fact]
    public void BuiltInDisplays_UpdateWhenLanguageChanges_WithoutChangingStoredValues()
    {
        var gear = new GearItem("Test", "MainHand", rarity: "Legendary");
        UiLabels.Slot(gear.Slot).Should().Be("Main principale");
        UiLabels.Rarity(gear.Rarity).Should().Be("Légendaire");
        UiLabels.SystemType(SystemType.DicePoolSystem).Should().Be("Réserve de dés");

        configuration.Language = Language.English;
        UiLabels.Slot(gear.Slot).Should().Be("Main Hand");
        UiLabels.Rarity(gear.Rarity).Should().Be("Legendary");
        UiLabels.SystemType(SystemType.DicePoolSystem).Should().Be("Dice pool");
        gear.Slot.Should().Be("MainHand");
        gear.Rarity.Should().Be("Legendary");
    }

    [Fact]
    public void CustomSlotsAndRarities_ArePreserved()
    {
        UiLabels.Slot("Custom Implant").Should().Be("Custom Implant");
        UiLabels.Rarity("Unique").Should().Be("Unique");
        UiLabels.Slot("mainhand").Should().Be("Main principale");
        UiLabels.Slot("General").Should().Be("Général");
    }

    [Fact]
    public void RelayStatus_IsLocalized()
    {
        UiLabels.ConnectionStatus("Connection failed").Should().Be("Échec de la connexion");
        UiLabels.ConnectionStatus("Custom status").Should().Be("Custom status");
    }

    [Fact]
    public void BuffChoices_IncludeCharacterAndSystemStats_DeduplicateAndDoNotPopulateTheSheet()
    {
        var sheet = new CharacterSheet();
        sheet.characterAttributes["Strength"] = new Soulstone.Datamodels.Attribute("Strength", 10);
        sheet.characterSkills["Stealth"] = new Skill("Stealth", 2);
        var system = new DiceSystem();
        system.SystemAttributes["strength"] = new Soulstone.Datamodels.Attribute("strength", 10);
        system.SystemAbilities["Fireball"] = new Ability { abilityName = "Fireball" };
        system.SystemResources.Add(new ResourceDefinition { Name = "Mana" });

        var targets = UiLabels.BuffTargets(sheet, system);

        targets.Should().Contain(["All", "Initiative", "Strength", "Stealth", "Fireball", "Mana"]);
        targets.Count(t => t.Equals("Strength", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
        sheet.characterAbilities.Should().BeEmpty();
        sheet.characterResources.Should().BeEmpty();
        targets.Should().NotContain("Tous", "dropdown values must remain compatible with stored modifier keys");
    }

    [Fact]
    public void BuffModifiers_LocalizeReservedTargetsAndPreserveCustomStatNames()
    {
        var buff = new Buff("Effect", 3, new Dictionary<string, int> { ["All"] = 2, ["Stealth"] = -1 });

        UiLabels.BuffModifiers(buff).Should().Be("+2 Tous, -1 Stealth");
        buff.StatModifiers.Should().ContainKey("All").WhoseValue.Should().Be(2);
        UiLabels.BuffTarget("all").Should().Be("Tous");
    }

    [Fact]
    public void BuiltInOptions_HaveTranslationsInBothLanguages()
    {
        string[] categories = ["General", "Combat", "Magic", "Passive", "Active", "Origin", "Racial", "Class", "Custom"];
        string[] rarities = ["Common", "Uncommon", "Rare", "Epic", "Legendary", "Artifact"];
        var keys = categories.Select(v => "FeatCategory" + v)
            .Concat(rarities.Select(v => "Rarity" + v))
            .Concat(GearItem.StandardSlots.Concat(GearItem.StandardAugmentationSlots).Select(v => "Slot" + v))
            .Concat(Enum.GetValues<SystemType>().Select(v => "SystemType" + v))
            .Concat(["NoAugmentationsInstalled", "DuplicateButton", "DiceSysResourcePreviewHeader", "SaveButton"]);

        foreach (var language in new[] { Language.English, Language.Français })
        {
            var strings = LocalizationManager.Instance.LocalizedLanguages[language].LocalizedStrings;
            foreach (var key in keys)
                strings.Should().ContainKey(key).WhoseValue.Should().NotBeNullOrWhiteSpace();
        }
    }
}
