using System.Collections.Concurrent;
using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("Checkup", DisableParallelization = true)]
public sealed class CheckupCollection;

[Collection("Checkup")]
public sealed class CheckupCancellationTests
{
    [Fact]
    public void Cancel_StopsProgressingDiagnosisWithoutPublishingCompletion()
    {
        using var started = new ManualResetEventSlim();
        using var cancelled = new ManualResetEventSlim();
        using var checkup = new TCheckupJob((_, token) =>
        {
            started.Set();
            token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
            if (token.IsCancellationRequested)
            {
                cancelled.Set();
            }

            token.ThrowIfCancellationRequested();
        });

        checkup.Start("progressing.mp4");
        Assert.True(started.Wait(TimeSpan.FromSeconds(5)));

        checkup.Cancel("progressing.mp4");

        Assert.True(cancelled.Wait(TimeSpan.FromSeconds(5)));
        Assert.DoesNotContain(checkup.ResultsRead(), result => result.Completed);
    }

    [Fact]
    public void Cancel_RemovesScheduledDiagnosisBeforeItStarts()
    {
        using var firstStarted = new ManualResetEventSlim();
        using var releaseFirst = new ManualResetEventSlim();
        var scanned = new ConcurrentQueue<string>();
        using var checkup = new TCheckupJob((path, _) =>
        {
            scanned.Enqueue(path);
            if (path == "progressing.mp4")
            {
                firstStarted.Set();
                releaseFirst.Wait(TimeSpan.FromSeconds(5));
            }
        });
        try
        {
            checkup.Start("progressing.mp4");
            Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(5)));
            checkup.Start("scheduled.mp4");

            checkup.Cancel("scheduled.mp4");
            releaseFirst.Set();

            Assert.True(SpinWait.SpinUntil(
                () => checkup.ResultsRead().Any(result => result.Path == "progressing.mp4" && result.Clean),
                TimeSpan.FromSeconds(5)));
            Assert.DoesNotContain("scheduled.mp4", scanned);
        }
        finally
        {
            releaseFirst.Set();
        }
    }

    [Fact]
    public void Cancel_AfterCompletionDoesNotRetractCompletedResult()
    {
        using var completed = new ManualResetEventSlim();
        using var checkup = new TCheckupJob((_, _) => completed.Set());
        checkup.Start("completed.mp4");
        Assert.True(completed.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(SpinWait.SpinUntil(
            () => checkup.ResultsRead().Any(result => result.Clean),
            TimeSpan.FromSeconds(5)));

        checkup.Cancel("completed.mp4");

        Assert.True(checkup.ResultsRead().Last().Clean);
    }
}
