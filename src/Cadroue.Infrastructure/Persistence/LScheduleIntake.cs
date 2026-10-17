using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LScheduleIntake : LScheduleIntakeContract
{
    private readonly LSchedule lScheduleIntakeOwner;

    public LScheduleIntake(LSchedule lSchedule)
    {
        lScheduleIntakeOwner = lSchedule;
    }

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
        var lScheduleKnownIds = lScheduleIntakeOwner.LScheduleRecords
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

            if (lWorkItem.LWorkRelayTarget != Guid.Empty)
            {
                lWorkItem.LWorkRelayProcess = Environment.ProcessId;
                lWorkItem.LWorkRelayStamp = LSentinel.LSentinelStampRead();
            }

            lWorkItem.LWorkSignet = LSignet.LSignetCurrent;

            if (lWorkItem.LWorkLineage == Guid.Empty)
            {
                lWorkItem.LWorkLineage = LScheduleLineage.LScheduleLineageResolve(
                    lWorkItem, lScheduleIntakeOwner.LScheduleRecords.Concat(lScheduleAccepted));
            }

            var lWorkRecord = LWorkDocument.LWorkRecordCreate(lWorkItem);
            if (LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderScheduled))
            {
                lScheduleAccepted.Add(lWorkItem);
                continue;
            }

            foreach (LWorkItem lScheduleFiled in lScheduleAccepted)
            {
                LScheduleRemover.LScheduleFileRemove(lScheduleFiled.LWorkId);
            }

            LTraceLog.LTraceWarningRecord(
                $"Schedule: could not file work '{lWorkItem.LWorkOutputName}' " +
                $"[{LSchedule.LScheduleIdShorten(lWorkItem.LWorkId)}]; the whole submission of " +
                $"{lWorkItems.Count} item(s) was withdrawn");
            return Array.Empty<LWorkItem>();
        }

        if (lScheduleAccepted.Count > 0)
        {
            foreach (LWorkItem lWorkItem in lScheduleAccepted)
            {
                lScheduleIntakeOwner.LScheduleItemAdd(lWorkItem);
            }

            LTraceLog.LTraceInfoRecord($"Schedule: added {lScheduleAccepted.Count} work item(s)");
            lScheduleIntakeOwner.LScheduleChangeRaise();
        }

        return lScheduleAccepted;
    }

    public IReadOnlyList<LWorkItem> LScheduleDeliveredAdd(IReadOnlyList<LWorkItem> lWorkItems)
    {
        if (lWorkItems.Count == 0)
        {
            return Array.Empty<LWorkItem>();
        }

        LDepotIndex.LDepotIndexCreate();
        var lScheduleAccepted = new List<LWorkItem>(lWorkItems.Count);
        var lScheduleKnownIds = lScheduleIntakeOwner.LScheduleRecords
            .Select(lScheduleItem => lScheduleItem.LWorkId)
            .ToHashSet();
        foreach (LWorkItem lWorkItem in lWorkItems)
        {
            if (!lScheduleKnownIds.Add(lWorkItem.LWorkId))
            {
                continue;
            }

            if (lWorkItem.LWorkSignet == Guid.Empty)
            {
                lWorkItem.LWorkSignet = LSignet.LSignetCurrent;
            }

            if (lWorkItem.LWorkLineage == Guid.Empty)
            {
                lWorkItem.LWorkLineage = LScheduleLineage.LScheduleLineageResolve(
                    lWorkItem, lScheduleIntakeOwner.LScheduleRecords);
            }

            var lWorkRecord = LWorkDocument.LWorkRecordCreate(lWorkItem);
            lWorkRecord.LWorkStateName = lWorkItem.LWorkRunState.LWorkStateCurrent.ToString();
            lWorkRecord.LWorkProgress = 1;
            if (!LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderDone))
            {
                lScheduleKnownIds.Remove(lWorkItem.LWorkId);
                LTraceLog.LTraceWarningRecord(
                    $"Schedule: could not file delivered work '{lWorkItem.LWorkOutputName}' " +
                    $"[{LSchedule.LScheduleIdShorten(lWorkItem.LWorkId)}]");
                continue;
            }

            lScheduleAccepted.Add(lWorkItem);
            lScheduleIntakeOwner.LScheduleItemAdd(lWorkItem);
        }

        if (lScheduleAccepted.Count > 0)
        {
            LTraceLog.LTraceInfoRecord($"Schedule: added {lScheduleAccepted.Count} delivered work item(s)");
            lScheduleIntakeOwner.LScheduleChangeRaise();
        }

        return lScheduleAccepted;
    }

    public bool LScheduleOrderSet(Guid lWorkBatchId, IReadOnlyList<Guid> lWorkIds)
    {
        LWorkItem[] lScheduleBatchItems = lScheduleIntakeOwner.LScheduleRecords
            .Where(lWorkItem => lWorkItem.LWorkBatchId == lWorkBatchId
                && lWorkItem.LWorkRunState.LWorkStateCurrent == LWorkState.LWorkStatePending)
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
            LWorkDocument lWorkRecord = LWorkDocument.LWorkRecordCreate(lWorkItem);
            lWorkRecord.LWorkCreateTime = lScheduleOrderTimes[lScheduleIndex];
            if (!LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderScheduled))
            {
                return false;
            }
        }

        lScheduleIntakeOwner.LScheduleLoad();
        LTraceLog.LTraceInfoRecord(
            $"Schedule: reordered {lScheduleRequestedIds.Length} work item(s) " +
            $"in batch [{LSchedule.LScheduleIdShorten(lWorkBatchId)}]");
        return true;
    }
}
