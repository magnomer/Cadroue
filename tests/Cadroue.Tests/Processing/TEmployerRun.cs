using System.Text;

using Xunit;

namespace Cadroue.Tests;

public sealed class TEmployerRun
{
    [Fact]
    public async Task ExitCodeZero_ReturnsProductionSuccessResult()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.TEmployerStart("success");

        TEmployerResult result = await execution.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(TEmployerStatus.TEmployerSucceeded, result.TEmployerState);
        Assert.Equal(0, result.TEmployerExitCode);
    }

    [Fact]
    public async Task NonzeroExit_ReturnsProductionFailureResult()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.TEmployerStart("failure");

        TEmployerResult result = await execution.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(TEmployerStatus.TEmployerFailed, result.TEmployerState);
        Assert.Equal(23, result.TEmployerExitCode);
    }

    [Fact]
    public async Task StderrOutput_IsCapturedInProductionResult()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.TEmployerStart("stderr", "diagnostic from child");

        TEmployerResult result = await execution.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Contains("diagnostic from child", result.TEmployerError);
        Assert.Contains("diagnostic from child", result.TEmployerErrorOutput);
    }

    [Fact]
    public async Task StdoutProgress_DoesNotDeadlockExecution()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.TEmployerStart("stdout", "5000");

        TEmployerResult result = await execution.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(TEmployerStatus.TEmployerSucceeded, result.TEmployerState);
        Assert.Equal(5000, result.TEmployerOutput.Count);
    }

    [Fact]
    public async Task LargeStderr_DoesNotDeadlockExecution()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.TEmployerStart("large-stderr", "4096", "512");

        TEmployerResult result = await execution.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(TEmployerStatus.TEmployerSucceeded, result.TEmployerState);
        Assert.Equal(4096, result.TEmployerErrorOutput.Count);
        Assert.StartsWith("[Earlier FFmpeg stderr was truncated.]", result.TEmployerError);
        Assert.InRange(result.TEmployerError.Length, 1, 257 * 1024);
    }

    [Fact]
    public async Task ArgumentContainingSpaces_ReachesChildIntact()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.TEmployerStart("echo", "value with spaces");

        TEmployerResult result = await execution.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(new[] { "value with spaces" }, TEmployerArgumentParse(result));
    }

    [Fact]
    public async Task MultipleArguments_PreserveBoundariesAndOrder()
    {
        using var employer = new TEmployer();
        string[] expected = ["first", "second value", "third"];
        using TEmployerExecution execution = employer.TEmployerStart(["echo", .. expected]);

        TEmployerResult result = await execution.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(expected, TEmployerArgumentParse(result));
    }

    [Fact]
    public async Task MissingExecutable_IsReportedAsFailureRatherThanSuccess()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.TEmployerMissingStart();

        TEmployerResult result = await execution.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(TEmployerStatus.TEmployerFailed, result.TEmployerState);
        Assert.Null(result.TEmployerExitCode);
        Assert.NotNull(result.TEmployerException);
    }

    [Fact]
    public async Task Cancellation_TerminatesLongRunningOwnedChild()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.TEmployerStart("wait");
        _ = await execution.TEmployerProcessRead();

        execution.TEmployerCancel();
        TEmployerResult result = await execution.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(TEmployerStatus.TEmployerCancelled, result.TEmployerState);
        Assert.False(execution.TEmployerChildAlive);
    }

    [Fact]
    public async Task Cancellation_DoesNotLeaveOwnedChildAlive()
    {
        using var employer = new TEmployer();
        using TEmployerExecution execution = employer.TEmployerStart("wait");
        _ = await execution.TEmployerProcessRead();

        execution.TEmployerCancel();
        _ = await execution.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.False(execution.TEmployerChildAlive);
    }

    [Fact]
    public async Task LaterUnrelatedCancellation_DoesNotChangeCompletedExecution()
    {
        using var employer = new TEmployer();
        using TEmployerExecution completed = employer.TEmployerStart("success");
        TEmployerResult completedResult = await completed.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10));
        using TEmployerExecution unrelated = employer.TEmployerStart("wait");
        _ = await unrelated.TEmployerProcessRead();

        unrelated.TEmployerCancel();
        TEmployerResult unrelatedResult = await unrelated.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(TEmployerStatus.TEmployerSucceeded, completedResult.TEmployerState);
        Assert.Equal(TEmployerStatus.TEmployerSucceeded, (await completed.TEmployerCompletion).TEmployerState);
        Assert.Equal(TEmployerStatus.TEmployerCancelled, unrelatedResult.TEmployerState);
    }

    [Fact]
    public async Task IndependentExecutions_DoNotShareProcessOwnership()
    {
        using var employer = new TEmployer();
        using TEmployerExecution first = employer.TEmployerStart("wait");
        using TEmployerExecution second = employer.TEmployerStart("wait");
        int firstId = await first.TEmployerProcessRead();
        int secondId = await second.TEmployerProcessRead();
        Assert.NotEqual(firstId, secondId);

        first.TEmployerCancel();
        Assert.Equal(TEmployerStatus.TEmployerCancelled, (await first.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10))).TEmployerState);

        Assert.True(second.TEmployerChildAlive);
        second.TEmployerCancel();
        Assert.Equal(TEmployerStatus.TEmployerCancelled, (await second.TEmployerCompletion.WaitAsync(TimeSpan.FromSeconds(10))).TEmployerState);
        Assert.False(second.TEmployerChildAlive);
    }

    private static string[] TEmployerArgumentParse(TEmployerResult result) => result.TEmployerOutput
        .Where(line => line.StartsWith("ARG:", StringComparison.Ordinal))
        .Select(line => Encoding.UTF8.GetString(Convert.FromBase64String(line[4..])))
        .ToArray();
}
