using System.Windows;
using System.Windows.Input;

using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PViewer
{
    public bool PCropPersistent { get; set; }

    public bool PCropActive { get; private set; } = true;

    public void PCropActiveSet(bool pCropActive)
    {
        if (PCropActive == pCropActive)
        {
            return;
        }

        PCropActive = pCropActive;
        if (!pCropActive)
        {
            PCropToolSet(false);
            pViewerCropDrag = false;
            pViewerCropPoint = null;
            pViewerOverlay.ReleaseMouseCapture();
        }

        PCropBoxSet();
        PCropOverlayUpdate();
        PViewerPreviewApply();
    }

    private void PCropBoxSet()
    {
        pViewerCropBox.Visibility = PCropActive && PCropVideo is { Width: > 0, Height: > 0 }
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public void PCropLockSet(bool pCropLocked)
    {
        if (pViewerCropLocked == pCropLocked)
        {
            return;
        }

        pViewerCropLocked = pCropLocked;
        if (pCropLocked)
        {
            pViewerCropDrag = false;
            pViewerCropPoint = null;
            pViewerOverlay.ReleaseMouseCapture();
            pViewerOverlay.Cursor = null;
        }

        pViewerCropBox.Cursor = PCropEditableCheck() ? Cursors.SizeAll : null;
        PCropOverlayUpdate();
    }

    private bool PCropEditableCheck() =>
        PCropActive && !pViewerCropLocked && pViewerTool != PViewerTool.PViewerToolNeutral;

    public void PCropToolSet(bool pCropArmed)
    {
        if (pCropArmed && pViewerTool == PViewerTool.PViewerToolNeutral)
        {
            PViewerNeutralCancel();
        }

        pViewerTool = pCropArmed
            ? PViewerTool.PViewerToolCrop
            : pViewerTool == PViewerTool.PViewerToolCrop ? PViewerTool.PViewerToolNone : pViewerTool;
        pViewerOverlay.Cursor = pCropArmed && !pViewerCropLocked ? Cursors.Cross : null;
        pViewerCropBox.Cursor = PCropEditableCheck() ? Cursors.SizeAll : null;
        PCropOverlayUpdate();
    }

    public void PCropRatioSet(Size? pCropRatio)
    {
        pViewerCropRatio = pCropRatio is { Width: > 0, Height: > 0 } ? pCropRatio : null;
    }

    public (int Drive, int AnchorX, int AnchorY) PCropAnchorRead() =>
        (pViewerCropDrive, pViewerAnchorX, pViewerAnchorY);

    public void PCropVideoSet(Rect? pCropVideo)
    {
        pCropVideo = PCropSourceClamp(pCropVideo);
        if (pCropVideo is not { Width: > 0, Height: > 0 })
        {
            LTraceLog.LTraceInfoRecord("Viewer crop cleared: overlay hidden");
            PCropHide();
            return;
        }

        Size pCropDisplay = PCropDisplayRead();
        LTraceLog.LTraceInfoRecord(
            $"Viewer crop set: {pCropVideo.Value.X:0},{pCropVideo.Value.Y:0} "
            + $"{pCropVideo.Value.Width:0}x{pCropVideo.Value.Height:0} "
            + $"over display {pCropDisplay.Width:0}x{pCropDisplay.Height:0}"
            + (pCropVideo.Value.Width >= pCropDisplay.Width && pCropVideo.Value.Height >= pCropDisplay.Height
                ? " (full frame)"
                : string.Empty));

        PCropVideo = pCropVideo;
        LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(PViewerCropboxRead(PCropVideo));
        PCropBoxSet();
        PCropBoxRestore();
        PViewerMpvUpdate();
    }

    private Rect? PCropSourceClamp(Rect? pCropVideo)
    {
        if (pCropVideo is not { Width: > 0, Height: > 0 } pCropRect || PCropSourceRead() is not { } pCropSource)
        {
            return pCropVideo;
        }

        LCropbox pCropClamped = LCropbox.LCropboxRectClamp(
            PCropboxResolve(pCropRect), new LCropbox(0, 0, pCropSource.Width, pCropSource.Height), false);
        return pCropClamped is { LCropboxWidth: > 0, LCropboxHeight: > 0 } ? PCropRectResolve(pCropClamped) : null;
    }

    private void PCropHide()
    {
        pViewerCropBox.Visibility = Visibility.Collapsed;
        pViewerCropBox.Width = 0;
        pViewerCropBox.Height = 0;
        PCropVideo = null;
        LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(null);
        PCropOverlayUpdate();
        PViewerMpvUpdate();
        PCropVideoChange?.Invoke(null);
    }
}
