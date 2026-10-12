using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public static class QListRelay
{
    private static QStrip? qListStrip;
    private static CUsher? qListUsher;

    public static void QListRelayAttach(QStrip lStrip, CUsher lUsher)
    {
        qListStrip = lStrip;
        qListUsher = lUsher;
        LCartographer.LCartographerLockSeam = QListSourceClaim;
        LCartographer.LCartographerDeliverySeam = new LCartographerDelivery(
            QListDeliveredAdd,
            QListDeliveredCommit,
            QListDeliveredRemove,
            QAction.QActionAccept,
            QListBatchRemove,
            QListSourceRelease,
            LMedia.LMediaCheck);
        LMessenger.LMessengerDeliverSource = QListDeliverRun;
    }

    public static bool QListDeliveredAdd(Guid lTargetTab, string lPath, Guid lBatch) =>
        QListDeliveredApply(lTargetTab, lPath, lBatch, false);

    public static bool QListDeliveredCommit(Guid lOriginalTab, string lPath, Guid lBatch) =>
        QListDeliveredApply(lOriginalTab, lPath, lBatch, true);

    public static void QListDeliveredRemove(LWorkItem lWorkItem, bool lForce)
    {
        if ((!lForce && !LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty)
            || qListStrip?.QStripTabFind(lWorkItem) is not { } lSource)
        {
            return;
        }

        var lDropPaths = new List<string> { lWorkItem.LWorkSourcePath };
        lDropPaths.AddRange(lWorkItem.LWorkMergeSources);
        if (lSource.QStripTabDocket is { } lOwner)
        {
            int lDrained = lOwner.LDocketPathsRemove(lDropPaths);
            if (lDrained > 0)
            {
                LTraceLog.LTraceInfoRecord(
                    $"Relay removed {lDrained} source file(s) from tab '{lSource.QStripTabTitle}' after delivery");
            }
        }
    }

    public static void QListBatchRemove(IReadOnlyList<Guid> lRemovedBatches)
    {
        if (qListStrip is not { } lStrip)
        {
            return;
        }

        var lRemovedSet = lRemovedBatches.ToHashSet();
        foreach (QStripTab lTab in lStrip.QStripTabs)
        {
            if (lTab.QStripTabDocket is not { } lOwner)
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
                    $"Relay unlocked {lReleased} source file(s) in tab '{lTab.QStripTabTitle}' "
                    + "after their work left the worklist unfinished");
            }

            if (lRemovedPaths.Length == 0)
            {
                continue;
            }

            lOwner.LDocketPathsRemove(lRemovedPaths);
            LTraceLog.LTraceInfoRecord(
                $"Relay removed {lRemovedPaths.Length} delivered file(s) from tab '{lTab.QStripTabTitle}' "
                + "after their batch left the worklist");
        }
    }

    public static void QListSourceRelease(
        IReadOnlyList<(string PListPath, Guid PListBatch, LWorkItem PListOwner)> lUnlocks)
    {
        if (lUnlocks.Count == 0 || qListStrip is not { } lStrip)
        {
            return;
        }

        var lOrphans = new List<(string, Guid)>();
        foreach (IGrouping<QStripTab?, (string PListPath, Guid PListBatch, LWorkItem PListOwner)> lGroup
            in lUnlocks.GroupBy(lUnlock => lStrip.QStripTabFind(lUnlock.PListOwner)))
        {
            (string, Guid)[] lReleases = lGroup
                .Select(lUnlock => (lUnlock.PListPath, lUnlock.PListBatch))
                .Distinct()
                .ToArray();
            if (lGroup.Key is { } lTab)
            {
                lTab.QStripTabDocket?.LDocketRelease(lReleases);
                continue;
            }

            lOrphans.AddRange(lReleases);
        }

        if (lOrphans.Count == 0)
        {
            return;
        }

        foreach (QStripTab lTab in lStrip.QStripTabs)
        {
            lTab.QStripTabDocket?.LDocketRelease(lOrphans);
        }
    }

    public static bool QListSourceClaim(IReadOnlyList<LWorkItem> lAccepted)
    {
        bool lClaimed = true;
        if (qListStrip is not { } lStrip)
        {
            return lClaimed;
        }

        foreach (IGrouping<QStripTab?, LWorkItem> lGroup in lAccepted.GroupBy(lStrip.QStripTabFind))
        {
            if (lGroup.Key?.QStripTabDocket is not { } lOwner)
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

    private static bool QListDeliverRun(Guid lTarget, string lPath, Guid lCohort)
    {
        if (!QListDeliveredAdd(lTarget, lPath, lCohort))
        {
            return false;
        }

        QAction.QActionAccept(lTarget, lPath, lCohort);
        return true;
    }

    private static bool QListDeliveredApply(Guid lTab, string lPath, Guid lBatch, bool lLocked)
    {
        if (qListStrip is not { } lStrip || qListUsher is not { } lUsher)
        {
            string lPathName = lPath[(lPath.LastIndexOfAny(['\\', '/', ':']) + 1)..];
            LTraceLog.LTraceWarningRecord($"Relay skipped '{lPathName}': the destination tab is gone");
            return false;
        }

        string lName = lUsher.CUsherNameRead(lPath);
        if (lStrip.QStripTabFind(lTab) is not { } lTarget || lTarget.QStripTabDocket is not { } lOwner)
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{lName}': the destination tab is gone");
            return false;
        }

        IReadOnlyList<string> lScanned = LMedia.LMediaFileRead(lPath);
        if (lScanned.Count == 0)
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{lName}': the destination tab rejected the output");
            return false;
        }

        bool lListed = lOwner.LDocketItemFind(lPath) is not null;
        if (lOwner.LDocketDeliveredAdd(lScanned, lBatch, lLocked) == 0)
        {
            LTraceLog.LTraceWarningRecord(
                $"Relay skipped '{lName}': tab '{lTarget.QStripTabTitle}' still holds it for other work");
            return false;
        }

        LTraceLog.LTraceInfoRecord(lListed
            ? $"Relay took over '{lName}' already listed in tab '{lTarget.QStripTabTitle}'"
            : $"Relay added '{lName}' to tab '{lTarget.QStripTabTitle}'");
        return true;
    }
}
