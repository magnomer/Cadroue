using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

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

    private readonly QViewer qSplitViewer;
    private readonly QList qSplitList;
    private readonly QFlow qSplitFlow;

    public QSplitTab(CInspector lInspector, LPresetSelection lPreset, QViewer lViewer, QList lList, QFlow lFlow)
    {
        QSplitInspector = new QInspector(lInspector);
        qSplitViewer = lViewer;
        qSplitList = lList;
        qSplitFlow = lFlow;
        QSplitDetector = new QSplitTabDetector(QSplitInspector, lViewer, lList.QListDocket, QSplitProcessing);
        QSplitWork = new QSplitTabWork(lPreset, lList, lFlow);
        QSplitSweep = new QSplitTabSweep(QSplitInspector, lList, lFlow);
        lFlow.QFlowSectionSet(true);
        QSplitSection = new QSection(lFlow);
        QSplitHandlersAttach(lViewer, lList);
    }

    public QInspector QSplitInspector { get; }

    public QProcessing QSplitProcessing { get; } = new();

    public QSection QSplitSection { get; }

    public QSplitTabDetector QSplitDetector { get; }

    public QSplitTabWork QSplitWork { get; }

    public QSplitTabSweep QSplitSweep { get; }

    public void QSplitClose()
    {
        QSplitSweep.QSplitSweepCancel();
        QSplitHandlersDetach();
    }

    // The section is built before these subscriptions; building it raises none of their events.
    private void QSplitHandlersAttach(QViewer lViewer, QList lList)
    {
        QSplitInspector.QInspectorSensor.QSensorChanged += QSplitChangeHandle;
        QSplitInspector.QInspectorBlank.QBlankChanged += QSplitChangeHandle;
        QSplitInspector.QInspectorSensor.QSensorSweep.QSensorPersistentChanged += QSplitPersistentHandle;
        QSplitInspector.QInspectorBlank.QBlankPickChanged += QSplitPickHandle;
        lViewer.QViewerNeutral.QViewerNeutralChanged += QSplitSampleHandle;
        QSplitInspector.QInspectorSensor.QSensorSweep.QSensorRunApplied += QSplitRunHandle;
        QSplitInspector.QInspectorSensor.QSensorSweep.QSensorStopApplied += QSplitSweep.QSplitSweepCancel;
        lList.QListPathChanged += lViewer.QViewerWorkspace.CWorkspaceOpen;
        lViewer.QViewerOpenRequest += QSplitOpenHandle;
        lList.QListLockChanged += QSplitLockHandle;
        lViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
    }

    private void QSplitHandlersDetach()
    {
        QSplitInspector.QInspectorSensor.QSensorChanged -= QSplitChangeHandle;
        QSplitInspector.QInspectorBlank.QBlankChanged -= QSplitChangeHandle;
        QSplitInspector.QInspectorSensor.QSensorSweep.QSensorPersistentChanged -= QSplitPersistentHandle;
        QSplitInspector.QInspectorBlank.QBlankPickChanged -= QSplitPickHandle;
        qSplitViewer.QViewerNeutral.QViewerNeutralChanged -= QSplitSampleHandle;
        qSplitViewer.QViewerOpenRequest -= QSplitOpenHandle;
    }

    private void QSplitRunHandle() => _ = QSplitSweep.QSplitSweepStart();

    public void QSplitStart() => qSplitFlow.QFlowEdit.QFlowEditSet(!qSplitList.QListLockCheck());

    public LSceneTabEntry QSplitLayoutRead(LSceneTabEntry lLayout)
    {
        lLayout.LSceneDetectors = LDetectorRow.LDetectorSceneFormat(QSplitDetector.QSplitStateRead());
        lLayout.LSceneDetectPersistent = QSplitInspector.QInspectorSensor.QSensorSweep.QSensorPersistent;
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

        QSplitDetector.QSplitStateApply(LDetectorRow.LDetectorSceneParse(lLayout.LSceneDetectors));
        QSplitInspector.QInspectorSensor.QSensorSweep.QSensorPersistentSet(lLayout.LSceneDetectPersistent);
        QSplitDetector.QSplitActiveUpdate();
    }

    // After the viewer opens a new path: its saved split state loads unless the sensor is persistent.
    private void QSplitOpenHandle(string lPath)
    {
        if (!QSplitInspector.QInspectorSensor.QSensorSweep.QSensorPersistent)
        {
            QSplitDetector.QSplitStateLoad(lPath);
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
        QSplitDetector.QSplitActiveUpdate();
        QSplitDetector.QSplitStateSave();
    }

    public void QSplitPersistentHandle(bool lPersistent)
    {
        QSplitDetector.QSplitStateSave();
        if (lPersistent)
        {
            _ = QSplitSweep.QSplitSweepStart();
        }
    }

    private static QProcessingRow QSplitRowCreate(LDetectorKind lKind, string lIcon, string lLabel) =>
        new(QSensorName.QSensorNameRead(lKind), $"/PAsset/PPanel/{lIcon}.svg", $"Processing.Step.{lLabel}");
}
