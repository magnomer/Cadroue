using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class LogIntegrityTests
{
    [Fact]
    public void CommittedNotification_RefersToEntryAlreadySaved()
    {
        TLogCommitResult result = TLogIntegrity.CommitObserve();

        Assert.True(result.Observed);
        Assert.Contains("durable notification", result.Text);
    }

    [Fact]
    public void WorkspaceMove_ClosesAndRebindsCurrentLog()
    {
        TLogMoveResult result = TLogIntegrity.WorkspaceMove();

        Assert.True(result.Moved);
        Assert.EndsWith(Path.Combine("log-move", Path.GetFileName(Path.GetDirectoryName(result.Root))!, "target"), result.Root);
        Assert.False(result.SourceExists);
        Assert.Contains("before workspace move", result.Text);
        Assert.Contains("after workspace move", result.Text);
    }

    [Fact]
    public void ConcurrentArchiveAttempts_DoNotDeletePublishedArchive()
    {
        TLogArchiveResult result = TLogIntegrity.ArchiveConcurrent();

        Assert.True(result.ArchiveExists);
        Assert.Contains("archive payload", result.Text);
        Assert.Equal(0, result.TemporaryCount);
    }

    [Fact]
    public void StorageFailure_SuppressesUnsavedEntryAndReportsLossAfterRecovery()
    {
        TLogLossResult result = TLogIntegrity.StorageRecover();

        Assert.DoesNotContain("entry that cannot be saved", result.Text);
        Assert.Contains("Trace entries lost", result.Text);
        Assert.Contains("entry after recovery", result.Text);
        Assert.Equal(new[] { "Trace entries lost", "entry after recovery" }, result.Summaries);
    }
}
