using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TScheduleStale
{
    [Fact]
    public void EmptySchedule_RoundTripsAsEmpty()
    {
        using var recovery = new TScheduleRecovery();

        recovery.TScheduleRestore();

        Assert.Empty(recovery.TScheduleItemsRead());
    }

    [Fact]
    public void PendingItem_SurvivesSaveAndReload()
    {
        using var recovery = new TScheduleRecovery();
        TScheduleRecoveryWork work = recovery.TWorkCreate(Guid.NewGuid(), "pending");
        recovery.TScheduleSave(work);

        recovery.TScheduleRestore();

        TScheduleRecovered recovered = Assert.Single(recovery.TSchedulePendingRead());
        Assert.Equal("pending", recovered.TWorkName);
        Assert.Equal(LWorkState.LWorkStatePending, recovered.TScheduleState);
    }

    [Fact]
    public void MultipleItems_PreserveTheirRelativeOrder()
    {
        using var recovery = new TScheduleRecovery();
        Guid batchId = Guid.NewGuid();
        TScheduleRecoveryWork first = recovery.TWorkCreate(batchId, "first");
        TScheduleRecoveryWork second = recovery.TWorkCreate(batchId, "second");
        TScheduleRecoveryWork third = recovery.TWorkCreate(batchId, "third");
        recovery.TScheduleSave(first, second, third);
        Assert.True(recovery.TScheduleMove(batchId, third, first, second));

        recovery.TScheduleRestore();

        Assert.Equal(
            new[] { "third", "first", "second" },
            recovery.TSchedulePendingRead().Select(item => item.TWorkName));
    }

    [Fact]
    public void WorkIdentity_SurvivesReload()
    {
        using var recovery = new TScheduleRecovery();
        TScheduleRecoveryWork work = recovery.TWorkCreate(Guid.NewGuid(), "work-identity");
        recovery.TScheduleSave(work);

        recovery.TScheduleRestore();

        Assert.Equal(work.TWorkId, Assert.Single(recovery.TScheduleItemsRead()).TWorkId);
    }

    [Fact]
    public void BatchIdentity_SurvivesReload()
    {
        using var recovery = new TScheduleRecovery();
        Guid batchId = Guid.NewGuid();
        recovery.TScheduleSave(recovery.TWorkCreate(batchId, "batch-identity"));

        recovery.TScheduleRestore();

        Assert.Equal(batchId, Assert.Single(recovery.TScheduleItemsRead()).TScheduleBatchId);
    }

    [Fact]
    public void Lineage_SurvivesReload()
    {
        using var recovery = new TScheduleRecovery();
        Guid batchId = Guid.NewGuid();
        TScheduleRecoveryWork parent = recovery.TWorkCreate(batchId, "parent");
        TScheduleRecoveryWork child = recovery.TWorkCreate(batchId, "child", parent);
        recovery.TScheduleSave(parent, child);
        Guid[] savedLineages = recovery.TScheduleItemsRead().Select(item => item.TLineageId).ToArray();

        recovery.TScheduleRestore();

        Guid[] recoveredLineages = recovery.TScheduleItemsRead().Select(item => item.TLineageId).ToArray();
        Assert.All(savedLineages, lineage => Assert.NotEqual(Guid.Empty, lineage));
        Assert.Equal(savedLineages, recoveredLineages);
        Assert.Equal(recoveredLineages[0], recoveredLineages[1]);
    }

    [Fact]
    public void PersistedTerminalStates_AreRestoredCorrectly()
    {
        using var recovery = new TScheduleRecovery();
        Guid batchId = Guid.NewGuid();
        recovery.TScheduleSave(
            recovery.TWorkCreate(batchId, "done"),
            recovery.TWorkCreate(batchId, "failed"),
            recovery.TWorkCreate(batchId, "cancelled"));
        recovery.TScheduleCommit(recovery.TScheduleNextClaim(), succeeded: true);
        recovery.TScheduleCommit(recovery.TScheduleNextClaim(), succeeded: false, "failed detail");
        Assert.True(recovery.TScheduleCancel(recovery.TScheduleNextClaim()));

        recovery.TScheduleRestore();

        TScheduleRecovered[] recovered = recovery.TScheduleItemsRead().ToArray();
        Assert.Equal(
            new[]
            {
                LWorkState.LWorkStateDone,
                LWorkState.LWorkStateFailed,
                LWorkState.LWorkStateCancelled
            },
            recovered.Select(item => item.TScheduleState));
        Assert.Equal("failed detail", recovered[1].TScheduleMessage);
    }

    [Fact]
    public void RemovedItem_DoesNotReappearAfterReload()
    {
        using var recovery = new TScheduleRecovery();
        TScheduleRecoveryWork work = recovery.TWorkCreate(Guid.NewGuid(), "removed");
        recovery.TScheduleSave(work);
        Assert.True(recovery.TScheduleRemove(work.TWorkId));

        recovery.TScheduleRestore();

        Assert.Empty(recovery.TScheduleItemsRead());
    }

    [Fact]
    public void ChangedSchedule_DoesNotResurrectStaleState()
    {
        using var recovery = new TScheduleRecovery();
        TScheduleRecoveryWork work = recovery.TWorkCreate(Guid.NewGuid(), "changed");
        recovery.TScheduleSave(work);
        recovery.TScheduleCommit(recovery.TScheduleNextClaim(), succeeded: false, "stale failure");
        Assert.True(recovery.TScheduleReset(work.TWorkId));

        recovery.TScheduleRestore();

        TScheduleRecovered recovered = Assert.Single(recovery.TScheduleItemsRead());
        Assert.Equal(work.TWorkId, recovered.TWorkId);
        Assert.Equal(LWorkState.LWorkStatePending, recovered.TScheduleState);
        Assert.Equal(string.Empty, recovered.TScheduleMessage);
    }

    [Fact]
    public void MissingStorage_RecoversAsValidEmptySchedule()
    {
        using var recovery = new TScheduleRecovery();

        recovery.TScheduleStorageRestore();

        Assert.Empty(recovery.TScheduleItemsRead());
        Assert.Empty(recovery.TSchedulePendingRead());
    }

    [Fact]
    public void MalformedPersistedContent_DoesNotFabricateWork()
    {
        using var recovery = new TScheduleRecovery();
        TScheduleRecoveryWork work = recovery.TWorkCreate(Guid.NewGuid(), "malformed");
        recovery.TScheduleSave(work);
        recovery.TScheduleMalformSave(work);

        recovery.TScheduleRestore();

        Assert.Empty(recovery.TScheduleItemsRead());
    }

    [Fact]
    public void RepeatedRecovery_DoesNotDuplicateItems()
    {
        using var recovery = new TScheduleRecovery();
        TScheduleRecoveryWork work = recovery.TWorkCreate(Guid.NewGuid(), "single");
        recovery.TScheduleSave(work);

        recovery.TScheduleRestore();
        recovery.TScheduleRestore();

        TScheduleRecovered recovered = Assert.Single(recovery.TScheduleItemsRead());
        Assert.Equal(work.TWorkId, recovered.TWorkId);
    }
}
