using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TScheduleState
{
    [Fact]
    public void PendingWork_CanEnterProductionRunningState()
    {
        using var schedule = new TSchedule();
        TScheduleWork submitted = schedule.TWorkCreate(Guid.NewGuid(), "running");
        schedule.TScheduleAdd(submitted);

        TScheduleWork claimed = schedule.TScheduleNextClaim();

        TScheduleItem item = Assert.Single(schedule.TScheduleRecordsRead());
        Assert.Equal(submitted.TWorkId, claimed.TWorkId);
        Assert.Equal(LWorkState.LWorkStateRunning, item.TScheduleState);
    }

    [Fact]
    public void SuccessfulCompletion_ReachesDoneState()
    {
        using var schedule = new TSchedule();
        TScheduleWork submitted = schedule.TWorkCreate(Guid.NewGuid(), "success");
        schedule.TScheduleAdd(submitted);
        TScheduleWork claimed = schedule.TScheduleNextClaim();

        schedule.TScheduleCommit(claimed, succeeded: true);

        TScheduleItem item = Assert.Single(schedule.TScheduleRecordsRead());
        Assert.Equal(LWorkState.LWorkStateDone, item.TScheduleState);
        Assert.Equal(string.Empty, item.TScheduleMessage);
    }

    [Fact]
    public void FailedCompletion_PreservesExposedFailureInformation()
    {
        using var schedule = new TSchedule();
        TScheduleWork submitted = schedule.TWorkCreate(Guid.NewGuid(), "failure");
        schedule.TScheduleAdd(submitted);
        TScheduleWork claimed = schedule.TScheduleNextClaim();

        schedule.TScheduleCommit(claimed, succeeded: false, "encoder failed");

        TScheduleItem item = Assert.Single(schedule.TScheduleRecordsRead());
        Assert.Equal(LWorkState.LWorkStateFailed, item.TScheduleState);
        Assert.Equal("encoder failed", item.TScheduleMessage);
    }

    [Fact]
    public void StateTransitions_PreserveWorkAndBatchIdentity()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork submitted = schedule.TWorkCreate(batchId, "identity");
        schedule.TScheduleAdd(submitted);

        TScheduleWork claimed = schedule.TScheduleNextClaim();
        TScheduleItem running = Assert.Single(schedule.TScheduleRecordsRead());
        schedule.TScheduleCommit(claimed, succeeded: true);
        TScheduleItem completed = Assert.Single(schedule.TScheduleRecordsRead());

        Assert.Equal(submitted.TWorkId, running.TWorkId);
        Assert.Equal(submitted.TWorkId, completed.TWorkId);
        Assert.Equal(batchId, running.TScheduleBatchId);
        Assert.Equal(batchId, completed.TScheduleBatchId);
    }
}
