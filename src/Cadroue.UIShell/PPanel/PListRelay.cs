using System.IO;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PToolbar;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PList
{
    public static bool PListDeliveredAdd(Guid pListTargetTab, string pListPath, Guid pListBatch) =>
        PListDeliveredApply(pListTargetTab, pListPath, pListBatch, false);

    public static bool PListDeliveredCommit(Guid pListOriginalTab, string pListPath, Guid pListBatch) =>
        PListDeliveredApply(pListOriginalTab, pListPath, pListBatch, true);

    private static bool PListDeliveredApply(Guid pListTab, string pListPath, Guid pListBatch, bool pListLocked)
    {
        string pListName = Path.GetFileName(pListPath);
        if (PStrip.PStripTabFind(pListTab) is not { } pListTarget
            || pListTarget.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead() is not { } pListOwner)
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{pListName}': the destination tab is gone");
            return false;
        }

        IReadOnlyList<string> pListScanned = PListMediaScan(new[] { pListPath });
        if (pListScanned.Count == 0)
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{pListName}': the destination tab rejected the output");
            return false;
        }

        bool pListListed = pListOwner.LDocketItemFind(pListPath) is not null;
        if (pListOwner.LDocketDeliveredAdd(pListScanned, pListBatch, pListLocked) == 0)
        {
            LTraceLog.LTraceWarningRecord(
                $"Relay skipped '{pListName}': tab '{pListTarget.PTabTitle}' still holds it for other work");
            return false;
        }

        LTraceLog.LTraceInfoRecord(pListListed
            ? $"Relay took over '{pListName}' already listed in tab '{pListTarget.PTabTitle}'"
            : $"Relay added '{pListName}' to tab '{pListTarget.PTabTitle}'");
        return true;
    }

    public static void PListDeliveredRemove(LWorkItem lWorkItem, bool pListForce)
    {
        if ((!pListForce && !LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty)
            || PStrip.PStripTabFind(lWorkItem) is not { } pListSource)
        {
            return;
        }

        var pListDropPaths = new List<string> { lWorkItem.LWorkSourcePath };
        pListDropPaths.AddRange(lWorkItem.LWorkMergeSources);
        if (pListSource.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead() is { } pListOwner)
        {
            int pListDrained = pListOwner.LDocketPathsRemove(pListDropPaths);
            if (pListDrained > 0)
            {
                LTraceLog.LTraceInfoRecord(
                    $"Relay removed {pListDrained} source file(s) from tab '{pListSource.PTabTitle}' after delivery");
            }
        }
    }

    public static void PListBatchRemove(IReadOnlyList<Guid> pListRemovedBatches)
    {
        if (PStrip.PStripCurrent is not { } pListTabset)
        {
            return;
        }

        var pListRemovedSet = pListRemovedBatches.ToHashSet();
        foreach (PTabRecord pListTab in pListTabset.PStripRecords)
        {
            if (pListTab.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead() is not { } pListOwner)
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
                    $"Relay unlocked {pListReleased} source file(s) in tab '{pListTab.PTabTitle}' "
                    + "after their work left the worklist unfinished");
            }

            if (pListRemovedPaths.Length == 0)
            {
                continue;
            }

            pListOwner.LDocketPathsRemove(pListRemovedPaths);
            LTraceLog.LTraceInfoRecord(
                $"Relay removed {pListRemovedPaths.Length} delivered file(s) from tab '{pListTab.PTabTitle}' "
                + "after their batch left the worklist");
        }
    }

    public static void PListSourceRelease(
        IReadOnlyList<(string PListPath, Guid PListBatch, LWorkItem PListOwner)> pListUnlocks)
    {
        if (pListUnlocks.Count == 0 || PStrip.PStripCurrent is not { } pListTabset)
        {
            return;
        }

        var pListOrphans = new List<(string, Guid)>();
        foreach (IGrouping<PTabRecord?, (string PListPath, Guid PListBatch, LWorkItem PListOwner)> pListGroup
            in pListUnlocks.GroupBy(pListUnlock => PStrip.PStripTabFind(pListUnlock.PListOwner)))
        {
            (string, Guid)[] pListReleases = pListGroup
                .Select(pListUnlock => (pListUnlock.PListPath, pListUnlock.PListBatch))
                .Distinct()
                .ToArray();
            if (pListGroup.Key is { } pListTab)
            {
                pListTab.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead().LDocketRelease(pListReleases);
                continue;
            }

            pListOrphans.AddRange(pListReleases);
        }

        if (pListOrphans.Count == 0)
        {
            return;
        }

        foreach (PTabRecord pListTab in pListTabset.PStripRecords)
        {
            pListTab.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead().LDocketRelease(pListOrphans);
        }
    }

    public static bool PListSourceClaim(IReadOnlyList<LWorkItem> pListAccepted)
    {
        bool pListClaimed = true;
        foreach (IGrouping<PTabRecord?, LWorkItem> pListGroup in pListAccepted.GroupBy(PStrip.PStripTabFind))
        {
            if (pListGroup.Key?.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead() is not { } pListOwner)
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
