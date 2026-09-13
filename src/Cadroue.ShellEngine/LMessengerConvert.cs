using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LMessenger
{
    public static async Task<int> LMessengerConvertDescribe(
        LWorkPriority lMessengerPriority,
        IReadOnlyList<LWorkSource> lMessengerSources,
        LEncoding? lMessengerEncoding,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource)
    {
        if (lMessengerEncoding is not { } lMessengerOutput)
        {
            return 0;
        }

        string[] lMessengerSourcePaths = lMessengerSources
            .Select(lMessengerSource => lMessengerSource.LWorkSourcePath)
            .ToArray();
        var lMessengerRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkSource lMessengerSource in lMessengerSources)
        {
            lMessengerRelays[lMessengerSource.LWorkSourcePath] = lMessengerSource.LWorkSourceBatch;
        }

        LConvertWorkDescription lMessengerDescription =
            new(lMessengerSourcePaths, lMessengerOutput, null, lMessengerRelays);

        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        Func<string, TimeSpan> lMessengerDurationRead = await Cadroue.Application.LConvert
            .LConvertDurationResolve(lMessengerOutput, lMessengerSourcePaths).ConfigureAwait(true);
        IReadOnlyList<LWorkItem> lMessengerItems =
            Cadroue.Application.LConvert.LConvertItemsCreate(
                lMessengerPriority, lMessengerDescription, lMessengerTab,
                lMessengerMessage => LTraceLog.LTraceErrorRecord(lMessengerMessage),
                lMessengerDurationRead);

        int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Convert queued {lMessengerAdded} job(s) at {lMessengerPriority} from " +
            $"{lMessengerSourcePaths.Length} listed file(s)");

        await LMessengerSourceResolve(lMessengerItems).ConfigureAwait(false);
        return lMessengerAdded;
    }
}
