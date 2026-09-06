using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LMessenger
{
    public static async Task<int> LMessengerAudioDescribe(
        LWorkPriority lMessengerPriority,
        string? lMessengerSourcePath,
        LWorkAudio lMessengerProcessing,
        LEncoding? lMessengerEncoding,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        Guid lMessengerBatchId)
    {
        if (lMessengerEncoding is not { } lMessengerOutput)
        {
            return 0;
        }

        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        LWorkItem? lMessengerItem = Cadroue.Application.LAudio.LAudioItemCreate(
            lMessengerPriority, lMessengerSourcePath, lMessengerProcessing, lMessengerOutput, lMessengerTab,
            lMessengerMessage => LTraceLog.LTraceInfoRecord(lMessengerMessage),
            lMessengerMessage => LTraceLog.LTraceErrorRecord(lMessengerMessage),
            Cadroue.Application.LLibrarian.LLibrarianDurationRead,
            lMessengerBatchId);
        if (lMessengerItem is null)
        {
            return 0;
        }

        int lMessengerAdded = LMessengerDispatch(new[] { lMessengerItem }, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Audio queued {lMessengerAdded} job at {lMessengerPriority} from " +
            $"'{System.IO.Path.GetFileName(lMessengerSourcePath)}'");
        await LMessengerSourceResolve(new[] { lMessengerItem }).ConfigureAwait(false);
        return lMessengerAdded;
    }
    public static async Task<int> LMessengerAudioDescribe(
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

        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        Guid lMessengerLooseBatch = Cadroue.Application.LGate.LGateBatchCreate();
        var lMessengerItems = new List<LWorkItem>();
        foreach (LWorkSource lMessengerSource in lMessengerSources)
        {
            string lMessengerSourcePath = lMessengerSource.LWorkSourcePath;
            if (Cadroue.Application.LAudio.LAudioPlanRead(lMessengerSourcePath, Cadroue.Application.LLibrarian.LLibrarianAudioLoad)
                is not { LWorkAudioActive: true } lMessengerPlan)
            {
                continue;
            }

            Guid lMessengerBatch = lMessengerSource.LWorkSourceBatch != Guid.Empty
                ? lMessengerSource.LWorkSourceBatch
                : lMessengerLooseBatch;
            if (Cadroue.Application.LAudio.LAudioItemCreate(
                    lMessengerPriority, lMessengerSourcePath, lMessengerPlan, lMessengerOutput, lMessengerTab,
                    lMessengerMessage => LTraceLog.LTraceInfoRecord(lMessengerMessage),
                    lMessengerMessage => LTraceLog.LTraceErrorRecord(lMessengerMessage),
                    Cadroue.Application.LLibrarian.LLibrarianDurationRead,
                    lMessengerBatch)
                is { } lMessengerItem)
            {
                lMessengerItems.Add(lMessengerItem);
            }
        }

        int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        await LMessengerSourceResolve(lMessengerItems).ConfigureAwait(false);
        return lMessengerAdded;
    }
}
