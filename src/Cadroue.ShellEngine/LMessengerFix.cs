using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LMessengerFix
{
    public static int LMessengerFixDescribe(
        LWorkPriority lMessengerPriority,
        IReadOnlyList<LWorkSource> lMessengerSources,
        LEncoding? lMessengerEncoding,
        Guid lMessengerRelayTarget = default,
        Guid lMessengerRelaySource = default)
    {
        if (lMessengerEncoding is not { } lMessengerOutput)
        {
            return 0;
        }

        string[] lMessengerSourcePaths = lMessengerSources
            .Select(lMessengerSource => lMessengerSource.LWorkSourcePath)
            .ToArray();
        IReadOnlyDictionary<string, Guid> lMessengerRelays = LMessenger.LMessengerRelayRead(lMessengerSources);
        var lMessengerPlans = new Dictionary<string, LWorkFix>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkSource lMessengerSource in lMessengerSources)
        {
            if (Cadroue.Application.LFixSidecar.LFixSidecarRead(
                    lMessengerSource.LWorkSourcePath,
                    Cadroue.Application.LLibrarian.LLibrarianFixLoad) is { } lMessengerPlan)
            {
                lMessengerPlans[lMessengerSource.LWorkSourcePath] = lMessengerPlan;
            }
        }

        LFixWorkDescription lMessengerDescription =
            new(lMessengerSourcePaths, lMessengerOutput, null, lMessengerRelays, lMessengerPlans);

        string lMessengerTab = LMessenger.LMessengerTitleRead(lMessengerRelaySource);
        IReadOnlyList<LWorkItem> lMessengerItems =
            Cadroue.Application.LFix.LFixItemsCreate(
                lMessengerPriority, lMessengerDescription, lMessengerTab,
                lMessengerMessage => LTraceEngine.LTraceErrorSeam(lMessengerMessage, null),
                Cadroue.Application.LLibrarian.LLibrarianDurationRead);

        int lMessengerAdded =
            LMessenger.LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceEngine.LTraceInfoSeam(
            $"Fix queued {lMessengerAdded} job(s) at {lMessengerPriority} from " +
            $"{lMessengerSourcePaths.Length} listed file(s)");

        LMessengerMeasure.LMessengerSourceResolve(lMessengerItems);
        return lMessengerAdded;
    }
}
