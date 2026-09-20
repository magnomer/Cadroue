using Cadroue.Application;
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

    private readonly LPresetSelection lAudioPreset;
    private readonly LInspector lAudioInspector;
    private readonly LInspectorAudio lAudioSections;
    private readonly LViewer lAudioViewer;
    private readonly LList lAudioList;
    private readonly LDocket lAudioDocket;
    private readonly LProcessing lAudioProcessing;

    public LAudioTab(
        LPresetSelection lPreset,
        LInspector lInspector,
        LViewer lViewer,
        LList lList,
        LDocket lDocket,
        LProcessing lProcessing)
    {
        lAudioPreset = lPreset;
        lAudioInspector = lInspector;
        lAudioSections = lInspector.LInspectorAudio;
        lInspector.LInspectorPersistentChange += LAudioPersistentSave;
        lAudioSections.LInspectorAudioChange += LAudioChangeHandle;
        lInspector.LInspectorSkip.LSkipActiveChange += LAudioSkipHandle;
        lAudioViewer = lViewer;
        lAudioList = lList;
        lAudioDocket = lDocket;
        lAudioProcessing = lProcessing;
        lProcessing.LProcessingOrderedSet(true);
        lProcessing.LProcessingOrderChange += LAudioStateSave;
        lViewer.LViewerMediaChange += LAudioMediaHandle;
    }

    public event Action? LAudioPresetMissing;
    public event Action? LAudioPresetIncompatible;
    public event Action? LAudioViewerDefer;

    public LSMonitor LAudioMonitor { get; } = new();

    public static LAudioKind? LAudioKindRead(string lStep) =>
        lAudioKinds.TryGetValue(lStep, out LAudioKind lKind) ? lKind : null;

    public void LAudioClose()
    {
        lAudioInspector.LInspectorPersistentChange -= LAudioPersistentSave;
        lAudioSections.LInspectorAudioChange -= LAudioChangeHandle;
        lAudioInspector.LInspectorSkip.LSkipActiveChange -= LAudioSkipHandle;
        lAudioProcessing.LProcessingOrderChange -= LAudioStateSave;
        lAudioViewer.LViewerMediaChange -= LAudioMediaHandle;
        LAudioMonitor.Dispose();
    }

    public LSceneTabRecord LAudioLayoutRead(LSceneTabRecord lLayout)
    {
        if (lAudioSections.LInspectorPersistentCheck())
        {
            lLayout.LSceneInspector = new LSceneInspectorRecord
            {
                LSceneInspectorAudio = LAudio.LAudioPersistentCreate(lAudioSections.LInspectorPersistentRead()),
                LSceneInspectorSkip = lAudioInspector.LInspectorSkip.LSkipPersistent
            };
        }

        return lLayout;
    }

    public void LAudioLayoutApply(LSceneTabRecord? lLayout)
    {
        if (lLayout?.LSceneInspector is { LSceneInspectorAudio: { } lRecord } lInspector)
        {
            lAudioInspector.LInspectorSaveSuspend();
            try
            {
                LWorkAudio lPlan = LAudio.LAudioPersistentRead(lRecord);
                lAudioSections.LInspectorAudioApply(lPlan);
                lAudioSections.LInspectorPersistentApply(lPlan, lInspector.LSceneInspectorSkip);
                lAudioInspector.LInspectorSkip.LSkipActiveSet(lPlan.LWorkAudioSkip);
            }
            finally
            {
                lAudioInspector.LInspectorSaveResume();
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
        lAudioProcessing.LProcessingSkipSet(lAudioInspector.LInspectorSkip.LSkipActive);
        LAudioStateSave();
    }

    public void LAudioChangeHandle()
    {
        LAudioActiveUpdate();
        LAudioStateSave();
        LAudioMonitor.LSMonitorPlanApply(LAudioPlanRead());
        LAudioViewerDefer?.Invoke();
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
            lAudioProcessing.LProcessingSteps.Select(LAudioKindRead).OfType<LAudioKind>());

    public void LAudioStateSave()
    {
        if (lAudioInspector.LInspectorSaveSuspended
            || lAudioInspector.LInspectorOwnerPath is not { } lSourcePath
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
            lAudioInspector.LInspectorFailureSet(null);
        }
        else if (lAudioInspector.LInspectorFailureSet(lSourcePath))
        {
            LTraceLog.LTraceWarningRecord(
                $"Audio edit not saved for '{LUsher.LUsherNameRead(lSourcePath)}': the sidecar could not be written",
                lSourcePath);
        }

        LAudioPersistentSave();
    }

    public void LAudioPlanRestore(string lSourcePath, bool lOwnerFirst)
    {
        bool lAdopted = false;
        lAudioInspector.LInspectorSaveSuspend();
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
                lAudioInspector.LInspectorSkip.LSkipPersistent,
                lAudioInspector.LInspectorSkip.LSkipActive);
            lAudioSections.LInspectorAudioApply(lResolved);
            lAudioInspector.LInspectorSkip.LSkipActiveSet(lResolved.LWorkAudioSkip);
        }
        finally
        {
            lAudioInspector.LInspectorSaveResume();
        }

        lAudioProcessing.LProcessingSkipSet(lAudioInspector.LInspectorSkip.LSkipActive);
        LAudioActiveUpdate();
        LAudioViewerApply();
        if (lAdopted)
        {
            LAudioStateSave();
        }
    }

    public void LAudioActiveUpdate()
    {
        foreach (string lStep in lAudioProcessing.LProcessingSteps)
        {
            if (LAudioKindRead(lStep) is { } lKind)
            {
                lAudioProcessing.LProcessingActiveSet(lStep, lAudioSections.LInspectorStepRead(lKind).LWorkStepActive);
            }
        }
    }

    private void LAudioMediaHandle(LCargo lCargo)
    {
        bool lOwnerFirst = lAudioInspector.LInspectorOwnerSet(lCargo.LCargoSourcePath);
        LAudioPlanRestore(lCargo.LCargoSourcePath, lOwnerFirst);
        LAudioMonitor.LSMonitorSourceOpen(
            lCargo.LCargoSourcePath,
            lCargo.LCargoMediaInfo?.LMediaInfoDuration ?? TimeSpan.Zero,
            LAudioRateRead());
        LAudioMonitor.LSMonitorPlanApply(LAudioPlanRead());
    }

    private void LAudioFanoutSave(IEnumerable<string> lPaths)
    {
        if (lAudioInspector.LInspectorSaveSuspended || !lAudioSections.LInspectorPersistentCheck())
        {
            return;
        }

        LWorkAudio lPersistent = lAudioSections.LInspectorPersistentRead();
        bool lSkipPersistent = lAudioInspector.LInspectorSkip.LSkipPersistent;
        bool lSkipApply = lAudioInspector.LInspectorSkip.LSkipActive;
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
