using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

using Cadroue.Application;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PViewer
{
    private const double PCropSizeMinimum = 8;

    private void PCropGripHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (!PCropEditableCheck() || sender is not Rectangle { Tag: int pHandleIndex })
        {
            return;
        }

        pViewerEdgeX = PCropEdgeX[pHandleIndex];
        pViewerEdgeY = PCropEdgeY[pHandleIndex];
        pViewerCropDrive = pViewerEdgeX != 0 && pViewerEdgeY != 0 ? -1 : pViewerEdgeX != 0 ? 0 : 1;
        pViewerAnchorX = -pViewerEdgeX;
        pViewerAnchorY = -pViewerEdgeY;
        PCropDragStart(mouseEvent.GetPosition(pViewerOverlay));
        mouseEvent.Handled = true;
    }

    private void PCropBodyHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (!PCropEditableCheck() || pViewerCropBox.Visibility != Visibility.Visible)
        {
            return;
        }

        pViewerEdgeX = 0;
        pViewerEdgeY = 0;
        pViewerCropDrive = -1;
        pViewerAnchorX = -1;
        pViewerAnchorY = -1;
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

    private void PCropPressHandle(object sender, MouseButtonEventArgs mouseEvent)
    {
        if (pViewerTool == PViewerTool.PViewerToolNeutral)
        {
            PViewerPressHandle(mouseEvent);
            return;
        }

        if (pViewerTool != PViewerTool.PViewerToolCrop
            || !PCropActive
            || pViewerCropLocked
            || pViewerMediaInfo is null
            || !pViewerMediaInfo.LMediaVideoPresent)
        {
            return;
        }

        pViewerCropPoint = mouseEvent.GetPosition(pViewerOverlay);
        pViewerCropDrive = -1;
        pViewerAnchorX = -1;
        pViewerAnchorY = -1;
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
            PViewerMpvUpdate();
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
        PViewerMpvUpdate();
        PCropVideoChange?.Invoke(PCropVideo);
        mouseEvent.Handled = true;
    }

    private void PCropSizeHandle(object sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        PCropBoxRestore();
    }
}
