using Cadroue.Core;
using Cadroue.MigrationInterface;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static class LSeal
{
    private static readonly HashSet<(Guid lSealCohort, Guid lSealNode)> lSealFired = new();
    private static readonly Dictionary<Guid, int> lSealPending = new();
    private static bool lSealSweeping;

    private static volatile IReadOnlyList<(Guid lSealCohort, DateTimeOffset lSealBirth)> lSealActive =
        Array.Empty<(Guid, DateTimeOffset)>();

    public static void LSealPendingAdd(Guid lSealCohort)
    {
        if (lSealCohort == Guid.Empty)
        {
            return;
        }

        lSealPending[lSealCohort] = LSealPendingRead(lSealCohort) + 1;
    }

    public static void LSealPendingRemove(Guid lSealCohort)
    {
        if (!lSealPending.TryGetValue(lSealCohort, out int lSealCount))
        {
            return;
        }

        if (lSealCount <= 1)
        {
            lSealPending.Remove(lSealCohort);
        }
        else
        {
            lSealPending[lSealCohort] = lSealCount - 1;
        }
    }

    public static int LSealPendingRead(Guid lSealCohort) =>
        lSealPending.TryGetValue(lSealCohort, out int lSealCount) ? lSealCount : 0;

    public static void LSealSweep()
    {
        if (lSealSweeping || LTabset.LTabsetCurrent is not { } lSealTabset)
        {
            return;
        }

        lSealSweeping = true;
        try
        {
            IReadOnlyList<LWorkItem> lSealItems = PProgram.LScheduleCurrent.LScheduleRecords.ToArray();
            bool lSealFiredAny;
            do
            {
                lSealFiredAny = false;
                foreach (PTabRecord lSealTab in lSealTabset.PTabsetRecords)
                {
                    if (lSealTab.PTabWorkspace.PWorkspaceSurface is not PMergeTab
                        || lSealTab.PTabWorkspace.PWorkspaceSurface.PTabAction is not { PActionAutoRelay: true } lSealAction
                        || lSealTab.PTabWorkspace.PWorkspaceSurface.PTabList is not { } lSealList)
                    {
                        continue;
                    }

                    foreach (Guid lSealCohort in LSealCohortsHeld(lSealList))
                    {
                        if (lSealFired.Contains((lSealCohort, lSealTab.PTabId))
                            || !LSealNodeCheck(lSealCohort, lSealTab.PTabId, lSealItems, lSealTabset))
                        {
                            continue;
                        }

                        lSealFired.Add((lSealCohort, lSealTab.PTabId));
                        lSealAction.PActionAllRun();
                        lSealFiredAny = true;
                    }
                }

                if (lSealFiredAny)
                {
                    lSealItems = PProgram.LScheduleCurrent.LScheduleRecords.ToArray();
                }
            }
            while (lSealFiredAny);

            LSealActiveRefresh(lSealItems, lSealTabset);
            LSealClean(lSealItems, lSealTabset);
        }
        finally
        {
            lSealSweeping = false;
        }
    }

    public static bool LSealClaimCheck(Guid lSealCohort)
    {
        IReadOnlyList<(Guid lSealCohort, DateTimeOffset lSealBirth)> lSealSnapshot = lSealActive;
        DateTimeOffset? lSealSelf = null;
        DateTimeOffset? lSealOldestOther = null;
        foreach ((Guid lSealEntry, DateTimeOffset lSealBirth) in lSealSnapshot)
        {
            if (lSealEntry == lSealCohort)
            {
                lSealSelf = lSealBirth;
            }
            else if (lSealOldestOther is null || lSealBirth < lSealOldestOther)
            {
                lSealOldestOther = lSealBirth;
            }
        }

        if (lSealSelf is null)
        {
            return true;
        }

        return lSealOldestOther is null || lSealSelf <= lSealOldestOther;
    }

    private static void LSealActiveRefresh(IReadOnlyList<LWorkItem> lSealItems, LTabset lSealTabset)
    {
        var lSealBirths = new Dictionary<Guid, DateTimeOffset>();
        var lSealActiveSet = new HashSet<Guid>();
        foreach (LWorkItem lSealItem in lSealItems)
        {
            if (lSealItem.LWorkBatchId == Guid.Empty)
            {
                continue;
            }

            if (!lSealBirths.TryGetValue(lSealItem.LWorkBatchId, out DateTimeOffset lSealSeen)
                || lSealItem.LWorkCreateTime < lSealSeen)
            {
                lSealBirths[lSealItem.LWorkBatchId] = lSealItem.LWorkCreateTime;
            }

            if (LSealItemActive(lSealItem))
            {
                lSealActiveSet.Add(lSealItem.LWorkBatchId);
            }
        }

        foreach (Guid lSealCohort in lSealPending.Keys)
        {
            lSealActiveSet.Add(lSealCohort);
        }

        foreach (PTabRecord lSealTab in lSealTabset.PTabsetRecords)
        {
            if (lSealTab.PTabWorkspace.PWorkspaceSurface is not PMergeTab
                || lSealTab.PTabWorkspace.PWorkspaceSurface.PTabAction is not { PActionAutoRelay: true }
                || lSealTab.PTabWorkspace.PWorkspaceSurface.PTabList is not { } lSealList)
            {
                continue;
            }

            foreach (Guid lSealCohort in LSealCohortsHeld(lSealList))
            {
                if (!lSealFired.Contains((lSealCohort, lSealTab.PTabId)))
                {
                    lSealActiveSet.Add(lSealCohort);
                }
            }
        }

        lSealActive = lSealActiveSet
            .Select(lSealCohort => (lSealCohort,
                lSealBirths.TryGetValue(lSealCohort, out DateTimeOffset lSealBirth)
                    ? lSealBirth
                    : DateTimeOffset.MaxValue))
            .ToArray();
    }

    private static bool LSealItemActive(LWorkItem lSealItem) => lSealItem.LWorkStateCurrent switch
    {
        LWorkState.LWorkStatePending => true,
        LWorkState.LWorkStateRunning => true,
        LWorkState.LWorkStateDone =>
            lSealItem.LWorkRelayTarget != Guid.Empty
            && lSealItem.LWorkRelayTarget != LCourier.LCourierFinishTarget
            && lSealItem.LWorkOwnerProcess == Environment.ProcessId
            && !LCourier.LCourierDeliveredCheck(lSealItem.LWorkId),
        _ => false
    };

    private static bool LSealNodeCheck(
        Guid lSealCohort,
        Guid lSealNode,
        IReadOnlyList<LWorkItem> lSealItems,
        LTabset lSealTabset)
    {
        if (LSealPendingRead(lSealCohort) > 0)
        {
            return false;
        }

        foreach (LWorkItem lSealItem in lSealItems)
        {
            if (lSealItem.LWorkBatchId != lSealCohort || lSealItem.LWorkRelayTarget == Guid.Empty)
            {
                continue;
            }

            bool lSealProducing = lSealItem.LWorkStateCurrent != LWorkState.LWorkStateDone;
            bool lSealUndelivered = lSealItem.LWorkStateCurrent == LWorkState.LWorkStateDone
                && lSealItem.LWorkOwnerProcess == Environment.ProcessId
                && !LCourier.LCourierDeliveredCheck(lSealItem.LWorkId);
            if ((lSealProducing || lSealUndelivered) && LSealReach(lSealItem.LWorkRelayTarget, lSealNode))
            {
                return false;
            }
        }

        foreach (PTabRecord lSealOther in lSealTabset.PTabsetRecords)
        {
            if (lSealOther.PTabId == lSealNode
                || lSealOther.PTabWorkspace.PWorkspaceSurface is not PMergeTab
                || lSealOther.PTabWorkspace.PWorkspaceSurface.PTabList is not { } lSealOtherList
                || lSealFired.Contains((lSealCohort, lSealOther.PTabId))
                || !LSealCohortsHeld(lSealOtherList).Contains(lSealCohort))
            {
                continue;
            }

            if (LSealReach(lSealOther.PTabId, lSealNode))
            {
                return false;
            }
        }

        return true;
    }

    private static bool LSealReach(Guid lSealFrom, Guid lSealTarget)
    {
        var lSealSeen = new HashSet<Guid>();
        Guid lSealCurrent = lSealFrom;
        while (lSealCurrent != Guid.Empty
            && lSealCurrent != LCourier.LCourierFinishTarget
            && lSealSeen.Add(lSealCurrent))
        {
            if (lSealCurrent == lSealTarget)
            {
                return true;
            }

            lSealCurrent = LCartographer.LCartographerTargetRead(lSealCurrent);
        }

        return false;
    }

    private static IReadOnlyList<Guid> LSealCohortsHeld(PList lSealList) =>
        lSealList.PListItemsRead()
            .Where(lSealItem => lSealItem.PListItemDelivered && lSealItem.PListItemRelay != Guid.Empty)
            .Select(lSealItem => lSealItem.PListItemRelay)
            .Distinct()
            .ToArray();

    private static void LSealClean(IReadOnlyList<LWorkItem> lSealItems, LTabset lSealTabset)
    {
        var lSealLive = lSealItems.Select(lSealItem => lSealItem.LWorkBatchId).ToHashSet();
        foreach (PTabRecord lSealTab in lSealTabset.PTabsetRecords)
        {
            if (lSealTab.PTabWorkspace.PWorkspaceSurface.PTabList is { } lSealList)
            {
                foreach (Guid lSealCohort in LSealCohortsHeld(lSealList))
                {
                    lSealLive.Add(lSealCohort);
                }
            }
        }

        lSealFired.RemoveWhere(lSealKey => !lSealLive.Contains(lSealKey.lSealCohort));
    }
}
