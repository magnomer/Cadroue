using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class RunnerStopTests
{
    [Fact]
    public void Stop_TerminatesCurrentWorkAndDoesNotCompleteItsPartialOutput()
    {
        using var runner = new TRunner();
        Guid workId = runner.WorkAdd("stop-current", steps: 100);
        runner.Start();
        runner.WaitForProgress(workId, 0.01);
        Assert.True(runner.Read(workId).OutputExists);

        runner.Stop();

        TRunnerWork stopped = runner.WaitForState(workId, TRunnerWorkState.Pending);
        stopped = runner.WaitForOutputRemoved(workId);
        Assert.False(runner.Running);
        Assert.NotEqual(TRunnerWorkState.Done, stopped.State);
        Assert.False(stopped.OutputExists);
    }

    [Fact]
    public void Stop_PreventsQueuedWorkFromContinuing()
    {
        using var runner = new TRunner();
        Guid currentId = runner.WorkAdd("stop-active", steps: 100);
        Guid queuedId = runner.WorkAdd("stop-queued", steps: 10);
        runner.Start();
        runner.WaitForProgress(currentId, 0.01);

        runner.Stop();

        Assert.Equal(TRunnerWorkState.Pending, runner.WaitForState(currentId, TRunnerWorkState.Pending).State);
        Assert.Equal(TRunnerWorkState.Pending, runner.WaitForState(queuedId, TRunnerWorkState.Pending).State);
        Assert.Equal(0, runner.ExecutionCount(queuedId));
    }

    [Fact]
    public void StoppedRunner_CanStartNewEligibleWorkCleanly()
    {
        using var runner = new TRunner();
        Guid stoppedId = runner.WorkAdd("old", steps: 100);
        runner.Start();
        runner.WaitForProgress(stoppedId, 0.01);
        runner.Stop();
        runner.WaitForState(stoppedId, TRunnerWorkState.Pending);
        Assert.True(runner.Remove(stoppedId));
        Guid newId = runner.WorkAdd("new", steps: 5, delayMilliseconds: 20);

        runner.Start();

        TRunnerWork completed = runner.WaitForState(newId, TRunnerWorkState.Done);
        Assert.Equal(1, completed.Attempts);
        Assert.Equal(1, runner.ExecutionCount(newId));
    }

    [Fact]
    public void PerWorkCancellation_DoesNotCancelUnrelatedWork()
    {
        using var runner = new TRunner(parallelMaximum: 2);
        Guid cancelledId = runner.WorkAdd("cancel-one", steps: 100);
        Guid unrelatedId = runner.WorkAdd("keep-one", steps: 12);
        runner.Start();
        runner.WaitForExecutionCount(cancelledId, 1);
        runner.WaitForExecutionCount(unrelatedId, 1);

        runner.CancelWork(cancelledId);

        Assert.Equal(TRunnerWorkState.Cancelled, runner.WaitForState(cancelledId, TRunnerWorkState.Cancelled).State);
        Assert.Equal(TRunnerWorkState.Done, runner.WaitForState(unrelatedId, TRunnerWorkState.Done).State);
        Assert.Equal(1, runner.ExecutionCount(unrelatedId));
    }

    [Fact]
    public void CancellationBeforeProcessAttachment_IsHonoredSafely()
    {
        using var runner = new TRunner();
        Guid workId = runner.WorkAdd("cancel-before-attach", steps: 100);

        runner.CancelWork(workId);
        runner.Start();

        Assert.Equal(TRunnerWorkState.Cancelled, runner.WaitForState(workId, TRunnerWorkState.Cancelled).State);
        Assert.NotEqual(TRunnerWorkState.Done, runner.Read(workId).State);
    }
}
