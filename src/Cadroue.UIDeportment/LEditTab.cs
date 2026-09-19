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
        LCrop lCrop,
        LList lList,
        LDocket lDocket,
        LProcessing lProcessing)
    {
        lEditPreset = lPreset;
        lEditInspector = lInspector;
        lEditViewer = lViewer;
        lEditCrop = lCrop;
        lEditList = lList;
        lEditDocket = lDocket;
        lEditProcessing = lProcessing;
        LEditColor = new LEditTabColor(lInspector, lProcessing);
        LEditStore = new LEditTabPlan(lInspector, lViewer, lDocket, LEditColor);
        lProcessing.LProcessingOrderedSet(false);
        lInspector.LInspectorCropbox.LCropboxStateChange += LEditCropHandle;
        lInspector.LInspectorWhitebalance.LWhitebalanceEstimateChange += LEditColor.LEditEstimateHandle;
        lViewer.LViewerMediaChange += LEditMediaHandle;
        lProcessing.LProcessingStepChange += LEditStepHandle;
    }

    public event Action? LEditPresetMissing;
    public event Action? LEditPresetIncompatible;
    public event Action<LRotateFlip>? LEditRotateApply;
    public event Action<LCropbox?>? LEditRectApply;
    public event Action<bool>? LEditActiveApply;
    public event Action<bool>? LEditLockApply;
    public event Action<bool>? LEditToolApply;
    public event Action? LEditNeutralCancel;
    public event Action? LEditHistogramDefer;
    public event Action? LEditColorDefer;

    public LEditTabColor LEditColor { get; }

    public LEditTabPlan LEditStore { get; }

    public void LEditClose()
    {
        lEditInspector.LInspectorCropbox.LCropboxStateChange -= LEditCropHandle;
        lEditInspector.LInspectorWhitebalance.LWhitebalanceEstimateChange -= LEditColor.LEditEstimateHandle;
        lEditViewer.LViewerMediaChange -= LEditMediaHandle;
        lEditProcessing.LProcessingStepChange -= LEditStepHandle;
    }

    public void LEditStart()
    {
        LEditCropUpdate();
        LEditColor.LEditColorUpdate();
        LEditActiveApply?.Invoke(lEditInspector.LInspectorCropbox.LCropboxStateActive);
    }

    public LSceneTabRecord LEditLayoutRead(LSceneTabRecord lLayout)
    {
        if (LEditStore.LEditCarriedRead() is { } lCarried)
        {
            lLayout.LSceneInspector = new LSceneInspectorRecord
            {
                LSceneInspectorEdit = LEdit.LEditPersistentCreate(lCarried),
                LSceneInspectorCrop = lEditInspector.LInspectorCropbox.LCropboxStatePersistent,
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
                lEditInspector.LInspectorCropbox.LCropboxPersistentSet(true);
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
        LEditNeutralCancel?.Invoke();
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
            + $"persistent {(lEditInspector.LInspectorCropbox.LCropboxStatePersistent ? "on" : "off")}, "
            + $"inspector now {LEditTabPlan.LEditCropFormat(lEditInspector.LInspectorCropRead())}");
        lEditViewer.LViewerPathHandle(lPath);
    }

    public void LEditCropHandle()
    {
        LRotateFlip lRotate = lEditInspector.LInspectorRotateRead();
        if (lEditViewer.LViewerPreview.LRotateFlip != lRotate)
        {
            LEditRotateApply?.Invoke(lRotate);
            LEditSourceSync();
        }

        (bool lRatioFixed, _, int lRatioWidth, int lRatioHeight) =
            lEditInspector.LInspectorCropbox.LCropboxStateRatio;
        lEditCrop.LCropRatioSet(lRatioFixed ? lRatioWidth : 0, lRatioFixed ? lRatioHeight : 0);
        lEditCrop.LCropPersistentSet(lEditInspector.LInspectorCropbox.LCropboxStatePersistent);
        LEditActiveApply?.Invoke(lEditInspector.LInspectorCropbox.LCropboxStateActive);
        LEditRectApply?.Invoke(lEditInspector.LInspectorRectRead());
        LEditCropUpdate();
        LEditStore.LEditStateSave();
    }

    public void LEditCropShow()
    {
        LEditSourceSync();
        LTraceLog.LTraceInfoRecord(
            $"Edit crop from viewer: {LEditTabPlan.LEditRectFormat(lEditViewer.LViewerPreview.LCropbox)}");
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

    public void LEditLockHandle(bool lLocked)
    {
        if (lLocked)
        {
            LEditNeutralCancel?.Invoke();
        }

        LEditLockApply?.Invoke(lLocked);
        LEditToolApply?.Invoke(!lLocked && lEditInspector.LInspectorToolArmed);
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
            lEditInspector.LInspectorCropReset();

            bool lCarryWins = lPersistent is not null;
            LEditPlan lPlan = LEdit.LEditPlanResolve(
                lSaved,
                lPersistent,
                lEditInspector.LInspectorCropbox.LCropboxStatePersistent,
                lEditInspector.LInspectorSkip.LSkipPersistent);
            LTraceLog.LTraceInfoRecord(
                $"Edit applying {(lCarryWins ? "persistent" : "sidecar")} plan to '{lName}': "
                + $"{LEditTabPlan.LEditPlanFormat(lPlan)}");
            LEditRotateApply?.Invoke(LRotateFlip.LRotateCropResolve(lPlan.LEditCrop));
            LEditSourceSync();

            lApplied = lCarryWins ? lPlan : null;
            LEditCropApply(lPlan);
            lEditInspector.LInspectorVideoApply(lPlan.LEditVideo);
            lEditInspector.LInspectorSkip.LSkipActiveSet(lPlan.LEditSkip);
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
        lEditInspector.LInspectorCropApply(lPlan.LEditCrop, lPlan.LEditCropActive);
        lEditInspector.LInspectorRatioApply(
            lPlan.LEditRatioFixed, lPlan.LEditRatioLenient, lPlan.LEditRatioWidth, lPlan.LEditRatioHeight);
    }

    private void LEditViewerApply()
    {
        bool lSkip = lEditInspector.LInspectorSkip.LSkipActive;
        LRotateFlip lRotate = lSkip ? LRotateFlip.LRotateDefaultCreate() : lEditInspector.LInspectorRotateRead();
        LCropbox? lRect = lSkip ? null : lEditInspector.LInspectorRectRead();
        LTraceLog.LTraceInfoRecord(
            $"Edit viewer push: rotate {lRotate.LRotateKind}, "
            + $"H {lRotate.LRotateFlipHorizontal}, V {lRotate.LRotateFlipVertical}, "
            + $"{LEditTabPlan.LEditRectFormat(lRect)}");
        LEditRotateApply?.Invoke(lRotate);
        LEditRectApply?.Invoke(lRect);
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
        lEditProcessing.LProcessingActiveSet("Crop", lEditInspector.LInspectorCropbox.LCropboxStateActive);

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
