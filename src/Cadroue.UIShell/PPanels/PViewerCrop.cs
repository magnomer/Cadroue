using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private void PCropPressHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (pViewerMediaInfo is null || !pViewerMediaInfo.LMediaInfoVideoPresent)
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
        if (pViewerCropStartPoint is null || mouseEvent.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        PCropBoxPlace(pViewerCropStartPoint.Value, mouseEvent.GetPosition(pViewerOverlay));
        mouseEvent.Handled = true;
    }

    private void PCropReleaseHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (pViewerCropStartPoint is null)
        {
            return;
        }

        PCropBoxPlace(pViewerCropStartPoint.Value, mouseEvent.GetPosition(pViewerOverlay));
        pViewerCropStartPoint = null;
        pViewerOverlay.ReleaseMouseCapture();
        PCropVideo = PCropVideoRead();
        LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(LCropbox.LCropboxFromRect(PCropVideo));
        mouseEvent.Handled = true;
    }

    private void PCropBoxPlace(Point startPoint, Point endPoint)
    {
        Point clampedStart = PCropPointClamp(startPoint);
        Point clampedEnd = PCropPointClamp(endPoint);
        double left = Math.Min(clampedStart.X, clampedEnd.X);
        double top = Math.Min(clampedStart.Y, clampedEnd.Y);
        double width = Math.Abs(clampedStart.X - clampedEnd.X);
        double height = Math.Abs(clampedStart.Y - clampedEnd.Y);
        Canvas.SetLeft(pViewerCropBox, left);
        Canvas.SetTop(pViewerCropBox, top);
        pViewerCropBox.Width = width;
        pViewerCropBox.Height = height;
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

        double overlayLeft = Canvas.GetLeft(pViewerCropBox);
        double overlayTop = Canvas.GetTop(pViewerCropBox);
        double videoX = (overlayLeft - videoRect.Left) / videoRect.Width * pViewerMediaInfo.LMediaInfoVideoWidth;
        double videoY = (overlayTop - videoRect.Top) / videoRect.Height * pViewerMediaInfo.LMediaInfoVideoHeight;
        double videoWidth = pViewerCropBox.Width / videoRect.Width * pViewerMediaInfo.LMediaInfoVideoWidth;
        double videoHeight = pViewerCropBox.Height / videoRect.Height * pViewerMediaInfo.LMediaInfoVideoHeight;
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

        double videoWidth = pViewerMediaInfo.LMediaInfoVideoWidth;
        double videoHeight = pViewerMediaInfo.LMediaInfoVideoHeight;
        double scale = Math.Min(overlayWidth / videoWidth, overlayHeight / videoHeight);
        double displayWidth = videoWidth * scale;
        double displayHeight = videoHeight * scale;
        return new Rect((overlayWidth - displayWidth) / 2, (overlayHeight - displayHeight) / 2, displayWidth, displayHeight);
    }

    private void PCropSizeHandle(object sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        if (PCropVideo is null || pViewerMediaInfo is null
            || !pViewerMediaInfo.LMediaInfoVideoPresent)
        {
            return;
        }

        Rect videoRect = PCropRectRead();
        Rect cropVideo = PCropVideo.Value;
        Canvas.SetLeft(pViewerCropBox, videoRect.Left + cropVideo.X / pViewerMediaInfo.LMediaInfoVideoWidth * videoRect.Width);
        Canvas.SetTop(pViewerCropBox, videoRect.Top + cropVideo.Y / pViewerMediaInfo.LMediaInfoVideoHeight * videoRect.Height);
        pViewerCropBox.Width = cropVideo.Width / pViewerMediaInfo.LMediaInfoVideoWidth * videoRect.Width;
        pViewerCropBox.Height = cropVideo.Height / pViewerMediaInfo.LMediaInfoVideoHeight * videoRect.Height;
    }

    private void PCropHide()
    {
        pViewerCropBox.Visibility = Visibility.Collapsed;
        pViewerCropBox.Width = 0;
        pViewerCropBox.Height = 0;
    }
}
