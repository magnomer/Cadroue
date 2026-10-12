using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QEditTab
{
    private readonly LPresetSelection qEditPreset;
    private readonly QViewer qEditViewer;
    private readonly QCrop qEditCrop;
    private readonly QList qEditList;
    private readonly LDocket qEditDocket;

    public QEditTab(CUsher lUsher, LPresetSelection lPreset, QViewer lViewer, QList lList)
    {
        qEditPreset = lPreset;
        qEditViewer = lViewer;
        qEditCrop = lViewer.QCrop;
        qEditList = lList;
        qEditDocket = lList.QListDocket;
        QEditColor = new QEditTabColor(QEditInspector, QEditProcessing, lViewer);
        QEditStore = new QEditTabPlan(lUsher, QEditInspector, lViewer, qEditDocket, QEditColor);
        QEditTool = new QEditTool(QEditInspector, lViewer);
        QEditProcessing.QProcessingOrderedSet(false);
        QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateChanged += QEditCropHandle;
        QEditInspector.QInspectorVideoChanged += QEditChangeHandle;
        QEditInspector.QInspectorPersistentChanged += QEditStore.QEditPersistentSave;
        QEditInspector.QInspectorToolChanged += QEditTool.QEditToolHandle;
        QEditInspector.QInspectorSkip.QSkipActiveChanged += QEditSkipHandle;
        QEditInspector.QInspectorWhitebalance.QWhitebalanceEstimateChanged += QEditColor.QEditEstimateHandle;
        lViewer.QViewerMediaChanged += QEditMediaHandle;
        lViewer.QViewerEngineChanged += QEditColor.QEditCapableHandle;
        lViewer.QViewerEngineChanged += lViewer.QViewerNeutral.QViewerNeutralCancel;
        lViewer.QViewerNeutral.QViewerToolChanged += QEditInspector.QInspectorWhitebalance.QWhitebalanceToolSet;
        lViewer.QViewerNeutral.QViewerNeutralChanged += QEditColor.QEditNeutralHandle;
        lViewer.QViewerNeutral.QViewerEstimateChanged += QEditColor.QEditEstimateApply;
        QEditInspector.QInspectorWhitebalance.QWhitebalanceToolChanged += lViewer.QViewerNeutral.QViewerToolSet;
        lViewer.QCrop.QCropVideoChanged += QEditCropShow;
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

    public QEditTool QEditTool { get; }

    public void QEditClose()
    {
        QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateChanged -= QEditCropHandle;
        QEditInspector.QInspectorVideoChanged -= QEditChangeHandle;
        QEditInspector.QInspectorPersistentChanged -= QEditStore.QEditPersistentSave;
        QEditInspector.QInspectorToolChanged -= QEditTool.QEditToolHandle;
        QEditInspector.QInspectorSkip.QSkipActiveChanged -= QEditSkipHandle;
        QEditInspector.QInspectorWhitebalance.QWhitebalanceEstimateChanged -= QEditColor.QEditEstimateHandle;
        qEditViewer.QViewerMediaChanged -= QEditMediaHandle;
        qEditViewer.QViewerEngineChanged -= QEditColor.QEditCapableHandle;
        qEditViewer.QViewerEngineChanged -= qEditViewer.QViewerNeutral.QViewerNeutralCancel;
        qEditViewer.QViewerNeutral.QViewerToolChanged -= QEditInspector.QInspectorWhitebalance.QWhitebalanceToolSet;
        qEditViewer.QViewerNeutral.QViewerNeutralChanged -= QEditColor.QEditNeutralHandle;
        qEditViewer.QViewerNeutral.QViewerEstimateChanged -= QEditColor.QEditEstimateApply;
        QEditInspector.QInspectorWhitebalance.QWhitebalanceToolChanged -= qEditViewer.QViewerNeutral.QViewerToolSet;
        qEditCrop.QCropVideoChanged -= QEditCropShow;
        QEditProcessing.QProcessingStepChanged -= QEditStepHandle;
    }

    public void QEditStart()
    {
        QEditCropUpdate();
        QEditColor.QEditColorUpdate();
        qEditCrop.QCropActiveSet(QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateActive);
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

            QEditInspector.QInspectorVideoApply(lPlan.LEditVideo);
            QEditInspector.QInspectorPersistentApply(lPlan.LEditVideo);
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

    public void QEditCropHandle()
    {
        LRotateFlip lRotate = QEditInspector.QInspectorCrop.QInspectorRotateRead();
        if (qEditViewer.QViewerPreview.LRotateFlip != lRotate)
        {
            qEditViewer.QViewerRotateSet(lRotate);
            QEditSourceSync();
        }

        (bool lRatioFixed, _, int lRatioWidth, int lRatioHeight) =
            QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateRatio;
        qEditCrop.QCropRatioSet(lRatioFixed ? lRatioWidth : 0, lRatioFixed ? lRatioHeight : 0);
        qEditCrop.QCropPersistentSet(QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStatePersistent);
        qEditCrop.QCropActiveSet(QEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateActive);
        qEditCrop.QCropRectSet(QEditInspector.QInspectorCrop.QInspectorRectRead());
        QEditCropUpdate();
        QEditStore.QEditStateSave();
    }

    public void QEditCropShow()
    {
        QEditSourceSync();
        LTraceLog.LTraceInfoRecord(
            $"Edit crop from viewer: {QEditTabPlan.QEditRectFormat(qEditViewer.QViewerPreview.LCropbox)}");
        QEditInspector.QInspectorCrop.QInspectorCropSet(
            qEditViewer.QViewerPreview.LCropbox, qEditCrop.QCropDrive, qEditCrop.QCropAnchorX, qEditCrop.QCropAnchorY);
    }

    public void QEditSkipHandle()
    {
        QEditProcessing.QProcessingSkipSet(QEditInspector.QInspectorSkip.QSkipActive);
        QEditViewerApply();
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
        QEditColor.QEditEstimateHandle(QEditInspector.QInspectorWhitebalance.QWhitebalanceMethod);
        QEditHistogramDeferred?.Invoke();
        QEditCropRestore(qEditViewer.QViewerSourcePath is { } lPath ? QEditStore.QEditNameRead(lPath) : "(no media)");
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
            QEditSourceSync();
            LEditPlan? lPersistent = QEditStore.QEditCarriedRead();
            LEditPlan? lSaved = QEditStore.QEditSavedRead();
            LTraceLog.LTraceInfoRecord(
                $"Edit media ready '{lName}': display {QEditSourceFormat()}, "
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
            QEditSourceSync();

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
            QEditStore.QEditStateSave(lApplied);
        }
    }

    private void QEditCropApply(LEditPlan lPlan)
    {
        QEditInspector.QInspectorCrop.QInspectorCropApply(lPlan.LEditCrop, lPlan.LEditCropActive);
        QEditInspector.QInspectorCrop.QInspectorRatioApply(
            lPlan.LEditRatioFixed, lPlan.LEditRatioLenient, lPlan.LEditRatioWidth, lPlan.LEditRatioHeight);
    }

    private void QEditViewerApply()
    {
        bool lSkip = QEditInspector.QInspectorSkip.QSkipActive;
        LRotateFlip lRotate = lSkip
            ? LRotateFlip.LRotateDefaultCreate()
            : QEditInspector.QInspectorCrop.QInspectorRotateRead();
        LCropbox? lRect = lSkip ? null : QEditInspector.QInspectorCrop.QInspectorRectRead();
        LTraceLog.LTraceInfoRecord(
            $"Edit viewer push: rotate {lRotate.LRotateKind}, "
            + $"H {lRotate.LRotateFlipHorizontal}, V {lRotate.LRotateFlipVertical}, "
            + $"{QEditTabPlan.QEditRectFormat(lRect)}");
        qEditViewer.QViewerRotateSet(lRotate);
        qEditCrop.QCropRectSet(lRect);
        QEditColor.QEditColorApply();
    }

    private void QEditSourceSync()
    {
        LCropboxSize lSize = QEditSourceRead();
        QEditInspector.QInspectorSourceSet(lSize.LCropboxSizeWidth, lSize.LCropboxSizeHeight);
    }

    private LCropboxSize QEditSourceRead()
    {
        if (qEditViewer.QViewerMediaInfo is not { LMediaVideoPresent: true } lMediaInfo)
        {
            return new LCropboxSize(0, 0);
        }

        return LCropbox.LCropboxSourceResolve(
            lMediaInfo.LMediaVideoWidth,
            lMediaInfo.LMediaVideoHeight,
            qEditViewer.QViewerPreview.LRotateFlip.LRotateKind is LRotateKind.LRotate90 or LRotateKind.LRotate270);
    }

    private string QEditSourceFormat()
    {
        if (!qEditViewer.QViewerVideoPresent)
        {
            return "unknown";
        }

        LCropboxSize lSize = QEditSourceRead();
        return $"{lSize.LCropboxSizeWidth:0}x{lSize.LCropboxSizeHeight:0}";
    }

    private void QEditCropUpdate() =>
        QEditProcessing.QProcessingActiveSet("Crop", QEditInspector.QInspectorCrop.QInspectorActive);

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
