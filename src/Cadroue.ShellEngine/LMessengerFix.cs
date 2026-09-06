using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LMessenger
{
    public static async Task<int> LMessengerFixDescribe(
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
        var lMessengerRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var lMessengerPlans = new Dictionary<string, LWorkFix>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkSource lMessengerSource in lMessengerSources)
        {
            lMessengerRelays[lMessengerSource.LWorkSourcePath] = lMessengerSource.LWorkSourceBatch;
            if (Cadroue.Application.LFix.LFixPlanRead(
                    lMessengerSource.LWorkSourcePath,
                    Cadroue.Application.LLibrarian.LLibrarianFixLoad) is { } lMessengerPlan)
            {
                lMessengerPlans[lMessengerSource.LWorkSourcePath] = lMessengerPlan;
            }
        }

        LFixWorkDescription lMessengerDescription =
            new(lMessengerSourcePaths, lMessengerOutput, null, lMessengerRelays, lMessengerPlans);

        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        IReadOnlyList<LWorkItem> lMessengerItems =
            Cadroue.Application.LFix.LFixItemsCreate(
                lMessengerPriority, lMessengerDescription, lMessengerTab,
                lMessengerMessage => LTraceLog.LTraceErrorRecord(lMessengerMessage),
                Cadroue.Application.LLibrarian.LLibrarianDurationRead);

        int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Fix queued {lMessengerAdded} job(s) at {lMessengerPriority} from {lMessengerSourcePaths.Length} listed file(s)");

        await LMessengerSourceResolve(lMessengerItems).ConfigureAwait(false);
        return lMessengerAdded;
    }
}
