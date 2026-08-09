using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class ScheduleStateTests
{
    [Fact]
    public void PendingWork_CanEnterProductionRunningState()
    {
        using var schedule = new TSchedule();
        TScheduleWork submitted = schedule.WorkCreate(Guid.NewGuid(), "running");
        schedule.Submit(submitted);

        TScheduleWork claimed = schedule.ClaimNext();

        TScheduleItem item = Assert.Single(schedule.RecordsRead());
        Assert.Equal(submitted.WorkId, claimed.WorkId);
        Assert.Equal(LWorkState.LWorkStateRunning, item.State);
    }

    [Fact]
    public void SuccessfulCompletion_ReachesDoneState()
    {
        using var schedule = new TSchedule();
        TScheduleWork submitted = schedule.WorkCreate(Guid.NewGuid(), "success");
        schedule.Submit(submitted);
        TScheduleWork claimed = schedule.ClaimNext();

        schedule.Complete(claimed, succeeded: true);

        TScheduleItem item = Assert.Single(schedule.RecordsRead());
        Assert.Equal(LWorkState.LWorkStateDone, item.State);
        Assert.Equal(string.Empty, item.Message);
    }

    [Fact]
    public void FailedCompletion_PreservesExposedFailureInformation()
    {
        using var schedule = new TSchedule();
        TScheduleWork submitted = schedule.WorkCreate(Guid.NewGuid(), "failure");
        schedule.Submit(submitted);
        TScheduleWork claimed = schedule.ClaimNext();

        schedule.Complete(claimed, succeeded: false, "encoder failed");

        TScheduleItem item = Assert.Single(schedule.RecordsRead());
        Assert.Equal(LWorkState.LWorkStateFailed, item.State);
        Assert.Equal("encoder failed", item.Message);
    }

    [Fact]
    public void StateTransitions_PreserveWorkAndBatchIdentity()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork submitted = schedule.WorkCreate(batchId, "identity");
        schedule.Submit(submitted);

        TScheduleWork claimed = schedule.ClaimNext();
        TScheduleItem running = Assert.Single(schedule.RecordsRead());
        schedule.Complete(claimed, succeeded: true);
        TScheduleItem completed = Assert.Single(schedule.RecordsRead());

        Assert.Equal(submitted.WorkId, running.WorkId);
        Assert.Equal(submitted.WorkId, completed.WorkId);
        Assert.Equal(batchId, running.BatchId);
        Assert.Equal(batchId, completed.BatchId);
    }
}
