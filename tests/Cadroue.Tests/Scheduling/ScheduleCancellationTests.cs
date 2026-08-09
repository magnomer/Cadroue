using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class ScheduleCancellationTests
{
    [Fact]
    public void CancelledQueuedWork_IsNotDispatchedAgain()
    {
        using var schedule = new TSchedule();
        TScheduleWork submitted = schedule.WorkCreate(Guid.NewGuid(), "cancelled");
        schedule.Submit(submitted);
        TScheduleWork claimed = schedule.ClaimNext();

        Assert.True(schedule.Cancel(claimed));

        TScheduleItem item = Assert.Single(schedule.RecordsRead());
        Assert.Equal(LWorkState.LWorkStateCancelled, item.State);
        Assert.Null(schedule.TryClaimNext());
    }

    [Fact]
    public void CancellingOneWorkItem_DoesNotCancelUnrelatedWork()
    {
        using var schedule = new TSchedule();
        TScheduleWork cancelled = schedule.WorkCreate(Guid.NewGuid(), "cancelled");
        TScheduleWork unrelated = schedule.WorkCreate(Guid.NewGuid(), "unrelated");
        schedule.Submit(cancelled, unrelated);
        TScheduleWork claimed = schedule.ClaimNext();

        Assert.True(schedule.Cancel(claimed));

        TScheduleItem[] records = schedule.RecordsRead().ToArray();
        Assert.Equal(LWorkState.LWorkStateCancelled,
            Assert.Single(records, item => item.WorkId == cancelled.WorkId).State);
        Assert.Equal(LWorkState.LWorkStatePending,
            Assert.Single(records, item => item.WorkId == unrelated.WorkId).State);
        Assert.Equal(unrelated.WorkId, schedule.ClaimNext().WorkId);
    }

    [Fact]
    public void BatchCancellation_AffectsOnlyRequestedBatch()
    {
        using var schedule = new TSchedule();
        Guid cancelledBatch = Guid.NewGuid();
        Guid unrelatedBatch = Guid.NewGuid();
        TScheduleWork first = schedule.WorkCreate(cancelledBatch, "first");
        TScheduleWork second = schedule.WorkCreate(cancelledBatch, "second");
        TScheduleWork unrelated = schedule.WorkCreate(unrelatedBatch, "unrelated");
        schedule.Submit(first, second, unrelated);
        TScheduleWork firstClaim = schedule.ClaimNext();
        TScheduleWork secondClaim = schedule.ClaimNext();

        Assert.Equal(2, schedule.Cancel(firstClaim, secondClaim));

        TScheduleItem[] records = schedule.RecordsRead().ToArray();
        Assert.All(records.Where(item => item.BatchId == cancelledBatch),
            item => Assert.Equal(LWorkState.LWorkStateCancelled, item.State));
        Assert.Equal(LWorkState.LWorkStatePending,
            Assert.Single(records, item => item.BatchId == unrelatedBatch).State);
    }

    [Fact]
    public void Reset_MakesOnlyEligibleTerminalWorkRunnableAgain()
    {
        using var schedule = new TSchedule();
        TScheduleWork failed = schedule.WorkCreate(Guid.NewGuid(), "failed");
        TScheduleWork cancelled = schedule.WorkCreate(Guid.NewGuid(), "cancelled");
        TScheduleWork done = schedule.WorkCreate(Guid.NewGuid(), "done");
        schedule.Submit(failed, cancelled, done);
        TScheduleWork failedClaim = schedule.ClaimNext();
        TScheduleWork cancelledClaim = schedule.ClaimNext();
        TScheduleWork doneClaim = schedule.ClaimNext();
        schedule.Complete(failedClaim, succeeded: false, "failed");
        schedule.Cancel(cancelledClaim);
        schedule.Complete(doneClaim, succeeded: true);

        Assert.True(schedule.Reset(failed));
        Assert.True(schedule.Reset(cancelled));
        Assert.False(schedule.Reset(done));

        Assert.Equal(new[] { failed.WorkId, cancelled.WorkId },
            schedule.PendingRead().Select(item => item.WorkId));
        Assert.Equal(LWorkState.LWorkStateDone,
            Assert.Single(schedule.RecordsRead(), item => item.WorkId == done.WorkId).State);
    }

    [Fact]
    public void Reset_DoesNotDuplicateExistingPendingWork()
    {
        using var schedule = new TSchedule();
        TScheduleWork pending = schedule.WorkCreate(Guid.NewGuid(), "pending");
        schedule.Submit(pending);

        Assert.False(schedule.Reset(pending));

        Assert.Equal(pending.WorkId, Assert.Single(schedule.PendingRead()).WorkId);
    }
}
