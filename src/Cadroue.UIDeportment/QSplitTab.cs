using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QSplitTab
{
    public static readonly IReadOnlyList<QProcessingRow> QSplitRows =
    [
        QSplitRowCreate(LDetectorKind.LDetectorKindBlank, "PProcessingBlank", "Blank"),
        QSplitRowCreate(LDetectorKind.LDetectorKindScene, "PProcessingScene", "Scene"),
        QSplitRowCreate(LDetectorKind.LDetectorKindStill, "PProcessingStill", "Still"),
        QSplitRowCreate(LDetectorKind.LDetectorKindLuminance, "PProcessingLuminance", "Luminance"),
        QSplitRowCreate(LDetectorKind.LDetectorKindSilence, "PProcessingSilence", "Silence"),
        QSplitRowCreate(LDetectorKind.LDetectorKindVolume, "PProcessingReducedVolume", "Volume"),
    ];

    private readonly LPresetSelection qSplitPreset;
    private readonly QViewer qSplitViewer;
    private readonly QList qSplitList;
    private readonly LDocket qSplitDocket;
    private readonly QFlow qSplitFlow;

    public QSplitTab(LPresetSelection lPreset, QViewer lViewer, QList lList, QFlow lFlow)
    {
        qSplitPreset = lPreset;
        qSplitViewer = lViewer;
        qSplitList = lList;
        qSplitDocket = lList.QListDocket;
        qSplitFlow = lFlow;
        QSplitSweep = new QSplitTabSweep(QSplitInspector, lList, qSplitDocket, lFlow);
        lFlow.QFlowSectionSet(true);
        QSplitInspector.QInspectorSensor.QSensorChanged += QSplitChangeHandle;
        QSplitInspector.QInspectorBlank.QBlankChanged += QSplitChangeHandle;
        QSplitInspector.QInspectorSensor.QSensorPersistentChanged += QSplitPersistentHandle;
        QSplitInspector.QInspectorBlank.QBlankPickChanged += QSplitPickHandle;
        lViewer.QViewerNeutral.QViewerNeutralChanged += QSplitSampleHandle;
        QSplitSection = new QSection(lFlow);
        QSplitInspector.QInspectorSensor.QSensorRunApplied += () => _ = QSplitSweep.QSplitSweepStart();
        QSplitInspector.QInspectorSensor.QSensorStopApplied += QSplitSweep.QSplitSweepCancel;
        lList.QListPathChanged += QSplitPathHandle;
        lList.QListLockChanged += QSplitLockHandle;
        lViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
    }

    public event Action? QSplitPresetMissing;

    public QInspector QSplitInspector { get; } = new();

    public QProcessing QSplitProcessing { get; } = new();

    public QSection QSplitSection { get; }

    public QSplitTabSweep QSplitSweep { get; }

    public void QSplitClose()
    {
        QSplitSweep.QSplitSweepCancel();
        QSplitInspector.QInspectorSensor.QSensorChanged -= QSplitChangeHandle;
        QSplitInspector.QInspectorBlank.QBlankChanged -= QSplitChangeHandle;
        QSplitInspector.QInspectorSensor.QSensorPersistentChanged -= QSplitPersistentHandle;
        QSplitInspector.QInspectorBlank.QBlankPickChanged -= QSplitPickHandle;
        qSplitViewer.QViewerNeutral.QViewerNeutralChanged -= QSplitSampleHandle;
    }

    public void QSplitStart() => qSplitFlow.QFlowEdit.QFlowEditSet(!QSplitLockedCheck());

    public LSceneTabEntry QSplitLayoutRead(LSceneTabEntry lLayout)
    {
        lLayout.LSceneDetectors = LDetectorBundle.LDetectorSceneFormat(QSplitStateRead());
        lLayout.LSceneDetectPersistent = QSplitInspector.QInspectorSensor.QSensorPersistent;
        return lLayout;
    }

    public void QSplitLayoutApply(LSceneTabEntry? lLayout)
    {
        if (lLayout is null)
        {
            QSplitProcessing.QProcessingMinimizedSet(true);
            QSplitInspector.QInspectorMinimizedSet(true);
            return;
        }

        QSplitStateApply(LDetectorBundle.LDetectorSceneParse(lLayout.LSceneDetectors));
        QSplitInspector.QInspectorSensor.QSensorPersistentSet(lLayout.LSceneDetectPersistent);
        QSplitActiveUpdate();
    }

    public void QSplitRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QSplitPresetCheck() || QSplitSelectedRead() is not { } lSelected)
        {
            return;
        }

        LMessenger.LMessengerSplitDescribe(
            lPriority,
            lSelected.LDocketEntryPath,
            qSplitFlow.QFlowSection.QFlowSplitRead(),
            qSplitPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab,
            lSelected.LDocketEntryBatch);
    }

    public void QSplitAllRun(Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QSplitPresetCheck())
        {
            return;
        }

        _ = LMessenger.LMessengerSplitDescribe(
            LWorkPriority.LWorkPriorityNormal,
            qSplitDocket.LDocketUnlockedRead().Select(QSplitSourceCreate).ToArray(),
            qSplitPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void QSplitItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QSplitPresetCheck())
        {
            return;
        }

        _ = LMessenger.LMessengerSplitDescribe(
            LWorkPriority.LWorkPriorityNormal,
            qSplitDocket.LDocketUnlockedRead()
                .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .Select(QSplitSourceCreate)
                .ToArray(),
            qSplitPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void QSplitPathHandle(string? lPath)
    {
        if (string.IsNullOrWhiteSpace(lPath) || qSplitViewer.QViewerSourceMatch(lPath))
        {
            return;
        }

        qSplitViewer.QViewerPathHandle(lPath);
        if (!QSplitInspector.QInspectorSensor.QSensorPersistent)
        {
            QSplitStateLoad(lPath);
        }
    }

    public void QSplitLockHandle(bool lLocked) => qSplitFlow.QFlowEdit.QFlowEditSet(!lLocked);

    public void QSplitPickHandle(bool lArmed) =>
        qSplitViewer.QViewerNeutral.QViewerToolSet(lArmed, LNeutralTarget.LNeutralTargetGrey);

    public void QSplitSampleHandle(LNeutralSample lSample) =>
        QSplitInspector.QInspectorBlank.QBlankSampleSet(
            lSample.LNeutralRed, lSample.LNeutralGreen, lSample.LNeutralBlue);

    public void QSplitChangeHandle()
    {
        QSplitActiveUpdate();
        QSplitStateSave();
    }

    public void QSplitPersistentHandle(bool lPersistent)
    {
        QSplitStateSave();
        if (lPersistent)
        {
            _ = QSplitSweep.QSplitSweepStart();
        }
    }

    public void QSplitActiveUpdate()
    {
        QSensor lSensor = QSplitInspector.QInspectorSensor;
        foreach (LDetectorKind lKind in LDetector.LDetectorKinds)
        {
            bool lActive = lKind == LDetectorKind.LDetectorKindBlank
                ? QSplitInspector.QInspectorBlank.QBlankStep.LDetectorBlankEnabled
                : lSensor.QSensorStepRead(lKind).LDetectorStepEnabled;
            QSplitProcessing.QProcessingActiveSet(QSensor.QSensorNameRead(lKind), lActive);
        }
    }

    public void QSplitStateSave()
    {
        if (QSplitInspector.QInspectorSaveSuspended
            || qSplitViewer.QViewerSourcePath is not { } lPath
            || qSplitDocket.LDocketLockCheck(lPath))
        {
            return;
        }

        LSidecarSplitEntry lRecord = LDetectorBundle.LDetectorSidecarFormat(QSplitStateRead());
        if (!lRecord.LSidecarSplitActive && LLibrarian.LLibrarianSplitLoad(lPath) is null)
        {
            return;
        }

        LLibrarian.LLibrarianSplitSave(lPath, lRecord);
    }

    public void QSplitStateLoad(string lPath)
    {
        if (LLibrarian.LLibrarianSplitLoad(lPath) is not { } lRecord)
        {
            return;
        }

        QSplitStateApply(LDetectorBundle.LDetectorSidecarParse(lRecord));
        QSplitActiveUpdate();
    }

    public LDetectorBundle QSplitStateRead()
    {
        QSensor lSensor = QSplitInspector.QInspectorSensor;
        IReadOnlyList<LDetectorStep> lSteps = LDetector.LDetectorKinds
            .Where(lKind => lKind != LDetectorKind.LDetectorKindBlank)
            .Select(lSensor.QSensorStepRead)
            .ToArray();
        Dictionary<LDetectorKind, string> lPresets = lSteps.ToDictionary(
            lStep => lStep.LDetectorStepKind,
            lStep => lSensor.QSensorPreset.QSensorTokenRead(lStep.LDetectorStepKind) ?? string.Empty);
        return new LDetectorBundle(
            lSteps,
            QSplitInspector.QInspectorBlank.QBlankStep,
            lSensor.QSensorMode,
            lSensor.QSensorSpeed,
            lSensor.QSensorMetric,
            lPresets);
    }

    private void QSplitStateApply(LDetectorBundle lSet)
    {
        QSensor lSensor = QSplitInspector.QInspectorSensor;
        QSplitInspector.QInspectorSaveSuspend();
        try
        {
            if (lSet.LDetectorSetBlank is { } lBlank)
            {
                QSplitInspector.QInspectorBlank.QBlankStepSet(lBlank);
            }

            foreach (LDetectorStep lStep in lSet.LDetectorSetSteps)
            {
                lSensor.QSensorStepSet(lStep);
            }

            if (lSet.LDetectorSetStill is { } lStill)
            {
                lSensor.QSensorModeSet(lStill);
            }

            if (lSet.LDetectorSetLuminance is { } lLuminance)
            {
                lSensor.QSensorSpeedSet(lLuminance);
            }

            if (lSet.LDetectorSetMetric is { } lMetric)
            {
                lSensor.QSensorMetricSet(lMetric);
            }

            foreach (KeyValuePair<LDetectorKind, string> lPreset in lSet.LDetectorSetPresets)
            {
                lSensor.QSensorPreset.QSensorTokenSet(lPreset.Key, lPreset.Value);
            }
        }
        finally
        {
            QSplitInspector.QInspectorSaveResume();
        }
    }

    private bool QSplitLockedCheck() =>
        qSplitList.QListPathCurrent is { } lPath && qSplitDocket.LDocketLockCheck(lPath);

    private bool QSplitPresetCheck()
    {
        if (qSplitPreset.LPresetSelectionValid)
        {
            return true;
        }

        QSplitPresetMissing?.Invoke();
        return false;
    }

    private LDocketEntry? QSplitSelectedRead() =>
        qSplitList.QListPathCurrent is { } lPath
        && qSplitDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    private static QProcessingRow QSplitRowCreate(LDetectorKind lKind, string lIcon, string lLabel) =>
        new(QSensor.QSensorNameRead(lKind), $"/PAsset/PPanel/{lIcon}.svg", $"Processing.Step.{lLabel}");

    private static LWorkSource QSplitSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);
}
