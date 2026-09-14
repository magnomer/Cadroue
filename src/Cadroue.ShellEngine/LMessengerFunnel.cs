using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LMessenger
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
            if (LCartographer.LCartographerCycleCheck(lMessengerSourceTab, lMessengerTarget, lMessengerName))
            {
                LTraceLog.LTraceWarningRecord(
                    $"Funnel refused '{lMessengerName}': its rule route loops back through automatic funnels");
                continue;
            }

            if (LMessengerDeliverSource?.Invoke(lMessengerTarget, lMessengerPath, lMessengerCohort) == true)
            {
                lMessengerRelayed.Add(lMessengerPath);
            }
        }

        if (Cadroue.Application.LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty
            && lMessengerRelayed.Count > 0)
        {
            LMessengerDrainSource?.Invoke(lMessengerSourceTab, lMessengerRelayed);
        }

        LSeal.LSealRun();
        LTraceLog.LTraceInfoRecord(
            $"Funnel relayed {lMessengerRelayed.Count} of {lMessengerItems.Count} file(s) by filename rule");
        return lMessengerRelayed.Count;
    }
}
