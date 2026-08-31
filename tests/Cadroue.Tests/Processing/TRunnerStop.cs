using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TRunnerStop
{
    [Fact]
    public void Stop_TerminatesCurrentWorkAndDoesNotCompleteItsPartialOutput()
    {
        using var runner = new TRunner();
        Guid workId = runner.TWorkAdd("stop-current", steps: 100);
        runner.TRunnerStart();
        runner.TRunnerProgressRead(workId, 0.01);
        Assert.True(runner.TRunnerRead(workId).TRunnerOutputFlag);

        runner.TRunnerStop();

        TRunnerWork stopped = runner.TRunnerStateRead(workId, TRunnerWorkState.TRunnerPending);
        stopped = runner.TRunnerRemovedRead(workId);
        Assert.False(runner.TRunnerRunning);
        Assert.NotEqual(TRunnerWorkState.TRunnerDone, stopped.TRunnerState);
        Assert.False(stopped.TRunnerOutputFlag);
    }

    [Fact]
    public void Stop_PreventsQueuedWorkFromContinuing()
    {
        using var runner = new TRunner();
        Guid currentId = runner.TWorkAdd("stop-active", steps: 100);
        Guid queuedId = runner.TWorkAdd("stop-queued", steps: 10);
        runner.TRunnerStart();
        runner.TRunnerProgressRead(currentId, 0.01);

        runner.TRunnerStop();

        Assert.Equal(TRunnerWorkState.TRunnerPending, runner.TRunnerStateRead(currentId, TRunnerWorkState.TRunnerPending).TRunnerState);
        Assert.Equal(TRunnerWorkState.TRunnerPending, runner.TRunnerStateRead(queuedId, TRunnerWorkState.TRunnerPending).TRunnerState);
        Assert.Equal(0, runner.TRunnerExecutionRead(queuedId));
    }

    [Fact]
    public void StoppedRunner_CanStartNewEligibleWorkCleanly()
    {
        using var runner = new TRunner();
        Guid stoppedId = runner.TWorkAdd("old", steps: 100);
        runner.TRunnerStart();
        runner.TRunnerProgressRead(stoppedId, 0.01);
        runner.TRunnerStop();
        runner.TRunnerStateRead(stoppedId, TRunnerWorkState.TRunnerPending);
        Assert.True(runner.TRunnerRemove(stoppedId));
        Guid newId = runner.TWorkAdd("new", steps: 5, delayMilliseconds: 20);

        runner.TRunnerStart();

        TRunnerWork completed = runner.TRunnerStateRead(newId, TRunnerWorkState.TRunnerDone);
        Assert.Equal(1, completed.TRunnerAttempts);
        Assert.Equal(1, runner.TRunnerExecutionRead(newId));
    }

    [Fact]
    public void PerWorkCancellation_DoesNotCancelUnrelatedWork()
    {
        using var runner = new TRunner(workerCount: 2);
        Guid cancelledId = runner.TWorkAdd("cancel-one", steps: 100);
        Guid unrelatedId = runner.TWorkAdd("keep-one", steps: 12);
        runner.TRunnerStart();
        runner.TRunnerCountRead(cancelledId, 1);
        runner.TRunnerCountRead(unrelatedId, 1);

        runner.TRunnerWorkCancel(cancelledId);

        Assert.Equal(TRunnerWorkState.TRunnerCancelled, runner.TRunnerStateRead(cancelledId, TRunnerWorkState.TRunnerCancelled).TRunnerState);
        Assert.Equal(TRunnerWorkState.TRunnerDone, runner.TRunnerStateRead(unrelatedId, TRunnerWorkState.TRunnerDone).TRunnerState);
        Assert.Equal(1, runner.TRunnerExecutionRead(unrelatedId));
    }

    [Fact]
    public void CancellationBeforeProcessAttachment_IsHonoredSafely()
    {
        using var runner = new TRunner();
        Guid workId = runner.TWorkAdd("cancel-before-attach", steps: 100);

        runner.TRunnerWorkCancel(workId);
        runner.TRunnerStart();

        Assert.Equal(TRunnerWorkState.TRunnerCancelled, runner.TRunnerStateRead(workId, TRunnerWorkState.TRunnerCancelled).TRunnerState);
        Assert.NotEqual(TRunnerWorkState.TRunnerDone, runner.TRunnerRead(workId).TRunnerState);
    }
}
