using System.Text;

using Xunit;

namespace Cadroue.Tests;

public sealed class EmployerExecutionTests
{
    [Fact]
    public async Task ExitCodeZero_ReturnsProductionSuccessResult()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.Execute("success");

        TEmployerResult result = await execution.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(TEmployerStatus.Succeeded, result.Status);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task NonzeroExit_ReturnsProductionFailureResult()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.Execute("failure");

        TEmployerResult result = await execution.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(TEmployerStatus.Failed, result.Status);
        Assert.Equal(23, result.ExitCode);
    }

    [Fact]
    public async Task StderrOutput_IsCapturedInProductionResult()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.Execute("stderr", "diagnostic from child");

        TEmployerResult result = await execution.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Contains("diagnostic from child", result.Error);
        Assert.Contains("diagnostic from child", result.ErrorOutput);
    }

    [Fact]
    public async Task StdoutProgress_DoesNotDeadlockExecution()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.Execute("stdout", "5000");

        TEmployerResult result = await execution.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(TEmployerStatus.Succeeded, result.Status);
        Assert.Equal(5000, result.Output.Count);
    }

    [Fact]
    public async Task LargeStderr_DoesNotDeadlockExecution()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.Execute("large-stderr", "4096", "512");

        TEmployerResult result = await execution.Completion.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(TEmployerStatus.Succeeded, result.Status);
        Assert.Equal(4096, result.ErrorOutput.Count);
        Assert.True(result.Error.Length >= 4096 * 512);
    }

    [Fact]
    public async Task ArgumentContainingSpaces_ReachesChildIntact()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.Execute("echo", "value with spaces");

        TEmployerResult result = await execution.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(new[] { "value with spaces" }, ArgumentsDecode(result));
    }

    [Fact]
    public async Task MultipleArguments_PreserveBoundariesAndOrder()
    {
        using var employer = new TEmployer();
        string[] expected = ["first", "second value", "third"];
        using TEmployerExecution execution = employer.Execute(["echo", .. expected]);

        TEmployerResult result = await execution.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(expected, ArgumentsDecode(result));
    }

    [Fact]
    public async Task MissingExecutable_IsReportedAsFailureRatherThanSuccess()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.ExecuteMissingProgram();

        TEmployerResult result = await execution.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(TEmployerStatus.Failed, result.Status);
        Assert.Null(result.ExitCode);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task Cancellation_TerminatesLongRunningOwnedChild()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.Execute("wait");
        _ = await execution.ProcessIdRead();

        execution.Cancel();
        TEmployerResult result = await execution.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(TEmployerStatus.Cancelled, result.Status);
        Assert.False(execution.ChildAlive);
    }

    [Fact]
    public async Task Cancellation_DoesNotLeaveOwnedChildAlive()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.Execute("wait");
        _ = await execution.ProcessIdRead();

        execution.Cancel();
        _ = await execution.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.False(execution.ChildAlive);
    }

    [Fact]
    public async Task LaterUnrelatedCancellation_DoesNotChangeCompletedExecution()
    {
        using var employer = new TEmployer();
        using TEmployerExecution completed = employer.Execute("success");
        TEmployerResult completedResult = await completed.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        using TEmployerExecution unrelated = employer.Execute("wait");
        _ = await unrelated.ProcessIdRead();

        unrelated.Cancel();
        TEmployerResult unrelatedResult = await unrelated.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(TEmployerStatus.Succeeded, completedResult.Status);
        Assert.Equal(TEmployerStatus.Succeeded, (await completed.Completion).Status);
        Assert.Equal(TEmployerStatus.Cancelled, unrelatedResult.Status);
    }

    [Fact]
    public async Task IndependentExecutions_DoNotShareProcessOwnership()
    {
        using var employer = new TEmployer();
        using TEmployerExecution first = employer.Execute("wait");
        using TEmployerExecution second = employer.Execute("wait");
        int firstId = await first.ProcessIdRead();
        int secondId = await second.ProcessIdRead();
        Assert.NotEqual(firstId, secondId);

        first.Cancel();
        Assert.Equal(TEmployerStatus.Cancelled, (await first.Completion.WaitAsync(TimeSpan.FromSeconds(10))).Status);

        Assert.True(second.ChildAlive);
        second.Cancel();
        Assert.Equal(TEmployerStatus.Cancelled, (await second.Completion.WaitAsync(TimeSpan.FromSeconds(10))).Status);
        Assert.False(second.ChildAlive);
    }

    private static string[] ArgumentsDecode(TEmployerResult result) => result.Output
        .Where(line => line.StartsWith("ARG:", StringComparison.Ordinal))
        .Select(line => Encoding.UTF8.GetString(Convert.FromBase64String(line[4..])))
        .ToArray();
}
