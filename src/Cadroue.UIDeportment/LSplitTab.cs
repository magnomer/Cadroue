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
    private readonly LInspector lSplitInspector;
    private readonly LViewer lSplitViewer;
    private readonly LList lSplitList;
    private readonly LDocket lSplitDocket;
    private readonly LProcessing lSplitProcessing;
    private readonly LFlow lSplitFlow;

    public LSplitTab(
        LPresetSelection lPreset,
        LInspector lInspector,
        LViewer lViewer,
        LList lList,
        LDocket lDocket,
        LProcessing lProcessing,
        LFlow lFlow)
    {
        lSplitPreset = lPreset;
        lSplitInspector = lInspector;
        lSplitViewer = lViewer;
        lSplitList = lList;
        lSplitDocket = lDocket;
        lSplitProcessing = lProcessing;
        lSplitFlow = lFlow;
        LSplitSweep = new LSplitTabSweep(lInspector, lList, lDocket, lFlow);
        lFlow.LFlowSectionSet(true);
        lInspector.LInspectorSensor.LSensorChange += LSplitChangeHandle;
        lInspector.LInspectorBlank.LBlankChange += LSplitChangeHandle;
        lInspector.LInspectorSensor.LSensorPersistentChange += LSplitPersistentHandle;
    }

    public event Action? LSplitPresetMissing;

    public LSplitTabSweep LSplitSweep { get; }

    public void LSplitClose()
    {
        LSplitSweep.LSplitSweepCancel();
        lSplitInspector.LInspectorSensor.LSensorChange -= LSplitChangeHandle;
        lSplitInspector.LInspectorBlank.LBlankChange -= LSplitChangeHandle;
        lSplitInspector.LInspectorSensor.LSensorPersistentChange -= LSplitPersistentHandle;
    }

    public void LSplitStart() => lSplitFlow.LFlowEditSet(!LSplitLockedCheck());

    public LSceneTabRecord LSplitLayoutRead(LSceneTabRecord lLayout)
    {
        lLayout.LSceneDetectors = LDetectorSet.LDetectorSceneFormat(LSplitStateRead());
        lLayout.LSceneDetectPersistent = lSplitInspector.LInspectorSensor.LSensorPersistent;
        return lLayout;
    }

    public void LSplitLayoutApply(LSceneTabRecord? lLayout)
    {
        if (lLayout is null)
        {
            lSplitProcessing.LProcessingMinimizedSet(true);
            lSplitInspector.LInspectorMinimizedSet(true);
            return;
        }

        LSplitStateApply(LDetectorSet.LDetectorSceneParse(lLayout.LSceneDetectors));
        lSplitInspector.LInspectorSensor.LSensorPersistentSet(lLayout.LSceneDetectPersistent);
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
        if (!lSplitInspector.LInspectorSensor.LSensorPersistent)
        {
            LSplitStateLoad(lPath);
        }
    }

    public void LSplitLockHandle(bool lLocked) => lSplitFlow.LFlowEditSet(!lLocked);

    public void LSplitSampleHandle(LNeutralSample lSample) =>
        lSplitInspector.LInspectorBlank.LBlankSampleSet(
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
        LSensor lSensor = lSplitInspector.LInspectorSensor;
        foreach (LDetectorKind lKind in LDetector.LDetectorKinds)
        {
            bool lActive = lKind == LDetectorKind.LDetectorKindBlank
                ? lSplitInspector.LInspectorBlank.LBlankStep.LDetectorBlankEnabled
                : lSensor.LSensorStepRead(lKind).LDetectorStepEnabled;
            lSplitProcessing.LProcessingActiveSet(LSensor.LSensorNameRead(lKind), lActive);
        }
    }

    public void LSplitStateSave()
    {
        if (lSplitInspector.LInspectorSaveSuspended
            || lSplitViewer.LViewerSourcePath is not { } lPath
            || lSplitDocket.LDocketLockCheck(lPath))
        {
            return;
        }

        LSidecarSplitRecord lRecord = LDetectorSet.LDetectorSidecarFormat(LSplitStateRead());
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

        LSplitStateApply(LDetectorSet.LDetectorSidecarParse(lRecord));
        LSplitActiveUpdate();
    }

    public LDetectorSet LSplitStateRead()
    {
        LSensor lSensor = lSplitInspector.LInspectorSensor;
        IReadOnlyList<LDetectorStep> lSteps = LDetector.LDetectorKinds
            .Where(lKind => lKind != LDetectorKind.LDetectorKindBlank)
            .Select(lSensor.LSensorStepRead)
            .ToArray();
        Dictionary<LDetectorKind, string> lPresets = lSteps.ToDictionary(
            lStep => lStep.LDetectorStepKind,
            lStep => lSensor.LSensorTokenRead(lStep.LDetectorStepKind) ?? string.Empty);
        return new LDetectorSet(
            lSteps,
            lSplitInspector.LInspectorBlank.LBlankStep,
            lSensor.LSensorMode,
            lSensor.LSensorSpeed,
            lSensor.LSensorMetric,
            lPresets);
    }

    private void LSplitStateApply(LDetectorSet lSet)
    {
        LSensor lSensor = lSplitInspector.LInspectorSensor;
        lSplitInspector.LInspectorSaveSuspend();
        try
        {
            if (lSet.LDetectorSetBlank is { } lBlank)
            {
                lSplitInspector.LInspectorBlank.LBlankStepSet(lBlank);
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
            lSplitInspector.LInspectorSaveResume();
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
