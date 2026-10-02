using FluentAssertions;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Windows;
using Xunit;

namespace Soulstone.Tests.Windows;

[Collection("NonParallelCollection")]
public class RollPresentationWindowTests
{
    [Fact]
    public void ClosingARequest_KeepsItPendingAndDoesNotReopenItAutomatically()
    {
        var pending = PartySyncManager.Instance.PendingRollRequests;
        pending.Clear();
        var request = new RollRequestPayload { RollName = "Stealth" };
        pending[request.RequestId] = request;
        using var window = new RollPresentationWindow(new Soulstone.Configuration());
        try
        {
            window.ProcessPending();
            window.IsOpen.Should().BeTrue();

            window.IsOpen = false;
            window.ProcessPending();
            window.IsOpen.Should().BeFalse();
            pending.Should().ContainKey(request.RequestId);

            window.OpenRequest(request);
            window.IsOpen.Should().BeTrue();

            pending.Clear();
            window.ProcessPending();
            window.IsOpen.Should().BeFalse();
        }
        finally
        {
            pending.Clear();
        }
    }

    [Fact]
    public void RemoteRolls_DoNotOpenTheLocalRollPanel()
    {
        PartySyncManager.Instance.PendingRollRequests.Clear();
        using var window = new RollPresentationWindow(new Soulstone.Configuration());
        var history = DiceHistoryManager.Instance;
        try
        {
            history.AddEntry(new DiceHistoryEntry { IsLocal = false, Total = 20 });
            window.ProcessPending();
            window.IsOpen.Should().BeFalse();
        }
        finally
        {
            history.Clear();
        }
    }

    [Fact]
    public void DisabledPresentation_DropsLocalAnimationsWithoutChangingHistory()
    {
        using var window = new RollPresentationWindow(new Soulstone.Configuration { ShowRollPresentation = false });
        var history = DiceHistoryManager.Instance;
        history.Clear();
        try
        {
            var result = new DiceHistoryEntry { IsLocal = true, IsPrivate = true, Total = 12 };
            history.AddEntry(result);
            window.ProcessPending();

            window.IsOpen.Should().BeFalse();
            history.GetHistory().Should().ContainSingle().Which.Should().BeSameAs(result);
        }
        finally
        {
            history.Clear();
        }
    }
}
