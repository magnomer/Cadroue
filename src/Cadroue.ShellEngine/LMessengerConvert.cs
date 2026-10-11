using Cadroue.Core;

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

        try
        {
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
                    lMessengerMessage => LTraceEngine.LTraceErrorSeam(lMessengerMessage, null),
                    lMessengerDurationRead);

            int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
            LTraceEngine.LTraceInfoSeam(
                $"Convert queued {lMessengerAdded} job(s) at {lMessengerPriority} from " +
                $"{lMessengerSourcePaths.Length} listed file(s)");

            LMessengerSourceResolve(lMessengerItems);
            return lMessengerAdded;
        }
        catch (Exception lMessengerException)
        {
            LTraceEngine.LTraceErrorSeam("Convert could not be queued", lMessengerException);
            return 0;
        }
    }
}
