using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QEditTab
{
    private readonly LPresetSelection qEditPreset;
    private readonly QViewer qEditViewer;
    private readonly QList qEditList;
    private readonly LDocket qEditDocket;

    public QEditTab(CUsher lUsher, LPresetSelection lPreset, QViewer lViewer, QList lList)
    {
        qEditPreset = lPreset;
        qEditViewer = lViewer;
        qEditList = lList;
        qEditDocket = lList.QListDocket;
        QEditColor = new QEditTabColor(QEditInspector, QEditProcessing, lViewer);
        QEditStore = new QEditTabPlan(lUsher, QEditInspector, lViewer, qEditDocket, QEditColor);
        QEditCrop = new QEditTabCrop(QEditInspector, QEditProcessing, lViewer, QEditStore, QEditColor);
        QEditTool = new QEditTool(QEditInspector, lViewer);
        QEditProcessing.QProcessingOrderedSet(false);
        QWhitebalance lWhitebalance = QEditInspector.QInspectorVideo.QInspectorWhitebalance;
        QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateChanged += QEditCrop.QEditCropHandle;
        QEditInspector.QInspectorVideo.QInspectorVideoChanged += QEditChangeHandle;
        QEditInspector.QInspectorPersistentChanged += QEditStore.QEditPersistentSave;
        QEditInspector.QInspectorToolChanged += QEditTool.QEditToolHandle;
        QEditInspector.QInspectorSkip.QSkipActiveChanged += QEditSkipHandle;
        lWhitebalance.QWhitebalanceEstimateChanged += QEditColor.QEditEstimateHandle;
        lViewer.QViewerMediaChanged += QEditMediaHandle;
        lViewer.QViewerEngineChanged += QEditColor.QEditCapableHandle;
        lViewer.QViewerEngineChanged += lViewer.QViewerNeutral.QViewerNeutralCancel;
        lViewer.QViewerNeutral.QViewerToolChanged += lWhitebalance.QWhitebalanceToolSet;
        lViewer.QViewerNeutral.QViewerNeutralChanged += QEditColor.QEditNeutralHandle;
        lViewer.QViewerNeutral.QViewerEstimateChanged += QEditColor.QEditEstimateApply;
        lWhitebalance.QWhitebalanceToolChanged += lViewer.QViewerNeutral.QViewerToolSet;
        lViewer.QCrop.QCropVideoChanged += QEditCrop.QEditCropShow;
        QEditProcessing.QProcessingStepChanged += QEditStepHandle;
        lViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
        lList.QListPathChanged += QEditPathHandle;
        lList.QListItemsAdded += QEditStore.QEditItemsHandle;
        lList.QListLockChanged += QEditTool.QEditLockHandle;
    }

    public event Action? QEditPresetMissing;
    public event Action? QEditPresetIncompatible;
    public event Action? QEditHistogramDeferred;
    public event Action? QEditColorDeferred;

    public QInspector QEditInspector { get; } = new();

    public QProcessing QEditProcessing { get; } = new();

    public QEditTabColor QEditColor { get; }

    public QEditTabPlan QEditStore { get; }

    public QEditTabCrop QEditCrop { get; }

    public QEditTool QEditTool { get; }

    public void QEditClose()
    {
        QWhitebalance lWhitebalance = QEditInspector.QInspectorVideo.QInspectorWhitebalance;
        QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateChanged -= QEditCrop.QEditCropHandle;
        QEditInspector.QInspectorVideo.QInspectorVideoChanged -= QEditChangeHandle;
        QEditInspector.QInspectorPersistentChanged -= QEditStore.QEditPersistentSave;
        QEditInspector.QInspectorToolChanged -= QEditTool.QEditToolHandle;
        QEditInspector.QInspectorSkip.QSkipActiveChanged -= QEditSkipHandle;
        lWhitebalance.QWhitebalanceEstimateChanged -= QEditColor.QEditEstimateHandle;
        qEditViewer.QViewerMediaChanged -= QEditMediaHandle;
        qEditViewer.QViewerEngineChanged -= QEditColor.QEditCapableHandle;
        qEditViewer.QViewerEngineChanged -= qEditViewer.QViewerNeutral.QViewerNeutralCancel;
        qEditViewer.QViewerNeutral.QViewerToolChanged -= lWhitebalance.QWhitebalanceToolSet;
        qEditViewer.QViewerNeutral.QViewerNeutralChanged -= QEditColor.QEditNeutralHandle;
        qEditViewer.QViewerNeutral.QViewerEstimateChanged -= QEditColor.QEditEstimateApply;
        lWhitebalance.QWhitebalanceToolChanged -= qEditViewer.QViewerNeutral.QViewerToolSet;
        qEditViewer.QCrop.QCropVideoChanged -= QEditCrop.QEditCropShow;
        QEditProcessing.QProcessingStepChanged -= QEditStepHandle;
    }

    public void QEditStart()
    {
        QEditCrop.QEditCropUpdate();
        QEditColor.QEditColorUpdate();
        qEditViewer.QCrop.QCropActiveSet(QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateActive);
    }

    public LSceneTabEntry QEditLayoutRead(LSceneTabEntry lLayout)
    {
        if (QEditStore.QEditCarriedRead() is { } lCarried)
        {
            lLayout.LSceneInspector = new LSceneInspectorEntry
            {
                LSceneInspectorEdit = LEdit.LEditPersistentCreate(lCarried),
                LSceneInspectorCrop = QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStatePersistent,
                LSceneInspectorSkip = QEditInspector.QInspectorSkip.QSkipPersistent
            };
        }

        return lLayout;
    }

    public void QEditLayoutApply(LSceneTabEntry? lLayout)
    {
        if (lLayout is null)
        {
            QEditInspector.QInspectorMinimizedSet(true);
        }

        if (lLayout?.LSceneInspector is not { LSceneInspectorEdit: { } lRecord } lPersistent)
        {
            return;
        }

        QEditInspector.QInspectorSaveSuspend();
        try
        {
            LEditPlan lPlan = LEdit.LEditPersistentRead(lRecord);
            if (lPersistent.LSceneInspectorCrop)
            {
                QEditCropApply(lPlan);
                QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxPersistentSet(true);
            }

            QEditInspector.QInspectorVideo.QInspectorVideoApply(lPlan.LEditVideo);
            QEditInspector.QInspectorVideo.QInspectorPersistentApply(lPlan.LEditVideo);
            QEditInspector.QInspectorSkip.QSkipActiveSet(lPlan.LEditSkip);
            QEditInspector.QInspectorSkip.QSkipPersistentSet(lPersistent.LSceneInspectorSkip);
        }
        finally
        {
            QEditInspector.QInspectorSaveResume();
        }
    }

    public void QEditRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QEditPresetCheck() || QEditSelectedRead() is not { } lSelected)
        {
            return;
        }

        (LWorkCrop lCrop, LWorkVideo lVideo) =
            LEdit.LEditWorkResolve(QEditStore.QEditStateRead(), QEditColor.QEditEqCheck());
        LMessenger.LMessengerEditDescribe(
            lPriority,
            lSelected.LDocketEntryPath,
            qEditViewer.QViewerDuration,
            lCrop,
            lVideo,
            qEditPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab,
            lSelected.LDocketEntryBatch);
    }

    public void QEditAllRun(Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QEditPresetCheck())
        {
            return;
        }

        LMessenger.LMessengerEditDescribe(
            LWorkPriority.LWorkPriorityNormal,
            qEditDocket.LDocketUnlockedRead().Select(QEditSourceCreate).ToArray(),
            qEditPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void QEditItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QEditPresetCheck())
        {
            return;
        }

        LMessenger.LMessengerEditDescribe(
            LWorkPriority.LWorkPriorityNormal,
            qEditDocket.LDocketUnlockedRead()
                .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .Select(QEditSourceCreate)
                .ToArray(),
            qEditPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void QEditPathHandle(string? lPath)
    {
        qEditViewer.QViewerNeutral.QViewerNeutralCancel();
        if (string.IsNullOrWhiteSpace(lPath))
        {
            LTraceLog.LTraceInfoRecord("Edit click: no file selected");
            return;
        }

        if (qEditViewer.QViewerSourceMatch(lPath))
        {
            return;
        }

        LTraceLog.LTraceInfoRecord(
            $"Edit click '{QEditStore.QEditNameRead(lPath)}': "
            + $"persistent {(QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStatePersistent ? "on" : "off")}, "
            + $"inspector now {QEditTabPlan.QEditCropFormat(QEditInspector.QInspectorCrop.QInspectorCropRead())}");
        qEditViewer.QViewerPathHandle(lPath);
    }

    public void QEditSkipHandle()
    {
        QEditProcessing.QProcessingSkipSet(QEditInspector.QInspectorSkip.QSkipActive);
        QEditCrop.QEditViewerApply();
        QEditStore.QEditStateSave();
    }

    public void QEditChangeHandle()
    {
        QEditColor.QEditColorUpdate();
        QEditColorDeferred?.Invoke();
        QEditStore.QEditStateSave();
    }

    private void QEditMediaHandle(LCargo lCargo)
    {
        QEditColor.QEditEstimateHandle(QEditInspector.QInspectorVideo.QInspectorWhitebalance.QWhitebalanceMethod);
        QEditHistogramDeferred?.Invoke();
        QEditCrop.QEditCropRestore(
            qEditViewer.QViewerSourcePath is { } lPath ? QEditStore.QEditNameRead(lPath) : "(no media)");
    }

    private void QEditStepHandle(string lStep)
    {
        if (lStep == "Curve")
        {
            QEditHistogramDeferred?.Invoke();
        }
    }

    private void QEditCropApply(LEditPlan lPlan)
    {
        QEditInspector.QInspectorCrop.QInspectorCropApply(lPlan.LEditCrop, lPlan.LEditCropActive);
        QEditInspector.QInspectorCrop.QInspectorRatioApply(
            lPlan.LEditRatioFixed, lPlan.LEditRatioLenient, lPlan.LEditRatioWidth, lPlan.LEditRatioHeight);
    }

    private bool QEditPresetCheck()
    {
        if (!qEditPreset.LPresetSelectionValid)
        {
            QEditPresetMissing?.Invoke();
            return false;
        }

        if (qEditPreset.LPresetSelectionEncoding is { } lEncoding
            && !lEncoding.LEncodingSupportCheck(LWorkKind.LWorkKindEdit))
        {
            QEditPresetIncompatible?.Invoke();
            return false;
        }

        return true;
    }

    private LDocketEntry? QEditSelectedRead() =>
        qEditList.QListPathCurrent is { } lPath
        && qEditDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    private static LWorkSource QEditSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);
}
