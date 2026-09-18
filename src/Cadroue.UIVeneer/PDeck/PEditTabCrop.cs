using Cadroue.Core;
using Cadroue.UIVeneer.PPanel;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;
using Cadroue.Infrastructure;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PEditTab
{
    private void PEditCropUpdate() =>
        pProcessing.PProcessingActiveSet("Crop", pCropOwner.LCropboxStateActive);

    private void PEditPathShow(string? pSourcePath)
    {
        pViewer.PViewerNeutralCancel();
        if (string.IsNullOrWhiteSpace(pSourcePath))
        {
            LTraceLog.LTraceInfoRecord("Edit click: no file selected");
            return;
        }

        if (pViewer.LViewer.LViewerSourceMatch(pSourcePath))
        {
            return;
        }

        LTraceLog.LTraceInfoRecord(
            $"Edit click '{System.IO.Path.GetFileName(pSourcePath)}': "
            + $"persistent {(pInspector.PCropPersistentCheck() ? "on" : "off")}, "
            + $"inspector now {PEditCropFormat(pInspector.PInspectorCropRead())}");
        pViewer.PViewerSourceOpen(pSourcePath);
    }

    private void PEditCropHandle()
    {
        LRotateFlip pEditRotate = pInspector.PInspectorRotateRead();
        if (pViewer.LViewer.LViewerPreview.LRotateFlip != pEditRotate)
        {
            pViewer.PViewerRotateSet(pEditRotate);
            PEditSourceSync();
        }

        (bool pRatioFixed, _, int pRatioWidth, int pRatioHeight) = pCropOwner.LCropboxStateRatio;
        pViewer.PCropRatioSet(pRatioFixed && pRatioWidth > 0 && pRatioHeight > 0
            ? new System.Windows.Size(pRatioWidth, pRatioHeight)
            : null);
        pViewer.PCropPersistent = pCropOwner.LCropboxStatePersistent;
        pViewer.PCropActiveSet(pCropOwner.LCropboxStateActive);
        pViewer.PCropVideoSet(pInspector.PInspectorRectRead());
        PEditCropUpdate();
        PEditPlanSave();
    }

    private void PEditSourceSync()
    {
        if (pViewer.PCropSourceRead() is System.Windows.Size pCropSource)
        {
            pInspector.PInspectorSourceSet(pCropSource.Width, pCropSource.Height);
        }
        else
        {
            pInspector.PInspectorSourceSet(0, 0);
        }
    }

    private void PEditCropShow(System.Windows.Rect? pCropVideo)
    {
        PEditSourceSync();
        LTraceLog.LTraceInfoRecord($"Edit crop from viewer: {PEditRectFormat(pCropVideo)}");
        (int pCropDrive, int pCropAnchorX, int pCropAnchorY) = pViewer.PCropAnchorRead();
        pInspector.PInspectorCropSet(pCropVideo, pCropDrive, pCropAnchorX, pCropAnchorY);
    }

    private void PEditCropRestore()
    {
        string pEditName = pViewer.PViewerSourcePath is { } pEditPath
            ? System.IO.Path.GetFileName(pEditPath)
            : "(no media)";

        LEditPlan? pEditApplied = null;
        pInspector.LInspector.LInspectorSaveSuspend();
        try
        {
            PEditSourceSync();
            LEditPlan? pEditPersistent = PEditCarriedRead();
            LEditPlan? pEditSaved = pViewer.PViewerSourcePath is { } pEditSourcePath
                ? LEdit.LEditPlanRead(pEditSourcePath, LLibrarian.LLibrarianEditLoad)
                : null;

            LTraceLog.LTraceInfoRecord(
                $"Edit media ready '{pEditName}': "
                + $"display {(pViewer.PCropSourceRead() is { } pLogSize ? $"{pLogSize.Width:0}x{pLogSize.Height:0}" : "unknown")}, "
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
            pViewer.PViewerRotateSet(PInspector.PInspectorRotateResolve(pEditPlan.LEditCrop));
            PEditSourceSync();

            pEditApplied = pEditCarryWins ? pEditPlan : null;
            pInspector.PCropPlanApply(pEditPlan.LEditCrop, pEditPlan.LEditCropActive);
            pInspector.PInspectorRatioApply(
                pEditPlan.LEditRatioFixed,
                pEditPlan.LEditRatioLenient,
                pEditPlan.LEditRatioWidth,
                pEditPlan.LEditRatioHeight);
            pInspector.PTonePlanApply(pEditPlan.LEditVideo);
            pInspector.PSkipApply(pEditPlan.LEditSkip);
        }
        finally
        {
            pInspector.LInspector.LInspectorSaveResume();
        }

        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PEditViewerApply();
        if (pEditApplied is not null)
        {
            PEditPlanSave(pEditApplied);
        }
    }

    private void PEditViewerApply()
    {
        bool pEditSkip = pInspector.PSkipActiveCheck();
        LRotateFlip pEditRotate = pEditSkip
            ? new LRotateFlip(LRotateKind.LRotateNone, false, false)
            : pInspector.PInspectorRotateRead();
        System.Windows.Rect? pEditRect = pEditSkip ? null : pInspector.PInspectorRectRead();
        LTraceLog.LTraceInfoRecord(
            $"Edit viewer push: rotate {pEditRotate.LRotateKind}, "
            + $"H {pEditRotate.LRotateFlipHorizontal}, V {pEditRotate.LRotateFlipVertical}, "
            + $"{PEditRectFormat(pEditRect)}");

        pViewer.PViewerRotateSet(pEditRotate);
        pViewer.PCropVideoSet(pEditRect);
        PEditColorApply();
    }
}
