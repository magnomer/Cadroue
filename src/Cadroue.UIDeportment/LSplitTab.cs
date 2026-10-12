using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LSplitTab
{
    public static readonly IReadOnlyList<LProcessingRow> LSplitRows =
    [
        LSplitRowCreate(LDetectorKind.LDetectorKindBlank, "PProcessingBlank", "Blank"),
        LSplitRowCreate(LDetectorKind.LDetectorKindScene, "PProcessingScene", "Scene"),
        LSplitRowCreate(LDetectorKind.LDetectorKindStill, "PProcessingStill", "Still"),
        LSplitRowCreate(LDetectorKind.LDetectorKindLuminance, "PProcessingLuminance", "Luminance"),
        LSplitRowCreate(LDetectorKind.LDetectorKindSilence, "PProcessingSilence", "Silence"),
        LSplitRowCreate(LDetectorKind.LDetectorKindVolume, "PProcessingReducedVolume", "Volume"),
    ];

    private readonly LPresetSelection lSplitPreset;
    private readonly LViewer lSplitViewer;
    private readonly LList lSplitList;
    private readonly LDocket lSplitDocket;
    private readonly LFlow lSplitFlow;

    public LSplitTab(LPresetSelection lPreset, LViewer lViewer, LList lList, LFlow lFlow)
    {
        lSplitPreset = lPreset;
        lSplitViewer = lViewer;
        lSplitList = lList;
        lSplitDocket = lList.LListDocket;
        lSplitFlow = lFlow;
        LSplitSweep = new LSplitTabSweep(LSplitInspector, lList, lSplitDocket, lFlow);
        lFlow.LFlowSectionSet(true);
        LSplitInspector.LInspectorSensor.LSensorChanged += LSplitChangeHandle;
        LSplitInspector.LInspectorBlank.LBlankChanged += LSplitChangeHandle;
        LSplitInspector.LInspectorSensor.LSensorPersistentChanged += LSplitPersistentHandle;
        LSplitInspector.LInspectorBlank.LBlankPickChanged += LSplitPickHandle;
        lViewer.LViewerNeutral.LViewerNeutralChanged += LSplitSampleHandle;
        LSplitSection = new LSection(lFlow);
        LSplitInspector.LInspectorSensor.LSensorRunApplied += () => _ = LSplitSweep.LSplitSweepStart();
        LSplitInspector.LInspectorSensor.LSensorStopApplied += LSplitSweep.LSplitSweepCancel;
        lList.LListPathChanged += LSplitPathHandle;
        lList.LListLockChanged += LSplitLockHandle;
        lViewer.LViewerSource.LViewerPathsDrop += lDropPaths => _ = lList.LListPathsAdd(lDropPaths);
    }

    public event Action? LSplitPresetMissing;

    public LInspector LSplitInspector { get; } = new();

    public LProcessing LSplitProcessing { get; } = new();

    public LSection LSplitSection { get; }

    public LSplitTabSweep LSplitSweep { get; }

    public void LSplitClose()
    {
        LSplitSweep.LSplitSweepCancel();
        LSplitInspector.LInspectorSensor.LSensorChanged -= LSplitChangeHandle;
        LSplitInspector.LInspectorBlank.LBlankChanged -= LSplitChangeHandle;
        LSplitInspector.LInspectorSensor.LSensorPersistentChanged -= LSplitPersistentHandle;
        LSplitInspector.LInspectorBlank.LBlankPickChanged -= LSplitPickHandle;
        lSplitViewer.LViewerNeutral.LViewerNeutralChanged -= LSplitSampleHandle;
    }

    public void LSplitStart() => lSplitFlow.QFlowEdit.LFlowEditSet(!LSplitLockedCheck());

    public LSceneTabEntry LSplitLayoutRead(LSceneTabEntry lLayout)
    {
        lLayout.LSceneDetectors = LDetectorBundle.LDetectorSceneFormat(LSplitStateRead());
        lLayout.LSceneDetectPersistent = LSplitInspector.LInspectorSensor.LSensorPersistent;
        return lLayout;
    }

    public void LSplitLayoutApply(LSceneTabEntry? lLayout)
    {
        if (lLayout is null)
        {
            LSplitProcessing.LProcessingMinimizedSet(true);
            LSplitInspector.LInspectorMinimizedSet(true);
            return;
        }

        LSplitStateApply(LDetectorBundle.LDetectorSceneParse(lLayout.LSceneDetectors));
        LSplitInspector.LInspectorSensor.LSensorPersistentSet(lLayout.LSceneDetectPersistent);
        LSplitActiveUpdate();
    }

    public void LSplitRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LSplitPresetCheck() || LSplitSelectedRead() is not { } lSelected)
        {
            return;
        }

        LMessenger.LMessengerSplitDescribe(
            lPriority,
            lSelected.LDocketEntryPath,
            lSplitFlow.LFlowSection.LFlowSplitRead(),
            lSplitPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab,
            lSelected.LDocketEntryBatch);
    }

    public void LSplitAllRun(Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LSplitPresetCheck())
        {
            return;
        }

        _ = LMessenger.LMessengerSplitDescribe(
            LWorkPriority.LWorkPriorityNormal,
            lSplitDocket.LDocketUnlockedRead().Select(LSplitSourceCreate).ToArray(),
            lSplitPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void LSplitItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LSplitPresetCheck())
        {
            return;
        }

        _ = LMessenger.LMessengerSplitDescribe(
            LWorkPriority.LWorkPriorityNormal,
            lSplitDocket.LDocketUnlockedRead()
                .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .Select(LSplitSourceCreate)
                .ToArray(),
            lSplitPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void LSplitPathHandle(string? lPath)
    {
        if (string.IsNullOrWhiteSpace(lPath) || lSplitViewer.LViewerSourceMatch(lPath))
        {
            return;
        }

        lSplitViewer.LViewerPathHandle(lPath);
        if (!LSplitInspector.LInspectorSensor.LSensorPersistent)
        {
            LSplitStateLoad(lPath);
        }
    }

    public void LSplitLockHandle(bool lLocked) => lSplitFlow.QFlowEdit.LFlowEditSet(!lLocked);

    public void LSplitPickHandle(bool lArmed) =>
        lSplitViewer.LViewerNeutral.LViewerToolSet(lArmed, LNeutralTarget.LNeutralTargetGrey);

    public void LSplitSampleHandle(LNeutralSample lSample) =>
        LSplitInspector.LInspectorBlank.LBlankSampleSet(
            lSample.LNeutralRed, lSample.LNeutralGreen, lSample.LNeutralBlue);

    public void LSplitChangeHandle()
    {
        LSplitActiveUpdate();
        LSplitStateSave();
    }

    public void LSplitPersistentHandle(bool lPersistent)
    {
        LSplitStateSave();
        if (lPersistent)
        {
            _ = LSplitSweep.LSplitSweepStart();
        }
    }

    public void LSplitActiveUpdate()
    {
        LSensor lSensor = LSplitInspector.LInspectorSensor;
        foreach (LDetectorKind lKind in LDetector.LDetectorKinds)
        {
            bool lActive = lKind == LDetectorKind.LDetectorKindBlank
                ? LSplitInspector.LInspectorBlank.LBlankStep.LDetectorBlankEnabled
                : lSensor.LSensorStepRead(lKind).LDetectorStepEnabled;
            LSplitProcessing.LProcessingActiveSet(LSensor.LSensorNameRead(lKind), lActive);
        }
    }

    public void LSplitStateSave()
    {
        if (LSplitInspector.LInspectorSaveSuspended
            || lSplitViewer.LViewerSourcePath is not { } lPath
            || lSplitDocket.LDocketLockCheck(lPath))
        {
            return;
        }

        LSidecarSplitEntry lRecord = LDetectorBundle.LDetectorSidecarFormat(LSplitStateRead());
        if (!lRecord.LSidecarSplitActive && LLibrarian.LLibrarianSplitLoad(lPath) is null)
        {
            return;
        }

        LLibrarian.LLibrarianSplitSave(lPath, lRecord);
    }

    public void LSplitStateLoad(string lPath)
    {
        if (LLibrarian.LLibrarianSplitLoad(lPath) is not { } lRecord)
        {
            return;
        }

        LSplitStateApply(LDetectorBundle.LDetectorSidecarParse(lRecord));
        LSplitActiveUpdate();
    }

    public LDetectorBundle LSplitStateRead()
    {
        LSensor lSensor = LSplitInspector.LInspectorSensor;
        IReadOnlyList<LDetectorStep> lSteps = LDetector.LDetectorKinds
            .Where(lKind => lKind != LDetectorKind.LDetectorKindBlank)
            .Select(lSensor.LSensorStepRead)
            .ToArray();
        Dictionary<LDetectorKind, string> lPresets = lSteps.ToDictionary(
            lStep => lStep.LDetectorStepKind,
            lStep => lSensor.LSensorTokenRead(lStep.LDetectorStepKind) ?? string.Empty);
        return new LDetectorBundle(
            lSteps,
            LSplitInspector.LInspectorBlank.LBlankStep,
            lSensor.LSensorMode,
            lSensor.LSensorSpeed,
            lSensor.LSensorMetric,
            lPresets);
    }

    private void LSplitStateApply(LDetectorBundle lSet)
    {
        LSensor lSensor = LSplitInspector.LInspectorSensor;
        LSplitInspector.LInspectorSaveSuspend();
        try
        {
            if (lSet.LDetectorSetBlank is { } lBlank)
            {
                LSplitInspector.LInspectorBlank.LBlankStepSet(lBlank);
            }

            foreach (LDetectorStep lStep in lSet.LDetectorSetSteps)
            {
                lSensor.LSensorStepSet(lStep);
            }

            if (lSet.LDetectorSetStill is { } lStill)
            {
                lSensor.LSensorModeSet(lStill);
            }

            if (lSet.LDetectorSetLuminance is { } lLuminance)
            {
                lSensor.LSensorSpeedSet(lLuminance);
            }

            if (lSet.LDetectorSetMetric is { } lMetric)
            {
                lSensor.LSensorMetricSet(lMetric);
            }

            foreach (KeyValuePair<LDetectorKind, string> lPreset in lSet.LDetectorSetPresets)
            {
                lSensor.LSensorTokenSet(lPreset.Key, lPreset.Value);
            }
        }
        finally
        {
            LSplitInspector.LInspectorSaveResume();
        }
    }

    private bool LSplitLockedCheck() =>
        lSplitList.LListPathCurrent is { } lPath && lSplitDocket.LDocketLockCheck(lPath);

    private bool LSplitPresetCheck()
    {
        if (lSplitPreset.LPresetSelectionValid)
        {
            return true;
        }

        LSplitPresetMissing?.Invoke();
        return false;
    }

    private LDocketEntry? LSplitSelectedRead() =>
        lSplitList.LListPathCurrent is { } lPath
        && lSplitDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    private static LProcessingRow LSplitRowCreate(LDetectorKind lKind, string lIcon, string lLabel) =>
        new(LSensor.LSensorNameRead(lKind), $"/PAsset/PPanel/{lIcon}.svg", $"Processing.Step.{lLabel}");

    private static LWorkSource LSplitSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);
}
