using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TScheduleDuration
{
    [Fact]
    public void DurationSet_BeforeClaim_PersistsIntoScheduledRecord()
    {
        using var schedule = new TSchedule();
        TScheduleWork open = schedule.TWorkCreateOpen(Guid.NewGuid(), "open");
        schedule.TScheduleAdd(open);

        schedule.TScheduleDurationSet(open.TWorkId, TimeSpan.FromSeconds(42));

        Assert.Equal(TimeSpan.FromSeconds(42), schedule.TScheduleReloadRead(open.TWorkId));
    }

    [Fact]
    public void DurationSet_AfterClaim_PersistsIntoRunningRecord()
    {
        using var schedule = new TSchedule();
        TScheduleWork open = schedule.TWorkCreateOpen(Guid.NewGuid(), "open");
        schedule.TScheduleAdd(open);
        TScheduleWork claimed = schedule.TScheduleNextClaim();

        schedule.TScheduleDurationSet(claimed.TWorkId, TimeSpan.FromSeconds(42));

        Assert.Equal(TimeSpan.FromSeconds(42), claimed.TWorkItem.LWorkEnd);
        Assert.Equal(TimeSpan.FromSeconds(42), schedule.TScheduleReloadRead(open.TWorkId));
    }

    [Fact]
    public void DurationSet_DoesNotOverwriteAResolvedDuration()
    {
        using var schedule = new TSchedule();
        TScheduleWork resolved = schedule.TWorkCreate(Guid.NewGuid(), "resolved");
        schedule.TScheduleAdd(resolved);

        schedule.TScheduleDurationSet(resolved.TWorkId, TimeSpan.FromSeconds(99));

        Assert.Equal(TimeSpan.FromSeconds(1), schedule.TScheduleReloadRead(resolved.TWorkId));
    }
}
