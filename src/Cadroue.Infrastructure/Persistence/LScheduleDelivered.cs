using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LSchedule
{
    // Files already-finished derived outputs (e.g. Fix salvage recoveries) straight into
    // the Done store as completed work. Each item is expected to arrive terminal, carrying
    // the source item's batch and lineage, so it flows through the existing Roster/Summary
    // grouping and the relay path exactly like any other finished output.
    public IReadOnlyList<LWorkItem> LScheduleDeliveredAdd(IReadOnlyList<LWorkItem> lWorkItems)
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

            if (lWorkItem.LWorkSignet == Guid.Empty)
            {
                lWorkItem.LWorkSignet = LSignet.LSignetCurrent;
            }

            if (lWorkItem.LWorkLineage == Guid.Empty)
            {
                lWorkItem.LWorkLineage = LScheduleLineage.LScheduleLineageResolve(lWorkItem, lScheduleItems);
            }

            var lWorkRecord = LWorkRecord.LWorkRecordCreate(lWorkItem);
            lWorkRecord.LWorkStateName = lWorkItem.LWorkStateCurrent.ToString();
            lWorkRecord.LWorkProgress = 1;
            if (!LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderDone))
            {
                lScheduleKnownIds.Remove(lWorkItem.LWorkId);
                LTraceLog.LTraceWarningRecord(
                    $"Schedule: could not file delivered work '{lWorkItem.LWorkOutputName}' [{LScheduleIdShorten(lWorkItem.LWorkId)}]");
                continue;
            }

            lScheduleAccepted.Add(lWorkItem);
            lScheduleItems.Add(lWorkItem);
        }

        if (lScheduleAccepted.Count > 0)
        {
            LTraceLog.LTraceInfoRecord($"Schedule: added {lScheduleAccepted.Count} delivered work item(s)");
            LScheduleChange?.Invoke(this);
        }

        return lScheduleAccepted;
    }
}
