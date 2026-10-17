using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QEditTab
{
    private readonly CEdit qEditGate;
    private readonly LPresetSelection qEditPreset;
    private readonly QViewer qEditViewer;
    private readonly QList qEditList;
    private readonly LDocket qEditDocket;

    public QEditTab(
        CUsher lUsher, CInspector lInspector, CEdit lEdit, LPresetSelection lPreset, QViewer lViewer, QList lList)
    {
        QEditInspector = new QInspector(lInspector);
        qEditGate = lEdit;
        qEditPreset = lPreset;
        qEditViewer = lViewer;
        qEditList = lList;
        qEditDocket = lList.QListDocket;
        QEditColor = new QEditTabColor(QEditInspector, QEditProcessing, lViewer);
        QEditStore = new QEditTabPlan(lUsher, QEditInspector, lViewer, qEditDocket, QEditColor);
        QEditCrop = new QEditTabCrop(QEditInspector, QEditProcessing, lViewer, QEditStore);
        QEditTool = new QEditTool(QEditInspector, lViewer);
        QEditProcessing.QProcessingOrderedSet(false);
        QEditAttach();
    }

    public event Action? QEditPresetMissing;
    public event Action? QEditPresetIncompatible;
    public event Action? QEditHistogramDeferred;
    public event Action? QEditColorDeferred;

    public QInspector QEditInspector { get; }

    public QProcessing QEditProcessing { get; } = new();

    public QEditTabColor QEditColor { get; }

    public QEditTabPlan QEditStore { get; }

    public QEditTabCrop QEditCrop { get; }

    public QEditTool QEditTool { get; }

    private void QEditAttach()
    {
        CWhitebalance lWhitebalance = QEditInspector.QInspectorVideo.QInspectorWhitebalance;
        QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateChanged += QEditCropboxHandle;
        QEditInspector.QInspectorVideo.QInspectorVideoChanged += QEditChangeHandle;
        QEditInspector.QInspectorPersistentChanged += QEditStore.QEditPersistentSave;
        QEditInspector.QInspectorToolChanged += QEditTool.QEditToolHandle;
        QEditInspector.QInspectorGate.CInspectorSkip.CSkipActiveChanged += QEditSkipHandle;
        lWhitebalance.CWhitebalanceEstimateChanged += QEditColor.QEditEstimateHandle;
        qEditViewer.QViewerMediaChanged += QEditColor.QEditEstimateStart;
        qEditViewer.QViewerMediaChanged += QEditMediaHandle;
        qEditViewer.QViewerEngineChanged += QEditColor.QEditCapableHandle;
        qEditViewer.QViewerEngineChanged += qEditViewer.QViewerNeutral.QViewerNeutralCancel;
        qEditViewer.QViewerNeutral.QViewerToolChanged += QEditColor.QEditTargetHandle;
        qEditViewer.QViewerNeutral.QViewerNeutralChanged += QEditColor.QEditNeutralHandle;
        qEditViewer.QViewerNeutral.QViewerEstimateChanged += QEditColor.QEditEstimateApply;
        lWhitebalance.CWhitebalanceToolChanged += QEditColor.QEditTargetApply;
        qEditViewer.QCrop.QCropVideoChanged += QEditCrop.QEditCropShow;
        QEditProcessing.QProcessingStepChanged += QEditStepHandle;
        qEditViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = qEditList.QListPathsAdd(lDropPaths);
        qEditList.QListPathChanged += QEditPathHandle;
        qEditViewer.QViewerOpenNotice += QEditOpenHandle;
        qEditList.QListItemsAdded += QEditStore.QEditItemsHandle;
        qEditList.QListLockChanged += QEditTool.QEditLockHandle;
    }

    public void QEditClose()
    {
        CWhitebalance lWhitebalance = QEditInspector.QInspectorVideo.QInspectorWhitebalance;
        QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateChanged -= QEditCropboxHandle;
        QEditInspector.QInspectorVideo.QInspectorVideoChanged -= QEditChangeHandle;
        QEditInspector.QInspectorPersistentChanged -= QEditStore.QEditPersistentSave;
        QEditInspector.QInspectorToolChanged -= QEditTool.QEditToolHandle;
        QEditInspector.QInspectorGate.CInspectorSkip.CSkipActiveChanged -= QEditSkipHandle;
        lWhitebalance.CWhitebalanceEstimateChanged -= QEditColor.QEditEstimateHandle;
        qEditViewer.QViewerMediaChanged -= QEditColor.QEditEstimateStart;
        qEditViewer.QViewerMediaChanged -= QEditMediaHandle;
        qEditViewer.QViewerOpenNotice -= QEditOpenHandle;
        qEditViewer.QViewerEngineChanged -= QEditColor.QEditCapableHandle;
        qEditViewer.QViewerEngineChanged -= qEditViewer.QViewerNeutral.QViewerNeutralCancel;
        qEditViewer.QViewerNeutral.QViewerToolChanged -= QEditColor.QEditTargetHandle;
        qEditViewer.QViewerNeutral.QViewerNeutralChanged -= QEditColor.QEditNeutralHandle;
        qEditViewer.QViewerNeutral.QViewerEstimateChanged -= QEditColor.QEditEstimateApply;
        lWhitebalance.CWhitebalanceToolChanged -= QEditColor.QEditTargetApply;
        qEditViewer.QCrop.QCropVideoChanged -= QEditCrop.QEditCropShow;
        QEditProcessing.QProcessingStepChanged -= QEditStepHandle;
    }

    public void QEditStart()
    {
        QEditCrop.QEditCropStart();
        QEditColor.QEditColorUpdate();
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
            QEditInspector.QInspectorVideo.QInspectorPersistentApply(lPlan.LEditVideo);
            QEditInspector.QInspectorPlanApply(lPlan, lPersistent.LSceneInspectorCrop, lPersistent.LSceneInspectorSkip);
        }
        finally
        {
            QEditInspector.QInspectorSaveResume();
        }
    }

    // The gate decides whether the selected item runs; the dispatch stays here until the edit plan can reach a gate.
    public void QEditRun(bool lUrgent, Guid lRelayTarget, Guid lSourceTab)
    {
        (CEditVerdict lVerdict, string? lPath, Guid lBatch) = qEditGate.CEditRun(
            qEditPreset.LPresetSelectionValid,
            qEditPreset.LPresetSelectionSupported,
            qEditList.QListPathCurrent,
            qEditDocket.LDocketItemsRead()
                .Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch, lItem.LDocketEntryLocked))
                .ToArray());
        if (!QEditVerdictApply(lVerdict))
        {
            return;
        }

        (LWorkCrop lCrop, LWorkVideo lVideo) =
            LEdit.LEditWorkResolve(QEditStore.QEditStateRead(), QEditColor.QEditEqCheck());
        LMessenger.LMessengerEditDescribe(
            lUrgent ? LWorkPriority.LWorkPriorityHigh : LWorkPriority.LWorkPriorityNormal,
            lPath,
            qEditViewer.QViewerDuration,
            lCrop,
            lVideo,
            qEditPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab,
            lBatch);
    }

    // No paths runs every unlocked item; the gate picks the items and dispatches.
    public void QEditItemsRun(IReadOnlyList<string>? lPaths, Guid lRelayTarget, Guid lSourceTab) =>
        QEditVerdictApply(qEditGate.CEditAllRun(
            qEditPreset.LPresetSelectionValid,
            qEditPreset.LPresetSelectionSupported,
            qEditPreset.LPresetSelectionName,
            qEditDocket.LDocketItemsRead()
                .Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch, lItem.LDocketEntryLocked))
                .ToArray(),
            lPaths,
            lRelayTarget,
            lSourceTab));

    public void QEditPathHandle(string? lPath)
    {
        qEditViewer.QViewerNeutral.QViewerNeutralCancel();
        if (string.IsNullOrWhiteSpace(lPath))
        {
            LTraceLog.LTraceInfoRecord("Edit click: no file selected");
            return;
        }

        qEditViewer.QViewerWorkspace.CWorkspaceOpen(lPath);
    }

    private void QEditOpenHandle(string lPath)
    {
        LTraceLog.LTraceInfoRecord(
            $"Edit click '{QEditStore.QEditNameRead(lPath)}': "
            + $"persistent {(QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStatePersistent ? "on" : "off")}, "
            + $"inspector now {QEditTabPlan.QEditCropFormat(QEditInspector.QInspectorCrop.QInspectorCropRead())}");
    }

    public void QEditSkipHandle()
    {
        QEditProcessing.QProcessingSkipSet(QEditInspector.QInspectorSkip.QSkipActive);
        QEditViewerApply();
        QEditStore.QEditStateSave();
    }

    public void QEditCropboxHandle()
    {
        LRotateFlip lRotate = QEditInspector.QInspectorOrientation.QInspectorRotateRead();
        if (qEditViewer.QViewerPreview.LRotateFlip != lRotate)
        {
            qEditViewer.QViewerRotateSet(lRotate);
            QEditCrop.QEditSourceSync();
        }

        QEditCrop.QEditCropHandle();
    }

    public void QEditChangeHandle()
    {
        QEditColor.QEditColorUpdate();
        QEditColorDeferred?.Invoke();
        QEditStore.QEditStateSave();
    }

    private void QEditMediaHandle(LCargo lCargo)
    {
        QEditHistogramDeferred?.Invoke();
        string? lPath = qEditViewer.QViewerWorkspace.CWorkspaceSource;
        QEditCropRestore(lPath is null ? "(no media)" : QEditStore.QEditNameRead(lPath));
    }

    private void QEditStepHandle(string lStep)
    {
        if (lStep == "Curve")
        {
            QEditHistogramDeferred?.Invoke();
        }
    }

    private void QEditCropRestore(string lName)
    {
        LEditPlan? lApplied = null;
        QEditInspector.QInspectorSaveSuspend();
        try
        {
            QEditCrop.QEditSourceSync();
            LEditPlan? lPersistent = QEditStore.QEditCarriedRead();
            LEditPlan? lSaved = QEditStore.QEditSavedRead();
            LTraceLog.LTraceInfoRecord(
                $"Edit media ready '{lName}': display {QEditCrop.QEditSourceFormat()}, "
                + $"persistent {(lPersistent is null ? "off" : "on")}, "
                + $"carried {QEditTabPlan.QEditPlanFormat(lPersistent)}, "
                + $"sidecar {QEditTabPlan.QEditPlanFormat(lSaved)}");
            QEditInspector.QInspectorCrop.QInspectorCropReset();

            bool lCarryWins = lPersistent is not null;
            LEditPlan lPlan = LEdit.LEditPlanResolve(
                lSaved,
                lPersistent,
                QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStatePersistent,
                QEditInspector.QInspectorSkip.QSkipPersistent);
            LTraceLog.LTraceInfoRecord(
                $"Edit applying {(lCarryWins ? "persistent" : "sidecar")} plan to '{lName}': "
                + $"{QEditTabPlan.QEditPlanFormat(lPlan)}");
            qEditViewer.QViewerRotateSet(LRotateFlip.LRotateCropResolve(lPlan.LEditCrop));
            QEditCrop.QEditSourceSync();

            lApplied = lCarryWins ? lPlan : null;
            QEditInspector.QInspectorPlanApply(lPlan);
        }
        finally
        {
            QEditInspector.QInspectorSaveResume();
        }

        QEditProcessing.QProcessingSkipSet(QEditInspector.QInspectorSkip.QSkipActive);
        QEditViewerApply();
        if (lApplied is not null)
        {
            QEditStore.QEditStateSave(lApplied, _ => lName);
        }
    }

    private void QEditViewerApply()
    {
        bool lSkip = QEditInspector.QInspectorSkip.QSkipActive;
        LRotateFlip lRotate = lSkip
            ? LRotateFlip.LRotateDefaultCreate()
            : QEditInspector.QInspectorOrientation.QInspectorRotateRead();
        LCropbox? lRect = lSkip ? null : QEditInspector.QInspectorCrop.QInspectorRectRead();
        LTraceLog.LTraceInfoRecord(
            $"Edit viewer push: rotate {lRotate.LRotateKind}, "
            + $"H {lRotate.LRotateFlipHorizontal}, V {lRotate.LRotateFlipVertical}, "
            + $"{QEditTabPlan.QEditRectFormat(lRect)}");
        qEditViewer.QViewerRotateSet(lRotate);
        qEditViewer.QCrop.QCropRectSet(lRect);
        QEditColor.QEditColorApply();
    }

    // A preset verdict raises its notice; only a run that went answers true.
    private bool QEditVerdictApply(CEditVerdict lVerdict)
    {
        if (lVerdict == CEditVerdict.CEditVerdictMissing)
        {
            QEditPresetMissing?.Invoke();
        }
        else if (lVerdict == CEditVerdict.CEditVerdictIncompatible)
        {
            QEditPresetIncompatible?.Invoke();
        }

        return lVerdict == CEditVerdict.CEditVerdictRan;
    }
}
