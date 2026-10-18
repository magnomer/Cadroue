using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed record LSealNode(
    Guid LSealNodeId,
    bool LSealNodeMerge,
    bool LSealNodeRelay,
    IReadOnlyList<Guid> LSealNodeCohorts);

public static class LSeal
{
    private static readonly HashSet<(Guid lSealCohort, Guid lSealNode)> lSealFired = new();
    private static bool lSealSweeping;

    public static Func<IReadOnlyList<LSealNode>?>? LSealNodesSource { get; set; }

    public static Func<Guid, Guid, bool>? LSealFireSeam { get; set; }

    private static IReadOnlyList<LWorkItem> LSealScheduleRead() =>
        (IReadOnlyList<LWorkItem>?)LMessenger.LMessengerScheduleSource?.Invoke()?.LScheduleRecords
            ?? Array.Empty<LWorkItem>();

    public static void LSealRun()
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
                    if (!lSealNode.LSealNodeMerge || !lSealNode.LSealNodeRelay)
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

                        if (LSealFireSeam?.Invoke(lSealNode.LSealNodeId, lSealCohort) != true)
                        {
                            continue;
                        }

                        lSealFired.Add((lSealCohort, lSealNode.LSealNodeId));
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

            LSealStaleRemove(lSealItems, lSealNodes);
        }
        finally
        {
            lSealSweeping = false;
        }
    }

    private static bool LSealNodeCheck(
        Guid lSealCohort,
        Guid lSealNode,
        IReadOnlyList<LWorkItem> lSealItems,
        IReadOnlyList<LSealNode> lSealNodes)
    {
        if (LSealPending.LSealPendingRead(lSealCohort) > 0)
        {
            return false;
        }

        foreach (LWorkItem lSealItem in lSealItems)
        {
            if (lSealItem.LWorkBatchId != lSealCohort || lSealItem.LWorkRelayTarget == Guid.Empty)
            {
                continue;
            }

            bool lSealProducing = lSealItem.LWorkRunState.LWorkStateCurrent != LWorkState.LWorkStateDone;
            bool lSealUndelivered = lSealItem.LWorkRunState.LWorkStateCurrent == LWorkState.LWorkStateDone
                && lSealItem.LWorkRunState.LWorkOwnerProcess == Environment.ProcessId
                && !LCartographer.LCartographerDeliveredCheck(lSealItem.LWorkId);
            if ((lSealProducing || lSealUndelivered) && LSealReachCheck(lSealItem.LWorkRelayTarget, lSealNode))
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

            if (LSealReachCheck(lSealOther.LSealNodeId, lSealNode))
            {
                return false;
            }
        }

        return true;
    }

    private static bool LSealReachCheck(Guid lSealFrom, Guid lSealTarget)
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

            lSealCurrent = LCartographerRoute.LCartographerTargetRead(lSealCurrent);
        }

        return false;
    }

    private static void LSealStaleRemove(IReadOnlyList<LWorkItem> lSealItems, IReadOnlyList<LSealNode> lSealNodes)
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
