using System.IO;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

public static partial class LCartographer
{
    private static bool LCartographerStageAccept(
        LCartographerPlanRecord lCartographerPlan,
        LCartographerStageRecord lCartographerStage,
        string lCartographerPath,
        Guid lCartographerSourceStage,
        Guid lCartographerBatch,
        LCartographerDelivery lCartographerSeam,
        HashSet<Guid>? lCartographerVisited = null)
    {
        lCartographerVisited ??= new HashSet<Guid>();
        if (!lCartographerVisited.Add(lCartographerStage.LCartographerStageId))
        {
            LTraceLog.LTraceWarningRecord(
                $"Relay cycle terminated at stage '{lCartographerStage.LCartographerTitle}': " +
                $"already visited this delivery");
            return true;
        }

        if (!lCartographerStage.LCartographerPendingInputs.Any(lCartographerInput => string.Equals(
                lCartographerInput.LCartographerPath, lCartographerPath, StringComparison.OrdinalIgnoreCase)))
        {
            lCartographerStage.LCartographerPendingInputs.Add(new LCartographerInputRecord
            {
                LCartographerPath = lCartographerPath,
                LCartographerSourceStage = lCartographerSourceStage
            });
            LCartographerPlanStore.LCartographerPlanSave(lCartographerPlan);
        }

        if (!lCartographerStage.LCartographerLayout.LSceneAutoRelay)
        {
            if (!lCartographerSeam.LCartographerTabIntake(
                lCartographerStage.LCartographerOriginalTab, lCartographerPath, lCartographerBatch))
            {
                return false;
            }

            LTraceLog.LTraceInfoRecord(
                $"Relay plan {lCartographerPlan.LCartographerPlanId:N} paused at stage " +
                $"'{lCartographerStage.LCartographerTitle}'");
            return true;
        }

        if (string.Equals(lCartographerStage.LCartographerLayoutKey, "Funnel", StringComparison.Ordinal))
        {
            return LCartographerFunnelAccept(
                lCartographerPlan, lCartographerStage, lCartographerPath, lCartographerBatch,
                lCartographerSeam, lCartographerVisited);
        }

        if (!lCartographerSeam.LCartographerTabTrack(
            lCartographerStage.LCartographerOriginalTab, lCartographerPath, lCartographerBatch))
        {
            return false;
        }

        bool lCartographerMerge = string.Equals(
            lCartographerStage.LCartographerLayoutKey, "Merge", StringComparison.Ordinal);
        if (lCartographerMerge
            && LCartographerMergeCheck(
                lCartographerPlan, lCartographerStage, lCartographerBatch, LCartographerScheduleRead()))
        {
            return true;
        }

        HashSet<string> lCartographerCleared = LCartographerRepresentedRead(
            lCartographerBatch, lCartographerStage.LCartographerStageId);
        var lCartographerPaths = new List<string>();
        foreach (string lCartographerPending in lCartographerStage.LCartographerPendingInputs
            .Select(lCartographerInput => lCartographerInput.LCartographerPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(lCartographerPending => !lCartographerCleared.Contains(lCartographerPending)))
        {
            if (File.Exists(lCartographerPending) && LMedia.LMediaCheck(lCartographerPending))
            {
                lCartographerPaths.Add(lCartographerPending);
                continue;
            }

            LTraceLog.LTraceWarningRecord(
                $"Relay dropped '{Path.GetFileName(lCartographerPending)}' at stage " +
                $"'{lCartographerStage.LCartographerTitle}': the file is missing or unreadable");
            lCartographerCleared.Add(lCartographerPending);
        }

        LCartographerStageSet(lCartographerStage.LCartographerStageId, lCartographerStage.LCartographerTitle);
        if (lCartographerPaths.Count > 0)
        {
            LCartographerStageCommit(
                LCartographerStageRun(new LCartographerStagePlan(
                    lCartographerStage.LCartographerLayoutKey,
                    lCartographerStage.LCartographerExport,
                    lCartographerStage.LCartographerLayout.LSceneTabClone(),
                    lCartographerStage.LCartographerStageId,
                    lCartographerStage.LCartographerNextStage,
                    lCartographerBatch,
                    lCartographerPaths,
                    lCartographerMerge)),
                lCartographerPlan.LCartographerPlanId,
                lCartographerStage.LCartographerStageId);
        }

        LCartographerPendingRemove(lCartographerPlan, lCartographerStage, lCartographerCleared);
        return true;
    }

    private static bool LCartographerFunnelAccept(
        LCartographerPlanRecord lCartographerPlan,
        LCartographerStageRecord lCartographerStage,
        string lCartographerPath,
        Guid lCartographerBatch,
        LCartographerDelivery lCartographerSeam,
        HashSet<Guid> lCartographerVisited)
    {
        Guid lCartographerTargetId = LCartographerRouteRead(lCartographerStage, lCartographerPath);
        if (lCartographerPlan.LCartographerStages.FirstOrDefault(
                lCartographerCandidate => lCartographerCandidate.LCartographerStageId == lCartographerTargetId)
            is not { } lCartographerTarget)
        {
            LTraceLog.LTraceWarningRecord(
                $"Relay held '{Path.GetFileName(lCartographerPath)}' at funnel stage " +
                $"'{lCartographerStage.LCartographerTitle}': no rule routes it onward");
            return lCartographerSeam.LCartographerTabIntake(
                lCartographerStage.LCartographerOriginalTab, lCartographerPath, lCartographerBatch);
        }

        if (!LCartographerStageAccept(
            lCartographerPlan,
            lCartographerTarget,
            lCartographerPath,
            lCartographerStage.LCartographerStageId,
            lCartographerBatch,
            lCartographerSeam,
            lCartographerVisited))
        {
            return false;
        }

        LCartographerPendingRemove(
            lCartographerPlan, lCartographerStage,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { lCartographerPath });
        return true;
    }

    private static async void LCartographerStageCommit(
        Task<IReadOnlyList<string>> lCartographerRun,
        Guid lCartographerPlanId,
        Guid lCartographerStageId)
    {
        IReadOnlyList<string> lCartographerAcknowledged;
        try
        {
            lCartographerAcknowledged = await lCartographerRun.ConfigureAwait(true);
        }
        catch (Exception lCartographerError)
        {
            LTraceLog.LTraceErrorRecord("Relay stage execution failed", lCartographerError);
            return;
        }

        if (lCartographerAcknowledged.Count == 0
            || !LCartographerPlanStore.LCartographerPlanRead(
                lCartographerPlanId, out LCartographerPlanRecord lCartographerPlan)
            || lCartographerPlan.LCartographerStages.FirstOrDefault(
                    lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerStageId)
                is not { } lCartographerStage)
        {
            return;
        }

        LCartographerPendingRemove(
            lCartographerPlan, lCartographerStage,
            new HashSet<string>(lCartographerAcknowledged, StringComparer.OrdinalIgnoreCase));
    }

    private static void LCartographerPendingRemove(
        LCartographerPlanRecord lCartographerPlan,
        LCartographerStageRecord lCartographerStage,
        HashSet<string> lCartographerCleared)
    {
        int lCartographerRemoved = lCartographerStage.LCartographerPendingInputs.RemoveAll(
            lCartographerInput => lCartographerCleared.Contains(lCartographerInput.LCartographerPath));
        if (lCartographerRemoved > 0)
        {
            LCartographerPlanStore.LCartographerPlanSave(lCartographerPlan);
        }
    }

    public static bool LCartographerMergeCheck(
        LCartographerPlanRecord lCartographerPlan,
        LCartographerStageRecord lCartographerMerge,
        Guid lCartographerBatch,
        IReadOnlyList<LWorkItem> lCartographerSchedule)
    {
        foreach (LWorkItem lCartographerItem in lCartographerSchedule)
        {
            if (lCartographerItem.LWorkBatchId != lCartographerBatch
                || lCartographerItem.LWorkStateCurrent is LWorkState.LWorkStateFailed or LWorkState.LWorkStateCancelled)
            {
                continue;
            }

            if (lCartographerItem.LWorkStateCurrent == LWorkState.LWorkStateDone
                && (lCartographerPlan.LCartographerDeliveredWork.Contains(lCartographerItem.LWorkId)
                    || lCartographerMerge.LCartographerPendingInputs.Any(lCartographerInput => string.Equals(
                        lCartographerInput.LCartographerPath,
                        lCartographerItem.LWorkOutputPath,
                        StringComparison.OrdinalIgnoreCase))))
            {
                continue;
            }

            if (LCartographerReachCheck(
                lCartographerPlan, lCartographerItem.LWorkRelayTarget, lCartographerMerge.LCartographerStageId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LCartographerReachCheck(
        LCartographerPlanRecord lCartographerPlan,
        Guid lCartographerFrom,
        Guid lCartographerTarget)
    {
        var lCartographerSeen = new HashSet<Guid>();
        var lCartographerPending = new Queue<Guid>();
        lCartographerPending.Enqueue(lCartographerFrom);
        while (lCartographerPending.Count > 0)
        {
            Guid lCartographerCurrent = lCartographerPending.Dequeue();
            if (lCartographerCurrent == lCartographerTarget) return true;
            if (!lCartographerSeen.Add(lCartographerCurrent)) continue;
            if (lCartographerPlan.LCartographerStages.FirstOrDefault(
                    lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerCurrent)
                is not { } lCartographerStage) continue;
            if (lCartographerStage.LCartographerNextStage != Guid.Empty)
                lCartographerPending.Enqueue(lCartographerStage.LCartographerNextStage);
            foreach (LCartographerFunnelRule lCartographerRule in lCartographerStage.LCartographerFunnelRules)
            {
                if (lCartographerRule.LCartographerTargetStage != Guid.Empty)
                    lCartographerPending.Enqueue(lCartographerRule.LCartographerTargetStage);
            }
        }

        return false;
    }
}
