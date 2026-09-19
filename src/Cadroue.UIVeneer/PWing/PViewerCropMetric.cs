using System.Windows;
using System.Windows.Controls;

using Cadroue.Application;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PViewer
{
    private static LCropbox PCropboxResolve(Rect pCropRect) =>
        new LCropbox(pCropRect.X, pCropRect.Y, pCropRect.Width, pCropRect.Height);

    private static Rect PCropRectResolve(LCropbox pCropbox) =>
        new Rect(pCropbox.LCropboxX, pCropbox.LCropboxY, pCropbox.LCropboxWidth, pCropbox.LCropboxHeight);

    private static Rect? PCropVideoResolve(LCropbox? pCropbox) =>
        pCropbox is { } pCropRect ? PCropRectResolve(pCropRect) : null;

    private static LCropbox? PViewerCropboxRead(Rect? pViewerCropRect)
    {
        if (pViewerCropRect is not Rect pViewerRect || pViewerRect.Width <= 0 || pViewerRect.Height <= 0)
        {
            return null;
        }

        return new LCropbox(pViewerRect.X, pViewerRect.Y, pViewerRect.Width, pViewerRect.Height);
    }

    public Size? PCropSourceRead() => LViewer.LViewerVideoPresent ? PCropDisplayRead() : null;

    private bool PCropRotatedCheck() =>
        LViewer.LViewerPreview.LRotateFlip.LRotateKind is LRotateKind.LRotate90 or LRotateKind.LRotate270;

    private Size PCropDisplayRead()
    {
        if (LViewer.LViewerMediaInfo is not { LMediaVideoPresent: true } pViewerMediaInfo)
        {
            return new Size(0, 0);
        }

        (double pSourceWidth, double pSourceHeight) = LCropbox.LCropboxSourceResolve(
            pViewerMediaInfo.LMediaVideoWidth, pViewerMediaInfo.LMediaVideoHeight, PCropRotatedCheck());
        return new Size(pSourceWidth, pSourceHeight);
    }

    private Point PCropPointClamp(Point point)
    {
        (double pClampX, double pClampY) = LCropbox.LCropboxPointClamp(
            point.X, point.Y, PCropboxResolve(PCropRectRead()));
        return new Point(pClampX, pClampY);
    }

    private Rect? PCropVideoRead()
    {
        if (!LViewer.LViewerVideoPresent
            || pViewerCropBox.Visibility != Visibility.Visible)
        {
            return null;
        }

        Rect videoRect = PCropRectRead();
        if (videoRect.Width <= 0 || videoRect.Height <= 0 || pViewerCropBox.Width <= 1 || pViewerCropBox.Height <= 1)
        {
            return null;
        }

        Size displaySize = PCropDisplayRead();
        LCropbox pCropOverlay = new LCropbox(
            Canvas.GetLeft(pViewerCropBox), Canvas.GetTop(pViewerCropBox),
            pViewerCropBox.Width, pViewerCropBox.Height);
        LCropbox pCropPixel = LCropbox.LCropboxPixelResolve(
            pCropOverlay, PCropboxResolve(videoRect), displaySize.Width, displaySize.Height);
        return PCropRectResolve(pCropPixel);
    }

    private Rect PCropRectRead()
    {
        double overlayWidth = Math.Max(0, pViewerOverlay.ActualWidth);
        double overlayHeight = Math.Max(0, pViewerOverlay.ActualHeight);
        if (!LViewer.LViewerVideoPresent
            || overlayWidth <= 0 || overlayHeight <= 0)
        {
            return new Rect(0, 0, overlayWidth, overlayHeight);
        }

        Size displaySize = PCropDisplayRead();
        return PCropRectResolve(LCropbox.LCropboxDisplayResolve(
            displaySize.Width, displaySize.Height, overlayWidth, overlayHeight));
    }
}
