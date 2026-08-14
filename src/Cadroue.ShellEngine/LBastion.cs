using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LBastion
{
    public static IReadOnlySet<Guid> LBastionCohortsRead() =>
        LBastionCohortsRead(LCartographer.LCartographerScheduleContract?.LScheduleRecords
            ?? (IReadOnlyList<LWorkItem>)Array.Empty<LWorkItem>());

    public static IReadOnlySet<Guid> LBastionCohortsRead(IReadOnlyList<LWorkItem> lBastionRecords)
    {
        var lBastionCohorts = new HashSet<Guid>();
        foreach (LWorkItem lBastionItem in lBastionRecords)
        {
            if (lBastionItem.LWorkBatchId != Guid.Empty && LBastionItemActive(lBastionItem))
            {
                lBastionCohorts.Add(lBastionItem.LWorkBatchId);
            }
        }

        return lBastionCohorts;
    }

    private static bool LBastionItemActive(LWorkItem lBastionItem) => lBastionItem.LWorkStateCurrent switch
    {
        LWorkState.LWorkStateRunning => true,
        LWorkState.LWorkStateDone =>
            lBastionItem.LWorkRelayTarget != Guid.Empty
            && lBastionItem.LWorkRelayTarget != LCartographer.LCartographerFinishTarget
            && lBastionItem.LWorkOwnerProcess == Environment.ProcessId
            && !LCartographer.LCartographerDeliveredCheck(lBastionItem.LWorkId),
        _ => false
    };
}
