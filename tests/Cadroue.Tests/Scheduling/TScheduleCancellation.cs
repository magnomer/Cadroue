using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TScheduleCancellation
{
    [Fact]
    public void CancelledQueuedWork_IsNotDispatchedAgain()
    {
        using var schedule = new TSchedule();
        TScheduleWork submitted = schedule.TWorkCreate(Guid.NewGuid(), "cancelled");
        schedule.TScheduleAdd(submitted);
        TScheduleWork claimed = schedule.TScheduleNextClaim();

        Assert.True(schedule.TScheduleCancel(claimed));

        TScheduleItem item = Assert.Single(schedule.TScheduleRecordsRead());
        Assert.Equal(LWorkState.LWorkStateCancelled, item.TScheduleState);
        Assert.Null(schedule.TScheduleTryClaim());
    }

    [Fact]
    public void CancellingOneWorkItem_DoesNotCancelUnrelatedWork()
    {
        using var schedule = new TSchedule();
        TScheduleWork cancelled = schedule.TWorkCreate(Guid.NewGuid(), "cancelled");
        TScheduleWork unrelated = schedule.TWorkCreate(Guid.NewGuid(), "unrelated");
        schedule.TScheduleAdd(cancelled, unrelated);
        TScheduleWork claimed = schedule.TScheduleNextClaim();

        Assert.True(schedule.TScheduleCancel(claimed));

        TScheduleItem[] records = schedule.TScheduleRecordsRead().ToArray();
        Assert.Equal(LWorkState.LWorkStateCancelled,
            Assert.Single(records, item => item.TWorkId == cancelled.TWorkId).TScheduleState);
        Assert.Equal(LWorkState.LWorkStatePending,
            Assert.Single(records, item => item.TWorkId == unrelated.TWorkId).TScheduleState);
        Assert.Equal(unrelated.TWorkId, schedule.TScheduleNextClaim().TWorkId);
    }

    [Fact]
    public void BatchCancellation_AffectsOnlyRequestedBatch()
    {
        using var schedule = new TSchedule();
        Guid cancelledBatch = Guid.NewGuid();
        Guid unrelatedBatch = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(cancelledBatch, "first");
        TScheduleWork second = schedule.TWorkCreate(cancelledBatch, "second");
        TScheduleWork unrelated = schedule.TWorkCreate(unrelatedBatch, "unrelated");
        schedule.TScheduleAdd(first, second, unrelated);
        TScheduleWork firstClaim = schedule.TScheduleNextClaim();
        TScheduleWork secondClaim = schedule.TScheduleNextClaim();

        Assert.Equal(2, schedule.TScheduleCancel(firstClaim, secondClaim));

        TScheduleItem[] records = schedule.TScheduleRecordsRead().ToArray();
        Assert.All(records.Where(item => item.TScheduleBatchId == cancelledBatch),
            item => Assert.Equal(LWorkState.LWorkStateCancelled, item.TScheduleState));
        Assert.Equal(LWorkState.LWorkStatePending,
            Assert.Single(records, item => item.TScheduleBatchId == unrelatedBatch).TScheduleState);
    }

    [Fact]
    public void Reset_MakesOnlyEligibleTerminalWorkRunnableAgain()
    {
        using var schedule = new TSchedule();
        TScheduleWork failed = schedule.TWorkCreate(Guid.NewGuid(), "failed");
        TScheduleWork cancelled = schedule.TWorkCreate(Guid.NewGuid(), "cancelled");
        TScheduleWork done = schedule.TWorkCreate(Guid.NewGuid(), "done");
        schedule.TScheduleAdd(failed, cancelled, done);
        TScheduleWork failedClaim = schedule.TScheduleNextClaim();
        TScheduleWork cancelledClaim = schedule.TScheduleNextClaim();
        TScheduleWork doneClaim = schedule.TScheduleNextClaim();
        schedule.TScheduleCommit(failedClaim, succeeded: false, "failed");
        schedule.TScheduleCancel(cancelledClaim);
        schedule.TScheduleCommit(doneClaim, succeeded: true);

        Assert.True(schedule.TScheduleReset(failed));
        Assert.True(schedule.TScheduleReset(cancelled));
        Assert.False(schedule.TScheduleReset(done));

        Assert.Equal(new[] { failed.TWorkId, cancelled.TWorkId },
            schedule.TSchedulePendingRead().Select(item => item.TWorkId));
        Assert.Equal(LWorkState.LWorkStateDone,
            Assert.Single(schedule.TScheduleRecordsRead(), item => item.TWorkId == done.TWorkId).TScheduleState);
    }

    [Fact]
    public void Reset_DoesNotDuplicateExistingPendingWork()
    {
        using var schedule = new TSchedule();
        TScheduleWork pending = schedule.TWorkCreate(Guid.NewGuid(), "pending");
        schedule.TScheduleAdd(pending);

        Assert.False(schedule.TScheduleReset(pending));

        Assert.Equal(pending.TWorkId, Assert.Single(schedule.TSchedulePendingRead()).TWorkId);
    }
}
