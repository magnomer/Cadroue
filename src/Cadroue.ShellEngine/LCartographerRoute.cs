using System.IO;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LCartographer
{
    private static readonly Dictionary<Guid, Guid> lCartographerTargets = new();

    public static Guid LCartographerTargetRead(Guid lCartographerSourceTab) =>
        lCartographerTargets.TryGetValue(lCartographerSourceTab, out Guid lCartographerTarget)
            ? lCartographerTarget
            : Guid.Empty;

    public static void LCartographerTargetSet(Guid lCartographerSourceTab, Guid lCartographerTarget)
    {
        if (lCartographerTarget == Guid.Empty)
        {
            lCartographerTargets.Remove(lCartographerSourceTab);
            return;
        }

        if (lCartographerTarget == lCartographerSourceTab)
        {
            LTraceLog.LTraceWarningRecord("Relay target refused: a tab cannot relay into itself");
            return;
        }

        lCartographerTargets[lCartographerSourceTab] = lCartographerTarget;
    }

    public static void LCartographerTabRemove(Guid lCartographerTabId)
    {
        lCartographerTargets.Remove(lCartographerTabId);
        lCartographerStageTitles.Remove(lCartographerTabId);
        foreach (Guid lCartographerSourceTab in lCartographerTargets
            .Where(lCartographerEntry => lCartographerEntry.Value == lCartographerTabId)
            .Select(lCartographerEntry => lCartographerEntry.Key)
            .ToArray())
        {
            lCartographerTargets.Remove(lCartographerSourceTab);
        }
    }

    public static Guid LCartographerRouteRead(LCartographerStageRecord lCartographerStage, string lCartographerPath)
    {
        IReadOnlyList<LCartographerFunnelRule> lCartographerRules = lCartographerStage.LCartographerFunnelRules;
        int lCartographerMatch = LClassifier.LClassifierRouteRead(
            lCartographerRules.Select(lCartographerRule => lCartographerRule.LCartographerRule).ToList(),
            Path.GetFileName(lCartographerPath),
            lCartographerIndex => lCartographerRules[lCartographerIndex].LCartographerTargetStage != Guid.Empty);
        return lCartographerMatch < 0 ? Guid.Empty : lCartographerRules[lCartographerMatch].LCartographerTargetStage;
    }

    public static IReadOnlyList<Guid> LCartographerRelaySet(
        IReadOnlyList<LWorkItem> lCartographerItems,
        Guid lCartographerTarget,
        Guid lCartographerSource,
        LCartographerPlanRecord? lCartographerPreparedPlan)
    {
        var lCartographerCreated = new List<Guid>();
        foreach (IGrouping<Guid, LWorkItem> lCartographerBatch in lCartographerItems.GroupBy(
            lCartographerItem => lCartographerItem.LWorkBatchId))
        {
            if (LCartographerPlanStore.LCartographerPlanRead(
                lCartographerBatch.Key, out LCartographerPlanRecord lCartographerExisting))
            {
                LCartographerStageRecord? lCartographerSourceStage =
                    lCartographerExisting.LCartographerStages.FirstOrDefault(
                        lCartographerStage => lCartographerStage.LCartographerStageId == lCartographerSource
                            || lCartographerStage.LCartographerOriginalTab == lCartographerSource);
                LCartographerStageRecord? lCartographerTargetStage =
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

            if (lCartographerTarget == Guid.Empty || lCartographerTarget == LCartographerFinishTarget)
            {
                foreach (LWorkItem lCartographerItem in lCartographerBatch)
                {
                    lCartographerItem.LWorkRelayTarget = lCartographerTarget;
                    lCartographerItem.LWorkRelaySource = lCartographerSource;
                }
                continue;
            }

            LCartographerPlanRecord? lCartographerPlan = lCartographerPreparedPlan is null
                ? LCartographerPlanCreate(lCartographerBatch.Key, lCartographerTarget)
                : LCartographerPlanCopy(lCartographerPreparedPlan, lCartographerBatch.Key);
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
            LTraceLog.LTraceInfoRecord(
                $"Relay plan {lCartographerPlan.LCartographerPlanId:N} captured " +
                $"{lCartographerPlan.LCartographerStages.Count} stable stage(s)");
        }

        return lCartographerCreated;
    }

    public static bool LCartographerCycleCheck(
        Guid lCartographerSourceTab,
        Guid lCartographerTargetTab,
        string lCartographerName)
    {
        IReadOnlyList<LCartographerTab> lCartographerTabs =
            LCartographerTabsSource?.Invoke() ?? Array.Empty<LCartographerTab>();
        var lCartographerSeen = new HashSet<Guid> { lCartographerSourceTab };
        Guid lCartographerCurrent = lCartographerTargetTab;
        while (lCartographerCurrent != Guid.Empty)
        {
            if (!lCartographerSeen.Add(lCartographerCurrent))
            {
                return true;
            }

            LCartographerTab? lCartographerTab = lCartographerTabs
                .FirstOrDefault(lCartographerItem => lCartographerItem.LCartographerTabId == lCartographerCurrent);
            if (lCartographerTab is null
                || !lCartographerTab.LCartographerFunnel
                || !lCartographerTab.LCartographerLayout.LSceneAutoRelay)
            {
                return false;
            }

            IReadOnlyList<LSceneFunnelRule> lCartographerRules = lCartographerTab.LCartographerLayout.LSceneFunnelRules;
            int lCartographerMatch = LClassifier.LClassifierRouteRead(
                lCartographerRules,
                lCartographerName,
                lCartographerIndex => lCartographerRules[lCartographerIndex].LSceneFunnelTarget >= 0
                    && lCartographerRules[lCartographerIndex].LSceneFunnelTarget < lCartographerTabs.Count);
            lCartographerCurrent = lCartographerMatch < 0
                ? Guid.Empty
                : lCartographerTabs[lCartographerRules[lCartographerMatch].LSceneFunnelTarget].LCartographerTabId;
        }

        return false;
    }
}
