using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PList
{
    public static bool PListDeliveredAdd(Guid pListTargetTab, string pListPath, Guid pListBatch) =>
        PListDeliveredApply(pListTargetTab, pListPath, pListBatch, false);

    public static bool PListDeliveredCommit(Guid pListOriginalTab, string pListPath, Guid pListBatch) =>
        PListDeliveredApply(pListOriginalTab, pListPath, pListBatch, true);

    private static bool PListDeliveredApply(Guid pListTab, string pListPath, Guid pListBatch, bool pListLocked)
    {
        string pListName = System.IO.Path.GetFileName(pListPath);
        if (PWindow.PWindowStripRead()?.LStrip.LStripTabFind(pListTab) is not { } pListTarget
            || pListTarget.LStripTabDocket is not { } pListOwner)
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{pListName}': the destination tab is gone");
            return false;
        }

        IReadOnlyList<string> pListScanned = Cadroue.Media.LMedia.LMediaPathScan(new[] { pListPath })
            .GetAwaiter()
            .GetResult()
            .LMediaScanPaths;
        if (pListScanned.Count == 0)
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{pListName}': the destination tab rejected the output");
            return false;
        }

        bool pListListed = pListOwner.LDocketItemFind(pListPath) is not null;
        if (pListOwner.LDocketDeliveredAdd(pListScanned, pListBatch, pListLocked) == 0)
        {
            LTraceLog.LTraceWarningRecord(
                $"Relay skipped '{pListName}': tab '{pListTarget.LStripTabTitle}' still holds it for other work");
            return false;
        }

        LTraceLog.LTraceInfoRecord(pListListed
            ? $"Relay took over '{pListName}' already listed in tab '{pListTarget.LStripTabTitle}'"
            : $"Relay added '{pListName}' to tab '{pListTarget.LStripTabTitle}'");
        return true;
    }

    public static void PListDeliveredRemove(LWorkItem lWorkItem, bool pListForce)
    {
        if ((!pListForce && !LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty)
            || PWindow.PWindowStripRead()?.LStrip.LStripTabFind(lWorkItem) is not { } pListSource)
        {
            return;
        }

        var pListDropPaths = new List<string> { lWorkItem.LWorkSourcePath };
        pListDropPaths.AddRange(lWorkItem.LWorkMergeSources);
        if (pListSource.LStripTabDocket is { } pListOwner)
        {
            int pListDrained = pListOwner.LDocketPathsRemove(pListDropPaths);
            if (pListDrained > 0)
            {
                LTraceLog.LTraceInfoRecord(
                    $"Relay removed {pListDrained} source file(s) from tab '{pListSource.LStripTabTitle}' "
                    + "after delivery");
            }
        }
    }

    public static void PListBatchRemove(IReadOnlyList<Guid> pListRemovedBatches)
    {
        if (PWindow.PWindowStripRead() is not { } pListTabset)
        {
            return;
        }

        var pListRemovedSet = pListRemovedBatches.ToHashSet();
        foreach (LStripTab pListTab in pListTabset.LStrip.LStripTabs)
        {
            if (pListTab.LStripTabDocket is not { } pListOwner)
            {
                continue;
            }

            LDocketEntry[] pListDeparted = pListOwner.LDocketItemsRead()
                .Where(pListItem => pListRemovedSet.Contains(pListItem.LDocketEntryBatch))
                .ToArray();
            (string, Guid)[] pListOrphans = pListDeparted
                .Where(pListItem => pListItem.LDocketEntryLocked && !pListItem.LDocketEntryDelivered)
                .Select(pListItem => (pListItem.LDocketEntryPath, pListItem.LDocketEntryBatch))
                .ToArray();
            string[] pListRemovedPaths = pListDeparted
                .Where(pListItem => pListItem.LDocketEntryDelivered)
                .Select(pListItem => pListItem.LDocketEntryPath)
                .ToArray();

            int pListReleased = pListOrphans.Length == 0 ? 0 : pListOwner.LDocketRelease(pListOrphans);
            if (pListReleased > 0)
            {
                LTraceLog.LTraceInfoRecord(
                    $"Relay unlocked {pListReleased} source file(s) in tab '{pListTab.LStripTabTitle}' "
                    + "after their work left the worklist unfinished");
            }

            if (pListRemovedPaths.Length == 0)
            {
                continue;
            }

            pListOwner.LDocketPathsRemove(pListRemovedPaths);
            LTraceLog.LTraceInfoRecord(
                $"Relay removed {pListRemovedPaths.Length} delivered file(s) from tab '{pListTab.LStripTabTitle}' "
                + "after their batch left the worklist");
        }
    }

    public static void PListSourceRelease(
        IReadOnlyList<(string PListPath, Guid PListBatch, LWorkItem PListOwner)> pListUnlocks)
    {
        if (pListUnlocks.Count == 0 || PWindow.PWindowStripRead() is not { } pListTabset)
        {
            return;
        }

        var pListOrphans = new List<(string, Guid)>();
        foreach (IGrouping<LStripTab?, (string PListPath, Guid PListBatch, LWorkItem PListOwner)> pListGroup
            in pListUnlocks.GroupBy(pListUnlock => pListTabset.LStrip.LStripTabFind(pListUnlock.PListOwner)))
        {
            (string, Guid)[] pListReleases = pListGroup
                .Select(pListUnlock => (pListUnlock.PListPath, pListUnlock.PListBatch))
                .Distinct()
                .ToArray();
            if (pListGroup.Key is { } pListTab)
            {
                pListTab.LStripTabDocket?.LDocketRelease(pListReleases);
                continue;
            }

            pListOrphans.AddRange(pListReleases);
        }

        if (pListOrphans.Count == 0)
        {
            return;
        }

        foreach (LStripTab pListTab in pListTabset.LStrip.LStripTabs)
        {
            pListTab.LStripTabDocket?.LDocketRelease(pListOrphans);
        }
    }

    public static bool PListSourceClaim(IReadOnlyList<LWorkItem> pListAccepted)
    {
        bool pListClaimed = true;
        if (PWindow.PWindowStripRead() is not { } pListTabset)
        {
            return pListClaimed;
        }

        foreach (IGrouping<LStripTab?, LWorkItem> pListGroup in pListAccepted.GroupBy(pListTabset.LStrip.LStripTabFind))
        {
            if (pListGroup.Key?.LStripTabDocket is not { } pListOwner)
            {
                continue;
            }

            var pListLocks = new List<(string, Guid)>();
            foreach (LWorkItem pListItem in pListGroup)
            {
                pListLocks.Add((pListItem.LWorkSourcePath, pListItem.LWorkBatchId));
                foreach (string pListMergeSource in pListItem.LWorkMergeSources)
                {
                    pListLocks.Add((pListMergeSource, pListItem.LWorkBatchId));
                }
            }

            pListClaimed &= pListOwner.LDocketClaim(pListLocks.Distinct().ToArray());
        }

        return pListClaimed;
    }
}
