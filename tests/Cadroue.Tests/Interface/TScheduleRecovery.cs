using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.Tests;

internal sealed record TRecoveredScheduleItem(
    Guid WorkId,
    Guid BatchId,
    Guid LineageId,
    string Name,
    LWorkState State,
    string Message);

internal sealed class TScheduleRecoveryWork
{
    internal TScheduleRecoveryWork(LWorkItem workItem)
    {
        WorkItem = workItem;
    }

    internal LWorkItem WorkItem { get; }
    internal Guid WorkId => WorkItem.LWorkId;
    internal Guid BatchId => WorkItem.LWorkBatchId;
}

internal sealed class TScheduleRecovery : IDisposable
{
    private readonly string tStorageRoot;
    private LSchedule? tSchedule;
    private int tSequence;

    internal TScheduleRecovery()
    {
        tStorageRoot = Path.Combine(
            Path.GetTempPath(),
            "cadroue-schedule-recovery-" + Guid.NewGuid().ToString("N"));

        LDepotIndex.LDepotIndexRelease();
        LDepot.LDepotRootSet(tStorageRoot);
        tSchedule = ScheduleLoad();
    }

    internal TScheduleRecoveryWork WorkCreate(
        Guid batchId,
        string name,
        TScheduleRecoveryWork? parent = null)
    {
        DateTimeOffset created = DateTimeOffset.UnixEpoch.AddTicks(++tSequence);
        string sourcePath = parent?.WorkItem.LWorkOutputPath
            ?? Path.Combine(tStorageRoot, name + ".source");

        return new TScheduleRecoveryWork(new LWorkItem(
            batchId,
            LWorkKind.LWorkKindEdit,
            LWorkPriority.LWorkPriorityNormal,
            sourcePath,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            name,
            Path.Combine(tStorageRoot, name + ".output"),
            WorkCreationOutput.Create(),
            lWorkCreateTime: created));
    }

    internal int Save(params TScheduleRecoveryWork[] work) =>
        ScheduleRead().LScheduleAdd(work.Select(item => item.WorkItem).ToArray());

    internal bool Reorder(Guid batchId, params TScheduleRecoveryWork[] work) =>
        ScheduleRead().LScheduleOrderSet(batchId, work.Select(item => item.WorkId).ToArray());

    internal TScheduleRecoveryWork ClaimNext()
    {
        LWorkItem workItem = ScheduleRead().LScheduleClaim(Guid.NewGuid())
            ?? throw new InvalidOperationException("The recovered schedule had no claimable work.");
        ScheduleRead().LScheduleLoad();
        return new TScheduleRecoveryWork(workItem);
    }

    internal void Complete(TScheduleRecoveryWork work, bool succeeded, string message = "")
    {
        ScheduleRead().LScheduleCommit(work.WorkItem, succeeded, message);
        ScheduleRead().LScheduleLoad();
    }

    internal bool Cancel(TScheduleRecoveryWork work) =>
        ScheduleRead().LScheduleItemCancel(work.WorkItem);

    internal bool Reset(Guid workId) => ScheduleRead().LScheduleItemReset(workId);

    internal bool Remove(Guid workId) => ScheduleRead().LScheduleRemove(workId);

    internal IReadOnlyList<TRecoveredScheduleItem> Read() =>
        ScheduleRead().LScheduleRecords.Select(Snapshot).ToArray();

    internal IReadOnlyList<TRecoveredScheduleItem> PendingRead() =>
        ScheduleRead().LSchedulePendingRead().Select(Snapshot).ToArray();

    internal void Recover()
    {
        MemoryClear();
        tSchedule = ScheduleLoad();
    }

    internal void RemoveStorageAndRecover()
    {
        MemoryClear();
        if (Directory.Exists(tStorageRoot))
        {
            Directory.Delete(tStorageRoot, recursive: true);
        }

        tSchedule = ScheduleLoad();
    }

    internal void MalformPersistedWork(TScheduleRecoveryWork work)
    {
        string persistedPath = LDepot.LDepotFileRead(LDepotFolder.LDepotFolderScheduled, work.WorkId);
        File.WriteAllText(persistedPath, "this is not a schedule record");
        LDepotIndex.LDepotIndexInvalidate();
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
            if (Directory.Exists(tStorageRoot))
            {
                Directory.Delete(tStorageRoot, recursive: true);
            }
        }
    }

    private LSchedule ScheduleRead() =>
        tSchedule ?? throw new InvalidOperationException("The schedule is between recovery instances.");

    private static LSchedule ScheduleLoad()
    {
        var schedule = new LSchedule();
        schedule.LScheduleLoad();
        return schedule;
    }

    private void MemoryClear()
    {
        tSchedule = null;
        LDepotIndex.LDepotIndexRelease();
    }

    private TRecoveredScheduleItem Snapshot(LWorkItem workItem) =>
        new(
            workItem.LWorkId,
            workItem.LWorkBatchId,
            ScheduleRead().LScheduleLineageRead(workItem),
            workItem.LWorkOutputName,
            workItem.LWorkStateCurrent,
            workItem.LWorkMessage);
}
