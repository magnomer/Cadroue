using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public static class LListRelay
{
    private static LStrip? lListStrip;

    public static void LListRelayAttach(LStrip lStrip)
    {
        lListStrip = lStrip;
        LCartographer.LCartographerLockSeam = LListSourceClaim;
        LCartographer.LCartographerDeliverySeam = new LCartographerDelivery(
            LListDeliveredAdd,
            LListDeliveredCommit,
            LListDeliveredRemove,
            LAction.LActionAccept,
            LListBatchRemove,
            LListSourceRelease);
        LMessenger.LMessengerDeliverSource = LListDeliverRun;
    }

    public static bool LListDeliveredAdd(Guid lTargetTab, string lPath, Guid lBatch) =>
        LListDeliveredApply(lTargetTab, lPath, lBatch, false);

    public static bool LListDeliveredCommit(Guid lOriginalTab, string lPath, Guid lBatch) =>
        LListDeliveredApply(lOriginalTab, lPath, lBatch, true);

    public static void LListDeliveredRemove(LWorkItem lWorkItem, bool lForce)
    {
        if ((!lForce && !LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty)
            || lListStrip?.LStripTabFind(lWorkItem) is not { } lSource)
        {
            return;
        }

        var lDropPaths = new List<string> { lWorkItem.LWorkSourcePath };
        lDropPaths.AddRange(lWorkItem.LWorkMergeSources);
        if (lSource.LStripTabDocket is { } lOwner)
        {
            int lDrained = lOwner.LDocketPathsRemove(lDropPaths);
            if (lDrained > 0)
            {
                LTraceLog.LTraceInfoRecord(
                    $"Relay removed {lDrained} source file(s) from tab '{lSource.LStripTabTitle}' after delivery");
            }
        }
    }

    public static void LListBatchRemove(IReadOnlyList<Guid> lRemovedBatches)
    {
        if (lListStrip is not { } lStrip)
        {
            return;
        }

        var lRemovedSet = lRemovedBatches.ToHashSet();
        foreach (LStripTab lTab in lStrip.LStripTabs)
        {
            if (lTab.LStripTabDocket is not { } lOwner)
            {
                continue;
            }

            LDocketEntry[] lDeparted = lOwner.LDocketItemsRead()
                .Where(lItem => lRemovedSet.Contains(lItem.LDocketEntryBatch))
                .ToArray();
            (string, Guid)[] lOrphans = lDeparted
                .Where(lItem => lItem.LDocketEntryLocked && !lItem.LDocketEntryDelivered)
                .Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch))
                .ToArray();
            string[] lRemovedPaths = lDeparted
                .Where(lItem => lItem.LDocketEntryDelivered)
                .Select(lItem => lItem.LDocketEntryPath)
                .ToArray();

            int lReleased = lOrphans.Length == 0 ? 0 : lOwner.LDocketRelease(lOrphans);
            if (lReleased > 0)
            {
                LTraceLog.LTraceInfoRecord(
                    $"Relay unlocked {lReleased} source file(s) in tab '{lTab.LStripTabTitle}' "
                    + "after their work left the worklist unfinished");
            }

            if (lRemovedPaths.Length == 0)
            {
                continue;
            }

            lOwner.LDocketPathsRemove(lRemovedPaths);
            LTraceLog.LTraceInfoRecord(
                $"Relay removed {lRemovedPaths.Length} delivered file(s) from tab '{lTab.LStripTabTitle}' "
                + "after their batch left the worklist");
        }
    }

    public static void LListSourceRelease(
        IReadOnlyList<(string PListPath, Guid PListBatch, LWorkItem PListOwner)> lUnlocks)
    {
        if (lUnlocks.Count == 0 || lListStrip is not { } lStrip)
        {
            return;
        }

        var lOrphans = new List<(string, Guid)>();
        foreach (IGrouping<LStripTab?, (string PListPath, Guid PListBatch, LWorkItem PListOwner)> lGroup
            in lUnlocks.GroupBy(lUnlock => lStrip.LStripTabFind(lUnlock.PListOwner)))
        {
            (string, Guid)[] lReleases = lGroup
                .Select(lUnlock => (lUnlock.PListPath, lUnlock.PListBatch))
                .Distinct()
                .ToArray();
            if (lGroup.Key is { } lTab)
            {
                lTab.LStripTabDocket?.LDocketRelease(lReleases);
                continue;
            }

            lOrphans.AddRange(lReleases);
        }

        if (lOrphans.Count == 0)
        {
            return;
        }

        foreach (LStripTab lTab in lStrip.LStripTabs)
        {
            lTab.LStripTabDocket?.LDocketRelease(lOrphans);
        }
    }

    public static bool LListSourceClaim(IReadOnlyList<LWorkItem> lAccepted)
    {
        bool lClaimed = true;
        if (lListStrip is not { } lStrip)
        {
            return lClaimed;
        }

        foreach (IGrouping<LStripTab?, LWorkItem> lGroup in lAccepted.GroupBy(lStrip.LStripTabFind))
        {
            if (lGroup.Key?.LStripTabDocket is not { } lOwner)
            {
                continue;
            }

            var lLocks = new List<(string, Guid)>();
            foreach (LWorkItem lItem in lGroup)
            {
                lLocks.Add((lItem.LWorkSourcePath, lItem.LWorkBatchId));
                foreach (string lMergeSource in lItem.LWorkMergeSources)
                {
                    lLocks.Add((lMergeSource, lItem.LWorkBatchId));
                }
            }

            lClaimed &= lOwner.LDocketClaim(lLocks.Distinct().ToArray());
        }

        return lClaimed;
    }

    private static bool LListDeliverRun(Guid lTarget, string lPath, Guid lCohort)
    {
        if (!LListDeliveredAdd(lTarget, lPath, lCohort))
        {
            return false;
        }

        LAction.LActionAccept(lTarget, lPath, lCohort);
        return true;
    }

    private static bool LListDeliveredApply(Guid lTab, string lPath, Guid lBatch, bool lLocked)
    {
        string lName = LUsher.LUsherNameRead(lPath);
        if (lListStrip?.LStripTabFind(lTab) is not { } lTarget || lTarget.LStripTabDocket is not { } lOwner)
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{lName}': the destination tab is gone");
            return false;
        }

        IReadOnlyList<string> lScanned = LMedia.LMediaPathScan(new[] { lPath })
            .GetAwaiter()
            .GetResult()
            .LMediaScanPaths;
        if (lScanned.Count == 0)
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{lName}': the destination tab rejected the output");
            return false;
        }

        bool lListed = lOwner.LDocketItemFind(lPath) is not null;
        if (lOwner.LDocketDeliveredAdd(lScanned, lBatch, lLocked) == 0)
        {
            LTraceLog.LTraceWarningRecord(
                $"Relay skipped '{lName}': tab '{lTarget.LStripTabTitle}' still holds it for other work");
            return false;
        }

        LTraceLog.LTraceInfoRecord(lListed
            ? $"Relay took over '{lName}' already listed in tab '{lTarget.LStripTabTitle}'"
            : $"Relay added '{lName}' to tab '{lTarget.LStripTabTitle}'");
        return true;
    }
}
