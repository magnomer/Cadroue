using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The tab's detector settings: read from and applied to the inspector, saved to and loaded from the media's
// sidecar, and shown as the processing panel's active steps.
public sealed class QSplitTabDetector
{
    private readonly QInspector qSplitInspector;
    private readonly QViewer qSplitViewer;
    private readonly LDocket qSplitDocket;
    private readonly QProcessing qSplitProcessing;

    public QSplitTabDetector(QInspector lInspector, QViewer lViewer, LDocket lDocket, QProcessing lProcessing)
    {
        qSplitInspector = lInspector;
        qSplitViewer = lViewer;
        qSplitDocket = lDocket;
        qSplitProcessing = lProcessing;
    }

    // The detector kinds switched on, in detector order.
    public IReadOnlyList<LDetectorKind> QSplitStepsRead()
    {
        QSensor lSensor = qSplitInspector.QInspectorSensor;
        return LDetector.LDetectorKinds
            .Where(lKind => lKind == LDetectorKind.LDetectorKindBlank
                ? qSplitInspector.QInspectorBlank.QBlankStep.LDetectorBlankEnabled
                : lSensor.QSensorStepRead(lKind).LDetectorStepEnabled)
            .ToArray();
    }

    public void QSplitActiveUpdate()
    {
        IReadOnlyList<LDetectorKind> lEnabled = QSplitStepsRead();
        foreach (LDetectorKind lKind in LDetector.LDetectorKinds)
        {
            qSplitProcessing.QProcessingActiveSet(QSensorName.QSensorNameRead(lKind), lEnabled.Contains(lKind));
        }
    }

    public void QSplitStateSave()
    {
        if (qSplitInspector.QInspectorSaveSuspended
            || qSplitViewer.QViewerWorkspace.CWorkspaceSource is not { } lPath
            || qSplitDocket.LDocketLockCheck(lPath))
        {
            return;
        }

        LSidecarSplitEntry lRecord = LDetectorRow.LDetectorSidecarFormat(QSplitStateRead());
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

        QSplitStateApply(LDetectorRow.LDetectorSidecarParse(lRecord));
        QSplitActiveUpdate();
    }

    public LDetectorBundle QSplitStateRead()
    {
        QSensor lSensor = qSplitInspector.QInspectorSensor;
        IReadOnlyList<LDetectorStep> lSteps = LDetector.LDetectorKinds
            .Where(lKind => lKind != LDetectorKind.LDetectorKindBlank)
            .Select(lSensor.QSensorStepRead)
            .ToArray();
        Dictionary<LDetectorKind, string> lPresets = lSteps.ToDictionary(
            lStep => lStep.LDetectorStepKind,
            lStep => lSensor.QSensorPreset.QSensorTokenRead(lStep.LDetectorStepKind) ?? string.Empty);
        return new LDetectorBundle(
            lSteps,
            qSplitInspector.QInspectorBlank.QBlankStep,
            lSensor.QSensorMode,
            lSensor.QSensorSpeed,
            lSensor.QSensorMetric,
            lPresets);
    }

    public void QSplitStateApply(LDetectorBundle lSet)
    {
        QSensor lSensor = qSplitInspector.QInspectorSensor;
        qSplitInspector.QInspectorSaveSuspend();
        try
        {
            if (lSet.LDetectorSetBlank is { } lBlank)
            {
                qSplitInspector.QInspectorBlank.QBlankStepSet(lBlank);
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
            qSplitInspector.QInspectorSaveResume();
        }
    }
}
