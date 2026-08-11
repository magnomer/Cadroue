using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static class LMessenger
{
    public static Func<Guid, string>? LMessengerTitleSource { get; set; }

    public static Func<IReadOnlyList<LWorkItem>, Guid, Guid, LCartographerPlanRecord?, int>? LMessengerRouteSource { get; set; }

    public static Func<LScheduleContract?>? LMessengerScheduleSource { get; set; }

    public static Func<Guid, string, Guid, bool>? LMessengerFunnelDeliverSource { get; set; }

    public static Action<IReadOnlyList<string>>? LMessengerFunnelDrainSource { get; set; }

    private static string LMessengerTitleRead(Guid lMessengerRelaySource) =>
        LMessengerTitleSource?.Invoke(lMessengerRelaySource) ?? string.Empty;

    private static int LMessengerRoute(
        IReadOnlyList<LWorkItem> lMessengerItems,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        LCartographerPlanRecord? lMessengerPlan = null) =>
        LMessengerRouteSource?.Invoke(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource, lMessengerPlan) ?? 0;

    public static async Task<int> LMessengerAudioDescribe(
        LWorkPriority lMessengerPriority,
        string? lMessengerSourcePath,
        LWorkAudio lMessengerProcessing,
        Cadroue.Application.LPresetSelection lMessengerOwner,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        Guid lMessengerBatchId)
    {
        if (lMessengerOwner.LPresetSelectionEncoding is not { } lMessengerOutput)
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

        int lMessengerAdded = LMessengerRoute(new[] { lMessengerItem }, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Audio queued {lMessengerAdded} job at {lMessengerPriority} from " +
            $"'{System.IO.Path.GetFileName(lMessengerSourcePath)}'");
        await LMessengerDurationResolve(new[] { lMessengerItem }).ConfigureAwait(false);
        return lMessengerAdded;
    }

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
        int lMessengerAdded = LMessengerRoute(
            lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource, lMessengerPlan);
        LTraceLog.LTraceInfoRecord(
            $"Split queued {lMessengerAdded} of {lMessengerItems.Count} job(s) at {lMessengerPriority} " +
            $"from '{System.IO.Path.GetFileName(lMessengerSourcePath)}'");
        return lMessengerAdded;
    }

    public static async Task<int> LMessengerAudioDescribe(
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

        int lMessengerAdded = LMessengerRoute(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        await LMessengerDurationResolve(lMessengerItems).ConfigureAwait(false);
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
        LMessengerPost(() =>
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

    private static IReadOnlyList<LSplitSectionDescription> LMessengerSplitRead(string lMessengerSourcePath)
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

        int lMessengerAdded = LMessengerRoute(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Edit queued {lMessengerAdded} job(s) at {lMessengerPriority} from " +
            $"'{System.IO.Path.GetFileName(lMessengerSourcePath)}'");
        return lMessengerAdded;
    }

    public static int LMessengerMergeDescribe(
        LWorkPriority lMessengerPriority,
        IReadOnlyList<LWorkGroup> lMessengerGroups,
        Cadroue.Application.LPresetSelection lMessengerOwner,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        IReadOnlyDictionary<string, Guid>? lMessengerRelays)
    {
        if (lMessengerOwner.LPresetSelectionEncoding is not { } lMessengerOutput)
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

        int lMessengerAdded = LMessengerRoute(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord($"Merge queued {lMessengerAdded} group(s) at {lMessengerPriority}");
        return lMessengerAdded;
    }

    public static async Task<int> LMessengerConvertDescribe(
        LWorkPriority lMessengerPriority,
        IReadOnlyList<LWorkSource> lMessengerSources,
        Cadroue.Application.LPresetSelection lMessengerOwner,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource)
    {
        if (lMessengerOwner.LPresetSelectionEncoding is not { } lMessengerOutput)
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
        IReadOnlyList<LWorkItem> lMessengerItems =
            Cadroue.Application.LConvert.LConvertItemsCreate(
                lMessengerPriority, lMessengerDescription, lMessengerTab,
                lMessengerMessage => LTraceLog.LTraceErrorRecord(lMessengerMessage),
                Cadroue.Application.LLibrarian.LLibrarianDurationRead);

        int lMessengerAdded = LMessengerRoute(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Convert queued {lMessengerAdded} job(s) at {lMessengerPriority} from {lMessengerSourcePaths.Length} listed file(s)");

        await LMessengerDurationResolve(lMessengerItems).ConfigureAwait(false);
        return lMessengerAdded;
    }

    private static async Task LMessengerDurationResolve(IReadOnlyList<LWorkItem> lMessengerItems)
    {
        LWorkItem[] lMessengerUnknown = lMessengerItems
            .Where(lWorkItem => lWorkItem.LWorkEnd <= TimeSpan.Zero)
            .ToArray();
        if (lMessengerUnknown.Length == 0)
        {
            return;
        }

        var lMessengerResolved = new TimeSpan[lMessengerUnknown.Length];
        await Task.Run(() => Parallel.For(
            0,
            lMessengerUnknown.Length,
            new ParallelOptions { MaxDegreeOfParallelism = LMessengerParallelRead() },
            lMessengerIndex => lMessengerResolved[lMessengerIndex] =
                Cadroue.Application.LLibrarian.LLibrarianDurationResolve(lMessengerUnknown[lMessengerIndex].LWorkSourcePath)))
            .ConfigureAwait(false);

        if (LMessengerScheduleSource?.Invoke() is not { } lMessengerSchedule)
        {
            return;
        }

        LMessengerPost(() =>
        {
            for (int lMessengerIndex = 0; lMessengerIndex < lMessengerUnknown.Length; lMessengerIndex++)
            {
                lMessengerSchedule.LScheduleDurationSet(
                    lMessengerUnknown[lMessengerIndex].LWorkId, lMessengerResolved[lMessengerIndex]);
            }
        });
    }

    private static void LMessengerPost(Action lMessengerAction)
    {
        if (Cadroue.ShellEngine.LStation.LStationPost is { } lMessengerPost)
        {
            lMessengerPost(lMessengerAction);
            return;
        }

        lMessengerAction();
    }

    private static int LMessengerParallelRead() => Math.Clamp(Environment.ProcessorCount, 1, 8);

    public static int LMessengerFunnelDescribe(
        IReadOnlyList<LSceneFunnelRule> lMessengerRules,
        IReadOnlyList<Guid> lMessengerTargets,
        IReadOnlyList<(string LMessengerPath, Guid LMessengerCohort)> lMessengerItems)
    {
        var lMessengerRelayed = new List<string>();
        foreach ((string lMessengerPath, Guid lMessengerCohort) in lMessengerItems)
        {
            int lMessengerMatch = LClassifier.LClassifierRouteRead(
                lMessengerRules, System.IO.Path.GetFileName(lMessengerPath));
            if (lMessengerMatch < 0 || lMessengerTargets[lMessengerMatch] == Guid.Empty)
            {
                continue;
            }

            if (LMessengerFunnelDeliverSource?.Invoke(
                    lMessengerTargets[lMessengerMatch], lMessengerPath, lMessengerCohort) == true)
            {
                lMessengerRelayed.Add(lMessengerPath);
            }
        }

        if (Cadroue.Application.LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty
            && lMessengerRelayed.Count > 0)
        {
            LMessengerFunnelDrainSource?.Invoke(lMessengerRelayed);
        }

        LSeal.LSealSweep();
        LTraceLog.LTraceInfoRecord(
            $"Funnel relayed {lMessengerRelayed.Count} of {lMessengerItems.Count} file(s) by filename rule");
        return lMessengerRelayed.Count;
    }

    public static async Task<int> LMessengerEditDescribe(
        LWorkPriority lMessengerPriority,
        IReadOnlyList<LWorkSource> lMessengerSources,
        Cadroue.Application.LPresetSelection lMessengerOwner,
        Guid lMessengerRelayTarget = default,
        Guid lMessengerRelaySource = default,
        bool lMessengerGammaCapable = true)
    {
        if (lMessengerOwner.LPresetSelectionEncoding is not { } lMessengerOutput)
        {
            return 0;
        }

        var lMessengerItems = new List<LWorkItem>();
        Guid lMessengerLooseBatch = Cadroue.Application.LGate.LGateBatchCreate();

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
            lMessengerItems.Add(Cadroue.Application.LEdit.LEditWorkCreate(
                lMessengerPriority,
                lMessengerSourcePath,
                Cadroue.Application.LLibrarian.LLibrarianDurationRead(lMessengerSourcePath),
                lMessengerPlan.LEditSkip ? LWorkCrop.LWorkCropCreate() : lMessengerPlan.LEditCrop,
                lMessengerPlan.LEditSkip
                    ? LWorkVideo.LWorkVideoCreate()
                    : Cadroue.Application.LEdit.LEditVideoCreate(
                        lMessengerPlan.LEditVideo.LWorkVideoSteps, lMessengerGammaCapable),
                lMessengerOutput,
                lMessengerBatch));
        }

        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        foreach (LWorkItem lMessengerItem in lMessengerItems)
        {
            lMessengerItem.LWorkTab = lMessengerTab;
        }

        int lMessengerAdded = LMessengerRoute(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Edit Add All: {lMessengerSources.Count} listed, {lMessengerAdded} queued from saved plans");

        await LMessengerDurationResolve(lMessengerItems).ConfigureAwait(false);
        return lMessengerAdded;
    }
}
