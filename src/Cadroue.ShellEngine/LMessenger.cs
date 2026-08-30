using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static class LMessenger
{
    public static Func<Guid, string>? LMessengerTitleSource { get; set; }

    public static Func<IReadOnlyList<LWorkItem>, Guid, Guid, LCartographerPlanRecord?, int>? LMessengerRouteSource { get; set; }

    public static Func<LScheduleContract?>? LMessengerScheduleSource { get; set; }

    public static Func<Guid, string, Guid, bool>? LMessengerDeliverSource { get; set; }

    public static Action<IReadOnlyList<string>>? LMessengerDrainSource { get; set; }

    private static string LMessengerTitleRead(Guid lMessengerRelaySource) =>
        LMessengerTitleSource?.Invoke(lMessengerRelaySource) ?? string.Empty;

    private static int LMessengerDispatch(
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

        int lMessengerAdded = LMessengerDispatch(new[] { lMessengerItem }, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Audio queued {lMessengerAdded} job at {lMessengerPriority} from " +
            $"'{System.IO.Path.GetFileName(lMessengerSourcePath)}'");
        await LMessengerSourceResolve(new[] { lMessengerItem }).ConfigureAwait(false);
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
        int lMessengerAdded = LMessengerDispatch(
            lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource, lMessengerPlan);
        LTraceLog.LTraceInfoRecord(
            $"Split queued {lMessengerAdded} of {lMessengerItems.Count} job(s) at {lMessengerPriority} " +
            $"from '{System.IO.Path.GetFileName(lMessengerSourcePath)}'");
        _ = LMessengerSourceResolve(lMessengerItems);
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

        int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        await LMessengerSourceResolve(lMessengerItems).ConfigureAwait(false);
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

        int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord($"Merge queued {lMessengerAdded} group(s) at {lMessengerPriority}");
        _ = LMessengerSourceResolve(lMessengerItems);
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

        int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Convert queued {lMessengerAdded} job(s) at {lMessengerPriority} from {lMessengerSourcePaths.Length} listed file(s)");

        await LMessengerSourceResolve(lMessengerItems).ConfigureAwait(false);
        return lMessengerAdded;
    }

    // Source figures are measured on a single low-priority background worker, one item at a
    // time, so measurement never competes with running jobs or bursts in parallel. Each added
    // item is queued; the worker measures its sources — reusing a cached result whenever the
    // file is unchanged — then stores the figures on the item, turning its "Measuring" rows
    // into values. Until an item is reached the worklist shows "Measuring", never "Unknown".
    private static readonly System.Collections.Concurrent.ConcurrentQueue<LWorkItem> lMessengerMeasureQueue = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, LMessengerSample> lMessengerMeasureCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly object lMessengerMeasureGate = new();
    private static bool lMessengerMeasureBusy;

    private static Task LMessengerSourceResolve(IReadOnlyList<LWorkItem> lMessengerItems)
    {
        foreach (LWorkItem lMessengerItem in lMessengerItems)
        {
            lMessengerMeasureQueue.Enqueue(lMessengerItem);
        }

        LMessengerMeasureStart();
        return Task.CompletedTask;
    }

    private static void LMessengerMeasureStart()
    {
        lock (lMessengerMeasureGate)
        {
            if (lMessengerMeasureBusy || lMessengerMeasureQueue.IsEmpty)
            {
                return;
            }

            lMessengerMeasureBusy = true;
        }

        var lMessengerThread = new System.Threading.Thread(LMessengerMeasureRun)
        {
            IsBackground = true,
            Priority = System.Threading.ThreadPriority.Lowest,
            Name = "Cadroue source measure"
        };
        lMessengerThread.Start();
    }

    private static void LMessengerMeasureRun()
    {
        try
        {
            while (lMessengerMeasureQueue.TryDequeue(out LWorkItem? lMessengerItem))
            {
                LMessengerItemResolve(lMessengerItem);
            }
        }
        finally
        {
            lock (lMessengerMeasureGate)
            {
                lMessengerMeasureBusy = false;
            }

            // An item queued between the queue draining and the flag clearing would otherwise
            // wait for the next add; restart the worker to pick it up.
            if (!lMessengerMeasureQueue.IsEmpty)
            {
                LMessengerMeasureStart();
            }
        }
    }

    private static void LMessengerItemResolve(LWorkItem lMessengerItem)
    {
        bool lMessengerMerge = lMessengerItem.LWorkMergeSources.Count > 1;
        LWorkMedia? lMessengerSourceMedia = null;
        long? lMessengerSourceBytes = null;
        var lMessengerMergeBytes = new List<long>();
        TimeSpan lMessengerMeasured = TimeSpan.Zero;

        foreach (string lMessengerSource in LMessengerSourcesRead(lMessengerItem))
        {
            LMessengerSample lMessengerSample = LMessengerSampleRead(lMessengerSource);
            if (lMessengerMerge)
            {
                lMessengerMergeBytes.Add(lMessengerSample.LMessengerBytes ?? 0);
            }
            else
            {
                lMessengerSourceMedia = lMessengerSample.LMessengerMedia;
                lMessengerSourceBytes = lMessengerSample.LMessengerBytes;
            }

            if (lMessengerSample.LMessengerMedia is { } lMessengerMedia)
            {
                lMessengerMeasured += lMessengerMedia.LWorkMediaDuration;
            }
        }

        if (LMessengerScheduleSource?.Invoke() is not { } lMessengerSchedule)
        {
            return;
        }

        TimeSpan lMessengerDuration = lMessengerItem.LWorkEnd > TimeSpan.Zero
            ? lMessengerItem.LWorkEnd
            : lMessengerMeasured;
        LMessengerDefer(() => lMessengerSchedule.LScheduleSourceSet(
            lMessengerItem.LWorkId,
            lMessengerDuration,
            lMessengerMerge ? null : lMessengerSourceMedia,
            lMessengerMerge ? null : lMessengerSourceBytes,
            lMessengerMerge ? lMessengerMergeBytes : Array.Empty<long>()));
    }

    // A measured source is reused whenever the file is unchanged (same path, length, and
    // write time); only a new or changed file is measured afresh.
    private static LMessengerSample LMessengerSampleRead(string lMessengerSource)
    {
        if (string.IsNullOrWhiteSpace(lMessengerSource))
        {
            return LMessengerSample.LMessengerEmpty;
        }

        string? lMessengerKey = LMessengerKeyRead(lMessengerSource);
        if (lMessengerKey is not null && lMessengerMeasureCache.TryGetValue(lMessengerKey, out LMessengerSample? lMessengerCached))
        {
            return lMessengerCached;
        }

        var lMessengerSample = new LMessengerSample(
            LScout.LScoutSourceRead(lMessengerSource), LScout.LScoutBytesRead(lMessengerSource));
        if (lMessengerKey is not null)
        {
            lMessengerMeasureCache[lMessengerKey] = lMessengerSample;
        }

        return lMessengerSample;
    }

    private static string? LMessengerKeyRead(string lMessengerSource)
    {
        try
        {
            var lMessengerInfo = new System.IO.FileInfo(lMessengerSource);
            if (!lMessengerInfo.Exists)
            {
                return null;
            }

            return string.Join(
                "|",
                System.IO.Path.GetFullPath(lMessengerSource).ToUpperInvariant(),
                lMessengerInfo.Length,
                lMessengerInfo.LastWriteTimeUtc.Ticks);
        }
        catch (Exception lMessengerException)
            when (lMessengerException is System.IO.IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    private static IEnumerable<string> LMessengerSourcesRead(LWorkItem lMessengerItem) =>
        lMessengerItem.LWorkMergeSources.Count > 1
            ? lMessengerItem.LWorkMergeSources
            : new[] { lMessengerItem.LWorkSourcePath };

    private sealed record LMessengerSample(LWorkMedia? LMessengerMedia, long? LMessengerBytes)
    {
        public static readonly LMessengerSample LMessengerEmpty = new(null, null);
    }

    private static void LMessengerDefer(Action lMessengerAction)
    {
        if (Cadroue.ShellEngine.LStation.LStationPost is { } lMessengerPost)
        {
            lMessengerPost(lMessengerAction);
            return;
        }

        lMessengerAction();
    }

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

            if (LMessengerDeliverSource?.Invoke(
                    lMessengerTargets[lMessengerMatch], lMessengerPath, lMessengerCohort) == true)
            {
                lMessengerRelayed.Add(lMessengerPath);
            }
        }

        if (Cadroue.Application.LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty
            && lMessengerRelayed.Count > 0)
        {
            LMessengerDrainSource?.Invoke(lMessengerRelayed);
        }

        LSeal.LSealRun();
        LTraceLog.LTraceInfoRecord(
            $"Funnel relayed {lMessengerRelayed.Count} of {lMessengerItems.Count} file(s) by filename rule");
        return lMessengerRelayed.Count;
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
            lMessengerItems.Add(Cadroue.Application.LEdit.LEditWorkCreate(
                lMessengerPriority,
                lMessengerSourcePath,
                Cadroue.Application.LLibrarian.LLibrarianDurationRead(lMessengerSourcePath),
                lMessengerPlan.LEditSkip ? LWorkCrop.LWorkCropCreate() : lMessengerPlan.LEditCrop,
                lMessengerPlan.LEditSkip
                    ? LWorkVideo.LWorkVideoCreate()
                    : Cadroue.Application.LEdit.LEditVideoCreate(
                        lMessengerPlan.LEditVideo.LWorkVideoSteps, true, lMessengerEqCapable),
                lMessengerOutput,
                lMessengerBatch));
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

    public static async Task<int> LMessengerFixDescribe(
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

        string[] lMessengerSourcePaths = lMessengerSources
            .Select(lMessengerSource => lMessengerSource.LWorkSourcePath)
            .ToArray();
        var lMessengerRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var lMessengerPlans = new Dictionary<string, LWorkFix>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkSource lMessengerSource in lMessengerSources)
        {
            lMessengerRelays[lMessengerSource.LWorkSourcePath] = lMessengerSource.LWorkSourceBatch;
            if (Cadroue.Application.LFix.LFixPlanRead(
                    lMessengerSource.LWorkSourcePath,
                    Cadroue.Application.LLibrarian.LLibrarianFixLoad) is { } lMessengerPlan)
            {
                lMessengerPlans[lMessengerSource.LWorkSourcePath] = lMessengerPlan;
            }
        }

        LFixWorkDescription lMessengerDescription =
            new(lMessengerSourcePaths, lMessengerOutput, null, lMessengerRelays, lMessengerPlans);

        string lMessengerTab = LMessengerTitleRead(lMessengerRelaySource);
        IReadOnlyList<LWorkItem> lMessengerItems =
            Cadroue.Application.LFix.LFixItemsCreate(
                lMessengerPriority, lMessengerDescription, lMessengerTab,
                lMessengerMessage => LTraceLog.LTraceErrorRecord(lMessengerMessage),
                Cadroue.Application.LLibrarian.LLibrarianDurationRead);

        int lMessengerAdded = LMessengerDispatch(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Fix queued {lMessengerAdded} job(s) at {lMessengerPriority} from {lMessengerSourcePaths.Length} listed file(s)");

        await LMessengerSourceResolve(lMessengerItems).ConfigureAwait(false);
        return lMessengerAdded;
    }
}
