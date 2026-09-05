using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LMessenger
{
    public static int LMessengerFunnelDescribe(
        IReadOnlyList<LSceneFunnelRule> lMessengerRules,
        IReadOnlyList<Guid> lMessengerTargets,
        IReadOnlyList<(string LMessengerPath, Guid LMessengerCohort)> lMessengerItems)
    {
        var lMessengerRelayed = new List<string>();
        foreach ((string lMessengerPath, Guid lMessengerCohort) in lMessengerItems)
        {
            int lMessengerMatch = LClassifier.LClassifierRouteRead(
                lMessengerRules, System.IO.Path.GetFileName(lMessengerPath));
            if (lMessengerMatch < 0 || lMessengerTargets[lMessengerMatch] == Guid.Empty)
            {
                continue;
            }

            if (LMessengerDeliverSource?.Invoke(
                    lMessengerTargets[lMessengerMatch], lMessengerPath, lMessengerCohort) == true)
            {
                lMessengerRelayed.Add(lMessengerPath);
            }
        }

        if (Cadroue.Application.LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty
            && lMessengerRelayed.Count > 0)
        {
            LMessengerDrainSource?.Invoke(lMessengerRelayed);
        }

        LSeal.LSealRun();
        LTraceLog.LTraceInfoRecord(
            $"Funnel relayed {lMessengerRelayed.Count} of {lMessengerItems.Count} file(s) by filename rule");
        return lMessengerRelayed.Count;
    }
}
