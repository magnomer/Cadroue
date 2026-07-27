using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private const double PCropHandleSize = 10;
    private const double PCropSizeMinimum = 8;

    private static readonly int[] PCropHandleEdgeX = [-1, 0, 1, 1, 1, 0, -1, -1];
    private static readonly int[] PCropHandleEdgeY = [-1, -1, -1, 0, 1, 1, 1, 0];

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
            pHandle.MouseLeftButtonDown += PCropHandlePressHandle;
            pViewerCropHandles[pHandleIndex] = pHandle;
            pViewerOverlay.Children.Add(pHandle);
        }
    }

    private void PCropHandlePressHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (!pViewerCropArmed || sender is not Rectangle { Tag: int pHandleIndex })
        {
            return;
        }

        pViewerEdgeX = PCropHandleEdgeX[pHandleIndex];
        pViewerEdgeY = PCropHandleEdgeY[pHandleIndex];
        PCropDragStart(mouseEvent.GetPosition(pViewerOverlay));
        mouseEvent.Handled = true;
    }

    private void PCropMovePressHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (!pViewerCropArmed || pViewerCropBox.Visibility != Visibility.Visible)
        {
            return;
        }

        pViewerEdgeX = 0;
        pViewerEdgeY = 0;
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

    private void PCropDragApply(Point pDragPoint)
    {
        Rect pVideoRect = PCropRectRead();
        Rect pDragRect = pViewerEdgeX == 0 && pViewerEdgeY == 0
            ? PCropMoveResolve(pDragPoint, pVideoRect)
            : PCropResizeResolve(PCropPointClamp(pDragPoint), pVideoRect);

        Canvas.SetLeft(pViewerCropBox, pDragRect.X);
        Canvas.SetTop(pViewerCropBox, pDragRect.Y);
        pViewerCropBox.Width = pDragRect.Width;
        pViewerCropBox.Height = pDragRect.Height;
        PCropOverlayUpdate();
    }

    private Rect PCropMoveResolve(Point pDragPoint, Rect pVideoRect)
    {
        double pMoveX = pViewerCropOrigin.X + (pDragPoint.X - pViewerCropGrab.X);
        double pMoveY = pViewerCropOrigin.Y + (pDragPoint.Y - pViewerCropGrab.Y);
        pMoveX = Math.Clamp(pMoveX, pVideoRect.Left, Math.Max(pVideoRect.Left, pVideoRect.Right - pViewerCropOrigin.Width));
        pMoveY = Math.Clamp(pMoveY, pVideoRect.Top, Math.Max(pVideoRect.Top, pVideoRect.Bottom - pViewerCropOrigin.Height));
        return new Rect(pMoveX, pMoveY, pViewerCropOrigin.Width, pViewerCropOrigin.Height);
    }

    private Rect PCropResizeResolve(Point pDragPoint, Rect pVideoRect)
    {
        double pLeft = pViewerEdgeX < 0 ? pDragPoint.X : pViewerCropOrigin.Left;
        double pRight = pViewerEdgeX > 0 ? pDragPoint.X : pViewerCropOrigin.Right;
        double pTop = pViewerEdgeY < 0 ? pDragPoint.Y : pViewerCropOrigin.Top;
        double pBottom = pViewerEdgeY > 0 ? pDragPoint.Y : pViewerCropOrigin.Bottom;

        double pWidth = Math.Max(PCropSizeMinimum, Math.Abs(pRight - pLeft));
        double pHeight = Math.Max(PCropSizeMinimum, Math.Abs(pBottom - pTop));

        if (pViewerCropRatio is Size pCropRatio)
        {
            if (pViewerEdgeX != 0 && pViewerEdgeY != 0)
            {
                if (pWidth * pCropRatio.Height > pHeight * pCropRatio.Width)
                {
                    pWidth = pHeight * pCropRatio.Width / pCropRatio.Height;
                }
                else
                {
                    pHeight = pWidth * pCropRatio.Height / pCropRatio.Width;
                }
            }
            else if (pViewerEdgeX != 0)
            {
                pHeight = pWidth * pCropRatio.Height / pCropRatio.Width;
            }
            else
            {
                pWidth = pHeight * pCropRatio.Width / pCropRatio.Height;
            }
        }

        double pRectX = pViewerEdgeX < 0 ? pViewerCropOrigin.Right - pWidth : pViewerCropOrigin.Left;
        double pRectY = pViewerEdgeY < 0 ? pViewerCropOrigin.Bottom - pHeight : pViewerCropOrigin.Top;

        if (pViewerEdgeX == 0)
        {
            pRectX = pViewerCropOrigin.Left + ((pViewerCropOrigin.Width - pWidth) / 2);
        }

        if (pViewerEdgeY == 0)
        {
            pRectY = pViewerCropOrigin.Top + ((pViewerCropOrigin.Height - pHeight) / 2);
        }

        return PCropRectFit(new Rect(pRectX, pRectY, pWidth, pHeight), pVideoRect);
    }

    private Rect PCropRectFit(Rect pCropRect, Rect pVideoRect)
    {
        double pWidth = Math.Min(pCropRect.Width, pVideoRect.Width);
        double pHeight = Math.Min(pCropRect.Height, pVideoRect.Height);

        if (pViewerCropRatio is not null)
        {
            double pScale = Math.Min(pWidth / pCropRect.Width, pHeight / pCropRect.Height);
            pWidth = pCropRect.Width * pScale;
            pHeight = pCropRect.Height * pScale;
        }

        double pFitX = Math.Clamp(pCropRect.X, pVideoRect.Left, Math.Max(pVideoRect.Left, pVideoRect.Right - pWidth));
        double pFitY = Math.Clamp(pCropRect.Y, pVideoRect.Top, Math.Max(pVideoRect.Top, pVideoRect.Bottom - pHeight));
        return new Rect(pFitX, pFitY, pWidth, pHeight);
    }

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

            int pEdgeX = PCropHandleEdgeX[pHandleIndex];
            int pEdgeY = PCropHandleEdgeY[pHandleIndex];
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

    public Size? PCropSourceRead()
    {
        if (pViewerMediaInfo is null || !pViewerMediaInfo.LMediaInfoVideoPresent)
        {
            return null;
        }

        return PCropDisplayRead();
    }

    private bool PCropRotatedCheck() =>
        LPreviewStateCurrent.LRotateFlip.LRotateKind is LRotateKind.LRotate90 or LRotateKind.LRotate270;

    private Size PCropDisplayRead()
    {
        if (pViewerMediaInfo is null || !pViewerMediaInfo.LMediaInfoVideoPresent)
        {
            return new Size(0, 0);
        }

        double pSourceWidth = pViewerMediaInfo.LMediaInfoVideoWidth;
        double pSourceHeight = pViewerMediaInfo.LMediaInfoVideoHeight;
        return PCropRotatedCheck()
            ? new Size(pSourceHeight, pSourceWidth)
            : new Size(pSourceWidth, pSourceHeight);
    }

    public void PCropVideoSet(Rect? pCropVideo)
    {
        if (pCropVideo is not { Width: > 0, Height: > 0 })
        {
            PCropHide();
            return;
        }

        PCropVideo = pCropVideo;
        LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(LCropbox.LCropboxFromRect(PCropVideo));
        pViewerCropBox.Visibility = Visibility.Visible;
        PCropBoxRestore();
    }

    private void PCropPressHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (!pViewerCropArmed || pViewerMediaInfo is null || !pViewerMediaInfo.LMediaInfoVideoPresent)
        {
            return;
        }

        pViewerCropStartPoint = mouseEvent.GetPosition(pViewerOverlay);
        pViewerCropBox.Visibility = Visibility.Visible;
        pViewerOverlay.CaptureMouse();
        PCropBoxPlace(pViewerCropStartPoint.Value, pViewerCropStartPoint.Value);
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

        if (pViewerCropStartPoint is null)
        {
            return;
        }

        PCropBoxPlace(pViewerCropStartPoint.Value, mouseEvent.GetPosition(pViewerOverlay));
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
            LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(LCropbox.LCropboxFromRect(PCropVideo));
            PCropVideoChange?.Invoke(PCropVideo);
            mouseEvent.Handled = true;
            return;
        }

        if (pViewerCropStartPoint is null)
        {
            return;
        }

        PCropBoxPlace(pViewerCropStartPoint.Value, mouseEvent.GetPosition(pViewerOverlay));
        pViewerCropStartPoint = null;
        pViewerOverlay.ReleaseMouseCapture();
        PCropOverlayUpdate();
        PCropVideo = PCropVideoRead();
        LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(LCropbox.LCropboxFromRect(PCropVideo));
        PCropVideoChange?.Invoke(PCropVideo);
        mouseEvent.Handled = true;
    }

    private void PCropBoxPlace(Point startPoint, Point endPoint)
    {
        Point clampedStart = PCropPointClamp(startPoint);
        Point clampedEnd = PCropPointClamp(endPoint);
        double width = Math.Abs(clampedStart.X - clampedEnd.X);
        double height = Math.Abs(clampedStart.Y - clampedEnd.Y);

        if (pViewerCropRatio is Size cropRatio)
        {
            if (width * cropRatio.Height > height * cropRatio.Width)
            {
                width = height * cropRatio.Width / cropRatio.Height;
            }
            else
            {
                height = width * cropRatio.Height / cropRatio.Width;
            }
        }

        double left = clampedEnd.X < clampedStart.X ? clampedStart.X - width : clampedStart.X;
        double top = clampedEnd.Y < clampedStart.Y ? clampedStart.Y - height : clampedStart.Y;
        Canvas.SetLeft(pViewerCropBox, left);
        Canvas.SetTop(pViewerCropBox, top);
        pViewerCropBox.Width = width;
        pViewerCropBox.Height = height;
        PCropOverlayUpdate();
    }

    private Point PCropPointClamp(Point point)
    {
        Rect videoRect = PCropRectRead();
        double x = Math.Max(videoRect.Left, Math.Min(videoRect.Right, point.X));
        double y = Math.Max(videoRect.Top, Math.Min(videoRect.Bottom, point.Y));
        return new Point(x, y);
    }

    private Rect? PCropVideoRead()
    {
        if (pViewerMediaInfo is null || !pViewerMediaInfo.LMediaInfoVideoPresent
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
        double overlayLeft = Canvas.GetLeft(pViewerCropBox);
        double overlayTop = Canvas.GetTop(pViewerCropBox);
        double videoX = (overlayLeft - videoRect.Left) / videoRect.Width * displaySize.Width;
        double videoY = (overlayTop - videoRect.Top) / videoRect.Height * displaySize.Height;
        double videoWidth = pViewerCropBox.Width / videoRect.Width * displaySize.Width;
        double videoHeight = pViewerCropBox.Height / videoRect.Height * displaySize.Height;
        return new Rect(videoX, videoY, videoWidth, videoHeight);
    }

    private Rect PCropRectRead()
    {
        double overlayWidth = Math.Max(0, pViewerOverlay.ActualWidth);
        double overlayHeight = Math.Max(0, pViewerOverlay.ActualHeight);
        if (pViewerMediaInfo is null || !pViewerMediaInfo.LMediaInfoVideoPresent
            || overlayWidth <= 0 || overlayHeight <= 0)
        {
            return new Rect(0, 0, overlayWidth, overlayHeight);
        }

        Size displaySize = PCropDisplayRead();
        double videoWidth = displaySize.Width;
        double videoHeight = displaySize.Height;
        double scale = Math.Min(overlayWidth / videoWidth, overlayHeight / videoHeight);
        double displayWidth = videoWidth * scale;
        double displayHeight = videoHeight * scale;
        return new Rect((overlayWidth - displayWidth) / 2, (overlayHeight - displayHeight) / 2, displayWidth, displayHeight);
    }

    private void PCropSizeHandle(object sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        PCropBoxRestore();
    }

    private void PCropBoxRestore()
    {
        if (PCropVideo is null || pViewerMediaInfo is null
            || !pViewerMediaInfo.LMediaInfoVideoPresent)
        {
            return;
        }

        Rect videoRect = PCropRectRead();
        Rect cropVideo = PCropVideo.Value;
        Size displaySize = PCropDisplayRead();
        if (displaySize.Width <= 0 || displaySize.Height <= 0)
        {
            return;
        }

        Canvas.SetLeft(pViewerCropBox, videoRect.Left + (cropVideo.X / displaySize.Width * videoRect.Width));
        Canvas.SetTop(pViewerCropBox, videoRect.Top + (cropVideo.Y / displaySize.Height * videoRect.Height));
        pViewerCropBox.Width = cropVideo.Width / displaySize.Width * videoRect.Width;
        pViewerCropBox.Height = cropVideo.Height / displaySize.Height * videoRect.Height;
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

