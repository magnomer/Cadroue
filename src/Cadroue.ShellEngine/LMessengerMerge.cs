using Cadroue.Core;
using Cadroue.Infrastructure;

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
            lMessengerMessage => LTraceLog.LTraceInfoRecord(lMessengerMessage),
            lMessengerMessage => LTraceLog.LTraceErrorRecord(lMessengerMessage),
            lMessengerRelays);
        if (lMessengerItems.Count == 0)
        {
            return 0;
        }

        int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord($"Merge queued {lMessengerAdded} group(s) at {lMessengerPriority}");
        _ = LMessengerSourceResolve(lMessengerItems);
        return lMessengerAdded;
    }
}
