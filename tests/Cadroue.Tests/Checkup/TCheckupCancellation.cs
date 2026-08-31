using System.Collections.Concurrent;
using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("Checkup", DisableParallelization = true)]
public sealed class TCheckupCollection;

[Collection("Checkup")]
public sealed class TCheckupCancellation
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

        checkup.TCheckupStart("progressing.mp4");
        Assert.True(started.Wait(TimeSpan.FromSeconds(5)));

        checkup.TCheckupCancel("progressing.mp4");

        Assert.True(cancelled.Wait(TimeSpan.FromSeconds(5)));
        Assert.DoesNotContain(checkup.TScoutResultsRead(), result => result.TCheckupCompleted);
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
            checkup.TCheckupStart("progressing.mp4");
            Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(5)));
            checkup.TCheckupStart("scheduled.mp4");

            checkup.TCheckupCancel("scheduled.mp4");
            releaseFirst.Set();

            Assert.True(SpinWait.SpinUntil(
                () => checkup.TScoutResultsRead().Any(result => result.TCheckupPath == "progressing.mp4" && result.TCheckupClean),
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
        checkup.TCheckupStart("completed.mp4");
        Assert.True(completed.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(SpinWait.SpinUntil(
            () => checkup.TScoutResultsRead().Any(result => result.TCheckupClean),
            TimeSpan.FromSeconds(5)));

        checkup.TCheckupCancel("completed.mp4");

        Assert.True(checkup.TScoutResultsRead().Last().TCheckupClean);
    }
}
