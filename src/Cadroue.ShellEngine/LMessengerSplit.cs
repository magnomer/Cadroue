using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LMessenger
{
    public static int LMessengerSplitDescribe(
        LWorkPriority lMessengerPriority,
        string? lMessengerSourcePath,
        IReadOnlyList<LSplitSectionDescription> lMessengerSections,
        Cadroue.Application.LPresetSelection lMessengerOwner,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        Guid lMessengerBatchId)
    {
        if (lMessengerOwner.LPresetSelectionEncoding is not { } lMessengerOutput)
        {
            return 0;
        }

        LSplitWorkDescription lMessengerDescription = new(
            lMessengerSourcePath, lMessengerSections, lMessengerOutput);
        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        IReadOnlyList<LWorkItem> lMessengerItems = Cadroue.Application.LSplit.LSplitItemsCreate(
            lMessengerPriority, lMessengerDescription, lMessengerTab,
            lMessengerMessage => LTraceLog.LTraceInfoRecord(lMessengerMessage),
            lMessengerMessage => LTraceLog.LTraceErrorRecord(lMessengerMessage),
            lMessengerBatchId);
        if (lMessengerItems.Count == 0)
        {
            return 0;
        }

        LCartographerPlanRecord? lMessengerPlan = LCartographer.LCartographerPlanPrepare(lMessengerRelayTarget);
        int lMessengerAdded = LMessengerDispatch(
            lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource, lMessengerPlan);
        LTraceLog.LTraceInfoRecord(
            $"Split queued {lMessengerAdded} of {lMessengerItems.Count} job(s) at {lMessengerPriority} " +
            $"from '{System.IO.Path.GetFileName(lMessengerSourcePath)}'");
        _ = LMessengerSourceResolve(lMessengerItems);
        return lMessengerAdded;
    }
    public static async Task<int> LMessengerSplitDescribe(
        LWorkPriority lMessengerPriority,
        IReadOnlyList<LWorkSource> lMessengerSources,
        Cadroue.Application.LPresetSelection lMessengerOwner,
        Guid lMessengerRelayTarget = default,
        Guid lMessengerRelaySource = default)
    {
        var lMessengerRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkSource lMessengerSource in lMessengerSources)
        {
            lMessengerRelays[lMessengerSource.LWorkSourcePath] = lMessengerSource.LWorkSourceBatch;
        }
        string[] lMessengerSourcePaths = lMessengerSources
            .Select(lMessengerSource => lMessengerSource.LWorkSourcePath)
            .ToArray();

        IReadOnlyList<LSplitPlanRecord> lMessengerPlans =
            await Task.Run(() => LMessengerSplitCreate(lMessengerSourcePaths)).ConfigureAwait(false);

        int lMessengerAdded = 0;
        LMessengerDefer(() =>
        {
            foreach (LSplitPlanRecord lMessengerPlan in lMessengerPlans)
            {
                lMessengerRelays.TryGetValue(lMessengerPlan.LSplitSourcePath, out Guid lMessengerBatch);
                lMessengerAdded += LMessengerSplitDescribe(
                    lMessengerPriority, lMessengerPlan.LSplitSourcePath, lMessengerPlan.LSplitPlanSections,
                    lMessengerOwner, lMessengerRelayTarget, lMessengerRelaySource, lMessengerBatch);
            }
        });
        return lMessengerAdded;
    }

    private static IReadOnlyList<LSplitPlanRecord> LMessengerSplitCreate(IReadOnlyList<string> lMessengerSourcePaths)
    {
        var lMessengerPlans = new List<LSplitPlanRecord>();
        foreach (string lMessengerSourcePath in lMessengerSourcePaths)
        {
            IReadOnlyList<LSplitSectionDescription> lMessengerSections = LMessengerSplitRead(lMessengerSourcePath);
            if (lMessengerSections.Count > 0)
            {
                lMessengerPlans.Add(new LSplitPlanRecord(lMessengerSourcePath, lMessengerSections));
            }
        }
        return lMessengerPlans;
    }

    internal static IReadOnlyList<LSplitSectionDescription> LMessengerSplitRead(string lMessengerSourcePath)
    {
        try
        {
            if (Cadroue.Application.LLibrarian.LLibrarianLoad(lMessengerSourcePath) is not { } lMessengerSidecar)
            {
                return Array.Empty<LSplitSectionDescription>();
            }
            return lMessengerSidecar.LSidecarSections
                .Where(lMessengerRecord => lMessengerRecord.LSidecarEndMilliseconds > lMessengerRecord.LSidecarStartMilliseconds)
                .Select(lMessengerRecord => new LSplitSectionDescription(
                    TimeSpan.FromMilliseconds(lMessengerRecord.LSidecarStartMilliseconds),
                    TimeSpan.FromMilliseconds(lMessengerRecord.LSidecarEndMilliseconds),
                    lMessengerRecord.LSidecarName,
                    lMessengerRecord.LSidecarPrefix,
                    lMessengerRecord.LSidecarSuffix,
                    lMessengerRecord.LSidecarHidden))
                .ToArray();
        }
        catch (Exception lMessengerException)
        {
            LTraceLog.LTraceErrorRecord($"Split plan could not be read for '{lMessengerSourcePath}'", lMessengerException);
            return Array.Empty<LSplitSectionDescription>();
        }
    }
}
