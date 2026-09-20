using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PViewer
{
    private const double PCropSizeMinimum = 8;

    private void PCropGripHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (!PCropEditableCheck() || sender is not Rectangle { Tag: int pHandleIndex })
        {
            return;
        }

        LCrop.LCropGripSet(PCropEdgeX[pHandleIndex], PCropEdgeY[pHandleIndex]);
        PCropDragStart(mouseEvent.GetPosition(pViewerOverlay));
        mouseEvent.Handled = true;
    }

    private void PCropBodyHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (!PCropEditableCheck() || pViewerCropBox.Visibility != Visibility.Visible)
        {
            return;
        }

        LCrop.LCropBodySet();
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
        pViewerCropPress = true;
        pViewerOverlay.CaptureMouse();
    }

    private void PCropDragApply(Point psNameDragPoint)
    {
        LCropbox pCropVideo = PCropboxResolve(PCropRectRead());
        LCropbox pCropOrigin = PCropboxResolve(pViewerCropOrigin);
        LCropbox pCropResult;
        if (LCrop.LCropMoveCheck())
        {
            pCropResult = LCropbox.LCropboxMoveResolve(
                pCropOrigin, pViewerCropGrab.X, pViewerCropGrab.Y, psNameDragPoint.X, psNameDragPoint.Y, pCropVideo);
        }
        else
        {
            Point pCropClamped = PCropPointClamp(psNameDragPoint);
            pCropResult = LCropbox.LCropboxResizeResolve(
                pCropOrigin, pCropClamped.X, pCropClamped.Y, LCrop.LCropEdgeX, LCrop.LCropEdgeY,
                LCrop.LCropRatioWidth, LCrop.LCropRatioHeight, pCropVideo, PCropSizeMinimum);
        }

        Canvas.SetLeft(pViewerCropBox, pCropResult.LCropboxX);
        Canvas.SetTop(pViewerCropBox, pCropResult.LCropboxY);
        pViewerCropBox.Width = pCropResult.LCropboxWidth;
        pViewerCropBox.Height = pCropResult.LCropboxHeight;
        PCropOverlayUpdate();
    }

    private void PCropPressHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (LViewer.LViewerTool == LViewerTool.LViewerToolNeutral)
        {
            PViewerPressHandle(mouseEvent);
            return;
        }

        if (LViewer.LViewerTool != LViewerTool.LViewerToolCrop
            || !PCropActive
            || LCrop.LCropLocked
            || !LViewer.LViewerVideoPresent)
        {
            return;
        }

        pViewerCropPoint = mouseEvent.GetPosition(pViewerOverlay);
        LCrop.LCropDrawSet();
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

        if (pViewerCropPress)
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
        if (pViewerCropPress)
        {
            PCropDragApply(mouseEvent.GetPosition(pViewerOverlay));
            pViewerCropPress = false;
            pViewerOverlay.ReleaseMouseCapture();
            PCropVideoCommit();
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
        PCropVideoCommit();
        mouseEvent.Handled = true;
    }

    private void PCropVideoCommit()
    {
        LViewer.LViewerPreviewSet(LViewer.LViewerPreview.LCropboxChange(PViewerCropboxRead(PCropVideoRead())));
        PViewerMpvUpdate();
        PCropVideoChange?.Invoke();
    }

    private void PCropSizeHandle(object sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        PCropBoxRestore();
    }
}
