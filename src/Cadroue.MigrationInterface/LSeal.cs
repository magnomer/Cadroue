using Cadroue.Core;

namespace Cadroue.MigrationInterface;

public sealed record LSealNode(
    Guid LSealNodeId,
    bool LSealNodeMerge,
    bool LSealNodeAutoRelay,
    IReadOnlyList<Guid> LSealNodeCohorts);

public static class LSeal
{
    private static readonly HashSet<(Guid lSealCohort, Guid lSealNode)> lSealFired = new();
    private static readonly Dictionary<Guid, int> lSealPending = new();
    private static bool lSealSweeping;

    private static volatile IReadOnlyList<(Guid lSealCohort, DateTimeOffset lSealBirth)> lSealActive =
        Array.Empty<(Guid, DateTimeOffset)>();

    public static Func<IReadOnlyList<LSealNode>?>? LSealNodesSource { get; set; }

    public static Action<Guid>? LSealFireSeam { get; set; }

    private static IReadOnlyList<LWorkItem> LSealScheduleRead() =>
        (IReadOnlyList<LWorkItem>?)LMessenger.LMessengerScheduleSource?.Invoke()?.LScheduleRecords
            ?? Array.Empty<LWorkItem>();

    private static bool LSealDeliveredCheck(Guid lSealWorkId) =>
        LCartographer.LCartographerDeliveredCheck(lSealWorkId);

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
        if (lSealSweeping || LSealNodesSource?.Invoke() is not { } lSealNodes)
        {
            return;
        }

        lSealSweeping = true;
        try
        {
            IReadOnlyList<LWorkItem> lSealItems = LSealScheduleRead();
            bool lSealFiredAny;
            do
            {
                lSealFiredAny = false;
                foreach (LSealNode lSealNode in lSealNodes)
                {
                    if (!lSealNode.LSealNodeMerge || !lSealNode.LSealNodeAutoRelay)
                    {
                        continue;
                    }

                    foreach (Guid lSealCohort in lSealNode.LSealNodeCohorts)
                    {
                        if (lSealFired.Contains((lSealCohort, lSealNode.LSealNodeId))
                            || !LSealNodeCheck(lSealCohort, lSealNode.LSealNodeId, lSealItems, lSealNodes))
                        {
                            continue;
                        }

                        lSealFired.Add((lSealCohort, lSealNode.LSealNodeId));
                        LSealFireSeam?.Invoke(lSealNode.LSealNodeId);
                        lSealFiredAny = true;
                    }
                }

                if (lSealFiredAny)
                {
                    lSealItems = LSealScheduleRead();
                    lSealNodes = LSealNodesSource?.Invoke() ?? lSealNodes;
                }
            }
            while (lSealFiredAny);

            LSealActiveRefresh(lSealItems, lSealNodes);
            LSealClean(lSealItems, lSealNodes);
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

    private static void LSealActiveRefresh(IReadOnlyList<LWorkItem> lSealItems, IReadOnlyList<LSealNode> lSealNodes)
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

        foreach (LSealNode lSealNode in lSealNodes)
        {
            if (!lSealNode.LSealNodeMerge || !lSealNode.LSealNodeAutoRelay)
            {
                continue;
            }

            foreach (Guid lSealCohort in lSealNode.LSealNodeCohorts)
            {
                if (!lSealFired.Contains((lSealCohort, lSealNode.LSealNodeId)))
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
            && lSealItem.LWorkRelayTarget != LCartographer.LCartographerFinishTarget
            && lSealItem.LWorkOwnerProcess == Environment.ProcessId
            && !LSealDeliveredCheck(lSealItem.LWorkId),
        _ => false
    };

    private static bool LSealNodeCheck(
        Guid lSealCohort,
        Guid lSealNode,
        IReadOnlyList<LWorkItem> lSealItems,
        IReadOnlyList<LSealNode> lSealNodes)
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
                && !LSealDeliveredCheck(lSealItem.LWorkId);
            if ((lSealProducing || lSealUndelivered) && LSealReach(lSealItem.LWorkRelayTarget, lSealNode))
            {
                return false;
            }
        }

        foreach (LSealNode lSealOther in lSealNodes)
        {
            if (lSealOther.LSealNodeId == lSealNode
                || !lSealOther.LSealNodeMerge
                || lSealFired.Contains((lSealCohort, lSealOther.LSealNodeId))
                || !lSealOther.LSealNodeCohorts.Contains(lSealCohort))
            {
                continue;
            }

            if (LSealReach(lSealOther.LSealNodeId, lSealNode))
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
            && lSealCurrent != LCartographer.LCartographerFinishTarget
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

    private static void LSealClean(IReadOnlyList<LWorkItem> lSealItems, IReadOnlyList<LSealNode> lSealNodes)
    {
        var lSealLive = lSealItems.Select(lSealItem => lSealItem.LWorkBatchId).ToHashSet();
        foreach (LSealNode lSealNode in lSealNodes)
        {
            foreach (Guid lSealCohort in lSealNode.LSealNodeCohorts)
            {
                lSealLive.Add(lSealCohort);
            }
        }

        lSealFired.RemoveWhere(lSealKey => !lSealLive.Contains(lSealKey.lSealCohort));
    }
}
