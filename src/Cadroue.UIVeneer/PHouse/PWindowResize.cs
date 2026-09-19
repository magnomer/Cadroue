using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PHouse;

public partial class PWindow
{
    private void PResizeHandlersAdd()
    {
        PreviewMouseMove += PResizeMoveHandle;
        PreviewMouseLeftButtonDown += PResizePressHandle;
        PreviewMouseLeftButtonUp += PResizeReleaseHandle;
        LostMouseCapture += PResizeCaptureHandle;
        MouseLeave += PResizeLeaveHandle;
    }

    private void PResizeHandlersRemove()
    {
        PreviewMouseMove -= PResizeMoveHandle;
        PreviewMouseLeftButtonDown -= PResizePressHandle;
        PreviewMouseLeftButtonUp -= PResizeReleaseHandle;
        LostMouseCapture -= PResizeCaptureHandle;
        MouseLeave -= PResizeLeaveHandle;
        Mouse.OverrideCursor = null;
    }

    private void PResizePressHandle(object sender, MouseButtonEventArgs e)
    {
        Point pPoint = e.GetPosition(this);
        Point pPointer = PSash.PSashPointerRead(this, pPoint);
        e.Handled = lSash.LSashPressHandle(
            PLook.PLookNormal[WindowState],
            PSash.PSashInteractiveCheck(e.OriginalSource as DependencyObject),
            pPoint.X,
            pPoint.Y,
            pPointer.X,
            pPointer.Y,
            PSash.PSashBoundsRead(this));
    }

    private void PResizeMoveHandle(object sender, MouseEventArgs e)
    {
        Point pPoint = e.GetPosition(this);
        Point pPointer = PSash.PSashPointerRead(this, pPoint);
        e.Handled = lSash.LSashMoveHandle(
            PLook.PLookNormal[WindowState],
            PSash.PSashInteractiveCheck(e.OriginalSource as DependencyObject),
            pPoint.X,
            pPoint.Y,
            ActualWidth,
            ActualHeight,
            pPointer.X,
            pPointer.Y,
            MinWidth,
            MinHeight);
        Mouse.OverrideCursor = PLook.PLookCursor[lSash.LSashDirection];
    }

    private void PResizeLeaveHandle(object sender, MouseEventArgs e)
    {
        Mouse.OverrideCursor = PLook.PLookCursor[lSash.LSashLeaveResolve()];
    }

    private void PResizeReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        e.Handled = lSash.LSashReleaseHandle();
    }

    private void PResizeCaptureHandle(object sender, MouseEventArgs e)
    {
        lSash.LSashCaptureHandle();
        Mouse.OverrideCursor = null;
    }

    private void PResizeCaptureStart() => Mouse.Capture(this);

    private static void PResizeCaptureStop() => Mouse.Capture(null);

    private static void PResizeRenderApply(bool pActive) =>
        RenderOptions.ProcessRenderMode = PLook.PLookRender[pActive];

    private void PResizeBoundsApply(LSashBounds lBounds) => PSash.PSashBoundsApply(this, lBounds);
}
