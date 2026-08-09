using Cadroue.Core;
using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests;

public sealed class WorkRemovalTests
{
    [Fact]
    public void PendingAndDoneWork_AreRemovableWhileRunningWorkIsNot()
    {
        Guid lFirst = Guid.NewGuid();
        Guid lSecond = Guid.NewGuid();
        Guid lRunning = Guid.NewGuid();
        var lScheduleStates = new Dictionary<Guid, LWorkState>
        {
            [lFirst] = LWorkState.LWorkStatePending,
            [lSecond] = LWorkState.LWorkStateDone,
            [lRunning] = LWorkState.LWorkStateRunning
        };

        IReadOnlyList<Guid> lRemovable = TInterface.ScheduleRemovableResolve(
            new[] { lFirst, lSecond, lRunning }, lScheduleStates);

        Assert.Equal(new[] { lFirst, lSecond }, lRemovable);
    }

    [Fact]
    public void PendingWork_IsRemovableWhileUnknownWorkIsSkipped()
    {
        Guid lKnown = Guid.NewGuid();
        var lScheduleStates = new Dictionary<Guid, LWorkState>
        {
            [lKnown] = LWorkState.LWorkStatePending
        };

        IReadOnlyList<Guid> lRemovable = TInterface.ScheduleRemovableResolve(
            new[] { lKnown, Guid.NewGuid() }, lScheduleStates);

        Assert.Equal(new[] { lKnown }, lRemovable);
    }
}
