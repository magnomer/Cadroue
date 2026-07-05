using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewerPanel
{
    private void PViewerPanelCropMouseDown(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (pViewerPanelMediaInfo is null || !pViewerPanelMediaInfo.LMediaInfoVideoPresent)
        {
            return;
        }

        pViewerPanelCropStartPoint = mouseEvent.GetPosition(pViewerPanelOverlay);
        pViewerPanelCropBox.Visibility = Visibility.Visible;
        pViewerPanelOverlay.CaptureMouse();
        PViewerPanelCropBoxPlace(pViewerPanelCropStartPoint.Value, pViewerPanelCropStartPoint.Value);
        mouseEvent.Handled = true;
    }

    private void PViewerPanelCropMouseMove(object sender, MouseEventArgs mouseEvent)
    {
        if (pViewerPanelCropStartPoint is null || mouseEvent.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        PViewerPanelCropBoxPlace(pViewerPanelCropStartPoint.Value, mouseEvent.GetPosition(pViewerPanelOverlay));
        mouseEvent.Handled = true;
    }

    private void PViewerPanelCropMouseUp(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (pViewerPanelCropStartPoint is null)
        {
            return;
        }

        PViewerPanelCropBoxPlace(pViewerPanelCropStartPoint.Value, mouseEvent.GetPosition(pViewerPanelOverlay));
        pViewerPanelCropStartPoint = null;
        pViewerPanelOverlay.ReleaseMouseCapture();
        PViewerPanelCropBoxVideo = PViewerPanelCropBoxVideoRead();
        LPreviewStateCurrent = LPreviewStateCurrent.LCropBoxChange(LCropBox.LCropBoxFromRect(PViewerPanelCropBoxVideo));
        mouseEvent.Handled = true;
    }

    private void PViewerPanelCropBoxPlace(Point startPoint, Point endPoint)
    {
        Point clampedStart = PViewerPanelPointClamp(startPoint);
        Point clampedEnd = PViewerPanelPointClamp(endPoint);
        double left = Math.Min(clampedStart.X, clampedEnd.X);
        double top = Math.Min(clampedStart.Y, clampedEnd.Y);
        double width = Math.Abs(clampedStart.X - clampedEnd.X);
        double height = Math.Abs(clampedStart.Y - clampedEnd.Y);
        Canvas.SetLeft(pViewerPanelCropBox, left);
        Canvas.SetTop(pViewerPanelCropBox, top);
        pViewerPanelCropBox.Width = width;
        pViewerPanelCropBox.Height = height;
    }

    private Point PViewerPanelPointClamp(Point point)
    {
        Rect videoRect = PViewerPanelVideoRectRead();
        double x = Math.Max(videoRect.Left, Math.Min(videoRect.Right, point.X));
        double y = Math.Max(videoRect.Top, Math.Min(videoRect.Bottom, point.Y));
        return new Point(x, y);
    }

    private Rect? PViewerPanelCropBoxVideoRead()
    {
        if (pViewerPanelMediaInfo is null || !pViewerPanelMediaInfo.LMediaInfoVideoPresent
            || pViewerPanelCropBox.Visibility != Visibility.Visible)
        {
            return null;
        }

        Rect videoRect = PViewerPanelVideoRectRead();
        if (videoRect.Width <= 0 || videoRect.Height <= 0 || pViewerPanelCropBox.Width <= 1 || pViewerPanelCropBox.Height <= 1)
        {
            return null;
        }

        double overlayLeft = Canvas.GetLeft(pViewerPanelCropBox);
        double overlayTop = Canvas.GetTop(pViewerPanelCropBox);
        double videoX = (overlayLeft - videoRect.Left) / videoRect.Width * pViewerPanelMediaInfo.LMediaInfoVideoWidth;
        double videoY = (overlayTop - videoRect.Top) / videoRect.Height * pViewerPanelMediaInfo.LMediaInfoVideoHeight;
        double videoWidth = pViewerPanelCropBox.Width / videoRect.Width * pViewerPanelMediaInfo.LMediaInfoVideoWidth;
        double videoHeight = pViewerPanelCropBox.Height / videoRect.Height * pViewerPanelMediaInfo.LMediaInfoVideoHeight;
        return new Rect(videoX, videoY, videoWidth, videoHeight);
    }

    private Rect PViewerPanelVideoRectRead()
    {
        double overlayWidth = Math.Max(0, pViewerPanelOverlay.ActualWidth);
        double overlayHeight = Math.Max(0, pViewerPanelOverlay.ActualHeight);
        if (pViewerPanelMediaInfo is null || !pViewerPanelMediaInfo.LMediaInfoVideoPresent
            || overlayWidth <= 0 || overlayHeight <= 0)
        {
            return new Rect(0, 0, overlayWidth, overlayHeight);
        }

        double videoWidth = pViewerPanelMediaInfo.LMediaInfoVideoWidth;
        double videoHeight = pViewerPanelMediaInfo.LMediaInfoVideoHeight;
        double scale = Math.Min(overlayWidth / videoWidth, overlayHeight / videoHeight);
        double displayWidth = videoWidth * scale;
        double displayHeight = videoHeight * scale;
        return new Rect((overlayWidth - displayWidth) / 2, (overlayHeight - displayHeight) / 2, displayWidth, displayHeight);
    }

    private void PViewerPanelOverlaySizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        if (PViewerPanelCropBoxVideo is null || pViewerPanelMediaInfo is null
            || !pViewerPanelMediaInfo.LMediaInfoVideoPresent)
        {
            return;
        }

        Rect videoRect = PViewerPanelVideoRectRead();
        Rect cropVideo = PViewerPanelCropBoxVideo.Value;
        Canvas.SetLeft(pViewerPanelCropBox, videoRect.Left + cropVideo.X / pViewerPanelMediaInfo.LMediaInfoVideoWidth * videoRect.Width);
        Canvas.SetTop(pViewerPanelCropBox, videoRect.Top + cropVideo.Y / pViewerPanelMediaInfo.LMediaInfoVideoHeight * videoRect.Height);
        pViewerPanelCropBox.Width = cropVideo.Width / pViewerPanelMediaInfo.LMediaInfoVideoWidth * videoRect.Width;
        pViewerPanelCropBox.Height = cropVideo.Height / pViewerPanelMediaInfo.LMediaInfoVideoHeight * videoRect.Height;
    }

    private void PViewerPanelCropBoxHide()
    {
        pViewerPanelCropBox.Visibility = Visibility.Collapsed;
        pViewerPanelCropBox.Width = 0;
        pViewerPanelCropBox.Height = 0;
    }
}
