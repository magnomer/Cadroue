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
        PResizeDetach();
        PResizeCursorClear();
    }

    private void PResizeDetach()
    {
        PreviewMouseMove -= PResizeMoveHandle;
        PreviewMouseLeftButtonDown -= PResizePressHandle;
        PreviewMouseLeftButtonUp -= PResizeReleaseHandle;
        LostMouseCapture -= PResizeCaptureHandle;
        MouseLeave -= PResizeLeaveHandle;
    }

    private static void PResizeCursorClear() => Mouse.OverrideCursor = null;

    private static void PResizeCursorApply(int pSash) => Mouse.OverrideCursor = PLook.PLookCursor[pSash];

    private void PResizePressHandle(object sender, MouseButtonEventArgs e)
    {
        Point pPoint = e.GetPosition(this);
        Point pPointer = QSashPointer.QSashPointerRead(this, pPoint);
        e.Handled = lSash.QSashPressHandle(
            PLook.PLookNormal[WindowState],
            QSashGuard.QSashGuardCheck(e.OriginalSource as DependencyObject),
            pPoint.X,
            pPoint.Y,
            pPointer.X,
            pPointer.Y,
            QSashWindow.QSashWindowRead(this));
    }

    private void PResizeMoveHandle(object sender, MouseEventArgs e)
    {
        Point pPoint = e.GetPosition(this);
        Point pPointer = QSashPointer.QSashPointerRead(this, pPoint);
        e.Handled = lSash.QSashMoveHandle(
            PLook.PLookNormal[WindowState],
            QSashGuard.QSashGuardCheck(e.OriginalSource as DependencyObject),
            pPoint.X,
            pPoint.Y,
            ActualWidth,
            ActualHeight,
            pPointer.X,
            pPointer.Y,
            MinWidth,
            MinHeight);
        PResizeCursorApply(lSash.QSashDirection);
    }

    private void PResizeLeaveHandle(object sender, MouseEventArgs e)
    {
        Mouse.OverrideCursor = PLook.PLookCursor[lSash.QSashLeaveResolve()];
    }

    private void PResizeReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        e.Handled = lSash.QSashReleaseHandle();
    }

    private void PResizeCaptureHandle(object sender, MouseEventArgs e)
    {
        lSash.QSashCaptureHandle();
        PResizeCursorClear();
    }

    private void PResizeCaptureStart() => Mouse.Capture(this);

    private static void PResizeCaptureStop() => Mouse.Capture(null);

    private static void PResizeRenderApply(bool pActive) =>
        RenderOptions.ProcessRenderMode = PLook.PLookRender[pActive];

    private void PResizeBoundsApply(QSashBounds lBounds) => QSashWindow.QSashWindowApply(this, lBounds);
}
