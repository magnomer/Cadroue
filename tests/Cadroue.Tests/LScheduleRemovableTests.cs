using Cadroue.Core;
using Cadroue.Infrastructure;
using Xunit;

namespace Cadroue.Tests;

public sealed class LScheduleRemovableTests
{
    [Fact]
    public void LScheduleRemovableResolveKeepsOnlyNonRunningIds()
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

        IReadOnlyList<Guid> lRemovable = LSchedule.LScheduleRemovableResolve(
            new[] { lFirst, lSecond, lRunning }, lScheduleStates);

        Assert.Equal(new[] { lFirst, lSecond }, lRemovable);
    }

    [Fact]
    public void LScheduleRemovableResolveSkipsUnknownIds()
    {
        Guid lKnown = Guid.NewGuid();
        var lScheduleStates = new Dictionary<Guid, LWorkState>
        {
            [lKnown] = LWorkState.LWorkStatePending
        };

        IReadOnlyList<Guid> lRemovable = LSchedule.LScheduleRemovableResolve(
            new[] { lKnown, Guid.NewGuid() }, lScheduleStates);

        Assert.Equal(new[] { lKnown }, lRemovable);
    }
}
