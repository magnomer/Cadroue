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

    public QEditTab(CUsher lUsher, CInspector lInspector, LPresetSelection lPreset, QViewer lViewer, QList lList)
    {
        QEditInspector = new QInspector(lInspector);
        qEditPreset = lPreset;
        qEditViewer = lViewer;
        qEditList = lList;
        qEditDocket = lList.QListDocket;
        QEditColor = new QEditTabColor(QEditInspector, QEditProcessing, lViewer);
        QEditStore = new QEditTabPlan(lUsher, QEditInspector, lViewer, qEditDocket, QEditColor);
        QEditCrop = new QEditTabCrop(QEditInspector, QEditProcessing, lViewer, QEditStore);
        QEditTool = new QEditTool(QEditInspector, lViewer);
        QEditProcessing.QProcessingOrderedSet(false);
        QWhitebalance lWhitebalance = QEditInspector.QInspectorVideo.QInspectorWhitebalance;
        QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateChanged += QEditCropboxHandle;
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
        lViewer.QViewerOpenNotice += QEditOpenHandle;
        lList.QListItemsAdded += QEditStore.QEditItemsHandle;
        lList.QListLockChanged += QEditTool.QEditLockHandle;
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

    public void QEditClose()
    {
        QWhitebalance lWhitebalance = QEditInspector.QInspectorVideo.QInspectorWhitebalance;
        QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateChanged -= QEditCropboxHandle;
        QEditInspector.QInspectorVideo.QInspectorVideoChanged -= QEditChangeHandle;
        QEditInspector.QInspectorPersistentChanged -= QEditStore.QEditPersistentSave;
        QEditInspector.QInspectorToolChanged -= QEditTool.QEditToolHandle;
        QEditInspector.QInspectorSkip.QSkipActiveChanged -= QEditSkipHandle;
        lWhitebalance.QWhitebalanceEstimateChanged -= QEditColor.QEditEstimateHandle;
        qEditViewer.QViewerMediaChanged -= QEditMediaHandle;
        qEditViewer.QViewerOpenNotice -= QEditOpenHandle;
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
        QEditColor.QEditEstimateHandle(QEditInspector.QInspectorVideo.QInspectorWhitebalance.QWhitebalanceMethod);
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

    private void QEditCropApply(LEditPlan lPlan)
    {
        QEditInspector.QInspectorCrop.QInspectorCropApply(lPlan.LEditCrop, lPlan.LEditCropActive);
        QEditInspector.QInspectorRatio.QInspectorRatioApply(
            lPlan.LEditRatioFixed, lPlan.LEditRatioLenient, lPlan.LEditRatioWidth, lPlan.LEditRatioHeight);
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
