using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LEditTab
{
    private readonly LPresetSelection lEditPreset;
    private readonly LViewer lEditViewer;
    private readonly LCrop lEditCrop;
    private readonly LList lEditList;
    private readonly LDocket lEditDocket;

    public LEditTab(CUsher lUsher, LPresetSelection lPreset, LViewer lViewer, LList lList)
    {
        lEditPreset = lPreset;
        lEditViewer = lViewer;
        lEditCrop = lViewer.LCrop;
        lEditList = lList;
        lEditDocket = lList.LListDocket;
        LEditColor = new LEditTabColor(LEditInspector, LEditProcessing, lViewer);
        LEditStore = new LEditTabPlan(lUsher, LEditInspector, lViewer, lEditDocket, LEditColor);
        QEditTool = new QEditTool(LEditInspector, lViewer);
        LEditProcessing.LProcessingOrderedSet(false);
        LEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateChanged += LEditCropHandle;
        LEditInspector.LInspectorVideoChanged += LEditChangeHandle;
        LEditInspector.LInspectorPersistentChanged += LEditStore.LEditPersistentSave;
        LEditInspector.LInspectorToolChanged += QEditTool.LEditToolHandle;
        LEditInspector.LInspectorSkip.LSkipActiveChanged += LEditSkipHandle;
        LEditInspector.LInspectorWhitebalance.LWhitebalanceEstimateChanged += LEditColor.LEditEstimateHandle;
        lViewer.LViewerMediaChanged += LEditMediaHandle;
        lViewer.LViewerEngineChanged += LEditColor.LEditCapableHandle;
        lViewer.LViewerEngineChanged += lViewer.LViewerNeutral.LViewerNeutralCancel;
        lViewer.LViewerNeutral.LViewerToolChanged += LEditInspector.LInspectorWhitebalance.LWhitebalanceToolSet;
        lViewer.LViewerNeutral.LViewerNeutralChanged += LEditColor.LEditNeutralHandle;
        lViewer.LViewerNeutral.LViewerEstimateChanged += LEditColor.LEditEstimateApply;
        LEditInspector.LInspectorWhitebalance.LWhitebalanceToolChanged += lViewer.LViewerNeutral.LViewerToolSet;
        lViewer.LCrop.LCropVideoChanged += LEditCropShow;
        LEditProcessing.LProcessingStepChanged += LEditStepHandle;
        lViewer.LViewerSource.LViewerPathsDrop += lDropPaths => _ = lList.LListPathsAdd(lDropPaths);
        lList.LListPathChanged += LEditPathHandle;
        lList.LListItemsAdded += LEditStore.LEditItemsHandle;
        lList.LListLockChanged += QEditTool.LEditLockHandle;
    }

    public event Action? LEditPresetMissing;
    public event Action? LEditPresetIncompatible;
    public event Action? LEditHistogramDeferred;
    public event Action? LEditColorDeferred;

    public LInspector LEditInspector { get; } = new();

    public LProcessing LEditProcessing { get; } = new();

    public LEditTabColor LEditColor { get; }

    public LEditTabPlan LEditStore { get; }

    public QEditTool QEditTool { get; }

    public void LEditClose()
    {
        LEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateChanged -= LEditCropHandle;
        LEditInspector.LInspectorVideoChanged -= LEditChangeHandle;
        LEditInspector.LInspectorPersistentChanged -= LEditStore.LEditPersistentSave;
        LEditInspector.LInspectorToolChanged -= QEditTool.LEditToolHandle;
        LEditInspector.LInspectorSkip.LSkipActiveChanged -= LEditSkipHandle;
        LEditInspector.LInspectorWhitebalance.LWhitebalanceEstimateChanged -= LEditColor.LEditEstimateHandle;
        lEditViewer.LViewerMediaChanged -= LEditMediaHandle;
        lEditViewer.LViewerEngineChanged -= LEditColor.LEditCapableHandle;
        lEditViewer.LViewerEngineChanged -= lEditViewer.LViewerNeutral.LViewerNeutralCancel;
        lEditViewer.LViewerNeutral.LViewerToolChanged -= LEditInspector.LInspectorWhitebalance.LWhitebalanceToolSet;
        lEditViewer.LViewerNeutral.LViewerNeutralChanged -= LEditColor.LEditNeutralHandle;
        lEditViewer.LViewerNeutral.LViewerEstimateChanged -= LEditColor.LEditEstimateApply;
        LEditInspector.LInspectorWhitebalance.LWhitebalanceToolChanged -= lEditViewer.LViewerNeutral.LViewerToolSet;
        lEditCrop.LCropVideoChanged -= LEditCropShow;
        LEditProcessing.LProcessingStepChanged -= LEditStepHandle;
    }

    public void LEditStart()
    {
        LEditCropUpdate();
        LEditColor.LEditColorUpdate();
        lEditCrop.LCropActiveSet(LEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateActive);
    }

    public LSceneTabEntry LEditLayoutRead(LSceneTabEntry lLayout)
    {
        if (LEditStore.LEditCarriedRead() is { } lCarried)
        {
            lLayout.LSceneInspector = new LSceneInspectorEntry
            {
                LSceneInspectorEdit = LEdit.LEditPersistentCreate(lCarried),
                LSceneInspectorCrop = LEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStatePersistent,
                LSceneInspectorSkip = LEditInspector.LInspectorSkip.LSkipPersistent
            };
        }

        return lLayout;
    }

    public void LEditLayoutApply(LSceneTabEntry? lLayout)
    {
        if (lLayout is null)
        {
            LEditInspector.LInspectorMinimizedSet(true);
        }

        if (lLayout?.LSceneInspector is not { LSceneInspectorEdit: { } lRecord } lPersistent)
        {
            return;
        }

        LEditInspector.LInspectorSaveSuspend();
        try
        {
            LEditPlan lPlan = LEdit.LEditPersistentRead(lRecord);
            if (lPersistent.LSceneInspectorCrop)
            {
                LEditCropApply(lPlan);
                LEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxPersistentSet(true);
            }

            LEditInspector.LInspectorVideoApply(lPlan.LEditVideo);
            LEditInspector.LInspectorPersistentApply(lPlan.LEditVideo);
            LEditInspector.LInspectorSkip.LSkipActiveSet(lPlan.LEditSkip);
            LEditInspector.LInspectorSkip.LSkipPersistentSet(lPersistent.LSceneInspectorSkip);
        }
        finally
        {
            LEditInspector.LInspectorSaveResume();
        }
    }

    public void LEditRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LEditPresetCheck() || LEditSelectedRead() is not { } lSelected)
        {
            return;
        }

        (LWorkCrop lCrop, LWorkVideo lVideo) =
            LEdit.LEditWorkResolve(LEditStore.LEditStateRead(), LEditColor.LEditEqCheck());
        LMessenger.LMessengerEditDescribe(
            lPriority,
            lSelected.LDocketEntryPath,
            lEditViewer.LViewerDuration,
            lCrop,
            lVideo,
            lEditPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab,
            lSelected.LDocketEntryBatch);
    }

    public void LEditAllRun(Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LEditPresetCheck())
        {
            return;
        }

        LMessenger.LMessengerEditDescribe(
            LWorkPriority.LWorkPriorityNormal,
            lEditDocket.LDocketUnlockedRead().Select(LEditSourceCreate).ToArray(),
            lEditPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void LEditItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LEditPresetCheck())
        {
            return;
        }

        LMessenger.LMessengerEditDescribe(
            LWorkPriority.LWorkPriorityNormal,
            lEditDocket.LDocketUnlockedRead()
                .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .Select(LEditSourceCreate)
                .ToArray(),
            lEditPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void LEditPathHandle(string? lPath)
    {
        lEditViewer.LViewerNeutral.LViewerNeutralCancel();
        if (string.IsNullOrWhiteSpace(lPath))
        {
            LTraceLog.LTraceInfoRecord("Edit click: no file selected");
            return;
        }

        if (lEditViewer.LViewerSourceMatch(lPath))
        {
            return;
        }

        LTraceLog.LTraceInfoRecord(
            $"Edit click '{LEditStore.LEditNameRead(lPath)}': "
            + $"persistent {(LEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStatePersistent ? "on" : "off")}, "
            + $"inspector now {LEditTabPlan.LEditCropFormat(LEditInspector.LInspectorCrop.LInspectorCropRead())}");
        lEditViewer.LViewerPathHandle(lPath);
    }

    public void LEditCropHandle()
    {
        LRotateFlip lRotate = LEditInspector.LInspectorCrop.LInspectorRotateRead();
        if (lEditViewer.LViewerPreview.LRotateFlip != lRotate)
        {
            lEditViewer.LViewerRotateSet(lRotate);
            LEditSourceSync();
        }

        (bool lRatioFixed, _, int lRatioWidth, int lRatioHeight) =
            LEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateRatio;
        lEditCrop.LCropRatioSet(lRatioFixed ? lRatioWidth : 0, lRatioFixed ? lRatioHeight : 0);
        lEditCrop.LCropPersistentSet(LEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStatePersistent);
        lEditCrop.LCropActiveSet(LEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateActive);
        lEditCrop.LCropRectSet(LEditInspector.LInspectorCrop.LInspectorRectRead());
        LEditCropUpdate();
        LEditStore.LEditStateSave();
    }

    public void LEditCropShow()
    {
        LEditSourceSync();
        LTraceLog.LTraceInfoRecord(
            $"Edit crop from viewer: {LEditTabPlan.LEditRectFormat(lEditViewer.LViewerPreview.LCropbox)}");
        LEditInspector.LInspectorCrop.LInspectorCropSet(
            lEditViewer.LViewerPreview.LCropbox, lEditCrop.LCropDrive, lEditCrop.LCropAnchorX, lEditCrop.LCropAnchorY);
    }

    public void LEditSkipHandle()
    {
        LEditProcessing.LProcessingSkipSet(LEditInspector.LInspectorSkip.LSkipActive);
        LEditViewerApply();
        LEditStore.LEditStateSave();
    }

    public void LEditChangeHandle()
    {
        LEditColor.LEditColorUpdate();
        LEditColorDeferred?.Invoke();
        LEditStore.LEditStateSave();
    }

    private void LEditMediaHandle(LCargo lCargo)
    {
        LEditColor.LEditEstimateHandle(LEditInspector.LInspectorWhitebalance.LWhitebalanceMethod);
        LEditHistogramDeferred?.Invoke();
        LEditCropRestore(lEditViewer.LViewerSourcePath is { } lPath ? LEditStore.LEditNameRead(lPath) : "(no media)");
    }

    private void LEditStepHandle(string lStep)
    {
        if (lStep == "Curve")
        {
            LEditHistogramDeferred?.Invoke();
        }
    }

    private void LEditCropRestore(string lName)
    {
        LEditPlan? lApplied = null;
        LEditInspector.LInspectorSaveSuspend();
        try
        {
            LEditSourceSync();
            LEditPlan? lPersistent = LEditStore.LEditCarriedRead();
            LEditPlan? lSaved = LEditStore.LEditSavedRead();
            LTraceLog.LTraceInfoRecord(
                $"Edit media ready '{lName}': display {LEditSourceFormat()}, "
                + $"persistent {(lPersistent is null ? "off" : "on")}, "
                + $"carried {LEditTabPlan.LEditPlanFormat(lPersistent)}, "
                + $"sidecar {LEditTabPlan.LEditPlanFormat(lSaved)}");
            LEditInspector.LInspectorCrop.LInspectorCropReset();

            bool lCarryWins = lPersistent is not null;
            LEditPlan lPlan = LEdit.LEditPlanResolve(
                lSaved,
                lPersistent,
                LEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStatePersistent,
                LEditInspector.LInspectorSkip.LSkipPersistent);
            LTraceLog.LTraceInfoRecord(
                $"Edit applying {(lCarryWins ? "persistent" : "sidecar")} plan to '{lName}': "
                + $"{LEditTabPlan.LEditPlanFormat(lPlan)}");
            lEditViewer.LViewerRotateSet(LRotateFlip.LRotateCropResolve(lPlan.LEditCrop));
            LEditSourceSync();

            lApplied = lCarryWins ? lPlan : null;
            LEditInspector.LInspectorPlanApply(lPlan);
        }
        finally
        {
            LEditInspector.LInspectorSaveResume();
        }

        LEditProcessing.LProcessingSkipSet(LEditInspector.LInspectorSkip.LSkipActive);
        LEditViewerApply();
        if (lApplied is not null)
        {
            LEditStore.LEditStateSave(lApplied);
        }
    }

    private void LEditCropApply(LEditPlan lPlan)
    {
        LEditInspector.LInspectorCrop.LInspectorCropApply(lPlan.LEditCrop, lPlan.LEditCropActive);
        LEditInspector.LInspectorCrop.LInspectorRatioApply(
            lPlan.LEditRatioFixed, lPlan.LEditRatioLenient, lPlan.LEditRatioWidth, lPlan.LEditRatioHeight);
    }

    private void LEditViewerApply()
    {
        bool lSkip = LEditInspector.LInspectorSkip.LSkipActive;
        LRotateFlip lRotate = lSkip
            ? LRotateFlip.LRotateDefaultCreate()
            : LEditInspector.LInspectorCrop.LInspectorRotateRead();
        LCropbox? lRect = lSkip ? null : LEditInspector.LInspectorCrop.LInspectorRectRead();
        LTraceLog.LTraceInfoRecord(
            $"Edit viewer push: rotate {lRotate.LRotateKind}, "
            + $"H {lRotate.LRotateFlipHorizontal}, V {lRotate.LRotateFlipVertical}, "
            + $"{LEditTabPlan.LEditRectFormat(lRect)}");
        lEditViewer.LViewerRotateSet(lRotate);
        lEditCrop.LCropRectSet(lRect);
        LEditColor.LEditColorApply();
    }

    private void LEditSourceSync()
    {
        LCropboxSize lSize = LEditSourceRead();
        LEditInspector.LInspectorSourceSet(lSize.LCropboxSizeWidth, lSize.LCropboxSizeHeight);
    }

    private LCropboxSize LEditSourceRead()
    {
        if (lEditViewer.LViewerMediaInfo is not { LMediaVideoPresent: true } lMediaInfo)
        {
            return new LCropboxSize(0, 0);
        }

        return LCropbox.LCropboxSourceResolve(
            lMediaInfo.LMediaVideoWidth,
            lMediaInfo.LMediaVideoHeight,
            lEditViewer.LViewerPreview.LRotateFlip.LRotateKind is LRotateKind.LRotate90 or LRotateKind.LRotate270);
    }

    private string LEditSourceFormat()
    {
        if (!lEditViewer.LViewerVideoPresent)
        {
            return "unknown";
        }

        LCropboxSize lSize = LEditSourceRead();
        return $"{lSize.LCropboxSizeWidth:0}x{lSize.LCropboxSizeHeight:0}";
    }

    private void LEditCropUpdate() =>
        LEditProcessing.LProcessingActiveSet("Crop", LEditInspector.LInspectorCrop.LInspectorActive);

    private bool LEditPresetCheck()
    {
        if (!lEditPreset.LPresetSelectionValid)
        {
            LEditPresetMissing?.Invoke();
            return false;
        }

        if (lEditPreset.LPresetSelectionEncoding is { } lEncoding
            && !lEncoding.LEncodingSupportCheck(LWorkKind.LWorkKindEdit))
        {
            LEditPresetIncompatible?.Invoke();
            return false;
        }

        return true;
    }

    private LDocketEntry? LEditSelectedRead() =>
        lEditList.LListPathCurrent is { } lPath
        && lEditDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    private static LWorkSource LEditSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);
}
