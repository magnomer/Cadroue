using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LMessengerFunnel
{
    public static int LMessengerFunnelDescribe(
        Guid lMessengerSourceTab,
        IReadOnlyList<LSceneFunnelRule> lMessengerRules,
        IReadOnlyList<Guid> lMessengerTargets,
        IReadOnlyList<(string LMessengerPath, Guid LMessengerCohort)> lMessengerItems)
    {
        var lMessengerRelayed = new List<string>();
        foreach ((string lMessengerPath, Guid lMessengerCohort) in lMessengerItems)
        {
            string lMessengerName = System.IO.Path.GetFileName(lMessengerPath);
            int lMessengerMatch = LClassifier.LClassifierRouteRead(
                lMessengerRules,
                lMessengerName,
                lMessengerIndex => lMessengerIndex < lMessengerTargets.Count
                    && lMessengerTargets[lMessengerIndex] != Guid.Empty);
            if (lMessengerMatch < 0)
            {
                continue;
            }

            Guid lMessengerTarget = lMessengerTargets[lMessengerMatch];
            if (LCartographerRoute.LCartographerCycleCheck(lMessengerSourceTab, lMessengerTarget, lMessengerName))
            {
                LTraceEngine.LTraceWarningSeam(
                    $"Funnel refused '{lMessengerName}': its rule route loops back through automatic funnels");
                continue;
            }

            if (LMessenger.LMessengerDeliverSource?.Invoke(lMessengerTarget, lMessengerPath, lMessengerCohort) == true)
            {
                lMessengerRelayed.Add(lMessengerPath);
            }
        }

        if (Cadroue.Application.LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty
            && lMessengerRelayed.Count > 0)
        {
            LMessenger.LMessengerDrainSource?.Invoke(lMessengerSourceTab, lMessengerRelayed);
        }

        LSeal.LSealRun();
        LTraceEngine.LTraceInfoSeam(
            $"Funnel relayed {lMessengerRelayed.Count} of {lMessengerItems.Count} file(s) by filename rule");
        return lMessengerRelayed.Count;
    }
}
