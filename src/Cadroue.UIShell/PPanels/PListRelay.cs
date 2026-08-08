using System.IO;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PMainArea;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PList
{
    public static bool PListDeliveredAdd(Guid pListTargetTab, string pListPath, Guid pListBatch)
    {
        if (PStrip.PStripTabFind(pListTargetTab) is not { } pListTarget
            || pListTarget.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead() is not { } pListOwner)
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{Path.GetFileName(pListPath)}': the destination tab is gone");
            return false;
        }

        int pListAdded = pListOwner.LDocketPathsAdd(PListMediaScan(new[] { pListPath }), pListBatch, true);
        bool pListAccepted = pListAdded > 0 || pListOwner.LDocketItemFind(pListPath) is not null;
        if (!pListAccepted)
        {
            LTraceLog.LTraceWarningRecord(
                $"Relay skipped '{Path.GetFileName(pListPath)}': the destination tab rejected the output");
            return false;
        }

        LTraceLog.LTraceInfoRecord(
            pListAdded > 0
                ? $"Relay added '{Path.GetFileName(pListPath)}' to tab '{pListTarget.PTabTitle}'"
                : $"Relay left '{Path.GetFileName(pListPath)}' out of tab '{pListTarget.PTabTitle}': already listed");
        return true;
    }

    public static void PListDeliveredPlace(Guid pListOriginalTab, string pListPath, Guid pListBatch)
    {
        if (PStrip.PStripTabFind(pListOriginalTab)?.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead() is { } pListOwner)
        {
            pListOwner.LDocketPathsAdd(PListMediaScan(new[] { pListPath }), pListBatch, true);
        }
    }

    public static void PListDeliveredTrack(Guid pListOriginalTab, string pListPath, Guid pListBatch)
    {
        if (PStrip.PStripTabFind(pListOriginalTab)?.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead() is { } pListOwner)
        {
            pListOwner.LDocketDeliveredAdd(PListMediaScan(new[] { pListPath }), pListBatch);
        }
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
        PTabSurface pListSurface = pListSource.PTabWorkspace.PWorkspaceSurface;

        if (pListSurface.PTabList?.PListDocketRead() is { } pListOwner)
        {
            int pListDrained = pListOwner.LDocketPathsRemove(pListDropPaths);
            if (pListDrained > 0)
            {
                LTraceLog.LTraceInfoRecord(
                    $"Relay removed {pListDrained} source file(s) from tab '{pListSource.PTabTitle}' after delivery");
            }
        }

        if (pListSurface.PTabGroup is { } pListSourceGroup
            && pListSourceGroup.PGroupPathsRemove(pListDropPaths))
        {
            LTraceLog.LTraceInfoRecord(
                $"Relay removed the delivered group from tab '{pListSource.PTabTitle}' after delivery");
        }
    }

    public static void PListBatchEvict(IReadOnlyList<Guid> pListRemovedBatches)
    {
        if (PStrip.PStripCurrent is not { } pListTabset)
        {
            return;
        }

        var pListRemovedSet = pListRemovedBatches.ToHashSet();
        foreach (PTabRecord pListTab in pListTabset.PStripRecords)
        {
            PTabSurface pListSurface = pListTab.PTabWorkspace.PWorkspaceSurface;
            if (pListSurface.PTabList?.PListDocketRead() is not { } pListOwner)
            {
                continue;
            }

            string[] pListRemovedPaths = pListOwner.LDocketItemsRead()
                .Where(pListItem => pListRemovedSet.Contains(pListItem.LDocketEntryBatch))
                .Select(pListItem => pListItem.LDocketEntryPath)
                .ToArray();
            if (pListRemovedPaths.Length == 0)
            {
                continue;
            }

            pListOwner.LDocketPathsRemove(pListRemovedPaths);
            pListSurface.PTabGroup?.PGroupPathsRemove(pListRemovedPaths);
            LTraceLog.LTraceInfoRecord(
                $"Relay removed {pListRemovedPaths.Length} file(s) from tab '{pListTab.PTabTitle}' after their batch left the worklist");
        }
    }

    public static void PListSourceUnlock(IReadOnlyList<(string PListPath, Guid PListBatch)> pListUnlocks)
    {
        if (pListUnlocks.Count == 0 || PStrip.PStripCurrent is not { } pListTabset)
        {
            return;
        }

        foreach (PTabRecord pListTab in pListTabset.PStripRecords)
        {
            if (pListTab.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead() is { } pListOwner)
            {
                pListOwner.LDocketRelease(pListUnlocks);
            }
        }
    }

    public static void PListSourceLock(IReadOnlyList<LWorkItem> pListAccepted, Guid pListSourceTab)
    {
        if (pListAccepted.Count == 0
            || PStrip.PStripTabFind(pListSourceTab)?.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead() is not { } pListOwner)
        {
            return;
        }

        var pListLocks = new List<(string, Guid)>();
        foreach (LWorkItem pListItem in pListAccepted)
        {
            pListLocks.Add((pListItem.LWorkSourcePath, pListItem.LWorkBatchId));
            foreach (string pListMergeSource in pListItem.LWorkMergeSources)
            {
                pListLocks.Add((pListMergeSource, pListItem.LWorkBatchId));
            }
        }

        pListOwner.LDocketClaim(pListLocks.Distinct().ToArray());
    }
}
