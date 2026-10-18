using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LMessengerEdit
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
        string lMessengerTab = LMessenger.LMessengerTitleRead(lMessengerRelaySource);
        IReadOnlyList<LWorkItem> lMessengerItems = Cadroue.Application.LEdit.LEditItemsCreate(
            lMessengerPriority, lMessengerDescription, lMessengerTab,
            lMessengerMessage => LTraceEngine.LTraceInfoSeam(lMessengerMessage),
            lMessengerMessage => LTraceEngine.LTraceErrorSeam(lMessengerMessage, null),
            lMessengerBatchId);
        if (lMessengerItems.Count == 0)
        {
            return 0;
        }

        int lMessengerAdded =
            LMessenger.LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceEngine.LTraceInfoSeam(
            $"Edit queued {lMessengerAdded} job(s) at {lMessengerPriority} from " +
            $"'{System.IO.Path.GetFileName(lMessengerSourcePath)}'");
        LMessengerMeasure.LMessengerSourceResolve(lMessengerItems);
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
            LInventoryEngine.LInventoryFilterSeam("eq"),
            Cadroue.Application.LGate.LGateBatchCreate());

        string lMessengerTab = LMessenger.LMessengerTitleRead(lMessengerRelaySource);
        foreach (LWorkItem lMessengerItem in lMessengerItems)
        {
            lMessengerItem.LWorkTab = lMessengerTab;
        }

        int lMessengerAdded =
            LMessenger.LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceEngine.LTraceInfoSeam(
            $"Edit Add All: {lMessengerSources.Count} listed, {lMessengerAdded} queued");

        LMessengerMeasure.LMessengerSourceResolve(lMessengerItems);
        return lMessengerAdded;
    }
}
