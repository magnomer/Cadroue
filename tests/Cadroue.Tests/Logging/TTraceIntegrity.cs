using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class TTraceIntegrity
{
    [Fact]
    public void CommittedNotification_RefersToEntryAlreadySaved()
    {
        TLogCommitResult result = TLogIntegrity.TLogCommitRead();

        Assert.True(result.TLogObserved);
        Assert.Contains("durable notification", result.TLogText);
    }

    [Fact]
    public void CommittedNotification_CanPersistWithoutDeadlock()
    {
        TLogCallbackResult result = TLogIntegrity.TLogCallbackPersist();

        Assert.True(result.TLogObserved);
        Assert.True(result.TLogPersisted);
    }

    [Fact]
    public void WorkspaceMove_ClosesAndRebindsCurrentLog()
    {
        TLogMoveResult result = TLogIntegrity.TWorkspaceMove();

        Assert.True(result.TLogMoved);
        Assert.EndsWith(Path.Combine("log-move", Path.GetFileName(Path.GetDirectoryName(result.TLogRoot))!, "target"), result.TLogRoot);
        Assert.False(result.TLogSourceFlag);
        Assert.Contains("before workspace move", result.TLogText);
        Assert.Contains("after workspace move", result.TLogText);
    }

    [Fact]
    public void ConcurrentWorkspaceLog_DoesNotRecreateSourceWorkspace()
    {
        TLogMoveResult result = TLogIntegrity.TLogConcurrentMove();

        Assert.True(result.TLogMoved);
        Assert.False(result.TLogSourceFlag);
        Assert.Contains("before concurrent workspace move", result.TLogText);
        Assert.DoesNotContain("during concurrent workspace move", result.TLogText);
        Assert.Contains("Trace entries lost", result.TLogText);
        Assert.Contains("after concurrent workspace move", result.TLogText);
    }

    [Fact]
    public void PersistTimeout_DoesNotBlockIndefinitely()
    {
        TLogPersistResult result = TLogIntegrity.TLogTimeoutPersist();

        Assert.False(result.TLogPersisted);
        Assert.True(result.TLogElapsed < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ConcurrentArchiveAttempts_DoNotDeletePublishedArchive()
    {
        TLogArchiveResult result = TLogIntegrity.TLogArchiveRun();

        Assert.True(result.TLogArchiveFlag);
        Assert.Contains("archive payload", result.TLogText);
        Assert.Equal(0, result.TLogTemporaryCount);
    }

    [Fact]
    public void StorageFailure_SuppressesUnsavedEntryAndReportsLossAfterRecovery()
    {
        TLogLossResult result = TLogIntegrity.TLogStorageRestore();

        Assert.DoesNotContain("entry that cannot be saved", result.TLogText);
        Assert.Contains("Trace entries lost", result.TLogText);
        Assert.Contains("entry after recovery", result.TLogText);
        Assert.Equal(new[] { "Trace entries lost", "entry after recovery" }, result.TLogSummaries);
    }

    [Fact]
    public void FileReadResult_DistinguishesEmptyContentFromReadFailures()
    {
        TLogReadResult result = TLogIntegrity.TLogReadResolve();

        Assert.True(result.TLogEmptySuccess);
        Assert.Equal(string.Empty, result.TLogEmptyText);
        Assert.False(result.TLogCorruptSuccess);
        Assert.NotEmpty(result.TLogCorruptError);
        Assert.False(result.TLogMissingSuccess);
        Assert.NotEmpty(result.TLogMissingError);
    }
}
