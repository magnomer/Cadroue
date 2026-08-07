using System.IO;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;
using Cadroue.MigrationInterface;

namespace Cadroue.UIShell.PMainArea;

public sealed record LCourierOption(Guid LCourierTabId, string LCourierTabTitle, ImageSource? LCourierTabIcon);

public static class LCourier
{
    public static readonly Guid LCourierFinishTarget = LCartographer.LCartographerFinishTarget;

    private const int LCourierFinishSlot = -2;

    private static bool lCourierWatching;

    public static void LCourierStart()
    {
        if (lCourierWatching)
        {
            return;
        }

        lCourierWatching = true;
        LCartographer.LCartographerDeliverySeam = new LCartographerDelivery(
            LCourierTabAdd, LCourierTabPlace, LCourierTabTrack, LCourierSourceRemove, LCourierStageRun, LCourierArrive,
            LCourierBatchEvict, LCourierSourceUnlock);
        LCartographer.LCartographerStart();
    }

    internal static void LCourierSourceLock(
        IReadOnlyList<LWorkItem> lCourierAccepted,
        Guid lCourierSourceTab)
    {
        if (lCourierAccepted.Count == 0
            || LCourierTabFind(lCourierSourceTab)?.PTabWorkspace.PWorkspaceSurface.PTabList is not { } lCourierList)
        {
            return;
        }

        var lCourierLocks = new List<(string PListPath, Guid PListBatch)>();
        foreach (LWorkItem lCourierItem in lCourierAccepted)
        {
            lCourierLocks.Add((lCourierItem.LWorkSourcePath, lCourierItem.LWorkBatchId));
            foreach (string lCourierMergeSource in lCourierItem.LWorkMergeSources)
            {
                lCourierLocks.Add((lCourierMergeSource, lCourierItem.LWorkBatchId));
            }
        }

        lCourierList.PListPathsLock(lCourierLocks.Distinct().ToArray());
    }

    public static void LCourierAttach(Guid lCourierSourceTab, PAction pCourierAction)
    {
        LCourierStart();
        pCourierAction.PActionSourceTab = lCourierSourceTab;
        pCourierAction.PActionRelaySource = () => LCourierOptionsRead(lCourierSourceTab);
        pCourierAction.PActionRelayChange += lCourierTarget =>
        {
            LCartographer.LCartographerTargetSet(lCourierSourceTab, lCourierTarget);
            pCourierAction.PActionRelayApply(LCartographer.LCartographerTargetRead(lCourierSourceTab));
        };
        pCourierAction.PActionRelayApply(LCartographer.LCartographerTargetRead(lCourierSourceTab));
    }

    public static void LCourierFaceUpdate()
    {
        if (PStrip.PStripCurrent is not { } lCourierTabset)
        {
            return;
        }

        foreach (PTabRecord pTabRecord in lCourierTabset.PStripRecords)
        {
            pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(
                LCartographer.LCartographerTargetRead(pTabRecord.PTabId));
        }
    }

    public static IReadOnlyList<LCourierOption> LCourierOptionsRead(Guid lCourierSourceTab)
    {
        if (PStrip.PStripCurrent is not { } lCourierTabset)
        {
            return Array.Empty<LCourierOption>();
        }

        var lCourierOptions = new List<LCourierOption>();
        foreach (PTabRecord pTabRecord in lCourierTabset.PStripRecords)
        {
            if (pTabRecord.PTabId == lCourierSourceTab
                || pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabList is null)
            {
                continue;
            }

            lCourierOptions.Add(new LCourierOption(
                pTabRecord.PTabId, pTabRecord.PTabTitle, pTabRecord.PTabIconSource));
        }

        return lCourierOptions;
    }

    public static IReadOnlyList<int> LCourierSlotsRead(IReadOnlyList<PTabRecord> pCourierTabRecords)
    {
        var lCourierSlots = new List<int>(pCourierTabRecords.Count);
        foreach (PTabRecord pTabRecord in pCourierTabRecords)
        {
            Guid lCourierTarget = LCartographer.LCartographerTargetRead(pTabRecord.PTabId);
            int lCourierSlot = lCourierTarget == LCourierFinishTarget ? LCourierFinishSlot : -1;
            for (int lCourierIndex = 0; lCourierSlot == -1 && lCourierIndex < pCourierTabRecords.Count; lCourierIndex++)
            {
                if (pCourierTabRecords[lCourierIndex].PTabId == lCourierTarget)
                {
                    lCourierSlot = lCourierIndex;
                    break;
                }
            }

            lCourierSlots.Add(lCourierSlot);
        }

        return lCourierSlots;
    }

    public static void LCourierSlotsApply(
        IReadOnlyList<PTabRecord> pCourierTabRecords,
        IReadOnlyList<int> lCourierSlots)
    {
        for (int lCourierIndex = 0; lCourierIndex < pCourierTabRecords.Count; lCourierIndex++)
        {
            if (lCourierIndex >= lCourierSlots.Count)
            {
                break;
            }

            int lCourierSlot = lCourierSlots[lCourierIndex];
            if (lCourierSlot == LCourierFinishSlot)
            {
                PTabRecord pCourierFinishSource = pCourierTabRecords[lCourierIndex];
                LCartographer.LCartographerTargetSet(pCourierFinishSource.PTabId, LCourierFinishTarget);
                pCourierFinishSource.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(LCourierFinishTarget);
                continue;
            }

            if (lCourierSlot < 0 || lCourierSlot >= pCourierTabRecords.Count || lCourierSlot == lCourierIndex)
            {
                continue;
            }

            PTabRecord pCourierSource = pCourierTabRecords[lCourierIndex];
            PTabRecord pCourierTarget = pCourierTabRecords[lCourierSlot];
            if (pCourierTarget.PTabWorkspace.PWorkspaceSurface.PTabList is null)
            {
                continue;
            }

            LCartographer.LCartographerTargetSet(pCourierSource.PTabId, pCourierTarget.PTabId);
            pCourierSource.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(pCourierTarget.PTabId);
        }
    }

    internal static void LCourierBatchEvict(IReadOnlyList<Guid> lCourierRemovedBatches)
    {
        if (PStrip.PStripCurrent is not { } lCourierTabset)
        {
            return;
        }

        var lCourierRemovedSet = lCourierRemovedBatches.ToHashSet();
        foreach (PTabRecord lCourierTab in lCourierTabset.PStripRecords)
        {
            PTabSurface lCourierSurface = lCourierTab.PTabWorkspace.PWorkspaceSurface;
            if (lCourierSurface.PTabList is not { } lCourierList)
            {
                continue;
            }

            string[] lCourierRemovedPaths = lCourierList.PListItemsRead()
                .Where(lCourierItem => lCourierRemovedSet.Contains(lCourierItem.PListItemRelay))
                .Select(lCourierItem => lCourierItem.PListItemPath)
                .ToArray();
            if (lCourierRemovedPaths.Length == 0)
            {
                continue;
            }

            lCourierList.PListPathsRemove(lCourierRemovedPaths);
            lCourierSurface.PTabGroup?.PGroupPathsRemove(lCourierRemovedPaths);
            LTraceLog.LTraceInfoRecord(
                $"Relay removed {lCourierRemovedPaths.Length} file(s) from tab '{lCourierTab.PTabTitle}' after their batch left the worklist");
        }
    }

    internal static void LCourierSourceUnlock(IReadOnlyList<(string PListPath, Guid PListBatch)> lCourierUnlocks)
    {
        if (lCourierUnlocks.Count == 0 || PStrip.PStripCurrent is not { } lCourierTabset)
        {
            return;
        }

        foreach (PTabRecord lCourierTab in lCourierTabset.PStripRecords)
        {
            if (lCourierTab.PTabWorkspace.PWorkspaceSurface.PTabList is { } lCourierList)
            {
                lCourierList.PListPathsUnlock(lCourierUnlocks);
            }
        }
    }

    public static void LCourierArrive(Guid lCourierTargetTab, string lCourierPath, Guid lCourierCohort)
    {
        if (LCourierTabFind(lCourierTargetTab) is not { } pCourierTarget
            || pCourierTarget.PTabWorkspace.PWorkspaceSurface is PMergeTab
            || pCourierTarget.PTabWorkspace.PWorkspaceSurface.PTabAction is not { PActionAutoRelay: true } pCourierAction)
        {
            return;
        }

        LSeal.LSealPendingAdd(lCourierCohort);
        void LCourierArriveRun()
        {
            try
            {
                pCourierAction.PActionItemsRun(new[] { lCourierPath });
            }
            finally
            {
                LSeal.LSealPendingRemove(lCourierCohort);
                LSeal.LSealSweep();
            }
        }

        if (System.Windows.Application.Current?.Dispatcher is { } pCourierDispatcher)
        {
            pCourierDispatcher.BeginInvoke(new Action(LCourierArriveRun));
        }
        else
        {
            LCourierArriveRun();
        }
    }

    private static bool LCourierTabAdd(Guid lCourierTargetTab, string lCourierPath, Guid lCourierBatch)
    {
        if (LCourierTabFind(lCourierTargetTab) is not { } pCourierTarget
            || pCourierTarget.PTabWorkspace.PWorkspaceSurface.PTabList is not { } pCourierList)
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{Path.GetFileName(lCourierPath)}': the destination tab is gone");
            return false;
        }

        int lCourierAdded = pCourierList.PListPathsAdd(new[] { lCourierPath }, lCourierBatch, true);
        bool lCourierAccepted = lCourierAdded > 0 || pCourierList.PListPathsRead().Any(
            lCourierExisting => string.Equals(lCourierExisting, lCourierPath, StringComparison.OrdinalIgnoreCase));
        if (!lCourierAccepted)
        {
            LTraceLog.LTraceWarningRecord(
                $"Relay skipped '{Path.GetFileName(lCourierPath)}': the destination tab rejected the output");
            return false;
        }

        LTraceLog.LTraceInfoRecord(
            lCourierAdded > 0
                ? $"Relay added '{Path.GetFileName(lCourierPath)}' to tab '{pCourierTarget.PTabTitle}'"
                : $"Relay left '{Path.GetFileName(lCourierPath)}' out of tab '{pCourierTarget.PTabTitle}': already listed");
        return true;
    }

    private static void LCourierTabPlace(Guid lCourierOriginalTab, string lCourierPath, Guid lCourierBatch)
    {
        if (LCourierTabFind(lCourierOriginalTab)?.PTabWorkspace.PWorkspaceSurface.PTabList is { } lCourierList)
        {
            lCourierList.PListPathsAdd(new[] { lCourierPath }, lCourierBatch, true);
        }
    }

    private static void LCourierTabTrack(Guid lCourierOriginalTab, string lCourierPath, Guid lCourierBatch)
    {
        if (LCourierTabFind(lCourierOriginalTab)?.PTabWorkspace.PWorkspaceSurface.PTabList is { } lCourierList)
        {
            lCourierList.PListPathsTrack(new[] { lCourierPath }, lCourierBatch);
        }
    }

    private static void LCourierSourceRemove(LWorkItem lWorkItem, bool lCourierForce)
    {
        if ((!lCourierForce && !LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty)
            || LCourierTabFind(lWorkItem) is not { } pCourierSource)
        {
            return;
        }

        var lCourierDropPaths = new List<string> { lWorkItem.LWorkSourcePath };
        lCourierDropPaths.AddRange(lWorkItem.LWorkMergeSources);

        if (pCourierSource.PTabWorkspace.PWorkspaceSurface.PTabList is { } pCourierSourceList)
        {
            int lCourierDrained = pCourierSourceList.PListPathsRemove(lCourierDropPaths);
            if (lCourierDrained > 0)
            {
                LTraceLog.LTraceInfoRecord(
                    $"Relay removed {lCourierDrained} source file(s) from tab '{pCourierSource.PTabTitle}' after delivery");
            }
        }

        if (pCourierSource.PTabWorkspace.PWorkspaceSurface.PTabGroup is { } pCourierSourceGroup
            && pCourierSourceGroup.PGroupPathsRemove(lCourierDropPaths))
        {
            LTraceLog.LTraceInfoRecord(
                $"Relay removed the delivered group from tab '{pCourierSource.PTabTitle}' after delivery");
        }
    }

    private static PTabRecord? LCourierTabFind(LWorkItem lWorkItem)
    {
        Guid lCourierSourceTab = lWorkItem.LWorkRelaySource;
        if (lCourierSourceTab == Guid.Empty)
        {
            return null;
        }

        if (LCartographerPlanStore.LCartographerPlanRead(lWorkItem.LWorkBatchId, out LCartographerPlanRecord lCourierPlan)
            && lCourierPlan.LCartographerStages.FirstOrDefault(
                lCourierStage => lCourierStage.LCartographerStageId == lCourierSourceTab) is { } lCourierSourceStage)
        {
            lCourierSourceTab = lCourierSourceStage.LCartographerOriginalTab;
        }

        return LCourierTabFind(lCourierSourceTab);
    }

    private static PTabRecord? LCourierTabFind(Guid lCourierTabId) =>
        PStrip.PStripCurrent?.PStripRecords.FirstOrDefault(pTabRecord => pTabRecord.PTabId == lCourierTabId);

    private static bool LCourierStageRun(LCartographerStagePlan lCourierPlan)
    {
        bool LCourierPlanRun()
        {
            LPreset lCourierPreset = LPreset.LPresetStateCreate(lCourierPlan.LCartographerExport);
            var lCourierWorkspace = new PWorkspace(
                lCourierPlan.LCartographerLayoutKey, lCourierPreset, lCourierPlan.LCartographerLayout);
            try
            {
                PTabSurface lCourierSurface = lCourierWorkspace.PWorkspaceSurface;
                if (lCourierSurface.PTabAction is not { } lCourierAction || lCourierSurface.PTabList is not { } lCourierList)
                {
                    return false;
                }

                lCourierAction.PActionSourceTab = lCourierPlan.LCartographerStageId;
                lCourierAction.PActionPlanApply(lCourierPlan.LCartographerNextStage);
                string[] lCourierPaths = lCourierPlan.LCartographerPaths.ToArray();
                lCourierList.PListPathsAdd(lCourierPaths, lCourierPlan.LCartographerBatch, true);
                if (lCourierPlan.LCartographerMerge)
                {
                    lCourierAction.PActionAllRun();
                }
                else
                {
                    lCourierAction.PActionItemsRun(lCourierPaths);
                }
                return true;
            }
            finally
            {
                lCourierWorkspace.PWorkspaceClose();
            }
        }

        if (System.Windows.Application.Current?.Dispatcher is { } lCourierDispatcher
            && !lCourierDispatcher.CheckAccess())
        {
            return lCourierDispatcher.Invoke(LCourierPlanRun);
        }

        return LCourierPlanRun();
    }

}
