using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LBastion
{
    public static IReadOnlySet<Guid> LBastionCohortsRead() =>
        LBastionCohortsRead(LCartographerSchedule.LCartographerScheduleRead());

    public static IReadOnlySet<Guid> LBastionCohortsRead(IReadOnlyList<LWorkItem> lBastionRecords)
    {
        var lBastionCohorts = new HashSet<Guid>();
        foreach (LWorkItem lBastionItem in lBastionRecords)
        {
            if (lBastionItem.LWorkBatchId != Guid.Empty && LBastionItemCheck(lBastionItem))
            {
                lBastionCohorts.Add(lBastionItem.LWorkBatchId);
            }
        }

        return lBastionCohorts;
    }

    private static bool LBastionItemCheck(LWorkItem lBastionItem) => lBastionItem.LWorkRunState.LWorkStateCurrent switch
    {
        LWorkState.LWorkStateRunning => true,
        LWorkState.LWorkStateDone =>
            lBastionItem.LWorkRelayTarget != Guid.Empty
            && lBastionItem.LWorkRelayTarget != LCartographer.LCartographerFinishTarget
            && lBastionItem.LWorkRunState.LWorkOwnerProcess == Environment.ProcessId
            && !LCartographer.LCartographerDeliveredCheck(lBastionItem.LWorkId),
        _ => false
    };
}
