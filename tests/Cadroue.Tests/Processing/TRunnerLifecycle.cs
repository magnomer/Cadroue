using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TRunnerLifecycle
{
    [Fact]
    public void Pause_PreventsProcessingFromAdvancingPastPausePoint()
    {
        using var runner = new TRunner();
        Guid workId = runner.TWorkAdd("pause", steps: 60);
        runner.TRunnerStart();
        runner.TRunnerCountRead(workId, 1);

        runner.TRunnerPause();
        double pausedProgress = runner.TRunnerPausedRead(workId);

        Assert.True(runner.TRunnerSuspended);
        Assert.False(runner.TRunnerRunning);
        Assert.Equal(pausedProgress, runner.TRunnerRead(workId).TRunnerProgress, 6);
        Assert.Equal(TRunnerWorkState.TRunnerRunning, runner.TRunnerRead(workId).TRunnerState);
    }

    [Fact]
    public void Resume_ContinuesSameWorkWithoutDuplicateJob()
    {
        using var runner = new TRunner();
        Guid workId = runner.TWorkAdd("resume", steps: 50);
        runner.TRunnerStart();
        runner.TRunnerCountRead(workId, 1);
        runner.TRunnerPause();
        double pausedProgress = runner.TRunnerPausedRead(workId);

        runner.TRunnerStart();

        TRunnerWork completed = runner.TRunnerStateRead(workId, TRunnerWorkState.TRunnerDone);
        Assert.True(completed.TRunnerProgress > pausedProgress);
        Assert.Equal(1, completed.TRunnerAttempts);
        Assert.Equal(1, runner.TRunnerExecutionRead(workId));
    }

    [Fact]
    public void RepeatedPauseRequests_DoNotCorruptRunnerState()
    {
        using var runner = new TRunner();
        Guid workId = runner.TWorkAdd("repeated-pause", steps: 50);
        runner.TRunnerStart();
        runner.TRunnerCountRead(workId, 1);

        runner.TRunnerPause();
        runner.TRunnerPause();
        runner.TRunnerPause();
        runner.TRunnerPausedRead(workId);

        Assert.True(runner.TRunnerSuspended);
        Assert.Equal(TRunnerWorkState.TRunnerRunning, runner.TRunnerRead(workId).TRunnerState);
        runner.TRunnerStart();
        Assert.Equal(TRunnerWorkState.TRunnerDone, runner.TRunnerStateRead(workId, TRunnerWorkState.TRunnerDone).TRunnerState);
        Assert.Equal(1, runner.TRunnerExecutionRead(workId));
    }
}
