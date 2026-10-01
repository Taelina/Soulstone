using FluentAssertions;
using Soulstone.Utils;
using Xunit;

namespace Soulstone.Tests.Utils;

public class DeleteConfirmationTests : IDisposable
{
    public DeleteConfirmationTests()
    {
        DeleteConfirmation.Cancel();
    }

    public void Dispose()
    {
        DeleteConfirmation.Cancel();
    }

    [Fact]
    public void Confirm_ExecutesPendingActionOnce()
    {
        var executionCount = 0;
        DeleteConfirmation.Request(() => executionCount++);

        DeleteConfirmation.Confirm();
        DeleteConfirmation.Confirm();

        executionCount.Should().Be(1);
        DeleteConfirmation.IsPending.Should().BeFalse();
    }

    [Fact]
    public void Cancel_DiscardsPendingAction()
    {
        var wasExecuted = false;
        DeleteConfirmation.Request(() => wasExecuted = true);

        DeleteConfirmation.Cancel();
        DeleteConfirmation.Confirm();

        wasExecuted.Should().BeFalse();
        DeleteConfirmation.IsPending.Should().BeFalse();
    }
}
