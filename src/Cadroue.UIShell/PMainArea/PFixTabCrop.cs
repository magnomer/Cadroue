using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PFixTab
{
    private void PFixCropSet() => pCropOwner.LCropboxCropSet(pInspector.PInspectorCropRead());

    private void PFixRatioSet()
    {
        (bool pRatioFixed, bool pRatioLenient, int pRatioWidth, int pRatioHeight) = pInspector.PInspectorRatioRead();
        pCropOwner.LCropboxRatioSet(pRatioFixed, pRatioLenient, pRatioWidth, pRatioHeight);
    }

    private void PFixActiveSet()
    {
        pCropOwner.LCropboxApplySet(pInspector.PCropActiveCheck());
        pViewer.PCropActiveSet(pInspector.PCropActiveCheck());
    }

    private void PFixCropUpdate() =>
        pProcessing.PProcessingActiveSet("Crop", pCropOwner.LCropboxStateActive);

    private void PFixPathShow(string? pSourcePath)
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
            + $"inspector now {PFixCropFormat(pInspector.PInspectorCropRead())}");
        pViewer.PViewerSourceOpen(pSourcePath);
    }

    private void PFixCropShow(System.Windows.Rect? pCropVideo)
    {
        if (pViewer.PCropSourceRead() is System.Windows.Size pCropSource)
        {
            pInspector.PInspectorSourceSet(pCropSource.Width, pCropSource.Height);
        }
        else
        {
            pInspector.PInspectorSourceSet(0, 0);
        }

        LTraceLog.LTraceInfoRecord($"Edit crop from viewer: {PFixRectFormat(pCropVideo)}");
        (int pCropDrive, int pCropAnchorX, int pCropAnchorY) = pViewer.PCropAnchorRead();
        pInspector.PInspectorCropSet(pCropVideo, pCropDrive, pCropAnchorX, pCropAnchorY);
    }

    private void PFixCropRestore()
    {
        string pFixName = pViewer.PViewerSourcePath is { } pFixPath
            ? System.IO.Path.GetFileName(pFixPath)
            : "(no media)";

        pFixPlanLoading = true;
        pInspector.PInspectorCropChange -= pViewer.PCropVideoSet;
        pInspector.PInspectorRotateChange -= PFixRotateHandle;
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

            LEditPlan? pFixPersistent = PFixCarriedRead();
            LEditPlan? pFixSaved = pViewer.PViewerSourcePath is { } pFixSourcePath
                ? LEdit.LEditPlanRead(pFixSourcePath, LLibrarian.LLibrarianEditLoad)
                : null;

            LTraceLog.LTraceInfoRecord(
                $"Edit media ready '{pFixName}': "
                + $"display {(pCropSource is { } pLogSize ? $"{pLogSize.Width:0}x{pLogSize.Height:0}" : "unknown")}, "
                + $"persistent {(pFixPersistent is null ? "off" : "on")}, "
                + $"carried {PFixPlanFormat(pFixPersistent)}, "
                + $"sidecar {PFixPlanFormat(pFixSaved)}");

            pInspector.PCropMediaReset();

            bool pFixCarryWins = pFixPersistent is not null;
            LEditPlan pFixPlan = LEdit.LEditPlanResolve(
                pFixSaved,
                pFixPersistent,
                pCropOwner.LCropboxStatePersistent,
                pInspector.PSkipPersistentCheck());

            LTraceLog.LTraceInfoRecord(
                $"Edit applying {(pFixCarryWins ? "persistent" : "sidecar")} plan to '{pFixName}': "
                + $"{PFixPlanFormat(pFixPlan)}");
            pViewer.PViewerRotateSet(PFixRotateResolve(pFixPlan.LEditCrop));
            if (pViewer.PCropSourceRead() is { } pFixRotatedSource)
            {
                pInspector.PInspectorSourceSet(pFixRotatedSource.Width, pFixRotatedSource.Height);
            }

            pInspector.PCropPlanApply(pFixPlan.LEditCrop, pFixPlan.LEditCropActive);
            pInspector.PInspectorRatioApply(pFixPlan.LEditRatioFixed, pFixPlan.LEditRatioLenient, pFixPlan.LEditRatioWidth, pFixPlan.LEditRatioHeight);
            pInspector.PTonePlanApply(pFixPlan.LEditVideo);
            pInspector.PSkipApply(pFixPlan.LEditSkip);
            pCropOwner.LCropboxStateSet(
                pFixPlan.LEditCrop,
                pFixPlan.LEditCropActive,
                pFixPlan.LEditRatioFixed,
                pFixPlan.LEditRatioLenient,
                pFixPlan.LEditRatioWidth,
                pFixPlan.LEditRatioHeight);
        }
        finally
        {
            pInspector.PInspectorCropChange += pViewer.PCropVideoSet;
            pInspector.PInspectorRotateChange += PFixRotateHandle;
            pFixPlanLoading = false;
        }

        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PFixViewerApply();
        PFixPlanSave();
    }

    private void PFixViewerApply()
    {
        LRotateFlip pFixRotate = pInspector.PInspectorRotateRead();
        System.Windows.Rect? pFixRect = pInspector.PInspectorRectRead();
        LTraceLog.LTraceInfoRecord(
            $"Edit viewer push: rotate {pFixRotate.LRotateKind}, "
            + $"H {pFixRotate.LRotateFlipHorizontal}, V {pFixRotate.LRotateFlipVertical}, "
            + $"{PFixRectFormat(pFixRect)}");

        pViewer.PViewerRotateSet(pFixRotate);
        pViewer.PCropVideoSet(pFixRect);
        PFixColorApply();
    }

    private void PFixRotateHandle(LRotateFlip pRotateFlip)
    {
        LRotateFlip pFixOldRotate = pViewer.LPreviewStateCurrent.LRotateFlip;
        pViewer.PViewerRotateSet(pRotateFlip);
        if (pViewer.PCropSourceRead() is { } pRotatedSource)
        {
            pInspector.PInspectorSourceSet(pRotatedSource.Width, pRotatedSource.Height);
        }

        pInspector.PInspectorOrientationApply(pFixOldRotate);
    }

    private static LRotateFlip PFixRotateResolve(LWorkCrop pFixCrop) => new(
        pFixCrop.LWorkCropRotation switch
        {
            90 => LRotateKind.LRotate90,
            180 => LRotateKind.LRotate180,
            270 => LRotateKind.LRotate270,
            _ => LRotateKind.LRotateNone
        },
        pFixCrop.LWorkFlipHorizontal,
        pFixCrop.LWorkFlipVertical);
}
