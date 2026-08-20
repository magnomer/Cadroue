using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class ScheduleDurationTests
{
    [Fact]
    public void DurationSet_BeforeClaim_PersistsIntoScheduledRecord()
    {
        using var schedule = new TSchedule();
        TScheduleWork open = schedule.WorkCreateOpen(Guid.NewGuid(), "open");
        schedule.Submit(open);

        schedule.DurationSet(open.WorkId, TimeSpan.FromSeconds(42));

        Assert.Equal(TimeSpan.FromSeconds(42), schedule.ReloadedDurationRead(open.WorkId));
    }

    [Fact]
    public void DurationSet_AfterClaim_PersistsIntoRunningRecord()
    {
        using var schedule = new TSchedule();
        TScheduleWork open = schedule.WorkCreateOpen(Guid.NewGuid(), "open");
        schedule.Submit(open);
        TScheduleWork claimed = schedule.ClaimNext();

        schedule.DurationSet(claimed.WorkId, TimeSpan.FromSeconds(42));

        Assert.Equal(TimeSpan.FromSeconds(42), claimed.WorkItem.LWorkEnd);
        Assert.Equal(TimeSpan.FromSeconds(42), schedule.ReloadedDurationRead(open.WorkId));
    }

    [Fact]
    public void DurationSet_DoesNotOverwriteAResolvedDuration()
    {
        using var schedule = new TSchedule();
        TScheduleWork resolved = schedule.WorkCreate(Guid.NewGuid(), "resolved");
        schedule.Submit(resolved);

        schedule.DurationSet(resolved.WorkId, TimeSpan.FromSeconds(99));

        Assert.Equal(TimeSpan.FromSeconds(1), schedule.ReloadedDurationRead(resolved.WorkId));
    }
}
