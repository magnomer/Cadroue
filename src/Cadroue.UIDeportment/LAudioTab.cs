using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LAudioTab
{
    public static readonly IReadOnlyList<LProcessingRow> LAudioRows =
    [
        new("High Pass", "/PAsset/PPanel/PProcessingHighPass.svg", "Processing.Step.HighPass"),
        new("Low Pass", "/PAsset/PPanel/PProcessingLowPass.svg", "Processing.Step.LowPass"),
        new("Noise Reduction", "/PAsset/PPanel/PProcessingNoiseReduction.svg", "Processing.Step.NoiseReduction"),
        new("Equalizer", "/PAsset/PPanel/PProcessingEqualizer.svg", "Processing.Step.Equalizer"),
        new("Volume", "/PAsset/PPanel/PProcessingVolume.svg", "Processing.Step.Volume"),
        new("Normalize", "/PAsset/PPanel/PProcessingNormalize.svg", "Processing.Step.Normalize"),
    ];

    private static readonly IReadOnlyDictionary<string, LAudioKind> lAudioKinds = new Dictionary<string, LAudioKind>
    {
        ["Volume"] = LAudioKind.LAudioKindVolume,
        ["Normalize"] = LAudioKind.LAudioKindLeveling,
        ["Noise Reduction"] = LAudioKind.LAudioKindDenoise,
        ["High Pass"] = LAudioKind.LAudioKindHighpass,
        ["Low Pass"] = LAudioKind.LAudioKindLowpass,
        ["Equalizer"] = LAudioKind.LAudioKindEqualizer,
    };

    private readonly CUsher lAudioUsher;
    private readonly LPresetSelection lAudioPreset;
    private readonly LInspectorAudio lAudioSections;
    private readonly LViewer lAudioViewer;
    private readonly LList lAudioList;
    private readonly LDocket lAudioDocket;

    public LAudioTab(CUsher lUsher, LPresetSelection lPreset, LViewer lViewer, LList lList)
    {
        lAudioUsher = lUsher;
        lAudioPreset = lPreset;
        lAudioSections = LAudioInspector.LInspectorAudio;
        LAudioInspector.LInspectorPersistentChanged += LAudioPersistentSave;
        lAudioSections.LInspectorAudioChanged += LAudioChangeHandle;
        LAudioInspector.LInspectorSkip.LSkipActiveChanged += LAudioSkipHandle;
        lAudioViewer = lViewer;
        lAudioList = lList;
        lAudioDocket = lList.LListDocket;
        LAudioProcessing.LProcessingOrderedSet(true);
        LAudioProcessing.LProcessingOrderChanged += LAudioStateSave;
        lViewer.LViewerMediaChanged += LAudioMediaHandle;
        lList.LListPathChanged += LAudioPathHandle;
        lList.LListItemsAdded += LAudioItemsHandle;
        lViewer.LViewerSource.LViewerPathsDrop += lDropPaths => _ = lList.LListPathsAdd(lDropPaths);
    }

    public event Action? LAudioPresetMissing;
    public event Action? LAudioPresetIncompatible;
    public event Action? LAudioViewerDeferred;

    public LInspector LAudioInspector { get; } = new();

    public LProcessing LAudioProcessing { get; } = new();

    public LSMonitor LAudioMonitor { get; } = new();

    public static LAudioKind? LAudioKindRead(string lStep) =>
        lAudioKinds.TryGetValue(lStep, out LAudioKind lKind) ? lKind : null;

    public void LAudioClose()
    {
        LAudioInspector.LInspectorPersistentChanged -= LAudioPersistentSave;
        lAudioSections.LInspectorAudioChanged -= LAudioChangeHandle;
        LAudioInspector.LInspectorSkip.LSkipActiveChanged -= LAudioSkipHandle;
        LAudioProcessing.LProcessingOrderChanged -= LAudioStateSave;
        lAudioViewer.LViewerMediaChanged -= LAudioMediaHandle;
        LAudioMonitor.Dispose();
    }

    public LSceneTabEntry LAudioLayoutRead(LSceneTabEntry lLayout)
    {
        if (lAudioSections.LInspectorPersistentCheck())
        {
            lLayout.LSceneInspector = new LSceneInspectorEntry
            {
                LSceneInspectorAudio = LAudio.LAudioPersistentCreate(lAudioSections.LInspectorPersistentRead()),
                LSceneInspectorSkip = LAudioInspector.LInspectorSkip.LSkipPersistent
            };
        }

        return lLayout;
    }

    public void LAudioLayoutApply(LSceneTabEntry? lLayout)
    {
        if (lLayout?.LSceneInspector is { LSceneInspectorAudio: { } lRecord } lInspector)
        {
            LAudioInspector.LInspectorSaveSuspend();
            try
            {
                LWorkAudio lPlan = LAudio.LAudioPersistentRead(lRecord);
                lAudioSections.LInspectorAudioApply(lPlan);
                lAudioSections.LInspectorPersistentApply(lPlan, lInspector.LSceneInspectorSkip);
                LAudioInspector.LInspectorSkip.LSkipActiveSet(lPlan.LWorkAudioSkip);
            }
            finally
            {
                LAudioInspector.LInspectorSaveResume();
            }
        }

        LAudioActiveUpdate();
    }

    public void LAudioRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LAudioPresetCheck() || LAudioSelectedRead() is not { } lSelected)
        {
            return;
        }

        LAudioStateSave();
        LMessenger.LMessengerAudioDescribe(
            lPriority,
            lSelected.LDocketEntryPath,
            LAudioPlanRead(),
            lAudioPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab,
            lSelected.LDocketEntryBatch);
    }

    public void LAudioAllRun(Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LAudioPresetCheck())
        {
            return;
        }

        LAudioStateSave();
        LMessenger.LMessengerAudioDescribe(
            LWorkPriority.LWorkPriorityNormal,
            lAudioDocket.LDocketUnlockedRead().Select(LAudioSourceCreate).ToArray(),
            lAudioPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void LAudioItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LAudioPresetCheck())
        {
            return;
        }

        LAudioStateSave();
        LMessenger.LMessengerAudioDescribe(
            LWorkPriority.LWorkPriorityNormal,
            lAudioDocket.LDocketUnlockedRead()
                .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .Select(LAudioSourceCreate)
                .ToArray(),
            lAudioPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void LAudioPathHandle(string? lPath)
    {
        if (string.IsNullOrWhiteSpace(lPath) || lAudioViewer.LViewerSourceMatch(lPath))
        {
            return;
        }

        LAudioStateSave();
        lAudioViewer.LViewerPathHandle(lPath);
    }

    public void LAudioSkipHandle()
    {
        LAudioProcessing.LProcessingSkipSet(LAudioInspector.LInspectorSkip.LSkipActive);
        LAudioStateSave();
    }

    public void LAudioChangeHandle()
    {
        LAudioActiveUpdate();
        LAudioStateSave();
        LAudioMonitor.LSMonitorPlanApply(LAudioPlanRead());
        LAudioViewerDeferred?.Invoke();
    }

    public void LAudioItemsHandle(IReadOnlyList<LDocketEntry> lAdded) =>
        LAudioFanoutSave(lAdded.Select(lItem => lItem.LDocketEntryPath));

    public void LAudioPersistentSave() =>
        LAudioFanoutSave(lAudioDocket.LDocketUnlockedRead().Select(lItem => lItem.LDocketEntryPath));

    public void LAudioViewerApply()
    {
        LWorkAudio lPlan = LAudioPlanRead();
        lAudioViewer.LViewerGraphSet(lPlan.LWorkAudioSkip ? string.Empty : lPlan.LWorkAudioFormat(LAudioRateRead()));
    }

    public LWorkAudio LAudioPlanRead() =>
        lAudioSections.LInspectorAudioRead(
            LAudioProcessing.LProcessingSteps.Select(LAudioKindRead).OfType<LAudioKind>());

    public void LAudioStateSave()
    {
        if (LAudioInspector.LInspectorSaveSuspended
            || LAudioInspector.LInspectorOwnerPath is not { } lSourcePath
            || lAudioDocket.LDocketLockCheck(lSourcePath))
        {
            return;
        }

        LWorkAudio lPlan = LAudioPlanRead();
        if (!lPlan.LWorkAudioActive && LAudio.LAudioPlanRead(lSourcePath, LLibrarian.LLibrarianAudioLoad) is null)
        {
            return;
        }

        if (LAudio.LAudioPlanSave(lSourcePath, lPlan, LLibrarian.LLibrarianAudioSave))
        {
            LAudioInspector.LInspectorFailureSet(null);
        }
        else if (LAudioInspector.LInspectorFailureSet(lSourcePath))
        {
            LTraceLog.LTraceWarningRecord(
                $"Audio edit not saved for '{
                    lAudioUsher.CUsherNameRead(lSourcePath)}': the sidecar could not be written",
                lSourcePath);
        }

        LAudioPersistentSave();
    }

    public void LAudioPlanRestore(string lSourcePath, bool lOwnerFirst)
    {
        bool lAdopted = false;
        LAudioInspector.LInspectorSaveSuspend();
        try
        {
            LWorkAudio? lSaved = LAudio.LAudioPlanRead(lSourcePath, LLibrarian.LLibrarianAudioLoad);
            LWorkAudio? lPersistent = lAudioSections.LInspectorPersistentCheck()
                ? lAudioSections.LInspectorPersistentRead()
                : null;
            if (lSaved is null && lPersistent is null && lOwnerFirst
                && LAudioPlanRead() is { LWorkAudioActive: true } lPending)
            {
                lSaved = lPending;
                lAdopted = true;
            }

            LWorkAudio lResolved = LAudio.LAudioPlanResolve(
                lSaved,
                lPersistent,
                LAudioInspector.LInspectorSkip.LSkipPersistent,
                LAudioInspector.LInspectorSkip.LSkipActive);
            lAudioSections.LInspectorAudioApply(lResolved);
            LAudioInspector.LInspectorSkip.LSkipActiveSet(lResolved.LWorkAudioSkip);
        }
        finally
        {
            LAudioInspector.LInspectorSaveResume();
        }

        LAudioProcessing.LProcessingSkipSet(LAudioInspector.LInspectorSkip.LSkipActive);
        LAudioActiveUpdate();
        LAudioViewerApply();
        if (lAdopted)
        {
            LAudioStateSave();
        }
    }

    public void LAudioActiveUpdate()
    {
        foreach (string lStep in LAudioProcessing.LProcessingSteps)
        {
            if (LAudioKindRead(lStep) is { } lKind)
            {
                LAudioProcessing.LProcessingActiveSet(lStep, lAudioSections.LInspectorStepRead(lKind).LWorkStepActive);
            }
        }
    }

    private void LAudioMediaHandle(LCargo lCargo)
    {
        bool lOwnerFirst = LAudioInspector.LInspectorOwnerSet(lCargo.LCargoSourcePath);
        LAudioPlanRestore(lCargo.LCargoSourcePath, lOwnerFirst);
        LAudioMonitor.LSMonitorSourceOpen(
            lCargo.LCargoSourcePath,
            lCargo.LCargoMediaInfo?.LMediaInfoDuration ?? TimeSpan.Zero,
            LAudioRateRead());
        LAudioMonitor.LSMonitorPlanApply(LAudioPlanRead());
    }

    private void LAudioFanoutSave(IEnumerable<string> lPaths)
    {
        if (LAudioInspector.LInspectorSaveSuspended || !lAudioSections.LInspectorPersistentCheck())
        {
            return;
        }

        LWorkAudio lPersistent = lAudioSections.LInspectorPersistentRead();
        bool lSkipPersistent = LAudioInspector.LInspectorSkip.LSkipPersistent;
        bool lSkipApply = LAudioInspector.LInspectorSkip.LSkipActive;
        var lFailed = new List<string>();
        foreach (string lPath in lPaths)
        {
            bool lStored = LAudio.LAudioPlanSave(
                lPath,
                LAudio.LAudioPlanResolve(
                    LAudio.LAudioPlanRead(lPath, LLibrarian.LLibrarianAudioLoad),
                    lPersistent,
                    lSkipPersistent,
                    lSkipApply),
                LLibrarian.LLibrarianAudioSave);
            if (!lStored)
            {
                lFailed.Add(lPath);
            }
        }

        if (lFailed.Count > 0)
        {
            LTraceLog.LTraceWarningRecord(
                $"Audio persistent save failed for {lFailed.Count} file(s): those sidecars were not written",
                string.Join(Environment.NewLine, lFailed));
        }
    }

    private int LAudioRateRead() => lAudioViewer.LViewerMediaInfo?.LMediaSampleRate ?? 0;

    private bool LAudioPresetCheck()
    {
        if (!lAudioPreset.LPresetSelectionValid)
        {
            LAudioPresetMissing?.Invoke();
            return false;
        }

        if (lAudioPreset.LPresetSelectionEncoding is { } lEncoding
            && !lEncoding.LEncodingSupportCheck(LWorkKind.LWorkKindAudio))
        {
            LAudioPresetIncompatible?.Invoke();
            return false;
        }

        return true;
    }

    private LDocketEntry? LAudioSelectedRead() =>
        lAudioList.LListPathCurrent is { } lPath
        && lAudioDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    private static LWorkSource LAudioSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);
}
