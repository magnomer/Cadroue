using Cadroue.Core;
using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("Schedule", DisableParallelization = true)]
public sealed class LScheduleCollection { }

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
    private readonly LSchedule tSchedule;
    private int tScheduleSequence;

    internal TSchedule()
    {
        tScheduleRoot = Path.Combine(
            Path.GetTempPath(),
            "cadroue-schedule-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tScheduleRoot);
        LDepotIndex.LDepotIndexRelease();
        LDepot.LDepotRootSet(tScheduleRoot);
        tSchedule = new LSchedule();
        tSchedule.LScheduleLoad();
    }

    internal TScheduleWork TWorkCreate(
        Guid batchId,
        string name,
        LWorkPriority priority = LWorkPriority.LWorkPriorityNormal,
        TScheduleWork? parent = null)
    {
        DateTimeOffset created = DateTimeOffset.UnixEpoch.AddTicks(++tScheduleSequence);
        return new TScheduleWork(new LWorkItem(
            batchId,
            LWorkKind.LWorkKindEdit,
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
        tSchedule.LScheduleDurationSet(workId, duration);

    internal TimeSpan TScheduleReloadRead(Guid workId)
    {
        var reloaded = new LSchedule();
        reloaded.LScheduleLoad();
        return reloaded.LScheduleRecords.First(workItem => workItem.LWorkId == workId).LWorkEnd;
    }

    internal int TScheduleAdd(params TScheduleWork[] work) =>
        tSchedule.LScheduleAdd(work.Select(item => item.TWorkItem).ToArray());

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
            LWorkStateCurrent = LWorkState.LWorkStateDone,
            LWorkOutputBytes = 123
        };
        return new TScheduleWork(derived);
    }

    internal int TScheduleDeliverAdd(params TScheduleWork[] work) =>
        tSchedule.LScheduleDeliveredAdd(work.Select(item => item.TWorkItem).ToArray()).Count;

    internal Guid TLineageRead(TScheduleWork work) => tSchedule.LScheduleLineageRead(work.TWorkItem);

    internal void TScheduleDiskLoad() => tSchedule.LScheduleLoad();

    internal bool TScheduleMove(Guid batchId, params TScheduleWork[] work) =>
        tSchedule.LScheduleOrderSet(batchId, work.Select(item => item.TWorkId).ToArray());

    internal IReadOnlyList<TScheduleItem> TSchedulePendingRead() =>
        tSchedule.LSchedulePendingRead().Select(TScheduleSnapshotCreate).ToArray();

    internal IReadOnlyList<TScheduleItem> TScheduleRecordsRead() =>
        tSchedule.LScheduleRecords.Select(TScheduleSnapshotCreate).ToArray();

    internal TScheduleWork TScheduleNextClaim()
    {
        LWorkItem workItem = tSchedule.LScheduleClaim(Guid.NewGuid())
            ?? throw new InvalidOperationException("The schedule had no claimable work.");
        tSchedule.LScheduleLoad();
        return new TScheduleWork(workItem);
    }

    internal TScheduleWork? TScheduleTryClaim()
    {
        LWorkItem? workItem = tSchedule.LScheduleClaim(Guid.NewGuid());
        if (workItem is null)
        {
            return null;
        }

        tSchedule.LScheduleLoad();
        return new TScheduleWork(workItem);
    }

    internal void TScheduleCommit(TScheduleWork work, bool succeeded, string message = "")
    {
        tSchedule.LScheduleCommit(work.TWorkItem, succeeded, message);
        tSchedule.LScheduleLoad();
    }

    internal bool TScheduleCancel(TScheduleWork work) =>
        tSchedule.LScheduleItemCancel(work.TWorkItem);

    internal int TScheduleCancel(params TScheduleWork[] work) =>
        work.Count(item => tSchedule.LScheduleItemCancel(item.TWorkItem));

    internal bool TScheduleReset(TScheduleWork work) =>
        tSchedule.LScheduleItemReset(work.TWorkId);

    internal int TScheduleEligibleRemove(params TScheduleWork[] work)
    {
        IReadOnlyList<Guid> removable = tSchedule.LScheduleRemovableRead(
            work.Select(item => item.TWorkId));
        return tSchedule.LScheduleBatchRemove(removable);
    }

    internal int TScheduleDoneClear() => tSchedule.LScheduleDoneClear();

    internal int TScheduleAllClear() => tSchedule.LScheduleAllClear();

    internal IReadOnlyList<TScheduleItem> TScheduleOrderRead()
    {
        var claimed = new List<TScheduleItem>();
        while (tSchedule.LScheduleClaim(Guid.NewGuid()) is { } workItem)
        {
            claimed.Add(TScheduleSnapshotCreate(workItem));
        }

        return claimed;
    }

    public void Dispose()
    {
        tSchedule.LScheduleBatchRemove(
            tSchedule.LScheduleRecords.Select(workItem => workItem.LWorkId).ToArray());
        LDepotIndex.LDepotIndexRelease();
        LDepot.LDepotRootSet(null);
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
            workItem.LWorkStateCurrent,
            workItem.LWorkMessage);
}
