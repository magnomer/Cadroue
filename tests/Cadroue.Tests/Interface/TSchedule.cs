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
    LWorkState State);

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

    internal int Submit(params TScheduleWork[] work) =>
        tSchedule.LScheduleAdd(work.Select(item => item.WorkItem).ToArray());

    internal bool Reorder(Guid batchId, params TScheduleWork[] work) =>
        tSchedule.LSchedulePendingOrderSet(batchId, work.Select(item => item.WorkId).ToArray());

    internal IReadOnlyList<TScheduleItem> PendingRead() =>
        tSchedule.LSchedulePendingRead().Select(Snapshot).ToArray();

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
            workItem.LWorkStateCurrent);
}
