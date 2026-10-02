using Soulstone.Managers;
using System;

namespace Soulstone.Utils;

// Presentation only: the result has already been generated and synchronized.
internal sealed class RollReveal
{
    public const double DurationSeconds = 1.5;
    public const double ResultHoldSeconds = 2.5;
    private readonly double startedAt;
    private double? skippedAt;

    public DiceHistoryEntry Result { get; }

    public RollReveal(DiceHistoryEntry result, double startedAt)
    {
        Result = result;
        this.startedAt = startedAt;
    }

    public float Progress(double now) => skippedAt.HasValue ? 1f : (float)Math.Clamp((now - startedAt) / DurationSeconds, 0, 1);

    public bool IsRevealed(double now) => Progress(now) >= 1f;

    public double RevealElapsed(double now) => Math.Max(0, now - (skippedAt ?? startedAt + DurationSeconds));

    public bool ShouldClose(double now) => IsRevealed(now) && RevealElapsed(now) >= ResultHoldSeconds;

    public void Skip(double now) => skippedAt ??= Math.Min(now, startedAt + DurationSeconds);
}
