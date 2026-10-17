using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.Tests.Interface;

internal sealed record TScheduleItem(
    Guid TWorkId,
    Guid TScheduleBatchId,
    string TWorkName,
    LWorkPriority TSchedulePriority,
    LWorkState TScheduleState,
    string TScheduleMessage);

internal sealed class TScheduleWork
{
    internal TScheduleWork(LWorkItem workItem)
    {
        TWorkItem = workItem;
    }

    internal LWorkItem TWorkItem { get; }
    internal Guid TWorkId => TWorkItem.LWorkId;
    internal Guid TScheduleBatchId => TWorkItem.LWorkBatchId;
}

internal sealed class TSchedule : IDisposable
{
    private readonly string tScheduleRoot;
    private readonly string _tSchedulePrevious;
    private readonly LSchedule tSchedule;
    private readonly LScheduleIntake tScheduleIntake;
    private readonly LScheduleLease tScheduleLease;
    private readonly LScheduleTransition tScheduleTransition;
    private readonly LScheduleMeasurement tScheduleMeasurement;
    private readonly LScheduleRemover tScheduleRemover;
    private int tScheduleSequence;

    internal TSchedule()
    {
        tScheduleRoot = Path.Combine(
            Path.GetTempPath(),
            "cadroue-schedule-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tScheduleRoot);
        LDepotIndex.LDepotIndexRelease();
        _tSchedulePrevious = LDepot.LDepotRootRead();
        LDepot.LDepotRootSet(tScheduleRoot);
        tSchedule = new LSchedule();
        tScheduleIntake = new LScheduleIntake(tSchedule);
        tScheduleLease = new LScheduleLease(tSchedule);
        tScheduleTransition = new LScheduleTransition(tSchedule);
        tScheduleMeasurement = new LScheduleMeasurement(tSchedule);
        tScheduleRemover = new LScheduleRemover(tSchedule);
        tSchedule.LScheduleLoad();
    }

    internal LScheduleContract TScheduleRead() => tSchedule;

    internal TScheduleWork TWorkCreate(
        Guid batchId,
        string name,
        LWorkPriority priority = LWorkPriority.LWorkPriorityNormal,
        TScheduleWork? parent = null,
        LWorkKind kind = LWorkKind.LWorkKindEdit)
    {
        DateTimeOffset created = DateTimeOffset.UnixEpoch.AddTicks(++tScheduleSequence);
        return new TScheduleWork(new LWorkItem(
            batchId,
            kind,
            priority,
            parent?.TWorkItem.LWorkOutputPath ?? Path.Combine(tScheduleRoot, name + ".source"),
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            name,
            Path.Combine(tScheduleRoot, name + ".output"),
            TWorkOutput.TWorkOutputCreate(),
            lWorkCreateTime: created));
    }

    internal TScheduleWork TWorkCreateOpen(Guid batchId, string name)
    {
        DateTimeOffset created = DateTimeOffset.UnixEpoch.AddTicks(++tScheduleSequence);
        return new TScheduleWork(new LWorkItem(
            batchId,
            LWorkKind.LWorkKindEdit,
            LWorkPriority.LWorkPriorityNormal,
            Path.Combine(tScheduleRoot, name + ".source"),
            TimeSpan.Zero,
            TimeSpan.Zero,
            name,
            Path.Combine(tScheduleRoot, name + ".output"),
            TWorkOutput.TWorkOutputCreate(),
            lWorkCreateTime: created));
    }

    internal void TScheduleDurationSet(Guid workId, TimeSpan duration) =>
        tScheduleMeasurement.LScheduleDurationSet(workId, duration);

    internal TimeSpan TScheduleReloadRead(Guid workId)
    {
        var reloaded = new LSchedule();
        reloaded.LScheduleLoad();
        return reloaded.LScheduleRecords.First(workItem => workItem.LWorkId == workId).LWorkEnd;
    }

    internal int TScheduleAdd(params TScheduleWork[] work) =>
        tScheduleIntake.LScheduleAcceptedAdd(work.Select(item => item.TWorkItem).ToArray()).Count;

    internal TScheduleWork TWorkMergeCreate(Guid batchId, string name, IReadOnlyList<string> sources)
    {
        DateTimeOffset created = DateTimeOffset.UnixEpoch.AddTicks(++tScheduleSequence);
        return new TScheduleWork(new LWorkItem(
            batchId,
            LWorkKind.LWorkKindMerge,
            LWorkPriority.LWorkPriorityNormal,
            Path.Combine(tScheduleRoot, name + ".source"),
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            name,
            Path.Combine(tScheduleRoot, name + ".output"),
            TWorkOutput.TWorkOutputCreate(),
            lWorkCreateTime: created,
            lWorkMergeSources: sources));
    }

    internal TScheduleWork TWorkDeriveCreate(TScheduleWork origin, string name)
    {
        DateTimeOffset created = DateTimeOffset.UnixEpoch.AddTicks(++tScheduleSequence);
        var derived = new LWorkItem(
            origin.TScheduleBatchId,
            LWorkKind.LWorkKindFix,
            LWorkPriority.LWorkPriorityNormal,
            origin.TWorkItem.LWorkSourcePath,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            name,
            Path.Combine(tScheduleRoot, name + ".output"),
            TWorkOutput.TWorkOutputCreate(),
            lWorkCreateTime: created)
        {
            LWorkRunState =
            {
                LWorkStateCurrent = LWorkState.LWorkStateDone,
                LWorkOutputBytes = 123
            }
        };
        return new TScheduleWork(derived);
    }

    internal int TScheduleDeliverAdd(params TScheduleWork[] work) =>
        tScheduleIntake.LScheduleDeliveredAdd(work.Select(item => item.TWorkItem).ToArray()).Count;

    internal Guid TLineageRead(TScheduleWork work) => LScheduleLineage.LScheduleLineageRead(work.TWorkItem);

    internal void TScheduleDiskLoad() => tSchedule.LScheduleLoad();

    internal bool TScheduleMove(Guid batchId, params TScheduleWork[] work) =>
        tScheduleIntake.LScheduleOrderSet(batchId, work.Select(item => item.TWorkId).ToArray());

    internal IReadOnlyList<TScheduleItem> TSchedulePendingRead() =>
        tSchedule.LSchedulePendingRead().Select(TScheduleSnapshotCreate).ToArray();

    internal IReadOnlyList<TScheduleItem> TScheduleRecordsRead() =>
        tSchedule.LScheduleRecords.Select(TScheduleSnapshotCreate).ToArray();

    internal TScheduleWork TScheduleNextClaim()
    {
        LWorkItem workItem = tScheduleLease.LScheduleClaim(Guid.NewGuid())
            ?? throw new InvalidOperationException("The schedule had no claimable work.");
        tSchedule.LScheduleLoad();
        return new TScheduleWork(workItem);
    }

    internal TScheduleWork? TScheduleTryClaim()
    {
        LWorkItem? workItem = tScheduleLease.LScheduleClaim(Guid.NewGuid());
        if (workItem is null)
        {
            return null;
        }

        tSchedule.LScheduleLoad();
        return new TScheduleWork(workItem);
    }

    internal void TScheduleCommit(TScheduleWork work, bool succeeded, string message = "")
    {
        tScheduleTransition.LScheduleCommit(work.TWorkItem, succeeded, message);
        tSchedule.LScheduleLoad();
    }

    internal bool TScheduleCancel(TScheduleWork work) =>
        tScheduleTransition.LScheduleItemCancel(work.TWorkItem);

    internal int TScheduleCancel(params TScheduleWork[] work) =>
        work.Count(item => tScheduleTransition.LScheduleItemCancel(item.TWorkItem));

    internal bool TScheduleReset(TScheduleWork work) =>
        tScheduleTransition.LScheduleItemReset(work.TWorkId);

    internal int TScheduleEligibleRemove(params TScheduleWork[] work)
    {
        IReadOnlyList<Guid> removable = tScheduleRemover.LScheduleRemovableRead(
            work.Select(item => item.TWorkId));
        return tScheduleRemover.LScheduleBatchRemove(removable).Values
            .Count(outcome => outcome == LScheduleRemoval.LScheduleRemovalRemoved);
    }

    internal int TScheduleDoneClear() => tScheduleRemover.LScheduleDoneClear();

    internal int TScheduleAllClear() => tScheduleRemover.LScheduleAllClear();

    internal IReadOnlyList<TScheduleItem> TScheduleOrderRead()
    {
        var claimed = new List<TScheduleItem>();
        while (tScheduleLease.LScheduleClaim(Guid.NewGuid()) is { } workItem)
        {
            claimed.Add(TScheduleSnapshotCreate(workItem));
        }

        return claimed;
    }

    public void Dispose()
    {
        tScheduleRemover.LScheduleBatchRemove(
            tSchedule.LScheduleRecords.Select(workItem => workItem.LWorkId).ToArray());
        LDepotIndex.LDepotIndexRelease();
        LDepot.LDepotRootSet(_tSchedulePrevious);
        try
        {
            Directory.Delete(tScheduleRoot, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static TScheduleItem TScheduleSnapshotCreate(LWorkItem workItem) =>
        new(
            workItem.LWorkId,
            workItem.LWorkBatchId,
            workItem.LWorkOutputName,
            workItem.LWorkPriority,
            workItem.LWorkRunState.LWorkStateCurrent,
            workItem.LWorkRunState.LWorkMessage);
}
