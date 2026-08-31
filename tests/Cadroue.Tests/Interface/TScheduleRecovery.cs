using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.Tests;

internal sealed record TScheduleRecovered(
    Guid TWorkId,
    Guid TScheduleBatchId,
    Guid TLineageId,
    string TWorkName,
    LWorkState TScheduleState,
    string TScheduleMessage);

internal sealed class TScheduleRecoveryWork
{
    internal TScheduleRecoveryWork(LWorkItem workItem)
    {
        TWorkItem = workItem;
    }

    internal LWorkItem TWorkItem { get; }
    internal Guid TWorkId => TWorkItem.LWorkId;
    internal Guid TScheduleBatchId => TWorkItem.LWorkBatchId;
}

internal sealed class TScheduleRecovery : IDisposable
{
    private readonly string tScheduleStorageRoot;
    private LSchedule? tSchedule;
    private int tScheduleSequence;

    internal TScheduleRecovery()
    {
        tScheduleStorageRoot = Path.Combine(
            Path.GetTempPath(),
            "cadroue-schedule-recovery-" + Guid.NewGuid().ToString("N"));

        LDepotIndex.LDepotIndexRelease();
        LDepot.LDepotRootSet(tScheduleStorageRoot);
        tSchedule = TScheduleLoad();
    }

    internal TScheduleRecoveryWork TWorkCreate(
        Guid batchId,
        string name,
        TScheduleRecoveryWork? parent = null)
    {
        DateTimeOffset created = DateTimeOffset.UnixEpoch.AddTicks(++tScheduleSequence);
        string sourcePath = parent?.TWorkItem.LWorkOutputPath
            ?? Path.Combine(tScheduleStorageRoot, name + ".source");

        return new TScheduleRecoveryWork(new LWorkItem(
            batchId,
            LWorkKind.LWorkKindEdit,
            LWorkPriority.LWorkPriorityNormal,
            sourcePath,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            name,
            Path.Combine(tScheduleStorageRoot, name + ".output"),
            TWorkOutput.TWorkOutputCreate(),
            lWorkCreateTime: created));
    }

    internal int TScheduleSave(params TScheduleRecoveryWork[] work) =>
        TScheduleRead().LScheduleAdd(work.Select(item => item.TWorkItem).ToArray());

    internal bool TScheduleMove(Guid batchId, params TScheduleRecoveryWork[] work) =>
        TScheduleRead().LScheduleOrderSet(batchId, work.Select(item => item.TWorkId).ToArray());

    internal TScheduleRecoveryWork TScheduleNextClaim()
    {
        LWorkItem workItem = TScheduleRead().LScheduleClaim(Guid.NewGuid())
            ?? throw new InvalidOperationException("The recovered schedule had no claimable work.");
        TScheduleRead().LScheduleLoad();
        return new TScheduleRecoveryWork(workItem);
    }

    internal void TScheduleCommit(TScheduleRecoveryWork work, bool succeeded, string message = "")
    {
        TScheduleRead().LScheduleCommit(work.TWorkItem, succeeded, message);
        TScheduleRead().LScheduleLoad();
    }

    internal bool TScheduleCancel(TScheduleRecoveryWork work) =>
        TScheduleRead().LScheduleItemCancel(work.TWorkItem);

    internal bool TScheduleReset(Guid workId) => TScheduleRead().LScheduleItemReset(workId);

    internal bool TScheduleRemove(Guid workId) => TScheduleRead().LScheduleRemove(workId);

    internal IReadOnlyList<TScheduleRecovered> TScheduleItemsRead() =>
        TScheduleRead().LScheduleRecords.Select(TScheduleSnapshotCreate).ToArray();

    internal IReadOnlyList<TScheduleRecovered> TSchedulePendingRead() =>
        TScheduleRead().LSchedulePendingRead().Select(TScheduleSnapshotCreate).ToArray();

    internal void TScheduleRestore()
    {
        TScheduleMemoryClear();
        tSchedule = TScheduleLoad();
    }

    internal void TScheduleStorageRestore()
    {
        TScheduleMemoryClear();
        if (Directory.Exists(tScheduleStorageRoot))
        {
            Directory.Delete(tScheduleStorageRoot, recursive: true);
        }

        tSchedule = TScheduleLoad();
    }

    internal void TScheduleMalformSave(TScheduleRecoveryWork work)
    {
        string persistedPath = LDepot.LDepotFileRead(LDepotFolder.LDepotFolderScheduled, work.TWorkId);
        File.WriteAllText(persistedPath, "this is not a schedule record");
        LDepotIndex.LDepotDirtySet();
    }

    public void Dispose()
    {
        try
        {
            tSchedule?.LScheduleAllClear();
        }
        finally
        {
            tSchedule = null;
            LDepotIndex.LDepotIndexRelease();
            LTraceWriter.LTraceWriterClear();
            LDepot.LDepotRootSet(null);
            if (Directory.Exists(tScheduleStorageRoot))
            {
                Directory.Delete(tScheduleStorageRoot, recursive: true);
            }
        }
    }

    private LSchedule TScheduleRead() =>
        tSchedule ?? throw new InvalidOperationException("The schedule is between recovery instances.");

    private static LSchedule TScheduleLoad()
    {
        var schedule = new LSchedule();
        schedule.LScheduleLoad();
        return schedule;
    }

    private void TScheduleMemoryClear()
    {
        tSchedule = null;
        LDepotIndex.LDepotIndexRelease();
    }

    private TScheduleRecovered TScheduleSnapshotCreate(LWorkItem workItem) =>
        new(
            workItem.LWorkId,
            workItem.LWorkBatchId,
            TScheduleRead().LScheduleLineageRead(workItem),
            workItem.LWorkOutputName,
            workItem.LWorkStateCurrent,
            workItem.LWorkMessage);
}
