using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LMessenger
{
    public static int LMessengerEditDescribe(
        LWorkPriority lMessengerPriority,
        string? lMessengerSourcePath,
        TimeSpan lMessengerDuration,
        LWorkCrop lMessengerCrop,
        LWorkVideo lMessengerVideo,
        LEncoding? lMessengerEncoding,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        Guid lMessengerBatchId)
    {
        if (lMessengerEncoding is not { } lMessengerOutput)
        {
            return 0;
        }

        LEditWorkDescription lMessengerDescription = new(
            lMessengerSourcePath, lMessengerDuration, lMessengerCrop, lMessengerVideo,
            lMessengerOutput);
        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        IReadOnlyList<LWorkItem> lMessengerItems = Cadroue.Application.LEdit.LEditItemsCreate(
            lMessengerPriority, lMessengerDescription, lMessengerTab,
            lMessengerMessage => LTraceLog.LTraceInfoRecord(lMessengerMessage),
            lMessengerMessage => LTraceLog.LTraceErrorRecord(lMessengerMessage),
            lMessengerBatchId);
        if (lMessengerItems.Count == 0)
        {
            return 0;
        }

        int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Edit queued {lMessengerAdded} job(s) at {lMessengerPriority} from " +
            $"'{System.IO.Path.GetFileName(lMessengerSourcePath)}'");
        LMessengerSourceResolve(lMessengerItems);
        return lMessengerAdded;
    }
    public static int LMessengerEditDescribe(
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

        IReadOnlyList<LWorkItem> lMessengerItems = Cadroue.Application.LEdit.LEditSourcesCreate(
            lMessengerPriority,
            lMessengerSources,
            lMessengerOutput,
            Cadroue.Application.LLibrarian.LLibrarianEditLoad,
            Cadroue.Application.LLibrarian.LLibrarianDurationRead,
            Cadroue.Infrastructure.LInventory.LInventoryFilterExist("eq"),
            Cadroue.Application.LGate.LGateBatchCreate());

        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        foreach (LWorkItem lMessengerItem in lMessengerItems)
        {
            lMessengerItem.LWorkTab = lMessengerTab;
        }

        int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Edit Add All: {lMessengerSources.Count} listed, {lMessengerAdded} queued");

        LMessengerSourceResolve(lMessengerItems);
        return lMessengerAdded;
    }
}
