using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TScheduleRemoval
{
    [Fact]
    public void RemovingOneEligibleItem_RemovesOnlyThatItem()
    {
        using var schedule = new TSchedule();
        TScheduleWork removed = schedule.TWorkCreate(Guid.NewGuid(), "removed");
        TScheduleWork retained = schedule.TWorkCreate(Guid.NewGuid(), "retained");
        schedule.TScheduleAdd(removed, retained);

        Assert.Equal(1, schedule.TScheduleEligibleRemove(removed));

        Assert.Equal(retained.TWorkId, Assert.Single(schedule.TScheduleRecordsRead()).TWorkId);
    }

    [Fact]
    public void RemovingBatch_RemovesOnlyEligibleWorkInRequestedBatch()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork running = schedule.TWorkCreate(batchId, "running");
        TScheduleWork pending = schedule.TWorkCreate(batchId, "pending");
        TScheduleWork unrelated = schedule.TWorkCreate(Guid.NewGuid(), "unrelated");
        schedule.TScheduleAdd(running, pending, unrelated);
        TScheduleWork claimed = schedule.TScheduleNextClaim();

        Assert.Equal(1, schedule.TScheduleEligibleRemove(running, pending));

        TScheduleItem[] records = schedule.TScheduleRecordsRead().ToArray();
        Assert.Equal(LWorkState.LWorkStateRunning,
            Assert.Single(records, item => item.TWorkId == claimed.TWorkId).TScheduleState);
        Assert.DoesNotContain(records, item => item.TWorkId == pending.TWorkId);
        Assert.Equal(LWorkState.LWorkStatePending,
            Assert.Single(records, item => item.TWorkId == unrelated.TWorkId).TScheduleState);
    }

    [Fact]
    public void ClearingCompletedWork_LeavesPendingAndRunningWorkIntact()
    {
        using var schedule = new TSchedule();
        TScheduleWork completed = schedule.TWorkCreate(Guid.NewGuid(), "completed");
        TScheduleWork running = schedule.TWorkCreate(Guid.NewGuid(), "running");
        TScheduleWork pending = schedule.TWorkCreate(Guid.NewGuid(), "pending");
        schedule.TScheduleAdd(completed, running, pending);
        TScheduleWork completedClaim = schedule.TScheduleNextClaim();
        schedule.TScheduleCommit(completedClaim, succeeded: true);
        TScheduleWork runningClaim = schedule.TScheduleNextClaim();

        Assert.Equal(1, schedule.TScheduleDoneClear());

        TScheduleItem[] records = schedule.TScheduleRecordsRead().ToArray();
        Assert.DoesNotContain(records, item => item.TWorkId == completed.TWorkId);
        Assert.Equal(LWorkState.LWorkStateRunning,
            Assert.Single(records, item => item.TWorkId == runningClaim.TWorkId).TScheduleState);
        Assert.Equal(LWorkState.LWorkStatePending,
            Assert.Single(records, item => item.TWorkId == pending.TWorkId).TScheduleState);
    }

    [Fact]
    public void ClearAll_PreservesProtectedRunningWork()
    {
        using var schedule = new TSchedule();
        TScheduleWork running = schedule.TWorkCreate(Guid.NewGuid(), "running");
        TScheduleWork pending = schedule.TWorkCreate(Guid.NewGuid(), "pending");
        schedule.TScheduleAdd(running, pending);
        TScheduleWork claimed = schedule.TScheduleNextClaim();

        Assert.Equal(1, schedule.TScheduleAllClear());

        TScheduleItem item = Assert.Single(schedule.TScheduleRecordsRead());
        Assert.Equal(claimed.TWorkId, item.TWorkId);
        Assert.Equal(LWorkState.LWorkStateRunning, item.TScheduleState);
    }
}
