using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LCartographerRoute
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
            LTraceEngine.LTraceWarningSeam("Relay target refused: a tab cannot relay into itself");
            return;
        }

        lCartographerTargets[lCartographerSourceTab] = lCartographerTarget;
    }

    public static void LCartographerTabRemove(Guid lCartographerTabId)
    {
        lCartographerTargets.Remove(lCartographerTabId);
        LCartographerTitle.LCartographerTitleRemove(lCartographerTabId);
        foreach (Guid lCartographerSourceTab in lCartographerTargets
            .Where(lCartographerEntry => lCartographerEntry.Value == lCartographerTabId)
            .Select(lCartographerEntry => lCartographerEntry.Key)
            .ToArray())
        {
            lCartographerTargets.Remove(lCartographerSourceTab);
        }
    }

    public static Guid LCartographerRouteRead(LCartographerStageEntry lCartographerStage, string lCartographerPath)
    {
        IReadOnlyList<LCartographerFunnelRule> lCartographerRules = lCartographerStage.LCartographerFunnelRules;
        int lCartographerMatch = LClassifier.LClassifierRouteRead(
            lCartographerRules.Select(lCartographerRule => lCartographerRule.LCartographerRule).ToList(),
            Path.GetFileName(lCartographerPath),
            lCartographerIndex => lCartographerRules[lCartographerIndex].LCartographerTargetStage != Guid.Empty);
        return lCartographerMatch < 0 ? Guid.Empty : lCartographerRules[lCartographerMatch].LCartographerTargetStage;
    }

    public static bool LCartographerCycleCheck(
        Guid lCartographerSourceTab,
        Guid lCartographerTargetTab,
        string lCartographerName)
    {
        IReadOnlyList<LCartographerTab> lCartographerTabs =
            LCartographerPlan.LCartographerTabsSource?.Invoke() ?? Array.Empty<LCartographerTab>();
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
