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
        Cadroue.Application.LPresetSelection lMessengerOwner,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        Guid lMessengerBatchId)
    {
        if (lMessengerOwner.LPresetSelectionEncoding is not { } lMessengerOutput)
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
        _ = LMessengerSourceResolve(lMessengerItems);
        return lMessengerAdded;
    }
    public static async Task<int> LMessengerEditDescribe(
        LWorkPriority lMessengerPriority,
        IReadOnlyList<LWorkSource> lMessengerSources,
        Cadroue.Application.LPresetSelection lMessengerOwner,
        Guid lMessengerRelayTarget = default,
        Guid lMessengerRelaySource = default)
    {
        if (lMessengerOwner.LPresetSelectionEncoding is not { } lMessengerOutput)
        {
            return 0;
        }

        var lMessengerItems = new List<LWorkItem>();
        Guid lMessengerLooseBatch = Cadroue.Application.LGate.LGateBatchCreate();
        bool lMessengerEqCapable = Cadroue.Infrastructure.LInventory.LInventoryFilterExist("eq");

        foreach (LWorkSource lMessengerSource in lMessengerSources)
        {
            string lMessengerSourcePath = lMessengerSource.LWorkSourcePath;
            if (Cadroue.Application.LEdit.LEditPlanRead(lMessengerSourcePath, Cadroue.Application.LLibrarian.LLibrarianEditLoad)
                is not { LEditPlanActive: true } lMessengerPlan)
            {
                continue;
            }

            Guid lMessengerBatch = lMessengerSource.LWorkSourceBatch != Guid.Empty
                ? lMessengerSource.LWorkSourceBatch
                : lMessengerLooseBatch;
            (LWorkCrop lMessengerCrop, LWorkVideo lMessengerVideo) =
                Cadroue.Application.LEdit.LEditWorkResolve(lMessengerPlan, lMessengerEqCapable);
            if (Cadroue.Application.LEdit.LEditWorkCreate(
                    lMessengerPriority,
                    lMessengerSourcePath,
                    Cadroue.Application.LLibrarian.LLibrarianDurationRead(lMessengerSourcePath),
                    lMessengerCrop,
                    lMessengerVideo,
                    lMessengerOutput,
                    lMessengerBatch) is { } lMessengerEditItem)
            {
                lMessengerItems.Add(lMessengerEditItem);
            }
        }

        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        foreach (LWorkItem lMessengerItem in lMessengerItems)
        {
            lMessengerItem.LWorkTab = lMessengerTab;
        }

        int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Edit Add All: {lMessengerSources.Count} listed, {lMessengerAdded} queued from saved plans");

        await LMessengerSourceResolve(lMessengerItems).ConfigureAwait(false);
        return lMessengerAdded;
    }
}
