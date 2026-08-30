using System.IO;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public sealed record LCartographerTab(
    Guid LCartographerTabId,
    string LCartographerLayoutKey,
    string LCartographerTitle,
    LPresetRecord LCartographerExport,
    LSceneTabRecord LCartographerLayout,
    bool LCartographerFunnel);

public sealed record LCartographerStagePlan(
    string LCartographerLayoutKey,
    LPresetRecord LCartographerExport,
    LSceneTabRecord LCartographerLayout,
    Guid LCartographerStageId,
    Guid LCartographerNextStage,
    Guid LCartographerBatch,
    IReadOnlyList<string> LCartographerPaths,
    bool LCartographerMerge);

public sealed record LCartographerDelivery(
    Func<Guid, string, Guid, bool> LCartographerTabIntake,
    Action<Guid, string, Guid> LCartographerTabHold,
    Action<Guid, string, Guid> LCartographerTabTrack,
    Action<LWorkItem, bool> LCartographerSourceDrop,
    Action<Guid, string, Guid> LCartographerTabArrive,
    Action<IReadOnlyList<Guid>> LCartographerBatchEvict,
    Action<IReadOnlyList<(string PListPath, Guid PListBatch)>> LCartographerSourceUnlock);

public static partial class LCartographer
{
    public static readonly Guid LCartographerFinishTarget = new("feed0000-0000-0000-0000-0000000ffff0");

    private static readonly HashSet<Guid> lCartographerDelivered = new();
    private static bool lCartographerDispatching;
    private static bool lCartographerDispatchPending;

    public static LCartographerDelivery? LCartographerDeliverySeam { get; set; }

    public static bool LCartographerOwnershipCheck(LWorkItem lCartographerItem) =>
        LCartographerPlanStore.LCartographerPlanRead(lCartographerItem.LWorkBatchId, out LCartographerPlanRecord lCartographerPlan)
        && lCartographerPlan.LCartographerStages.Any(
            lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerItem.LWorkRelayTarget);

    public static bool LCartographerDeliveredCheck(Guid lCartographerWorkId) =>
        lCartographerDelivered.Contains(lCartographerWorkId);

    public static void LCartographerDeliveredRemove(IReadOnlySet<Guid> lCartographerLiveWork) =>
        lCartographerDelivered.RemoveWhere(lCartographerWorkId => !lCartographerLiveWork.Contains(lCartographerWorkId));

    public static void LCartographerDispatch(IReadOnlyList<LWorkItem> lCartographerSchedule)
    {
        if (lCartographerDispatching)
        {
            lCartographerDispatchPending = true;
            return;
        }

        lCartographerDispatching = true;
        try
        {
            do
            {
                lCartographerDispatchPending = false;
                foreach (LWorkItem lCartographerItem in lCartographerSchedule.ToArray())
                {
                    if (!LCartographerDeliverableCheck(lCartographerItem))
                    {
                        continue;
                    }

                    lCartographerDelivered.Add(lCartographerItem.LWorkId);
                    try
                    {
                        if (!LCartographerItemDispatch(lCartographerItem, lCartographerSchedule))
                        {
                            lCartographerDelivered.Remove(lCartographerItem.LWorkId);
                        }
                    }
                    catch
                    {
                        lCartographerDelivered.Remove(lCartographerItem.LWorkId);
                        throw;
                    }
                }
            }
            while (lCartographerDispatchPending);
        }
        finally
        {
            lCartographerDispatching = false;
        }

        LSeal.LSealRun();
    }

    public static bool LCartographerDeliverableCheck(LWorkItem lCartographerItem)
    {
        if (lCartographerItem.LWorkStateCurrent != LWorkState.LWorkStateDone
            || lCartographerItem.LWorkRelayTarget == Guid.Empty
            || lCartographerDelivered.Contains(lCartographerItem.LWorkId))
        {
            return false;
        }

        bool lCartographerPlanOwned = LCartographerOwnershipCheck(lCartographerItem);
        return lCartographerItem.LWorkOwnerProcess == Environment.ProcessId
            || lCartographerPlanOwned && !LSentinel.LSentinelOwnerCheck(
                lCartographerItem.LWorkOwnerProcess, lCartographerItem.LWorkOwnerStamp);
    }

    public static bool LCartographerItemDispatch(LWorkItem lCartographerItem, IReadOnlyList<LWorkItem> lCartographerSchedule)
    {
        if (LCartographerDeliverySeam is not { } lCartographerSeam)
        {
            return false;
        }

        bool lCartographerRetain = LCartographerRetainCheck(lCartographerItem, lCartographerSchedule);

        if (lCartographerItem.LWorkRelayTarget == LCartographerFinishTarget)
        {
            LTraceLog.LTraceInfoRecord(
                $"Relay finished '{lCartographerItem.LWorkOutputName}': removed at source, delivered to no tab");
            if (!lCartographerRetain)
            {
                lCartographerSeam.LCartographerSourceDrop(lCartographerItem, true);
            }

            return true;
        }

        if (string.IsNullOrWhiteSpace(lCartographerItem.LWorkOutputPath) || !File.Exists(lCartographerItem.LWorkOutputPath))
        {
            LTraceLog.LTraceWarningRecord($"Relay skipped '{lCartographerItem.LWorkOutputName}': the output file is missing");
            return false;
        }

        if (LCartographerPlanStore.LCartographerPlanRead(lCartographerItem.LWorkBatchId, out LCartographerPlanRecord lCartographerPlan)
            && lCartographerPlan.LCartographerStages.FirstOrDefault(
                lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerItem.LWorkRelayTarget) is { } lCartographerStage)
        {
            if (!LCartographerStageDispatch(lCartographerItem, lCartographerPlan, lCartographerStage, lCartographerSeam))
            {
                return false;
            }

            if (!lCartographerRetain)
            {
                lCartographerSeam.LCartographerSourceDrop(lCartographerItem, false);
            }

            return true;
        }

        if (!lCartographerSeam.LCartographerTabIntake(
            lCartographerItem.LWorkRelayTarget, lCartographerItem.LWorkOutputPath, lCartographerItem.LWorkBatchId))
        {
            return false;
        }

        if (!lCartographerRetain)
        {
            lCartographerSeam.LCartographerSourceDrop(lCartographerItem, false);
        }

        lCartographerSeam.LCartographerTabArrive(
            lCartographerItem.LWorkRelayTarget, lCartographerItem.LWorkOutputPath, lCartographerItem.LWorkBatchId);
        return true;
    }

    private static bool LCartographerRetainCheck(
        LWorkItem lCartographerItem, IReadOnlyList<LWorkItem> lCartographerSchedule)
    {
        HashSet<string> lCartographerSources = LCartographerSourceRead(lCartographerItem);
        foreach (LWorkItem lCartographerOther in lCartographerSchedule)
        {
            if (lCartographerOther.LWorkId == lCartographerItem.LWorkId
                || lCartographerOther.LWorkBatchId != lCartographerItem.LWorkBatchId
                || lCartographerDelivered.Contains(lCartographerOther.LWorkId))
            {
                continue;
            }

            if (LCartographerSourceRead(lCartographerOther).Any(lCartographerSources.Contains))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> LCartographerSourceRead(LWorkItem lCartographerItem)
    {
        var lCartographerPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            lCartographerItem.LWorkSourcePath
        };
        foreach (string lCartographerMerge in lCartographerItem.LWorkMergeSources)
        {
            lCartographerPaths.Add(lCartographerMerge);
        }

        return lCartographerPaths;
    }

    private static bool LCartographerStageDispatch(
        LWorkItem lCartographerItem,
        LCartographerPlanRecord lCartographerPlan,
        LCartographerStageRecord lCartographerStage,
        LCartographerDelivery lCartographerSeam)
    {
        if (lCartographerPlan.LCartographerDeliveredWork.Contains(lCartographerItem.LWorkId))
        {
            return true;
        }

        LCartographerStageAccept(
            lCartographerPlan, lCartographerStage, lCartographerItem.LWorkOutputPath,
            lCartographerItem.LWorkRelaySource, lCartographerItem.LWorkBatchId, lCartographerSeam);
        lCartographerPlan.LCartographerDeliveredWork.Add(lCartographerItem.LWorkId);
        return LCartographerPlanStore.LCartographerPlanSave(lCartographerPlan);
    }
}
