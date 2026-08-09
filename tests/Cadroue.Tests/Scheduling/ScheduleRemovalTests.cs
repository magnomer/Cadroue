using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class ScheduleRemovalTests
{
    [Fact]
    public void RemovingOneEligibleItem_RemovesOnlyThatItem()
    {
        using var schedule = new TSchedule();
        TScheduleWork removed = schedule.WorkCreate(Guid.NewGuid(), "removed");
        TScheduleWork retained = schedule.WorkCreate(Guid.NewGuid(), "retained");
        schedule.Submit(removed, retained);

        Assert.Equal(1, schedule.RemoveEligible(removed));

        Assert.Equal(retained.WorkId, Assert.Single(schedule.RecordsRead()).WorkId);
    }

    [Fact]
    public void RemovingBatch_RemovesOnlyEligibleWorkInRequestedBatch()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork running = schedule.WorkCreate(batchId, "running");
        TScheduleWork pending = schedule.WorkCreate(batchId, "pending");
        TScheduleWork unrelated = schedule.WorkCreate(Guid.NewGuid(), "unrelated");
        schedule.Submit(running, pending, unrelated);
        TScheduleWork claimed = schedule.ClaimNext();

        Assert.Equal(1, schedule.RemoveEligible(running, pending));

        TScheduleItem[] records = schedule.RecordsRead().ToArray();
        Assert.Equal(LWorkState.LWorkStateRunning,
            Assert.Single(records, item => item.WorkId == claimed.WorkId).State);
        Assert.DoesNotContain(records, item => item.WorkId == pending.WorkId);
        Assert.Equal(LWorkState.LWorkStatePending,
            Assert.Single(records, item => item.WorkId == unrelated.WorkId).State);
    }

    [Fact]
    public void ClearingCompletedWork_LeavesPendingAndRunningWorkIntact()
    {
        using var schedule = new TSchedule();
        TScheduleWork completed = schedule.WorkCreate(Guid.NewGuid(), "completed");
        TScheduleWork running = schedule.WorkCreate(Guid.NewGuid(), "running");
        TScheduleWork pending = schedule.WorkCreate(Guid.NewGuid(), "pending");
        schedule.Submit(completed, running, pending);
        TScheduleWork completedClaim = schedule.ClaimNext();
        schedule.Complete(completedClaim, succeeded: true);
        TScheduleWork runningClaim = schedule.ClaimNext();

        Assert.Equal(1, schedule.ClearCompleted());

        TScheduleItem[] records = schedule.RecordsRead().ToArray();
        Assert.DoesNotContain(records, item => item.WorkId == completed.WorkId);
        Assert.Equal(LWorkState.LWorkStateRunning,
            Assert.Single(records, item => item.WorkId == runningClaim.WorkId).State);
        Assert.Equal(LWorkState.LWorkStatePending,
            Assert.Single(records, item => item.WorkId == pending.WorkId).State);
    }

    [Fact]
    public void ClearAll_PreservesProtectedRunningWork()
    {
        using var schedule = new TSchedule();
        TScheduleWork running = schedule.WorkCreate(Guid.NewGuid(), "running");
        TScheduleWork pending = schedule.WorkCreate(Guid.NewGuid(), "pending");
        schedule.Submit(running, pending);
        TScheduleWork claimed = schedule.ClaimNext();

        Assert.Equal(1, schedule.ClearAll());

        TScheduleItem item = Assert.Single(schedule.RecordsRead());
        Assert.Equal(claimed.WorkId, item.WorkId);
        Assert.Equal(LWorkState.LWorkStateRunning, item.State);
    }
}
