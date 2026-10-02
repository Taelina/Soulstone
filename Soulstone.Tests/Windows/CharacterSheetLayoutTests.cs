using System.Numerics;
using FluentAssertions;
using Soulstone.Windows;
using Xunit;

namespace Soulstone.Tests.Windows;

public class CharacterSheetLayoutTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void WideHero_BirthplaceStaysRightOfPortraitAndBelowBadges(float scale)
    {
        var portraitPosition = new Vector2(20, 80);
        var portraitSize = new Vector2(210, 230) * scale;
        var badgesBottom = portraitPosition.Y + 170 * scale;

        var position = CharacterWindow.GetHeroBirthplacePosition(portraitPosition, portraitSize, false, badgesBottom, scale);

        position.X.Should().BeGreaterThan(portraitPosition.X + portraitSize.X);
        position.Y.Should().BeGreaterThan(badgesBottom);
    }

    [Theory]
    [InlineData(1f, 100f)]
    [InlineData(1.5f, 500f)]
    public void CompactHero_BirthplaceStaysBelowPortraitAndWrappedBadges(float scale, float badgesBottom)
    {
        var portraitPosition = new Vector2(20, 80);
        var portraitSize = new Vector2(108, 140) * scale;

        var position = CharacterWindow.GetHeroBirthplacePosition(portraitPosition, portraitSize, true, badgesBottom, scale);

        position.Y.Should().BeGreaterThan(portraitPosition.Y + portraitSize.Y);
        position.Y.Should().BeGreaterThan(badgesBottom);
    }
}
