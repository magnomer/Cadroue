using Cadroue.Core;
using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("Schedule", DisableParallelization = true)]
public sealed class LScheduleCollection { }

internal sealed record TScheduleItem(
    Guid WorkId,
    Guid BatchId,
    string Name,
    LWorkPriority Priority,
    LWorkState State,
    string Message);

internal sealed class TScheduleWork
{
    internal TScheduleWork(LWorkItem workItem)
    {
        WorkItem = workItem;
    }

    internal LWorkItem WorkItem { get; }
    internal Guid WorkId => WorkItem.LWorkId;
    internal Guid BatchId => WorkItem.LWorkBatchId;
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

    internal TScheduleWork WorkCreate(
        Guid batchId,
        string name,
        LWorkPriority priority = LWorkPriority.LWorkPriorityNormal)
    {
        DateTimeOffset created = DateTimeOffset.UnixEpoch.AddTicks(++tScheduleSequence);
        return new TScheduleWork(new LWorkItem(
            batchId,
            LWorkKind.LWorkKindEdit,
            priority,
            Path.Combine(tScheduleRoot, name + ".source"),
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            name,
            Path.Combine(tScheduleRoot, name + ".output"),
            WorkCreationOutput.Create(),
            lWorkCreateTime: created));
    }

    internal TScheduleWork WorkCreateOpen(Guid batchId, string name)
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
            WorkCreationOutput.Create(),
            lWorkCreateTime: created));
    }

    internal void DurationSet(Guid workId, TimeSpan duration) =>
        tSchedule.LScheduleDurationSet(workId, duration);

    internal TimeSpan ReloadedDurationRead(Guid workId)
    {
        var reloaded = new LSchedule();
        reloaded.LScheduleLoad();
        return reloaded.LScheduleRecords.First(workItem => workItem.LWorkId == workId).LWorkEnd;
    }

    internal int Submit(params TScheduleWork[] work) =>
        tSchedule.LScheduleAdd(work.Select(item => item.WorkItem).ToArray());

    internal TScheduleWork WorkCreateDerived(TScheduleWork origin, string name)
    {
        DateTimeOffset created = DateTimeOffset.UnixEpoch.AddTicks(++tScheduleSequence);
        var derived = new LWorkItem(
            origin.BatchId,
            LWorkKind.LWorkKindFix,
            LWorkPriority.LWorkPriorityNormal,
            origin.WorkItem.LWorkSourcePath,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            name,
            Path.Combine(tScheduleRoot, name + ".output"),
            WorkCreationOutput.Create(),
            lWorkCreateTime: created)
        {
            LWorkStateCurrent = LWorkState.LWorkStateDone,
            LWorkOutputBytes = 123
        };
        return new TScheduleWork(derived);
    }

    internal int DeliveredAdd(params TScheduleWork[] work) =>
        tSchedule.LScheduleDeliveredAdd(work.Select(item => item.WorkItem).ToArray()).Count;

    internal Guid LineageRead(TScheduleWork work) => tSchedule.LScheduleLineageRead(work.WorkItem);

    internal void Reload() => tSchedule.LScheduleLoad();

    internal bool Reorder(Guid batchId, params TScheduleWork[] work) =>
        tSchedule.LScheduleOrderSet(batchId, work.Select(item => item.WorkId).ToArray());

    internal IReadOnlyList<TScheduleItem> PendingRead() =>
        tSchedule.LSchedulePendingRead().Select(Snapshot).ToArray();

    internal IReadOnlyList<TScheduleItem> RecordsRead() =>
        tSchedule.LScheduleRecords.Select(Snapshot).ToArray();

    internal TScheduleWork ClaimNext()
    {
        LWorkItem workItem = tSchedule.LScheduleClaim(Guid.NewGuid())
            ?? throw new InvalidOperationException("The schedule had no claimable work.");
        tSchedule.LScheduleLoad();
        return new TScheduleWork(workItem);
    }

    internal TScheduleWork? TryClaimNext()
    {
        LWorkItem? workItem = tSchedule.LScheduleClaim(Guid.NewGuid());
        if (workItem is null)
        {
            return null;
        }

        tSchedule.LScheduleLoad();
        return new TScheduleWork(workItem);
    }

    internal void Complete(TScheduleWork work, bool succeeded, string message = "")
    {
        tSchedule.LScheduleCommit(work.WorkItem, succeeded, message);
        tSchedule.LScheduleLoad();
    }

    internal bool Cancel(TScheduleWork work) =>
        tSchedule.LScheduleItemCancel(work.WorkItem);

    internal int Cancel(params TScheduleWork[] work) =>
        work.Count(item => tSchedule.LScheduleItemCancel(item.WorkItem));

    internal bool Reset(TScheduleWork work) =>
        tSchedule.LScheduleItemReset(work.WorkId);

    internal int RemoveEligible(params TScheduleWork[] work)
    {
        IReadOnlyList<Guid> removable = tSchedule.LScheduleRemovableRead(
            work.Select(item => item.WorkId));
        return tSchedule.LScheduleBatchRemove(removable);
    }

    internal int ClearCompleted() => tSchedule.LScheduleDoneClear();

    internal int ClearAll() => tSchedule.LScheduleAllClear();

    internal IReadOnlyList<TScheduleItem> ExecutionOrderRead()
    {
        var claimed = new List<TScheduleItem>();
        while (tSchedule.LScheduleClaim(Guid.NewGuid()) is { } workItem)
        {
            claimed.Add(Snapshot(workItem));
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

    private static TScheduleItem Snapshot(LWorkItem workItem) =>
        new(
            workItem.LWorkId,
            workItem.LWorkBatchId,
            workItem.LWorkOutputName,
            workItem.LWorkPriority,
            workItem.LWorkStateCurrent,
            workItem.LWorkMessage);
}
