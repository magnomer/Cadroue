using System.Windows;
using System.Windows.Input;

using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public readonly record struct PCropAnchor(int PCropAnchorDrive, int PCropAnchorX, int PCropAnchorY);

public sealed partial class PViewer
{
    public bool PCropPersistent
    {
        get => LCrop.LCropPersistent;
        set => LCrop.LCropPersistentSet(value);
    }

    public bool PCropActive => LCrop.LCropActive;

    public void PCropActiveSet(bool pCropActive)
    {
        if (PCropActive == pCropActive)
        {
            return;
        }

        LCrop.LCropActiveSet(pCropActive);
        if (!pCropActive)
        {
            PCropToolSet(false);
            pViewerCropPress = false;
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
        if (LCrop.LCropLocked == pCropLocked)
        {
            return;
        }

        LCrop.LCropLockSet(pCropLocked);
        if (pCropLocked)
        {
            pViewerCropPress = false;
            pViewerCropPoint = null;
            pViewerOverlay.ReleaseMouseCapture();
            pViewerOverlay.Cursor = null;
        }

        pViewerCropBox.Cursor = PCropEditableCheck() ? Cursors.SizeAll : null;
        PCropOverlayUpdate();
    }

    private bool PCropEditableCheck() =>
        PCropActive && !LCrop.LCropLocked && LViewer.LViewerTool != LViewerTool.LViewerToolNeutral;

    public void PCropToolSet(bool pCropArmed)
    {
        if (pCropArmed && LViewer.LViewerTool == LViewerTool.LViewerToolNeutral)
        {
            PViewerNeutralCancel();
        }

        LViewer.LViewerToolSet(pCropArmed
            ? LViewerTool.LViewerToolCrop
            : LViewer.LViewerTool == LViewerTool.LViewerToolCrop ? LViewerTool.LViewerToolNone : LViewer.LViewerTool);
        pViewerOverlay.Cursor = pCropArmed && !LCrop.LCropLocked ? Cursors.Cross : null;
        pViewerCropBox.Cursor = PCropEditableCheck() ? Cursors.SizeAll : null;
        PCropOverlayUpdate();
    }

    public void PCropRatioSet(Size? pCropRatio) =>
        LCrop.LCropRatioSet(pCropRatio?.Width ?? 0, pCropRatio?.Height ?? 0);

    public PCropAnchor PCropAnchorRead() =>
        new(LCrop.LCropDrive, LCrop.LCropAnchorX, LCrop.LCropAnchorY);

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

        LViewer.LViewerPreviewSet(LViewer.LViewerPreview.LCropboxChange(PViewerCropboxRead(pCropVideo)));
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
        LViewer.LViewerPreviewSet(LViewer.LViewerPreview.LCropboxChange(null));
        PCropOverlayUpdate();
        PViewerMpvUpdate();
    }
}
