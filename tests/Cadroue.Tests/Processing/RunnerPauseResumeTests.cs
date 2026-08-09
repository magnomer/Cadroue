using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class RunnerPauseResumeTests
{
    [Fact]
    public void Pause_PreventsProcessingFromAdvancingPastPausePoint()
    {
        using var runner = new TRunner();
        Guid workId = runner.WorkAdd("pause", steps: 60);
        runner.Start();
        runner.WaitForExecutionCount(workId, 1);

        runner.Pause();
        double pausedProgress = runner.WaitForPausedProgress(workId);

        Assert.True(runner.Suspended);
        Assert.False(runner.Running);
        Assert.Equal(pausedProgress, runner.Read(workId).Progress, 6);
        Assert.Equal(TRunnerWorkState.Running, runner.Read(workId).State);
    }

    [Fact]
    public void Resume_ContinuesSameWorkWithoutDuplicateJob()
    {
        using var runner = new TRunner();
        Guid workId = runner.WorkAdd("resume", steps: 50);
        runner.Start();
        runner.WaitForExecutionCount(workId, 1);
        runner.Pause();
        double pausedProgress = runner.WaitForPausedProgress(workId);

        runner.Start();

        TRunnerWork completed = runner.WaitForState(workId, TRunnerWorkState.Done);
        Assert.True(completed.Progress > pausedProgress);
        Assert.Equal(1, completed.Attempts);
        Assert.Equal(1, runner.ExecutionCount(workId));
    }

    [Fact]
    public void RepeatedPauseRequests_DoNotCorruptRunnerState()
    {
        using var runner = new TRunner();
        Guid workId = runner.WorkAdd("repeated-pause", steps: 50);
        runner.Start();
        runner.WaitForExecutionCount(workId, 1);

        runner.Pause();
        runner.Pause();
        runner.Pause();
        runner.WaitForPausedProgress(workId);

        Assert.True(runner.Suspended);
        Assert.Equal(TRunnerWorkState.Running, runner.Read(workId).State);
        runner.Start();
        Assert.Equal(TRunnerWorkState.Done, runner.WaitForState(workId, TRunnerWorkState.Done).State);
        Assert.Equal(1, runner.ExecutionCount(workId));
    }
}
