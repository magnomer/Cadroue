using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LCartographerAdmission
{
    public static LScheduleIntakeContract? LCartographerScheduleIntake { get; set; }

    public static LScheduleRemoverContract? LCartographerScheduleRemover { get; set; }

    public static int LCartographerAccept(
        IReadOnlyList<LWorkItem> lCartographerItems,
        Guid lCartographerTarget = default,
        Guid lCartographerSource = default,
        LCartographerPlanDocument? lCartographerPreparedPlan = null)
    {
        if (lCartographerItems.Count == 0
            || LCartographerScheduleIntake is not { } lCartographerIntake
            || LCartographerScheduleRemover is not { } lCartographerRemover)
        {
            return 0;
        }

        IReadOnlyList<Guid> lCartographerCreatedPlans = LCartographerRelaySet(
            lCartographerItems, lCartographerTarget, lCartographerSource, lCartographerPreparedPlan);
        IReadOnlyList<LWorkItem> lCartographerAccepted = lCartographerIntake.LScheduleAcceptedAdd(lCartographerItems);
        if (lCartographerAccepted.Count > 0
            && LCartographerSchedule.LCartographerLockSeam?.Invoke(lCartographerAccepted) == false)
        {
            lCartographerRemover.LScheduleBatchRemove(
                lCartographerAccepted.Select(lCartographerItem => lCartographerItem.LWorkId).ToArray());
            LTraceEngine.LTraceWarningSeam(
                $"Work admission withdrew {lCartographerAccepted.Count} item(s): " +
                "a source file is already held by other work");
            lCartographerAccepted = Array.Empty<LWorkItem>();
        }

        if (lCartographerAccepted.Count == 0)
        {
            foreach (Guid lCartographerPlanId in lCartographerCreatedPlans)
            {
                LCartographerPlanStore.LCartographerPlanDelete(lCartographerPlanId);
            }

            return 0;
        }

        LCartographerPendingCommit(lCartographerAccepted);
        return lCartographerAccepted.Count;
    }

    private static void LCartographerPendingCommit(IReadOnlyList<LWorkItem> lCartographerAccepted)
    {
        foreach (IGrouping<Guid, LWorkItem> lCartographerBatch in lCartographerAccepted.GroupBy(
            lCartographerItem => lCartographerItem.LWorkBatchId))
        {
            if (!LCartographerPlanStore.LCartographerPlanRead(
                lCartographerBatch.Key, out LCartographerPlanDocument lCartographerPlan))
            {
                continue;
            }

            foreach (IGrouping<Guid, LWorkItem> lCartographerStageItems in lCartographerBatch.GroupBy(
                lCartographerItem => lCartographerItem.LWorkRelaySource))
            {
                if (lCartographerPlan.LCartographerStages.FirstOrDefault(
                        lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerStageItems.Key)
                    is { } lCartographerSourceStage)
                {
                    LCartographerStage.LCartographerPendingRemove(
                        lCartographerPlan, lCartographerSourceStage,
                        lCartographerStageItems.SelectMany(LCartographerSchedule.LCartographerSourcesRead)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase));
                }
            }
        }
    }

    public static IReadOnlyList<Guid> LCartographerRelaySet(
        IReadOnlyList<LWorkItem> lCartographerItems,
        Guid lCartographerTarget,
        Guid lCartographerSource,
        LCartographerPlanDocument? lCartographerPreparedPlan)
    {
        var lCartographerCreated = new List<Guid>();
        foreach (IGrouping<Guid, LWorkItem> lCartographerBatch in lCartographerItems.GroupBy(
            lCartographerItem => lCartographerItem.LWorkBatchId))
        {
            if (LCartographerPlanStore.LCartographerPlanRead(
                lCartographerBatch.Key, out LCartographerPlanDocument lCartographerExisting))
            {
                LCartographerStageEntry? lCartographerSourceStage =
                    lCartographerExisting.LCartographerStages.FirstOrDefault(
                        lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerSource
                            || lCartographerStage.LCartographerOriginalTab == lCartographerSource);
                LCartographerStageEntry? lCartographerTargetStage =
                    lCartographerExisting.LCartographerStages.FirstOrDefault(
                        lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerTarget
                            || lCartographerStage.LCartographerOriginalTab == lCartographerTarget);
                Guid lCartographerStableSource = lCartographerSourceStage?.LCartographerStageId ?? lCartographerSource;
                Guid lCartographerStableTarget = lCartographerSourceStage?.LCartographerNextStage
                    ?? lCartographerTargetStage?.LCartographerStageId
                    ?? lCartographerTarget;
                foreach (LWorkItem lCartographerItem in lCartographerBatch)
                {
                    lCartographerItem.LWorkRelayTarget = lCartographerStableTarget;
                    lCartographerItem.LWorkRelaySource = lCartographerStableSource;
                }

                continue;
            }

            if (lCartographerTarget == Guid.Empty || lCartographerTarget == LCartographer.LCartographerFinishTarget)
            {
                foreach (LWorkItem lCartographerItem in lCartographerBatch)
                {
                    lCartographerItem.LWorkRelayTarget = lCartographerTarget;
                    lCartographerItem.LWorkRelaySource = lCartographerSource;
                }
                continue;
            }

            LCartographerPlanDocument? lCartographerPlan = lCartographerPreparedPlan is null
                ? LCartographerPlan.LCartographerPlanCreate(lCartographerBatch.Key, lCartographerTarget)
                : LCartographerPlan.LCartographerPlanCopy(lCartographerPreparedPlan, lCartographerBatch.Key);
            if (lCartographerPlan is null || !LCartographerPlanStore.LCartographerPlanSave(lCartographerPlan))
            {
                foreach (LWorkItem lCartographerItem in lCartographerBatch)
                {
                    lCartographerItem.LWorkRelayTarget = lCartographerTarget;
                    lCartographerItem.LWorkRelaySource = lCartographerSource;
                }
                continue;
            }

            lCartographerCreated.Add(lCartographerPlan.LCartographerPlanId);
            foreach (LWorkItem lCartographerItem in lCartographerBatch)
            {
                lCartographerItem.LWorkRelayTarget = lCartographerPlan.LCartographerEntryStage;
                lCartographerItem.LWorkRelaySource = lCartographerSource;
            }
            LTraceEngine.LTraceInfoSeam(
                $"Relay plan {lCartographerPlan.LCartographerPlanId:N} captured " +
                $"{lCartographerPlan.LCartographerStages.Count} stable stage(s)");
        }

        return lCartographerCreated;
    }
}
