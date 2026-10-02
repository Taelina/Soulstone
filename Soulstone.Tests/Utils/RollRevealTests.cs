using FluentAssertions;
using Soulstone.Managers;
using Soulstone.Utils;
using Xunit;

namespace Soulstone.Tests.Utils;

public class RollRevealTests
{
    [Fact]
    public void Reveal_WaitsForAnimationBeforeShowingTheResult()
    {
        var reveal = new RollReveal(new DiceHistoryEntry { Total = 23 }, 10);

        reveal.Progress(9).Should().Be(0);
        reveal.IsRevealed(10).Should().BeFalse();
        reveal.IsRevealed(10.5).Should().BeFalse();
        reveal.IsRevealed(10 + RollReveal.DurationSeconds).Should().BeTrue();
        reveal.Progress(100).Should().Be(1);
    }

    [Fact]
    public void Skip_RevealsTheExistingResultWithoutChangingIt()
    {
        var result = new DiceHistoryEntry { Total = -2, Details = "1, 4", IsPrivate = true };
        var reveal = new RollReveal(result, 10);

        reveal.Skip(10);
        reveal.Skip(10);

        reveal.IsRevealed(10).Should().BeTrue();
        reveal.Result.Should().BeSameAs(result);
        reveal.Result.Total.Should().Be(-2);
        reveal.Result.Details.Should().Be("1, 4");
        reveal.Result.IsPrivate.Should().BeTrue();
    }

    [Fact]
    public void Result_StaysVisibleForTheHoldDurationBeforeClosing()
    {
        var reveal = new RollReveal(new DiceHistoryEntry(), 10);
        var revealedAt = 10 + RollReveal.DurationSeconds;

        reveal.ShouldClose(10).Should().BeFalse();
        reveal.ShouldClose(revealedAt).Should().BeFalse();
        reveal.ShouldClose(revealedAt + RollReveal.ResultHoldSeconds - 0.01).Should().BeFalse();
        reveal.ShouldClose(revealedAt + RollReveal.ResultHoldSeconds).Should().BeTrue();
    }

    [Fact]
    public void Skipping_StartsTheHoldTimerImmediately_AndRepeatedClicksDoNotExtendIt()
    {
        var reveal = new RollReveal(new DiceHistoryEntry(), 10);
        reveal.Skip(10.25);
        reveal.Skip(11);

        reveal.ShouldClose(10.25 + RollReveal.ResultHoldSeconds - 0.01).Should().BeFalse();
        reveal.ShouldClose(10.25 + RollReveal.ResultHoldSeconds).Should().BeTrue();
    }

    [Fact]
    public void SkippingAfterTheReveal_DoesNotRestartTheCloseTimer()
    {
        var reveal = new RollReveal(new DiceHistoryEntry(), 10);
        var revealedAt = 10 + RollReveal.DurationSeconds;
        reveal.Skip(revealedAt + 1);

        reveal.ShouldClose(revealedAt + RollReveal.ResultHoldSeconds).Should().BeTrue();
    }
}
