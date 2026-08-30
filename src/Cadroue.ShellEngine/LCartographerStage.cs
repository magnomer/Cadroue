using System.IO;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

public static partial class LCartographer
{
    private static void LCartographerStageAccept(
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
                $"Relay cycle terminated at stage '{lCartographerStage.LCartographerTitle}': already visited this delivery");
            return;
        }

        lCartographerStage.LCartographerPendingInputs.Add(new LCartographerInputRecord
        {
            LCartographerPath = lCartographerPath,
            LCartographerSourceStage = lCartographerSourceStage
        });
        LCartographerPlanStore.LCartographerPlanSave(lCartographerPlan);

        if (!lCartographerStage.LCartographerLayout.LSceneAutoRelay)
        {
            lCartographerSeam.LCartographerTabHold(lCartographerStage.LCartographerOriginalTab, lCartographerPath, lCartographerBatch);
            LTraceLog.LTraceInfoRecord(
                $"Relay plan {lCartographerPlan.LCartographerPlanId:N} paused at stage '{lCartographerStage.LCartographerTitle}'");
            return;
        }

        if (string.Equals(lCartographerStage.LCartographerLayoutKey, "Funnel", StringComparison.Ordinal))
        {
            Guid lCartographerTargetId = LCartographerRouteRead(lCartographerStage, lCartographerPath);
            if (lCartographerPlan.LCartographerStages.FirstOrDefault(
                lCartographerCandidate => lCartographerCandidate.LCartographerStageId == lCartographerTargetId) is { } lCartographerTarget)
            {
                LCartographerStageAccept(
                    lCartographerPlan, lCartographerTarget, lCartographerPath,
                    lCartographerStage.LCartographerStageId, lCartographerBatch, lCartographerSeam, lCartographerVisited);
            }
            lCartographerStage.LCartographerPendingInputs.Clear();
            return;
        }

        lCartographerSeam.LCartographerTabTrack(lCartographerStage.LCartographerOriginalTab, lCartographerPath, lCartographerBatch);

        bool lCartographerMerge = string.Equals(lCartographerStage.LCartographerLayoutKey, "Merge", StringComparison.Ordinal);
        if (lCartographerMerge
            && LCartographerMergeCheck(
                lCartographerPlan, lCartographerStage, lCartographerBatch, LCartographerScheduleRead()))
        {
            return;
        }

        var lCartographerRepresented = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem lCartographerWork in LCartographerScheduleRead())
        {
            if (lCartographerWork.LWorkBatchId != lCartographerBatch
                || lCartographerWork.LWorkRelaySource != lCartographerStage.LCartographerStageId)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(lCartographerWork.LWorkSourcePath))
            {
                lCartographerRepresented.Add(lCartographerWork.LWorkSourcePath);
            }
            foreach (string lCartographerMergeSource in lCartographerWork.LWorkMergeSources)
            {
                lCartographerRepresented.Add(lCartographerMergeSource);
            }
        }

        string[] lCartographerPaths = lCartographerStage.LCartographerPendingInputs
            .Select(lCartographerInput => lCartographerInput.LCartographerPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(lCartographerPath => !lCartographerRepresented.Contains(lCartographerPath)
                && File.Exists(lCartographerPath) && LMedia.LMediaCheck(lCartographerPath))
            .ToArray();
        LCartographerStageSet(lCartographerStage.LCartographerStageId, lCartographerStage.LCartographerTitle);

        IReadOnlyList<string> lCartographerAcknowledged = lCartographerPaths.Length == 0
            ? Array.Empty<string>()
            : LCartographerStageRun(new LCartographerStagePlan(
                lCartographerStage.LCartographerLayoutKey,
                lCartographerStage.LCartographerExport,
                lCartographerStage.LCartographerLayout.LSceneTabClone(),
                lCartographerStage.LCartographerStageId,
                lCartographerStage.LCartographerNextStage,
                lCartographerBatch,
                lCartographerPaths,
                lCartographerMerge));

        var lCartographerCleared = new HashSet<string>(lCartographerAcknowledged, StringComparer.OrdinalIgnoreCase);
        lCartographerCleared.UnionWith(lCartographerRepresented);
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
                        lCartographerInput.LCartographerPath, lCartographerItem.LWorkOutputPath, StringComparison.OrdinalIgnoreCase))))
            {
                continue;
            }

            if (LCartographerReachCheck(lCartographerPlan, lCartographerItem.LWorkRelayTarget, lCartographerMerge.LCartographerStageId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LCartographerReachCheck(LCartographerPlanRecord lCartographerPlan, Guid lCartographerFrom, Guid lCartographerTarget)
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
                lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerCurrent) is not { } lCartographerStage) continue;
            if (lCartographerStage.LCartographerNextStage != Guid.Empty) lCartographerPending.Enqueue(lCartographerStage.LCartographerNextStage);
            foreach (LCartographerFunnelRule lCartographerRule in lCartographerStage.LCartographerFunnelRules)
            {
                if (lCartographerRule.LCartographerTargetStage != Guid.Empty) lCartographerPending.Enqueue(lCartographerRule.LCartographerTargetStage);
            }
        }

        return false;
    }
}
