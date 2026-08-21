using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PEditTab
{
    private void PEditCropSet() => pCropOwner.LCropboxCropSet(pInspector.PInspectorCropRead());

    private void PEditRatioSet()
    {
        (bool pRatioFixed, bool pRatioLenient, int pRatioWidth, int pRatioHeight) = pInspector.PInspectorRatioRead();
        pCropOwner.LCropboxRatioSet(pRatioFixed, pRatioLenient, pRatioWidth, pRatioHeight);
    }

    private void PEditActiveSet()
    {
        pCropOwner.LCropboxApplySet(pInspector.PCropActiveCheck());
        pViewer.PCropActiveSet(pInspector.PCropActiveCheck());
    }

    private void PEditActiveUpdate()
    {
        pProcessing.PProcessingActiveSet("Crop", pCropOwner.LCropboxStateActive);
        pProcessing.PProcessingActiveSet("Brightness",
            pInspector.PToneStepRead(LColorKind.LColorKindBrightness).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Contrast",
            pInspector.PToneStepRead(LColorKind.LColorKindContrast).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Saturation",
            pInspector.PToneStepRead(LColorKind.LColorKindSaturation).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Gamma",
            PEditMpvCheck()
            && pInspector.PToneStepRead(LColorKind.LColorKindGamma).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Exposure",
            PEditMpvCheck()
            && pInspector.PToneStepRead(LColorKind.LColorKindExposure).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Whitebalance",
            PEditMpvCheck()
            && pInspector.PToneStepRead(LColorKind.LColorKindWhitebalance).LWorkStepActive);
    }

    private void PEditPathShow(string? pSourcePath)
    {
        pViewer.PViewerNeutralCancel();
        if (string.IsNullOrWhiteSpace(pSourcePath))
        {
            LTraceLog.LTraceInfoRecord("Edit click: no file selected");
            return;
        }

        LTraceLog.LTraceInfoRecord(
            $"Edit click '{System.IO.Path.GetFileName(pSourcePath)}': "
            + $"persistent {(pInspector.PCropPersistentCheck() ? "on" : "off")}, "
            + $"inspector now {PEditCropFormat(pInspector.PInspectorCropRead())}");
        pViewer.PViewerSourceOpen(pSourcePath);
    }

    private void PEditCropShow(System.Windows.Rect? pCropVideo)
    {
        if (pViewer.PCropSourceRead() is System.Windows.Size pCropSource)
        {
            pInspector.PInspectorSourceSet(pCropSource.Width, pCropSource.Height);
        }
        else
        {
            pInspector.PInspectorSourceSet(0, 0);
        }

        LTraceLog.LTraceInfoRecord($"Edit crop from viewer: {PEditRectFormat(pCropVideo)}");
        (int pCropDrive, int pCropAnchorX, int pCropAnchorY) = pViewer.PCropAnchorRead();
        pInspector.PInspectorCropSet(pCropVideo, pCropDrive, pCropAnchorX, pCropAnchorY);
    }

    private void PEditCropRestore()
    {
        string pEditName = pViewer.PViewerSourcePath is { } pEditPath
            ? System.IO.Path.GetFileName(pEditPath)
            : "(no media)";

        pEditPlanLoading = true;
        pInspector.PInspectorCropChange -= pViewer.PCropVideoSet;
        pInspector.PInspectorRotateChange -= PEditRotateHandle;
        try
        {
            System.Windows.Size? pCropSource = pViewer.PCropSourceRead();
            if (pCropSource is { } pCropSize)
            {
                pInspector.PInspectorSourceSet(pCropSize.Width, pCropSize.Height);
            }
            else
            {
                pInspector.PInspectorSourceSet(0, 0);
            }

            LEditPlan? pEditPersistent = PEditCarriedRead();
            LEditPlan? pEditSaved = pViewer.PViewerSourcePath is { } pEditSourcePath
                ? LEdit.LEditPlanRead(pEditSourcePath, LLibrarian.LLibrarianEditLoad)
                : null;

            LTraceLog.LTraceInfoRecord(
                $"Edit media ready '{pEditName}': "
                + $"display {(pCropSource is { } pLogSize ? $"{pLogSize.Width:0}x{pLogSize.Height:0}" : "unknown")}, "
                + $"persistent {(pEditPersistent is null ? "off" : "on")}, "
                + $"carried {PEditPlanFormat(pEditPersistent)}, "
                + $"sidecar {PEditPlanFormat(pEditSaved)}");

            pInspector.PCropMediaReset();

            bool pEditCarryWins = pEditPersistent is not null;
            LEditPlan pEditPlan = LEdit.LEditPlanResolve(
                pEditSaved,
                pEditPersistent,
                pCropOwner.LCropboxStatePersistent,
                pInspector.PSkipPersistentCheck());

            LTraceLog.LTraceInfoRecord(
                $"Edit applying {(pEditCarryWins ? "persistent" : "sidecar")} plan to '{pEditName}': "
                + $"{PEditPlanFormat(pEditPlan)}");
            pViewer.PViewerRotateSet(PEditRotateResolve(pEditPlan.LEditCrop));
            if (pViewer.PCropSourceRead() is { } pEditRotatedSource)
            {
                pInspector.PInspectorSourceSet(pEditRotatedSource.Width, pEditRotatedSource.Height);
            }

            pInspector.PCropPlanApply(pEditPlan.LEditCrop, pEditPlan.LEditCropActive);
            pInspector.PInspectorRatioApply(pEditPlan.LEditRatioFixed, pEditPlan.LEditRatioLenient, pEditPlan.LEditRatioWidth, pEditPlan.LEditRatioHeight);
            pInspector.PTonePlanApply(pEditPlan.LEditVideo);
            pInspector.PSkipApply(pEditPlan.LEditSkip);
            pCropOwner.LCropboxStateSet(
                pEditPlan.LEditCrop,
                pEditPlan.LEditCropActive,
                pEditPlan.LEditRatioFixed,
                pEditPlan.LEditRatioLenient,
                pEditPlan.LEditRatioWidth,
                pEditPlan.LEditRatioHeight);
        }
        finally
        {
            pInspector.PInspectorCropChange += pViewer.PCropVideoSet;
            pInspector.PInspectorRotateChange += PEditRotateHandle;
            pEditPlanLoading = false;
        }

        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PEditViewerApply();
        PEditPlanSave();
    }

    private void PEditViewerApply()
    {
        LRotateFlip pEditRotate = pInspector.PInspectorRotateRead();
        System.Windows.Rect? pEditRect = pInspector.PInspectorRectRead();
        LTraceLog.LTraceInfoRecord(
            $"Edit viewer push: rotate {pEditRotate.LRotateKind}, "
            + $"H {pEditRotate.LRotateFlipHorizontal}, V {pEditRotate.LRotateFlipVertical}, "
            + $"{PEditRectFormat(pEditRect)}");

        pViewer.PViewerRotateSet(pEditRotate);
        pViewer.PCropVideoSet(pEditRect);
        PEditColorApply();
    }

    private void PEditRotateHandle(LRotateFlip pRotateFlip)
    {
        LRotateFlip pEditOldRotate = pViewer.LPreviewStateCurrent.LRotateFlip;
        pViewer.PViewerRotateSet(pRotateFlip);
        if (pViewer.PCropSourceRead() is { } pRotatedSource)
        {
            pInspector.PInspectorSourceSet(pRotatedSource.Width, pRotatedSource.Height);
        }

        pInspector.PInspectorOrientationApply(pEditOldRotate);
    }

    private static LRotateFlip PEditRotateResolve(LWorkCrop pEditCrop) => new(
        pEditCrop.LWorkCropRotation switch
        {
            90 => LRotateKind.LRotate90,
            180 => LRotateKind.LRotate180,
            270 => LRotateKind.LRotate270,
            _ => LRotateKind.LRotateNone
        },
        pEditCrop.LWorkFlipHorizontal,
        pEditCrop.LWorkFlipVertical);
}
