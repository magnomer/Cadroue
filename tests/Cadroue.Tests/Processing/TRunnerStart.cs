using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TRunnerStart
{
    [Fact]
    public void StartingWithPendingWork_BeginsProcessing()
    {
        using var runner = new TRunner();
        Guid workId = runner.TWorkAdd("start", steps: 50);

        runner.TRunnerStart();

        TRunnerWork work = runner.TRunnerStateRead(workId, TRunnerWorkState.TRunnerRunning);
        runner.TRunnerCountRead(workId, 1);
        Assert.Equal(TRunnerWorkState.TRunnerRunning, work.TRunnerState);
        Assert.True(runner.TRunnerRunning);
        Assert.Equal(1, runner.TRunnerExecutionRead(workId));
    }

    [Fact]
    public void StartingWhileAlreadyRunning_DoesNotCreateDuplicateExecution()
    {
        using var runner = new TRunner();
        Guid workId = runner.TWorkAdd("single-execution", steps: 60);
        runner.TRunnerStart();
        runner.TRunnerCountRead(workId, 1);

        runner.TRunnerStart();
        runner.TRunnerStart();

        TRunnerWork completed = runner.TRunnerStateRead(workId, TRunnerWorkState.TRunnerDone);
        Assert.Equal(1, runner.TRunnerExecutionRead(workId));
        Assert.Equal(1, completed.TRunnerAttempts);
    }
}
