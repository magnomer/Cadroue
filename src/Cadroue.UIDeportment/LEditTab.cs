using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LEditTab
{
    public static readonly IReadOnlyList<LProcessingRow> LEditRows =
    [
        new("Crop", "/PAsset/PPanel/PProcessingCrop.svg", "Processing.Step.Crop"),
        new("Whitebalance", "/PAsset/PPanel/PProcessingWhitebalance.svg", "Processing.Step.Whitebalance"),
        new("Exposure", "/PAsset/PPanel/PProcessingExposure.svg", "Processing.Step.Exposure"),
        new("Brightness", "/PAsset/PPanel/PProcessingBrightness.svg", "Processing.Step.Brightness"),
        new("Contrast", "/PAsset/PPanel/PProcessingContrast.svg", "Processing.Step.Contrast"),
        new("Gamma", "/PAsset/PPanel/PProcessingGamma.svg", "Processing.Step.Gamma"),
        new("Saturation", "/PAsset/PPanel/PProcessingSaturation.svg", "Processing.Step.Saturation"),
        new("Curve", "/PAsset/PPanel/PProcessingCurve.svg", "Processing.Step.Curve"),
    ];

    private readonly LPresetSelection lEditPreset;
    private readonly LInspector lEditInspector;
    private readonly LViewer lEditViewer;
    private readonly LCrop lEditCrop;
    private readonly LList lEditList;
    private readonly LDocket lEditDocket;
    private readonly LProcessing lEditProcessing;

    public LEditTab(
        LPresetSelection lPreset,
        LInspector lInspector,
        LViewer lViewer,
        LList lList,
        LDocket lDocket,
        LProcessing lProcessing)
    {
        lEditPreset = lPreset;
        lEditInspector = lInspector;
        lEditViewer = lViewer;
        lEditCrop = lViewer.LCrop;
        lEditList = lList;
        lEditDocket = lDocket;
        lEditProcessing = lProcessing;
        LEditColor = new LEditTabColor(lInspector, lProcessing, lViewer);
        LEditStore = new LEditTabPlan(lInspector, lViewer, lDocket, LEditColor);
        lProcessing.LProcessingOrderedSet(false);
        lInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateChange += LEditCropHandle;
        lInspector.LInspectorVideoChange += LEditChangeHandle;
        lInspector.LInspectorPersistentChange += LEditStore.LEditPersistentSave;
        lInspector.LInspectorToolChange += LEditToolHandle;
        lInspector.LInspectorSkip.LSkipActiveChange += LEditSkipHandle;
        lInspector.LInspectorWhitebalance.LWhitebalanceEstimateChange += LEditColor.LEditEstimateHandle;
        lViewer.LViewerMediaChange += LEditMediaHandle;
        lViewer.LViewerEngineChange += LEditColor.LEditCapableHandle;
        lViewer.LViewerEngineChange += lViewer.LViewerNeutral.LViewerNeutralCancel;
        lViewer.LViewerNeutral.LViewerToolChange += lInspector.LInspectorWhitebalance.LWhitebalanceToolSet;
        lViewer.LViewerNeutral.LViewerNeutralChange += LEditColor.LEditNeutralHandle;
        lViewer.LViewerNeutral.LViewerEstimateChange += LEditColor.LEditEstimateApply;
        lInspector.LInspectorWhitebalance.LWhitebalanceToolChange += lViewer.LViewerNeutral.LViewerToolSet;
        lViewer.LCrop.LCropVideoChange += LEditCropShow;
        lProcessing.LProcessingStepChange += LEditStepHandle;
    }

    public event Action? LEditPresetMissing;
    public event Action? LEditPresetIncompatible;
    public event Action? LEditHistogramDefer;
    public event Action? LEditColorDefer;

    public LEditTabColor LEditColor { get; }

    public LEditTabPlan LEditStore { get; }

    public void LEditClose()
    {
        lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateChange -= LEditCropHandle;
        lEditInspector.LInspectorVideoChange -= LEditChangeHandle;
        lEditInspector.LInspectorPersistentChange -= LEditStore.LEditPersistentSave;
        lEditInspector.LInspectorToolChange -= LEditToolHandle;
        lEditInspector.LInspectorSkip.LSkipActiveChange -= LEditSkipHandle;
        lEditInspector.LInspectorWhitebalance.LWhitebalanceEstimateChange -= LEditColor.LEditEstimateHandle;
        lEditViewer.LViewerMediaChange -= LEditMediaHandle;
        lEditViewer.LViewerEngineChange -= LEditColor.LEditCapableHandle;
        lEditViewer.LViewerEngineChange -= lEditViewer.LViewerNeutral.LViewerNeutralCancel;
        lEditViewer.LViewerNeutral.LViewerToolChange -= lEditInspector.LInspectorWhitebalance.LWhitebalanceToolSet;
        lEditViewer.LViewerNeutral.LViewerNeutralChange -= LEditColor.LEditNeutralHandle;
        lEditViewer.LViewerNeutral.LViewerEstimateChange -= LEditColor.LEditEstimateApply;
        lEditInspector.LInspectorWhitebalance.LWhitebalanceToolChange -= lEditViewer.LViewerNeutral.LViewerToolSet;
        lEditCrop.LCropVideoChange -= LEditCropShow;
        lEditProcessing.LProcessingStepChange -= LEditStepHandle;
    }

    public void LEditStart()
    {
        LEditCropUpdate();
        LEditColor.LEditColorUpdate();
        lEditCrop.LCropActiveSet(lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateActive);
    }

    public LSceneTabRecord LEditLayoutRead(LSceneTabRecord lLayout)
    {
        if (LEditStore.LEditCarriedRead() is { } lCarried)
        {
            lLayout.LSceneInspector = new LSceneInspectorRecord
            {
                LSceneInspectorEdit = LEdit.LEditPersistentCreate(lCarried),
                LSceneInspectorCrop = lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStatePersistent,
                LSceneInspectorSkip = lEditInspector.LInspectorSkip.LSkipPersistent
            };
        }

        return lLayout;
    }

    public void LEditLayoutApply(LSceneTabRecord? lLayout)
    {
        if (lLayout is null)
        {
            lEditInspector.LInspectorMinimizedSet(true);
        }

        if (lLayout?.LSceneInspector is not { LSceneInspectorEdit: { } lRecord } lPersistent)
        {
            return;
        }

        lEditInspector.LInspectorSaveSuspend();
        try
        {
            LEditPlan lPlan = LEdit.LEditPersistentRead(lRecord);
            if (lPersistent.LSceneInspectorCrop)
            {
                LEditCropApply(lPlan);
                lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxPersistentSet(true);
            }

            lEditInspector.LInspectorVideoApply(lPlan.LEditVideo);
            lEditInspector.LInspectorPersistentApply(lPlan.LEditVideo);
            lEditInspector.LInspectorSkip.LSkipActiveSet(lPlan.LEditSkip);
            lEditInspector.LInspectorSkip.LSkipPersistentSet(lPersistent.LSceneInspectorSkip);
        }
        finally
        {
            lEditInspector.LInspectorSaveResume();
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
            $"Edit click '{LUsher.LUsherNameRead(lPath)}': "
            + $"persistent {(lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStatePersistent ? "on" : "off")}, "
            + $"inspector now {LEditTabPlan.LEditCropFormat(lEditInspector.LInspectorCrop.LInspectorCropRead())}");
        lEditViewer.LViewerPathHandle(lPath);
    }

    public void LEditCropHandle()
    {
        LRotateFlip lRotate = lEditInspector.LInspectorCrop.LInspectorRotateRead();
        if (lEditViewer.LViewerPreview.LRotateFlip != lRotate)
        {
            lEditViewer.LViewerRotateSet(lRotate);
            LEditSourceSync();
        }

        (bool lRatioFixed, _, int lRatioWidth, int lRatioHeight) =
            lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateRatio;
        lEditCrop.LCropRatioSet(lRatioFixed ? lRatioWidth : 0, lRatioFixed ? lRatioHeight : 0);
        lEditCrop.LCropPersistentSet(lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStatePersistent);
        lEditCrop.LCropActiveSet(lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateActive);
        lEditCrop.LCropRectSet(lEditInspector.LInspectorCrop.LInspectorRectRead());
        LEditCropUpdate();
        LEditStore.LEditStateSave();
    }

    public void LEditCropShow()
    {
        LEditSourceSync();
        LTraceLog.LTraceInfoRecord(
            $"Edit crop from viewer: {LEditTabPlan.LEditRectFormat(lEditViewer.LViewerPreview.LCropbox)}");
        lEditInspector.LInspectorCrop.LInspectorCropSet(
            lEditViewer.LViewerPreview.LCropbox, lEditCrop.LCropDrive, lEditCrop.LCropAnchorX, lEditCrop.LCropAnchorY);
    }

    public void LEditSkipHandle()
    {
        lEditProcessing.LProcessingSkipSet(lEditInspector.LInspectorSkip.LSkipActive);
        LEditViewerApply();
        LEditStore.LEditStateSave();
    }

    public void LEditChangeHandle()
    {
        LEditColor.LEditColorUpdate();
        LEditColorDefer?.Invoke();
        LEditStore.LEditStateSave();
    }

    private void LEditToolHandle(bool lArmed) => lEditCrop.LCropToolSet(lArmed);

    public void LEditLockHandle(bool lLocked)
    {
        if (lLocked)
        {
            lEditViewer.LViewerNeutral.LViewerNeutralCancel();
        }

        lEditCrop.LCropLockSet(lLocked);
        lEditCrop.LCropToolSet(!lLocked && lEditInspector.LInspectorToolArmed);
    }

    private void LEditMediaHandle(LCargo lCargo)
    {
        LEditColor.LEditEstimateHandle(lEditInspector.LInspectorWhitebalance.LWhitebalanceMethod);
        LEditHistogramDefer?.Invoke();
        LEditCropRestore();
    }

    private void LEditStepHandle(string lStep)
    {
        if (lStep == "Curve")
        {
            LEditHistogramDefer?.Invoke();
        }
    }

    private void LEditCropRestore()
    {
        string lName = lEditViewer.LViewerSourcePath is { } lPath ? LUsher.LUsherNameRead(lPath) : "(no media)";
        LEditPlan? lApplied = null;
        lEditInspector.LInspectorSaveSuspend();
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
            lEditInspector.LInspectorCrop.LInspectorCropReset();

            bool lCarryWins = lPersistent is not null;
            LEditPlan lPlan = LEdit.LEditPlanResolve(
                lSaved,
                lPersistent,
                lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStatePersistent,
                lEditInspector.LInspectorSkip.LSkipPersistent);
            LTraceLog.LTraceInfoRecord(
                $"Edit applying {(lCarryWins ? "persistent" : "sidecar")} plan to '{lName}': "
                + $"{LEditTabPlan.LEditPlanFormat(lPlan)}");
            lEditViewer.LViewerRotateSet(LRotateFlip.LRotateCropResolve(lPlan.LEditCrop));
            LEditSourceSync();

            lApplied = lCarryWins ? lPlan : null;
            lEditInspector.LInspectorPlanApply(lPlan);
        }
        finally
        {
            lEditInspector.LInspectorSaveResume();
        }

        lEditProcessing.LProcessingSkipSet(lEditInspector.LInspectorSkip.LSkipActive);
        LEditViewerApply();
        if (lApplied is not null)
        {
            LEditStore.LEditStateSave(lApplied);
        }
    }

    private void LEditCropApply(LEditPlan lPlan)
    {
        lEditInspector.LInspectorCrop.LInspectorCropApply(lPlan.LEditCrop, lPlan.LEditCropActive);
        lEditInspector.LInspectorCrop.LInspectorRatioApply(
            lPlan.LEditRatioFixed, lPlan.LEditRatioLenient, lPlan.LEditRatioWidth, lPlan.LEditRatioHeight);
    }

    private void LEditViewerApply()
    {
        bool lSkip = lEditInspector.LInspectorSkip.LSkipActive;
        LRotateFlip lRotate = lSkip
            ? LRotateFlip.LRotateDefaultCreate()
            : lEditInspector.LInspectorCrop.LInspectorRotateRead();
        LCropbox? lRect = lSkip ? null : lEditInspector.LInspectorCrop.LInspectorRectRead();
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
        lEditInspector.LInspectorSourceSet(lSize.LCropboxSizeWidth, lSize.LCropboxSizeHeight);
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
        lEditProcessing.LProcessingActiveSet("Crop", lEditInspector.LInspectorCrop.LInspectorActive);

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
