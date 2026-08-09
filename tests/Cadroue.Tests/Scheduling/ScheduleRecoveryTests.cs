using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class ScheduleRecoveryTests
{
    [Fact]
    public void EmptySchedule_RoundTripsAsEmpty()
    {
        using var recovery = new TScheduleRecovery();

        recovery.Recover();

        Assert.Empty(recovery.Read());
    }

    [Fact]
    public void PendingItem_SurvivesSaveAndReload()
    {
        using var recovery = new TScheduleRecovery();
        TScheduleRecoveryWork work = recovery.WorkCreate(Guid.NewGuid(), "pending");
        recovery.Save(work);

        recovery.Recover();

        TRecoveredScheduleItem recovered = Assert.Single(recovery.PendingRead());
        Assert.Equal("pending", recovered.Name);
        Assert.Equal(LWorkState.LWorkStatePending, recovered.State);
    }

    [Fact]
    public void MultipleItems_PreserveTheirRelativeOrder()
    {
        using var recovery = new TScheduleRecovery();
        Guid batchId = Guid.NewGuid();
        TScheduleRecoveryWork first = recovery.WorkCreate(batchId, "first");
        TScheduleRecoveryWork second = recovery.WorkCreate(batchId, "second");
        TScheduleRecoveryWork third = recovery.WorkCreate(batchId, "third");
        recovery.Save(first, second, third);
        Assert.True(recovery.Reorder(batchId, third, first, second));

        recovery.Recover();

        Assert.Equal(
            new[] { "third", "first", "second" },
            recovery.PendingRead().Select(item => item.Name));
    }

    [Fact]
    public void WorkIdentity_SurvivesReload()
    {
        using var recovery = new TScheduleRecovery();
        TScheduleRecoveryWork work = recovery.WorkCreate(Guid.NewGuid(), "work-identity");
        recovery.Save(work);

        recovery.Recover();

        Assert.Equal(work.WorkId, Assert.Single(recovery.Read()).WorkId);
    }

    [Fact]
    public void BatchIdentity_SurvivesReload()
    {
        using var recovery = new TScheduleRecovery();
        Guid batchId = Guid.NewGuid();
        recovery.Save(recovery.WorkCreate(batchId, "batch-identity"));

        recovery.Recover();

        Assert.Equal(batchId, Assert.Single(recovery.Read()).BatchId);
    }

    [Fact]
    public void Lineage_SurvivesReload()
    {
        using var recovery = new TScheduleRecovery();
        Guid batchId = Guid.NewGuid();
        TScheduleRecoveryWork parent = recovery.WorkCreate(batchId, "parent");
        TScheduleRecoveryWork child = recovery.WorkCreate(batchId, "child", parent);
        recovery.Save(parent, child);
        Guid[] savedLineages = recovery.Read().Select(item => item.LineageId).ToArray();

        recovery.Recover();

        Guid[] recoveredLineages = recovery.Read().Select(item => item.LineageId).ToArray();
        Assert.All(savedLineages, lineage => Assert.NotEqual(Guid.Empty, lineage));
        Assert.Equal(savedLineages, recoveredLineages);
        Assert.Equal(recoveredLineages[0], recoveredLineages[1]);
    }

    [Fact]
    public void PersistedTerminalStates_AreRestoredCorrectly()
    {
        using var recovery = new TScheduleRecovery();
        Guid batchId = Guid.NewGuid();
        recovery.Save(
            recovery.WorkCreate(batchId, "done"),
            recovery.WorkCreate(batchId, "failed"),
            recovery.WorkCreate(batchId, "cancelled"));
        recovery.Complete(recovery.ClaimNext(), succeeded: true);
        recovery.Complete(recovery.ClaimNext(), succeeded: false, "failed detail");
        Assert.True(recovery.Cancel(recovery.ClaimNext()));

        recovery.Recover();

        TRecoveredScheduleItem[] recovered = recovery.Read().ToArray();
        Assert.Equal(
            new[]
            {
                LWorkState.LWorkStateDone,
                LWorkState.LWorkStateFailed,
                LWorkState.LWorkStateCancelled
            },
            recovered.Select(item => item.State));
        Assert.Equal("failed detail", recovered[1].Message);
    }

    [Fact]
    public void RemovedItem_DoesNotReappearAfterReload()
    {
        using var recovery = new TScheduleRecovery();
        TScheduleRecoveryWork work = recovery.WorkCreate(Guid.NewGuid(), "removed");
        recovery.Save(work);
        Assert.True(recovery.Remove(work.WorkId));

        recovery.Recover();

        Assert.Empty(recovery.Read());
    }

    [Fact]
    public void ChangedSchedule_DoesNotResurrectStaleState()
    {
        using var recovery = new TScheduleRecovery();
        TScheduleRecoveryWork work = recovery.WorkCreate(Guid.NewGuid(), "changed");
        recovery.Save(work);
        recovery.Complete(recovery.ClaimNext(), succeeded: false, "stale failure");
        Assert.True(recovery.Reset(work.WorkId));

        recovery.Recover();

        TRecoveredScheduleItem recovered = Assert.Single(recovery.Read());
        Assert.Equal(work.WorkId, recovered.WorkId);
        Assert.Equal(LWorkState.LWorkStatePending, recovered.State);
        Assert.Equal(string.Empty, recovered.Message);
    }

    [Fact]
    public void MissingStorage_RecoversAsValidEmptySchedule()
    {
        using var recovery = new TScheduleRecovery();

        recovery.RemoveStorageAndRecover();

        Assert.Empty(recovery.Read());
        Assert.Empty(recovery.PendingRead());
    }

    [Fact]
    public void MalformedPersistedContent_DoesNotFabricateWork()
    {
        using var recovery = new TScheduleRecovery();
        TScheduleRecoveryWork work = recovery.WorkCreate(Guid.NewGuid(), "malformed");
        recovery.Save(work);
        recovery.MalformPersistedWork(work);

        recovery.Recover();

        Assert.Empty(recovery.Read());
    }

    [Fact]
    public void RepeatedRecovery_DoesNotDuplicateItems()
    {
        using var recovery = new TScheduleRecovery();
        TScheduleRecoveryWork work = recovery.WorkCreate(Guid.NewGuid(), "single");
        recovery.Save(work);

        recovery.Recover();
        recovery.Recover();

        TRecoveredScheduleItem recovered = Assert.Single(recovery.Read());
        Assert.Equal(work.WorkId, recovered.WorkId);
    }
}
