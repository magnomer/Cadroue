using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.MigrationInterface;

public static class LMessenger
{
    public static Func<Guid, string>? LMessengerTitleSource { get; set; }

    public static Func<IReadOnlyList<LWorkItem>, Guid, Guid, LRelayPlanRecord?, int>? LMessengerRouteSource { get; set; }

    public static Func<LScheduleContract?>? LMessengerScheduleSource { get; set; }

    private static string LMessengerTitleRead(Guid lMessengerRelaySource) =>
        LMessengerTitleSource?.Invoke(lMessengerRelaySource) ?? string.Empty;

    private static int LMessengerRoute(
        IReadOnlyList<LWorkItem> lMessengerItems,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        LRelayPlanRecord? lMessengerPlan = null) =>
        LMessengerRouteSource?.Invoke(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource, lMessengerPlan) ?? 0;

    public static async Task<int> LMessengerAudioDescribe(
        LWorkPriority lMessengerPriority,
        string? lMessengerSourcePath,
        LWorkAudio lMessengerProcessing,
        LPreset lMessengerPreset,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        Guid lMessengerBatchId)
    {
        LEncoding lMessengerOutput = lMessengerPreset.LPresetOutputCreate();
        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        LWorkItem? lMessengerItem = Cadroue.Application.LAudio.LAudioItemCreate(
            lMessengerPriority, lMessengerSourcePath, lMessengerProcessing, lMessengerOutput, lMessengerTab,
            lMessengerMessage => LTraceLog.LTraceInfoRecord(lMessengerMessage),
            lMessengerMessage => LTraceLog.LTraceErrorRecord(lMessengerMessage),
            Cadroue.Media.LSidecarStore.LSidecarDurationRead,
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
        LPreset lMessengerPreset,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        Guid lMessengerBatchId,
        LRelayPlanRecord? lMessengerPreparedPlan)
    {
        LSplitWorkDescription lMessengerDescription = new(
            lMessengerSourcePath, lMessengerSections, lMessengerPreset.LPresetOutputCreate());
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

        int lMessengerAdded = LMessengerRoute(
            lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource, lMessengerPreparedPlan);
        LTraceLog.LTraceInfoRecord(
            $"Split queued {lMessengerAdded} of {lMessengerItems.Count} job(s) at {lMessengerPriority} " +
            $"from '{System.IO.Path.GetFileName(lMessengerSourcePath)}'");
        return lMessengerAdded;
    }

    public static async Task<int> LMessengerAudioAllDescribe(
        LWorkPriority lMessengerPriority,
        IReadOnlyList<LWorkSource> lMessengerSources,
        LPreset lMessengerPreset,
        Guid lMessengerRelayTarget = default,
        Guid lMessengerRelaySource = default)
    {
        LEncoding lMessengerOutput = lMessengerPreset.LPresetOutputCreate();
        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        Guid lMessengerLooseBatch = Guid.NewGuid();
        var lMessengerItems = new List<LWorkItem>();
        foreach (LWorkSource lMessengerSource in lMessengerSources)
        {
            string lMessengerSourcePath = lMessengerSource.LWorkSourcePath;
            if (LAudio.LAudioPlanRead(lMessengerSourcePath) is not { LWorkAudioActive: true } lMessengerPlan)
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
                    Cadroue.Media.LSidecarStore.LSidecarDurationRead,
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

    public static async Task<int> LMessengerSplitAllDescribe(
        LWorkPriority lMessengerPriority,
        IReadOnlyList<LWorkSource> lMessengerSources,
        LPreset lMessengerPreset,
        Guid lMessengerRelayTarget = default,
        Guid lMessengerRelaySource = default,
        LRelayPlanRecord? lMessengerPreparedPlan = null)
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
            await Task.Run(() => LMessengerSplitPlanCreate(lMessengerSourcePaths)).ConfigureAwait(false);

        int lMessengerAdded = 0;
        LMessengerPost(() =>
        {
            foreach (LSplitPlanRecord lMessengerPlan in lMessengerPlans)
            {
                lMessengerRelays.TryGetValue(lMessengerPlan.LSplitSourcePath, out Guid lMessengerBatch);
                lMessengerAdded += LMessengerSplitDescribe(
                    lMessengerPriority, lMessengerPlan.LSplitSourcePath, lMessengerPlan.LSplitPlanSections,
                    lMessengerPreset, lMessengerRelayTarget, lMessengerRelaySource, lMessengerBatch, lMessengerPreparedPlan);
            }
        });
        return lMessengerAdded;
    }

    private static IReadOnlyList<LSplitPlanRecord> LMessengerSplitPlanCreate(IReadOnlyList<string> lMessengerSourcePaths)
    {
        var lMessengerPlans = new List<LSplitPlanRecord>();
        foreach (string lMessengerSourcePath in lMessengerSourcePaths)
        {
            IReadOnlyList<LSplitSectionDescription> lMessengerSections = LMessengerSplitPlanRead(lMessengerSourcePath);
            if (lMessengerSections.Count > 0)
            {
                lMessengerPlans.Add(new LSplitPlanRecord(lMessengerSourcePath, lMessengerSections));
            }
        }
        return lMessengerPlans;
    }

    private static IReadOnlyList<LSplitSectionDescription> LMessengerSplitPlanRead(string lMessengerSourcePath)
    {
        try
        {
            string lMessengerSidecarPath = Cadroue.Media.LSidecarStore.LSidecarPathRead(lMessengerSourcePath);
            if (Cadroue.Media.LSidecarStore.LSidecarRead(lMessengerSidecarPath) is not { } lMessengerSidecar)
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
        LPreset lMessengerPreset,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        Guid lMessengerBatchId)
    {
        LEditWorkDescription lMessengerDescription = new(
            lMessengerSourcePath, lMessengerDuration, lMessengerCrop, lMessengerVideo,
            lMessengerPreset.LPresetOutputCreate());
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
        LPreset lMessengerPreset,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        IReadOnlyDictionary<string, Guid>? lMessengerRelays)
    {
        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        IReadOnlyList<LWorkItem> lMessengerItems = Cadroue.Application.LMerge.LMergeItemsCreate(
            lMessengerPriority, lMessengerGroups, lMessengerPreset.LPresetOutputCreate(), lMessengerTab,
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
        LPreset lMessengerPreset,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource)
    {
        LEncoding lMessengerOutput = lMessengerPreset.LPresetOutputCreate();
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
                Cadroue.Media.LSidecarStore.LSidecarDurationRead);

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
                Cadroue.Media.LSidecarStore.LSidecarDurationResolve(lMessengerUnknown[lMessengerIndex].LWorkSourcePath)))
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

    public static async Task<int> LMessengerEditAllDescribe(
        LWorkPriority lMessengerPriority,
        IReadOnlyList<LWorkSource> lMessengerSources,
        LPreset lMessengerPreset,
        Guid lMessengerRelayTarget = default,
        Guid lMessengerRelaySource = default)
    {
        LEncoding lMessengerOutput = lMessengerPreset.LPresetOutputCreate();
        var lMessengerItems = new List<LWorkItem>();
        Guid lMessengerLooseBatch = Guid.NewGuid();

        foreach (LWorkSource lMessengerSource in lMessengerSources)
        {
            string lMessengerSourcePath = lMessengerSource.LWorkSourcePath;
            if (LEdit.LEditPlanRead(lMessengerSourcePath) is not { LEditPlanActive: true } lMessengerPlan)
            {
                continue;
            }

            Guid lMessengerBatch = lMessengerSource.LWorkSourceBatch != Guid.Empty
                ? lMessengerSource.LWorkSourceBatch
                : lMessengerLooseBatch;
            lMessengerItems.Add(Cadroue.Application.LEdit.LEditWorkCreate(
                lMessengerPriority,
                lMessengerSourcePath,
                Cadroue.Media.LSidecarStore.LSidecarDurationRead(lMessengerSourcePath),
                lMessengerPlan.LEditSkip ? LWorkCrop.LWorkCropCreate() : lMessengerPlan.LEditCrop,
                lMessengerPlan.LEditSkip ? LWorkVideo.LWorkVideoCreate() : lMessengerPlan.LEditVideo,
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
