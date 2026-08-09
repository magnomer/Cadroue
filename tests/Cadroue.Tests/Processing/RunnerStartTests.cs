using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class RunnerStartTests
{
    [Fact]
    public void StartingWithPendingWork_BeginsProcessing()
    {
        using var runner = new TRunner();
        Guid workId = runner.WorkAdd("start", steps: 50);

        runner.Start();

        TRunnerWork work = runner.WaitForState(workId, TRunnerWorkState.Running);
        runner.WaitForExecutionCount(workId, 1);
        Assert.Equal(TRunnerWorkState.Running, work.State);
        Assert.True(runner.Running);
        Assert.Equal(1, runner.ExecutionCount(workId));
    }

    [Fact]
    public void StartingWhileAlreadyRunning_DoesNotCreateDuplicateExecution()
    {
        using var runner = new TRunner();
        Guid workId = runner.WorkAdd("single-execution", steps: 60);
        runner.Start();
        runner.WaitForExecutionCount(workId, 1);

        runner.Start();
        runner.Start();

        TRunnerWork completed = runner.WaitForState(workId, TRunnerWorkState.Done);
        Assert.Equal(1, runner.ExecutionCount(workId));
        Assert.Equal(1, completed.Attempts);
    }
}
