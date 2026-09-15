using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LSchedule
{
    public int LScheduleAdd(
        IReadOnlyList<LWorkItem> lWorkItems,
        Guid lScheduleRelayTarget = default,
        Guid lScheduleRelaySource = default) =>
        LScheduleAcceptedAdd(lWorkItems, lScheduleRelayTarget, lScheduleRelaySource).Count;

    public IReadOnlyList<LWorkItem> LScheduleAcceptedAdd(
        IReadOnlyList<LWorkItem> lWorkItems,
        Guid lScheduleRelayTarget = default,
        Guid lScheduleRelaySource = default)
    {
        if (lWorkItems.Count == 0)
        {
            return Array.Empty<LWorkItem>();
        }

        LDepotIndex.LDepotIndexCreate();
        var lScheduleAccepted = new List<LWorkItem>(lWorkItems.Count);
        var lScheduleKnownIds = lScheduleItems
            .Select(lScheduleItem => lScheduleItem.LWorkId)
            .ToHashSet();
        foreach (LWorkItem lWorkItem in lWorkItems)
        {
            if (!lScheduleKnownIds.Add(lWorkItem.LWorkId))
            {
                continue;
            }

            if (lScheduleRelayTarget != Guid.Empty)
            {
                lWorkItem.LWorkRelayTarget = lScheduleRelayTarget;
            }

            if (lScheduleRelaySource != Guid.Empty)
            {
                lWorkItem.LWorkRelaySource = lScheduleRelaySource;
            }

            lWorkItem.LWorkSignet = LSignet.LSignetCurrent;

            if (lWorkItem.LWorkLineage == Guid.Empty)
            {
                lWorkItem.LWorkLineage = LScheduleLineage.LScheduleLineageResolve(
                    lWorkItem, lScheduleItems.Concat(lScheduleAccepted));
            }

            var lWorkRecord = LWorkRecord.LWorkRecordCreate(lWorkItem);
            if (LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderScheduled))
            {
                lScheduleAccepted.Add(lWorkItem);
                continue;
            }

            foreach (LWorkItem lScheduleFiled in lScheduleAccepted)
            {
                LScheduleFileRemove(lScheduleFiled.LWorkId);
            }

            LTraceLog.LTraceWarningRecord(
                $"Schedule: could not file work '{lWorkItem.LWorkOutputName}' " +
                $"[{LScheduleIdShorten(lWorkItem.LWorkId)}]; the whole submission of " +
                $"{lWorkItems.Count} item(s) was withdrawn");
            return Array.Empty<LWorkItem>();
        }

        if (lScheduleAccepted.Count > 0)
        {
            foreach (LWorkItem lWorkItem in lScheduleAccepted)
            {
                lScheduleItems.Add(lWorkItem);
            }

            LTraceLog.LTraceInfoRecord($"Schedule: added {lScheduleAccepted.Count} work item(s)");
            LScheduleChange?.Invoke(this);
        }

        return lScheduleAccepted;
    }

    public bool LScheduleOrderSet(Guid lWorkBatchId, IReadOnlyList<Guid> lWorkIds)
    {
        LWorkItem[] lScheduleBatchItems = lScheduleItems
            .Where(lWorkItem => lWorkItem.LWorkBatchId == lWorkBatchId
                && lWorkItem.LWorkStateCurrent == LWorkState.LWorkStatePending)
            .ToArray();
        Guid[] lScheduleRequestedIds = lWorkIds.ToArray();
        var lScheduleBatchIds = lScheduleBatchItems
            .Select(lWorkItem => lWorkItem.LWorkId)
            .ToHashSet();

        if (lScheduleBatchItems.Length == 0
            || lScheduleRequestedIds.Length != lScheduleBatchItems.Length
            || lScheduleRequestedIds.Distinct().Count() != lScheduleRequestedIds.Length
            || lScheduleRequestedIds.Any(lWorkId => !lScheduleBatchIds.Contains(lWorkId)))
        {
            return false;
        }

        DateTimeOffset[] lScheduleOrderTimes = lScheduleBatchItems
            .OrderBy(lWorkItem => lWorkItem.LWorkCreateTime)
            .Select(lWorkItem => lWorkItem.LWorkCreateTime)
            .ToArray();
        Dictionary<Guid, LWorkItem> lScheduleById = lScheduleBatchItems
            .ToDictionary(lWorkItem => lWorkItem.LWorkId);

        for (int lScheduleIndex = 0; lScheduleIndex < lScheduleRequestedIds.Length; lScheduleIndex++)
        {
            LWorkItem lWorkItem = lScheduleById[lScheduleRequestedIds[lScheduleIndex]];
            LWorkRecord lWorkRecord = LWorkRecord.LWorkRecordCreate(lWorkItem);
            lWorkRecord.LWorkCreateTime = lScheduleOrderTimes[lScheduleIndex];
            if (!LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderScheduled))
            {
                return false;
            }
        }

        LScheduleLoad();
        LTraceLog.LTraceInfoRecord(
            $"Schedule: reordered {lScheduleRequestedIds.Length} work item(s) " +
            $"in batch [{LScheduleIdShorten(lWorkBatchId)}]");
        return true;
    }

    public Guid LScheduleLineageRead(LWorkItem lWorkItem) =>
        LScheduleLineage.LScheduleLineageRead(lWorkItem);
}
