using System.IO;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed record LCourierOption(Guid LCourierTabId, string LCourierTabTitle, ImageSource? LCourierTabIcon);

public static class LCourier
{
    public static readonly Guid LCourierFinishTarget = new("feed0000-0000-0000-0000-0000000ffff0");

    private const int LCourierFinishSlot = -2;

    private static readonly Dictionary<Guid, Guid> lCourierTargets = new();
    private static readonly Dictionary<Guid, string> lCourierStageTitles = new();
    private static readonly HashSet<Guid> lCourierDelivered = new();
    private static bool lCourierWatching;
    private static bool lCourierDispatching;
    private static bool lCourierDispatchPending;

    public static void LCourierStart()
    {
        if (lCourierWatching)
        {
            return;
        }

        lCourierWatching = true;
        PProgram.LScheduleCurrent.LScheduleChange += LCourierScheduleHandle;
        LCourierDispatch(PProgram.LScheduleCurrent);
    }

    internal static int LCourierScheduleAdd(
        IReadOnlyList<LWorkItem> lCourierItems,
        Guid lCourierRelayTarget = default,
        Guid lCourierRelaySource = default,
        LRelayPlanRecord? lCourierPreparedPlan = null)
    {
        if (lCourierItems.Count == 0)
        {
            return 0;
        }

        foreach (IGrouping<Guid, LWorkItem> lCourierBatch in lCourierItems.GroupBy(lCourierItem => lCourierItem.LWorkBatchId))
        {
            if (LRelayPlanStore.LRelayPlanRead(lCourierBatch.Key, out LRelayPlanRecord lCourierExisting))
            {
                LRelayStageRecord? lCourierSourceStage = lCourierExisting.LRelayStages.FirstOrDefault(
                    lCourierStage => lCourierStage.LRelayStageId == lCourierRelaySource
                        || lCourierStage.LRelayOriginalTab == lCourierRelaySource);
                LRelayStageRecord? lCourierTargetStage = lCourierExisting.LRelayStages.FirstOrDefault(
                    lCourierStage => lCourierStage.LRelayStageId == lCourierRelayTarget
                        || lCourierStage.LRelayOriginalTab == lCourierRelayTarget);
                Guid lCourierStableSource = lCourierSourceStage?.LRelayStageId ?? lCourierRelaySource;
                Guid lCourierStableTarget = lCourierSourceStage?.LRelayNextStage
                    ?? lCourierTargetStage?.LRelayStageId
                    ?? lCourierRelayTarget;
                foreach (LWorkItem lCourierItem in lCourierBatch)
                {
                    lCourierItem.LWorkRelayTarget = lCourierStableTarget;
                    lCourierItem.LWorkRelaySource = lCourierStableSource;
                }
                continue;
            }

            if (lCourierRelayTarget == Guid.Empty || lCourierRelayTarget == LCourierFinishTarget)
            {
                foreach (LWorkItem lCourierItem in lCourierBatch)
                {
                    lCourierItem.LWorkRelayTarget = lCourierRelayTarget;
                    lCourierItem.LWorkRelaySource = lCourierRelaySource;
                }
                continue;
            }

            LRelayPlanRecord? lCourierPlan = lCourierPreparedPlan is null
                ? LRelayPlan.LRelayPlanCreate(lCourierBatch.Key, lCourierRelayTarget)
                : LRelayPlan.LRelayPlanCopy(lCourierPreparedPlan, lCourierBatch.Key);
            if (lCourierPlan is null || !LRelayPlanStore.LRelayPlanSave(lCourierPlan))
            {
                foreach (LWorkItem lCourierItem in lCourierBatch)
                {
                    lCourierItem.LWorkRelayTarget = lCourierRelayTarget;
                    lCourierItem.LWorkRelaySource = lCourierRelaySource;
                }
                continue;
            }

            foreach (LWorkItem lCourierItem in lCourierBatch)
            {
                lCourierItem.LWorkRelayTarget = lCourierPlan.LRelayEntryStage;
                lCourierItem.LWorkRelaySource = lCourierRelaySource;
            }
            LTraceLog.LTraceInfoRecord(
                $"Relay plan {lCourierPlan.LRelayPlanId:N} captured {lCourierPlan.LRelayStages.Count} stable stage(s)");
        }

        return PProgram.LScheduleCurrent.LScheduleAdd(lCourierItems);
    }

    internal static LRelayPlanRecord? LCourierPlanPrepare(Guid lCourierRelayTarget) =>
        lCourierRelayTarget == Guid.Empty || lCourierRelayTarget == LCourierFinishTarget
            ? null
            : LRelayPlan.LRelayPlanCreate(Guid.Empty, lCourierRelayTarget);

    public static void LCourierAttach(Guid lCourierSourceTab, PAction pCourierAction)
    {
        LCourierStart();
        pCourierAction.PActionSourceTab = lCourierSourceTab;
        pCourierAction.PActionRelaySource = () => LCourierOptionsRead(lCourierSourceTab);
        pCourierAction.PActionRelayChange += lCourierTarget =>
        {
            LCourierTargetSet(lCourierSourceTab, lCourierTarget);
            pCourierAction.PActionRelayApply(LCourierTargetRead(lCourierSourceTab));
        };
        pCourierAction.PActionRelayApply(LCourierTargetRead(lCourierSourceTab));
    }

    public static void LCourierFaceUpdate()
    {
        if (LTabset.LTabsetCurrent is not { } lCourierTabset)
        {
            return;
        }

        foreach (PTabRecord pTabRecord in lCourierTabset.PTabsetRecords)
        {
            pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(
                LCourierTargetRead(pTabRecord.PTabId));
        }
    }

    public static Guid LCourierTargetRead(Guid lCourierSourceTab) =>
        lCourierTargets.TryGetValue(lCourierSourceTab, out Guid lCourierTarget) ? lCourierTarget : Guid.Empty;

    public static string LCourierStageTitleRead(Guid lCourierStageId) =>
        lCourierStageTitles.TryGetValue(lCourierStageId, out string? lCourierTitle)
            ? lCourierTitle
            : string.Empty;

    public static string LCourierWorkTitleRead(LWorkItem lCourierItem)
    {
        string lCourierTitle = LTabset.LTabsetTitleRead(lCourierItem.LWorkRelaySource);
        if (!string.IsNullOrWhiteSpace(lCourierTitle))
        {
            return lCourierTitle;
        }

        if (LRelayPlanStore.LRelayPlanRead(lCourierItem.LWorkBatchId, out LRelayPlanRecord lCourierPlan)
            && lCourierPlan.LRelayStages.FirstOrDefault(
                lCourierStage => lCourierStage.LRelayStageId == lCourierItem.LWorkRelaySource)
                is { } lCourierSourceStage)
        {
            lCourierStageTitles[lCourierSourceStage.LRelayStageId] = lCourierSourceStage.LRelayTitle;
            return lCourierSourceStage.LRelayTitle;
        }

        return lCourierItem.LWorkTab;
    }

    public static void LCourierTargetSet(Guid lCourierSourceTab, Guid lCourierTarget)
    {
        if (lCourierTarget == Guid.Empty)
        {
            lCourierTargets.Remove(lCourierSourceTab);
            return;
        }

        if (lCourierTarget == lCourierSourceTab)
        {
            LTraceLog.LTraceWarningRecord("Relay target refused: a tab cannot relay into itself");
            return;
        }

        lCourierTargets[lCourierSourceTab] = lCourierTarget;
    }

    public static void LCourierTabRemove(Guid lCourierTabId)
    {
        lCourierTargets.Remove(lCourierTabId);
        foreach (Guid lCourierSourceTab in lCourierTargets
            .Where(lCourierEntry => lCourierEntry.Value == lCourierTabId)
            .Select(lCourierEntry => lCourierEntry.Key)
            .ToArray())
        {
            lCourierTargets.Remove(lCourierSourceTab);
        }
    }

    public static IReadOnlyList<LCourierOption> LCourierOptionsRead(Guid lCourierSourceTab)
    {
        if (LTabset.LTabsetCurrent is not { } lCourierTabset)
        {
            return Array.Empty<LCourierOption>();
        }

        var lCourierOptions = new List<LCourierOption>();
        foreach (PTabRecord pTabRecord in lCourierTabset.PTabsetRecords)
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
            Guid lCourierTarget = LCourierTargetRead(pTabRecord.PTabId);
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
                LCourierTargetSet(pCourierFinishSource.PTabId, LCourierFinishTarget);
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

            LCourierTargetSet(pCourierSource.PTabId, pCourierTarget.PTabId);
            pCourierSource.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(pCourierTarget.PTabId);
        }
    }

    public static bool LCourierDeliveredCheck(Guid lCourierWorkId) => lCourierDelivered.Contains(lCourierWorkId);

    private static void LCourierScheduleHandle(LScheduleContract lCourierSchedule)
    {
        if (System.Windows.Application.Current?.Dispatcher is { } lCourierDispatcher
            && !lCourierDispatcher.CheckAccess())
        {
            lCourierDispatcher.BeginInvoke(new Action(() => LCourierDispatch(lCourierSchedule)));
            return;
        }

        LCourierDispatch(lCourierSchedule);
    }

    private static void LCourierDispatch(LScheduleContract lCourierSchedule)
    {
        if (lCourierDispatching)
        {
            lCourierDispatchPending = true;
            return;
        }

        lCourierDispatching = true;
        try
        {
            do
            {
                lCourierDispatchPending = false;
                foreach (LWorkItem lWorkItem in lCourierSchedule.LScheduleRecords.ToArray())
                {
                    bool lCourierPlanOwned = LCourierPlanOwnedCheck(lWorkItem);
                    bool lCourierOwnerEligible = lWorkItem.LWorkOwnerProcess == Environment.ProcessId
                        || lCourierPlanOwned && !LSentinel.LSentinelOwnerAliveCheck(
                            lWorkItem.LWorkOwnerProcess, lWorkItem.LWorkOwnerStamp);
                    if (lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateDone
                        || lWorkItem.LWorkRelayTarget == Guid.Empty
                        || !lCourierOwnerEligible
                        || lCourierDelivered.Contains(lWorkItem.LWorkId))
                    {
                        continue;
                    }

                    lCourierDelivered.Add(lWorkItem.LWorkId);
                    try
                    {
                        if (!LCourierOutputAdd(lWorkItem))
                        {
                            lCourierDelivered.Remove(lWorkItem.LWorkId);
                        }
                    }
                    catch
                    {
                        lCourierDelivered.Remove(lWorkItem.LWorkId);
                        throw;
                    }
                }
            }
            while (lCourierDispatchPending);
        }
        finally
        {
            lCourierDispatching = false;
        }

        LSeal.LSealSweep();
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

    private static bool LCourierOutputAdd(LWorkItem lWorkItem)
    {
        if (lWorkItem.LWorkRelayTarget == LCourierFinishTarget)
        {
            LTraceLog.LTraceInfoRecord($"Relay finished '{lWorkItem.LWorkOutputName}': removed at source, delivered to no tab");
            LCourierSourceRemove(lWorkItem, true);
            return true;
        }

        if (string.IsNullOrWhiteSpace(lWorkItem.LWorkOutputPath) || !File.Exists(lWorkItem.LWorkOutputPath))
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{lWorkItem.LWorkOutputName}': the output file is missing");
            return false;
        }

        if (LRelayPlanStore.LRelayPlanRead(lWorkItem.LWorkBatchId, out LRelayPlanRecord lCourierPlan)
            && lCourierPlan.LRelayStages.FirstOrDefault(
                lCourierStage => lCourierStage.LRelayStageId == lWorkItem.LWorkRelayTarget) is { } lCourierStage)
        {
            if (!LCourierPlanOutputAdd(lWorkItem, lCourierPlan, lCourierStage))
            {
                return false;
            }

            LCourierSourceRemove(lWorkItem, false);
            return true;
        }

        if (LCourierTabFind(lWorkItem.LWorkRelayTarget) is not { } pCourierTarget
            || pCourierTarget.PTabWorkspace.PWorkspaceSurface.PTabList is not { } pCourierList)
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{lWorkItem.LWorkOutputName}': the destination tab is gone");
            return false;
        }

        int lCourierAdded = pCourierList.PListPathsAdd(
            new[] { lWorkItem.LWorkOutputPath }, lWorkItem.LWorkBatchId, true);
        bool lCourierAccepted = lCourierAdded > 0 || pCourierList.PListPathsRead().Any(
            lCourierPath => string.Equals(
                lCourierPath, lWorkItem.LWorkOutputPath, StringComparison.OrdinalIgnoreCase));
        if (!lCourierAccepted)
        {
            LTraceLog.LTraceWarningRecord(
                $"Relay skipped '{lWorkItem.LWorkOutputName}': the destination tab rejected the output");
            return false;
        }

        LTraceLog.LTraceInfoRecord(
            lCourierAdded > 0
                ? $"Relay added '{lWorkItem.LWorkOutputName}' to tab '{pCourierTarget.PTabTitle}'"
                : $"Relay left '{lWorkItem.LWorkOutputName}' out of tab '{pCourierTarget.PTabTitle}': already listed");

        LCourierSourceRemove(lWorkItem, false);
        LCourierArrive(lWorkItem.LWorkRelayTarget, lWorkItem.LWorkOutputPath, lWorkItem.LWorkBatchId);
        return true;
    }

    private static void LCourierSourceRemove(LWorkItem lWorkItem, bool lCourierForce)
    {
        if ((!lCourierForce && !LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty)
            || lWorkItem.LWorkRelaySource == Guid.Empty
            || LCourierTabFind(lWorkItem.LWorkRelaySource) is not { } pCourierSource)
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

    private static PTabRecord? LCourierTabFind(Guid lCourierTabId) =>
        LTabset.LTabsetCurrent?.PTabsetRecords.FirstOrDefault(pTabRecord => pTabRecord.PTabId == lCourierTabId);

    private static bool LCourierPlanOwnedCheck(LWorkItem lCourierItem) =>
        LRelayPlanStore.LRelayPlanRead(lCourierItem.LWorkBatchId, out LRelayPlanRecord lCourierPlan)
        && lCourierPlan.LRelayStages.Any(lCourierStage => lCourierStage.LRelayStageId == lCourierItem.LWorkRelayTarget);

    private static bool LCourierPlanOutputAdd(
        LWorkItem lCourierItem,
        LRelayPlanRecord lCourierPlan,
        LRelayStageRecord lCourierStage)
    {
        if (lCourierPlan.LRelayDeliveredWork.Contains(lCourierItem.LWorkId))
        {
            return true;
        }

        LCourierPlanStageArrive(
            lCourierPlan, lCourierStage, lCourierItem.LWorkOutputPath,
            lCourierItem.LWorkRelaySource, lCourierItem.LWorkBatchId);
        lCourierPlan.LRelayDeliveredWork.Add(lCourierItem.LWorkId);
        return LRelayPlanStore.LRelayPlanSave(lCourierPlan);
    }

    private static void LCourierPlanStageArrive(
        LRelayPlanRecord lCourierPlan,
        LRelayStageRecord lCourierStage,
        string lCourierPath,
        Guid lCourierSourceStage,
        Guid lCourierBatch)
    {
        lCourierStage.LRelayPendingInputs.Add(new LRelayInputRecord
        {
            LRelayPath = lCourierPath,
            LRelaySourceStage = lCourierSourceStage
        });
        LRelayPlanStore.LRelayPlanSave(lCourierPlan);

        if (!lCourierStage.LRelayLayout.LSceneAutoRelay)
        {
            if (LCourierTabFind(lCourierStage.LRelayOriginalTab)?.PTabWorkspace.PWorkspaceSurface.PTabList is { } lCourierList)
            {
                lCourierList.PListPathsAdd(new[] { lCourierPath }, lCourierBatch, true);
            }
            LTraceLog.LTraceInfoRecord(
                $"Relay plan {lCourierPlan.LRelayPlanId:N} paused at stage '{lCourierStage.LRelayTitle}'");
            return;
        }

        if (string.Equals(lCourierStage.LRelayLayoutKey, "Funnel", StringComparison.Ordinal))
        {
            Guid lCourierTargetId = LRelayPlan.LRelayFunnelTargetRead(lCourierStage, lCourierPath);
            if (lCourierPlan.LRelayStages.FirstOrDefault(
                lCourierCandidate => lCourierCandidate.LRelayStageId == lCourierTargetId) is { } lCourierTarget)
            {
                LCourierPlanStageArrive(
                    lCourierPlan, lCourierTarget, lCourierPath,
                    lCourierStage.LRelayStageId, lCourierBatch);
            }
            lCourierStage.LRelayPendingInputs.Clear();
            return;
        }

        if (string.Equals(lCourierStage.LRelayLayoutKey, "Merge", StringComparison.Ordinal)
            && LCourierPlanMergeBlocked(lCourierPlan, lCourierStage, lCourierBatch))
        {
            return;
        }

        void LCourierPlanRun()
        {
            LPreset lCourierPreset = LPreset.LPresetStateCreate(lCourierStage.LRelayExport);
            var lCourierWorkspace = new PWorkspace(
                lCourierStage.LRelayLayoutKey, lCourierPreset, lCourierStage.LRelayLayout.LSceneTabClone());
            try
            {
                PTabSurface lCourierSurface = lCourierWorkspace.PWorkspaceSurface;
                if (lCourierSurface.PTabAction is not { } lCourierAction || lCourierSurface.PTabList is not { } lCourierList)
                {
                    return;
                }

                lCourierAction.PActionSourceTab = lCourierStage.LRelayStageId;
                lCourierAction.PActionRelayPlanApply(lCourierStage.LRelayNextStage);
                lCourierStageTitles[lCourierStage.LRelayStageId] = lCourierStage.LRelayTitle;
                string[] lCourierPaths = lCourierStage.LRelayPendingInputs
                    .Select(lCourierInput => lCourierInput.LRelayPath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                lCourierList.PListPathsAdd(lCourierPaths, lCourierBatch, true);
                if (string.Equals(lCourierStage.LRelayLayoutKey, "Merge", StringComparison.Ordinal))
                {
                    lCourierAction.PActionAllRun();
                }
                else
                {
                    lCourierAction.PActionItemsRun(lCourierPaths);
                }
                lCourierStage.LRelayPendingInputs.Clear();
                LRelayPlanStore.LRelayPlanSave(lCourierPlan);
            }
            finally
            {
                lCourierWorkspace.PWorkspaceClose();
            }
        }

        if (System.Windows.Application.Current?.Dispatcher is { } lCourierDispatcher
            && !lCourierDispatcher.CheckAccess())
        {
            lCourierDispatcher.Invoke(LCourierPlanRun);
        }
        else
        {
            LCourierPlanRun();
        }
    }

    private static bool LCourierPlanMergeBlocked(
        LRelayPlanRecord lCourierPlan,
        LRelayStageRecord lCourierMerge,
        Guid lCourierBatch)
    {
        foreach (LWorkItem lCourierItem in PProgram.LScheduleCurrent.LScheduleRecords)
        {
            if (lCourierItem.LWorkBatchId != lCourierBatch
                || lCourierItem.LWorkStateCurrent is LWorkState.LWorkStateFailed or LWorkState.LWorkStateCancelled)
            {
                continue;
            }

            if (lCourierItem.LWorkStateCurrent == LWorkState.LWorkStateDone
                && (lCourierPlan.LRelayDeliveredWork.Contains(lCourierItem.LWorkId)
                    || lCourierMerge.LRelayPendingInputs.Any(lCourierInput => string.Equals(
                        lCourierInput.LRelayPath, lCourierItem.LWorkOutputPath, StringComparison.OrdinalIgnoreCase))))
            {
                continue;
            }

            if (LCourierPlanReach(lCourierPlan, lCourierItem.LWorkRelayTarget, lCourierMerge.LRelayStageId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LCourierPlanReach(LRelayPlanRecord lCourierPlan, Guid lCourierFrom, Guid lCourierTarget)
    {
        var lCourierSeen = new HashSet<Guid>();
        var lCourierPending = new Queue<Guid>();
        lCourierPending.Enqueue(lCourierFrom);
        while (lCourierPending.Count > 0)
        {
            Guid lCourierCurrent = lCourierPending.Dequeue();
            if (lCourierCurrent == lCourierTarget) return true;
            if (!lCourierSeen.Add(lCourierCurrent)) continue;
            if (lCourierPlan.LRelayStages.FirstOrDefault(
                lCourierStage => lCourierStage.LRelayStageId == lCourierCurrent) is not { } lCourierStage) continue;
            if (lCourierStage.LRelayNextStage != Guid.Empty) lCourierPending.Enqueue(lCourierStage.LRelayNextStage);
            foreach (LRelayFunnelRuleRecord lCourierRule in lCourierStage.LRelayFunnelRules)
            {
                if (lCourierRule.LRelayTargetStage != Guid.Empty) lCourierPending.Enqueue(lCourierRule.LRelayTargetStage);
            }
        }

        return false;
    }
}
