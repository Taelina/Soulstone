using FluentAssertions;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Xunit;

namespace Soulstone.Tests.Managers;

public class DiceSystemManagerTests
{
    [Fact]
    public void HasMajorSchemaConflict_ReturnsFalseForAdditiveUpdate()
    {
        var current = new DiceSystem();
        current.AddAttribute("Strength");
        current.AddSkill("Athletics");
        var updated = new DiceSystem();
        updated.AddAttribute("Strength");
        updated.AddSkill("Athletics");
        updated.AddSkill("Acrobatics");

        DiceSystemManager.HasMajorSchemaConflict(current, updated).Should().BeFalse();
    }

    [Fact]
    public void HasMajorSchemaConflict_ReturnsTrueWhenAtLeastHalfTheSchemaIsRemoved()
    {
        var current = new DiceSystem();
        current.AddAttribute("Strength");
        current.AddAttribute("Dexterity");
        var updated = new DiceSystem();
        updated.AddAttribute("Strength");

        DiceSystemManager.HasMajorSchemaConflict(current, updated).Should().BeTrue();
    }
}
