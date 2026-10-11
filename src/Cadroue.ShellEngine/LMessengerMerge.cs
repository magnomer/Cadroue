using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static partial class LMessenger
{
    public static int LMessengerMergeDescribe(
        LWorkPriority lMessengerPriority,
        IReadOnlyList<LWorkGroup> lMessengerGroups,
        LEncoding? lMessengerEncoding,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        IReadOnlyDictionary<string, Guid>? lMessengerRelays)
    {
        if (lMessengerEncoding is not { } lMessengerOutput)
        {
            return 0;
        }

        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        IReadOnlyList<LWorkItem> lMessengerItems = Cadroue.Application.LMerge.LMergeItemsCreate(
            lMessengerPriority, lMessengerGroups, lMessengerOutput, lMessengerTab,
            lMessengerMessage => LTraceEngine.LTraceInfoSeam(lMessengerMessage),
            lMessengerMessage => LTraceEngine.LTraceErrorSeam(lMessengerMessage, null),
            lMessengerRelays);
        if (lMessengerItems.Count == 0)
        {
            return 0;
        }

        int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceEngine.LTraceInfoSeam($"Merge queued {lMessengerAdded} group(s) at {lMessengerPriority}");
        LMessengerSourceResolve(lMessengerItems);
        return lMessengerAdded;
    }
}
