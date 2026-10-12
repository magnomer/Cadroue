using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;

namespace Cadroue.Application;

public static class LScheduleLookup
{
    public static IReadOnlyList<LWorkItem> LScheduleRecordsRead(LScheduleContract lSchedule, IReadOnlyList<Guid> lIds)
    {
        var lRecords = new Dictionary<Guid, LWorkItem>();
        foreach (LWorkItem lWorkItem in lSchedule.LScheduleRecords)
        {
            lRecords.TryAdd(lWorkItem.LWorkId, lWorkItem);
        }

        var lWorkItems = new List<LWorkItem>();
        foreach (Guid lId in lIds)
        {
            if (lRecords.TryGetValue(lId, out LWorkItem? lWorkItem))
            {
                lWorkItems.Add(lWorkItem);
            }
        }

        return lWorkItems;
    }

    public static IReadOnlyList<LWorkItem> LScheduleBatchRead(LScheduleContract lSchedule, Guid lBatchId) =>
        lSchedule.LScheduleRecords.Where(lWorkItem => lWorkItem.LWorkBatchId == lBatchId).ToArray();
}
