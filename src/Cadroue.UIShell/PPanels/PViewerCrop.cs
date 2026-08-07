using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.MigrationInterface;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private const double PCropHandleSize = 10;
    private const double PCropSizeMinimum = 8;

    private static readonly int[] PCropEdgeX = [-1, 0, 1, 1, 1, 0, -1, -1];
    private static readonly int[] PCropEdgeY = [-1, -1, -1, 0, 1, 1, 1, 0];

    private static readonly Cursor[] PCropHandleCursors =
    [
        Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE,
        Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE
    ];

    private void PCropHandlesBuild()
    {
        for (int pHandleIndex = 0; pHandleIndex < pViewerCropHandles.Length; pHandleIndex++)
        {
            var pHandle = new Rectangle
            {
                Width = PCropHandleSize,
                Height = PCropHandleSize,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7)),
                StrokeThickness = 1.5,
                Cursor = PCropHandleCursors[pHandleIndex],
                Visibility = Visibility.Collapsed,
                Tag = pHandleIndex
            };
            pHandle.MouseLeftButtonDown += PCropGripHandle;
            pViewerCropHandles[pHandleIndex] = pHandle;
            pViewerOverlay.Children.Add(pHandle);
        }
    }

    private void PCropGripHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (!pViewerCropArmed || sender is not Rectangle { Tag: int pHandleIndex })
        {
            return;
        }

        pViewerEdgeX = PCropEdgeX[pHandleIndex];
        pViewerEdgeY = PCropEdgeY[pHandleIndex];
        pViewerCropDrive = pViewerEdgeX != 0 && pViewerEdgeY != 0 ? -1 : pViewerEdgeX != 0 ? 0 : 1;
        pViewerCropAnchorX = -pViewerEdgeX;
        pViewerCropAnchorY = -pViewerEdgeY;
        PCropDragStart(mouseEvent.GetPosition(pViewerOverlay));
        mouseEvent.Handled = true;
    }

    private void PCropBodyHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (!pViewerCropArmed || pViewerCropBox.Visibility != Visibility.Visible)
        {
            return;
        }

        pViewerEdgeX = 0;
        pViewerEdgeY = 0;
        pViewerCropDrive = -1;
        pViewerCropAnchorX = -1;
        pViewerCropAnchorY = -1;
        PCropDragStart(mouseEvent.GetPosition(pViewerOverlay));
        mouseEvent.Handled = true;
    }

    private void PCropDragStart(Point pGrabPoint)
    {
        pViewerCropOrigin = new Rect(
            Canvas.GetLeft(pViewerCropBox),
            Canvas.GetTop(pViewerCropBox),
            pViewerCropBox.Width,
            pViewerCropBox.Height);
        pViewerCropGrab = pGrabPoint;
        pViewerCropDrag = true;
        pViewerOverlay.CaptureMouse();
    }

    private void PCropDragApply(Point psNameDragPoint)
    {
        LCropbox pCropVideo = PCropboxResolve(PCropRectRead());
        LCropbox pCropOrigin = PCropboxResolve(pViewerCropOrigin);
        LCropbox pCropResult;
        if (pViewerEdgeX == 0 && pViewerEdgeY == 0)
        {
            pCropResult = LCropbox.LCropboxMoveResolve(
                pCropOrigin, pViewerCropGrab.X, pViewerCropGrab.Y, psNameDragPoint.X, psNameDragPoint.Y, pCropVideo);
        }
        else
        {
            Point pCropClamped = PCropPointClamp(psNameDragPoint);
            pCropResult = LCropbox.LCropboxResizeResolve(
                pCropOrigin, pCropClamped.X, pCropClamped.Y, pViewerEdgeX, pViewerEdgeY,
                pViewerCropRatio?.Width ?? 0, pViewerCropRatio?.Height ?? 0, pCropVideo, PCropSizeMinimum);
        }

        Canvas.SetLeft(pViewerCropBox, pCropResult.LCropboxX);
        Canvas.SetTop(pViewerCropBox, pCropResult.LCropboxY);
        pViewerCropBox.Width = pCropResult.LCropboxWidth;
        pViewerCropBox.Height = pCropResult.LCropboxHeight;
        PCropOverlayUpdate();
    }

    private static LCropbox PCropboxResolve(Rect pCropRect) =>
        new LCropbox(pCropRect.X, pCropRect.Y, pCropRect.Width, pCropRect.Height);

    private static Rect PCropRectResolve(LCropbox pCropbox) =>
        new Rect(pCropbox.LCropboxX, pCropbox.LCropboxY, pCropbox.LCropboxWidth, pCropbox.LCropboxHeight);

    private void PCropOverlayUpdate()
    {
        PCropHandlesPlace();
        PCropShadeUpdate();
    }

    private void PCropShadeUpdate()
    {
        if (pViewerCropBox.Visibility != Visibility.Visible
            || pViewerCropBox.Width <= 0
            || pViewerCropBox.Height <= 0)
        {
            pViewerCropShade.Visibility = Visibility.Collapsed;
            return;
        }

        var pShadeGeometry = new GeometryGroup { FillRule = FillRule.EvenOdd };
        pShadeGeometry.Children.Add(new RectangleGeometry(PCropRectRead()));
        pShadeGeometry.Children.Add(new RectangleGeometry(new Rect(
            Canvas.GetLeft(pViewerCropBox),
            Canvas.GetTop(pViewerCropBox),
            pViewerCropBox.Width,
            pViewerCropBox.Height)));

        pViewerCropShade.Data = pShadeGeometry;
        pViewerCropShade.Visibility = Visibility.Visible;
    }

    private void PCropHandlesPlace()
    {
        bool pHandlesVisible = pViewerCropArmed
            && pViewerCropBox.Visibility == Visibility.Visible
            && pViewerCropBox.Width > 0
            && pViewerCropBox.Height > 0;

        double pBoxLeft = Canvas.GetLeft(pViewerCropBox);
        double pBoxTop = Canvas.GetTop(pViewerCropBox);

        for (int pHandleIndex = 0; pHandleIndex < pViewerCropHandles.Length; pHandleIndex++)
        {
            Rectangle pHandle = pViewerCropHandles[pHandleIndex];
            pHandle.Visibility = pHandlesVisible ? Visibility.Visible : Visibility.Collapsed;
            if (!pHandlesVisible)
            {
                continue;
            }

            int pEdgeX = PCropEdgeX[pHandleIndex];
            int pEdgeY = PCropEdgeY[pHandleIndex];
            double pPointX = pEdgeX == 0
                ? pBoxLeft + (pViewerCropBox.Width / 2)
                : pEdgeX < 0 ? pBoxLeft : pBoxLeft + pViewerCropBox.Width;
            double pPointY = pEdgeY == 0
                ? pBoxTop + (pViewerCropBox.Height / 2)
                : pEdgeY < 0 ? pBoxTop : pBoxTop + pViewerCropBox.Height;

            Canvas.SetLeft(pHandle, pPointX - (PCropHandleSize / 2));
            Canvas.SetTop(pHandle, pPointY - (PCropHandleSize / 2));
        }
    }

    public bool PCropPersistent { get; set; }

    public void PCropToolSet(bool pCropArmed)
    {
        pViewerCropArmed = pCropArmed;
        pViewerOverlay.Cursor = pCropArmed ? Cursors.Cross : null;
        pViewerCropBox.Cursor = pCropArmed ? Cursors.SizeAll : null;
        PCropOverlayUpdate();
    }

    public void PCropRatioSet(Size? pCropRatio)
    {
        pViewerCropRatio = pCropRatio is { Width: > 0, Height: > 0 } ? pCropRatio : null;
    }

    public (int Drive, int AnchorX, int AnchorY) PCropAnchorRead() =>
        (pViewerCropDrive, pViewerCropAnchorX, pViewerCropAnchorY);

    public Size? PCropSourceRead()
    {
        if (pViewerMediaInfo is null || !pViewerMediaInfo.LMediaVideoPresent)
        {
            return null;
        }

        return PCropDisplayRead();
    }

    private bool PCropRotatedCheck() =>
        LPreviewStateCurrent.LRotateFlip.LRotateKind is LRotateKind.LRotate90 or LRotateKind.LRotate270;

    private Size PCropDisplayRead()
    {
        if (pViewerMediaInfo is null || !pViewerMediaInfo.LMediaVideoPresent)
        {
            return new Size(0, 0);
        }

        (double pSourceWidth, double pSourceHeight) = LCropbox.LCropboxSourceResolve(
            pViewerMediaInfo.LMediaVideoWidth, pViewerMediaInfo.LMediaVideoHeight, PCropRotatedCheck());
        return new Size(pSourceWidth, pSourceHeight);
    }

    public void PCropVideoSet(Rect? pCropVideo)
    {
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
        pViewerCropBox.Visibility = Visibility.Visible;
        PCropBoxRestore();
    }

    private void PCropPressHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (!pViewerCropArmed || pViewerMediaInfo is null || !pViewerMediaInfo.LMediaVideoPresent)
        {
            return;
        }

        pViewerCropPoint = mouseEvent.GetPosition(pViewerOverlay);
        pViewerCropDrive = -1;
        pViewerCropAnchorX = -1;
        pViewerCropAnchorY = -1;
        pViewerCropBox.Visibility = Visibility.Visible;
        pViewerOverlay.CaptureMouse();
        PCropBoxPlace(pViewerCropPoint.Value, pViewerCropPoint.Value);
        mouseEvent.Handled = true;
    }

    private void PCropMoveHandle(object sender, MouseEventArgs mouseEvent)
    {
        if (mouseEvent.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (pViewerCropDrag)
        {
            PCropDragApply(mouseEvent.GetPosition(pViewerOverlay));
            mouseEvent.Handled = true;
            return;
        }

        if (pViewerCropPoint is null)
        {
            return;
        }

        PCropBoxPlace(pViewerCropPoint.Value, mouseEvent.GetPosition(pViewerOverlay));
        mouseEvent.Handled = true;
    }

    private void PCropReleaseHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (pViewerCropDrag)
        {
            PCropDragApply(mouseEvent.GetPosition(pViewerOverlay));
            pViewerCropDrag = false;
            pViewerOverlay.ReleaseMouseCapture();
            PCropVideo = PCropVideoRead();
            LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(PViewerCropboxRead(PCropVideo));
            PCropVideoChange?.Invoke(PCropVideo);
            mouseEvent.Handled = true;
            return;
        }

        if (pViewerCropPoint is null)
        {
            return;
        }

        PCropBoxPlace(pViewerCropPoint.Value, mouseEvent.GetPosition(pViewerOverlay));
        pViewerCropPoint = null;
        pViewerOverlay.ReleaseMouseCapture();
        PCropOverlayUpdate();
        PCropVideo = PCropVideoRead();
        LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(PViewerCropboxRead(PCropVideo));
        PCropVideoChange?.Invoke(PCropVideo);
        mouseEvent.Handled = true;
    }

    private static LCropbox? PViewerCropboxRead(Rect? pViewerCropRect)
    {
        if (pViewerCropRect is not Rect pViewerRect || pViewerRect.Width <= 0 || pViewerRect.Height <= 0)
        {
            return null;
        }

        return new LCropbox(pViewerRect.X, pViewerRect.Y, pViewerRect.Width, pViewerRect.Height);
    }

    private void PCropBoxPlace(Point startPoint, Point endPoint)
    {
        Point clampedStart = PCropPointClamp(startPoint);
        Point clampedEnd = PCropPointClamp(endPoint);
        LCropbox pCropDrawn = LCropbox.LCropboxDrawResolve(
            clampedStart.X, clampedStart.Y, clampedEnd.X, clampedEnd.Y,
            pViewerCropRatio?.Width ?? 0, pViewerCropRatio?.Height ?? 0);
        Canvas.SetLeft(pViewerCropBox, pCropDrawn.LCropboxX);
        Canvas.SetTop(pViewerCropBox, pCropDrawn.LCropboxY);
        pViewerCropBox.Width = pCropDrawn.LCropboxWidth;
        pViewerCropBox.Height = pCropDrawn.LCropboxHeight;
        PCropOverlayUpdate();
    }

    private Point PCropPointClamp(Point point)
    {
        (double pClampX, double pClampY) = LCropbox.LCropboxPointClamp(
            point.X, point.Y, PCropboxResolve(PCropRectRead()));
        return new Point(pClampX, pClampY);
    }

    private Rect? PCropVideoRead()
    {
        if (pViewerMediaInfo is null || !pViewerMediaInfo.LMediaVideoPresent
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
        if (pViewerMediaInfo is null || !pViewerMediaInfo.LMediaVideoPresent
            || overlayWidth <= 0 || overlayHeight <= 0)
        {
            return new Rect(0, 0, overlayWidth, overlayHeight);
        }

        Size displaySize = PCropDisplayRead();
        return PCropRectResolve(LCropbox.LCropboxDisplayResolve(
            displaySize.Width, displaySize.Height, overlayWidth, overlayHeight));
    }

    private void PCropSizeHandle(object sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        PCropBoxRestore();
    }

    private void PCropBoxRestore()
    {
        if (PCropVideo is null || pViewerMediaInfo is null
            || !pViewerMediaInfo.LMediaVideoPresent)
        {
            return;
        }

        Rect videoRect = PCropRectRead();
        Size displaySize = PCropDisplayRead();
        if (displaySize.Width <= 0 || displaySize.Height <= 0)
        {
            return;
        }

        LCropbox pCropOverlay = LCropbox.LCropboxOverlayResolve(
            PCropboxResolve(PCropVideo.Value), PCropboxResolve(videoRect), displaySize.Width, displaySize.Height);
        Canvas.SetLeft(pViewerCropBox, pCropOverlay.LCropboxX);
        Canvas.SetTop(pViewerCropBox, pCropOverlay.LCropboxY);
        pViewerCropBox.Width = pCropOverlay.LCropboxWidth;
        pViewerCropBox.Height = pCropOverlay.LCropboxHeight;
        PCropOverlayUpdate();
    }

    private void PCropHide()
    {
        pViewerCropBox.Visibility = Visibility.Collapsed;
        pViewerCropBox.Width = 0;
        pViewerCropBox.Height = 0;
        PCropVideo = null;
        PCropOverlayUpdate();
        PCropVideoChange?.Invoke(null);
    }
}

