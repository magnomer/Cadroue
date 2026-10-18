using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PWindowResize(Window pResizeWindow, QSash lSash)
{
    public void PResizeSashAttach()
    {
        lSash.QSashCaptureStarted += PResizeCaptureStart;
        lSash.QSashCaptureStopped += PResizeCaptureStop;
        lSash.QSashActiveChanged += PResizeRenderApply;
        lSash.QSashBoundsChanged += PResizeBoundsApply;
    }

    public void PResizeHandlersAdd()
    {
        pResizeWindow.PreviewMouseMove += PResizeMoveHandle;
        pResizeWindow.PreviewMouseLeftButtonDown += PResizePressHandle;
        pResizeWindow.PreviewMouseLeftButtonUp += PResizeReleaseHandle;
        pResizeWindow.LostMouseCapture += PResizeCaptureHandle;
        pResizeWindow.MouseLeave += PResizeLeaveHandle;
    }

    public void PResizeHandlersRemove()
    {
        PResizeDetach();
        PResizeCursorClear();
    }

    private void PResizeDetach()
    {
        pResizeWindow.PreviewMouseMove -= PResizeMoveHandle;
        pResizeWindow.PreviewMouseLeftButtonDown -= PResizePressHandle;
        pResizeWindow.PreviewMouseLeftButtonUp -= PResizeReleaseHandle;
        pResizeWindow.LostMouseCapture -= PResizeCaptureHandle;
        pResizeWindow.MouseLeave -= PResizeLeaveHandle;
    }

    private static void PResizeCursorClear() => Mouse.OverrideCursor = null;

    private static void PResizeCursorApply(int pSash) => Mouse.OverrideCursor = PLook.PLookCursor[pSash];

    private void PResizePressHandle(object sender, MouseButtonEventArgs e)
    {
        Point pPoint = e.GetPosition(pResizeWindow);
        Point pPointer = QSashPointer.QSashPointerRead(pResizeWindow, pPoint);
        e.Handled = lSash.QSashPressHandle(
            PLook.PLookNormal[pResizeWindow.WindowState],
            QSashGuard.QSashGuardCheck(e.OriginalSource as DependencyObject),
            pPoint.X,
            pPoint.Y,
            pPointer.X,
            pPointer.Y,
            QSashWindow.QSashWindowRead(pResizeWindow));
    }

    private void PResizeMoveHandle(object sender, MouseEventArgs e)
    {
        Point pPoint = e.GetPosition(pResizeWindow);
        Point pPointer = QSashPointer.QSashPointerRead(pResizeWindow, pPoint);
        e.Handled = lSash.QSashMoveHandle(
            PLook.PLookNormal[pResizeWindow.WindowState],
            QSashGuard.QSashGuardCheck(e.OriginalSource as DependencyObject),
            pPoint.X,
            pPoint.Y,
            pResizeWindow.ActualWidth,
            pResizeWindow.ActualHeight,
            pPointer.X,
            pPointer.Y,
            pResizeWindow.MinWidth,
            pResizeWindow.MinHeight);
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

    private void PResizeCaptureStart() => Mouse.Capture(pResizeWindow);

    private static void PResizeCaptureStop() => Mouse.Capture(null);

    private static void PResizeRenderApply(bool pActive) =>
        RenderOptions.ProcessRenderMode = PLook.PLookRender[pActive];

    private void PResizeBoundsApply(QSashBounds lBounds) => QSashWindow.QSashWindowApply(pResizeWindow, lBounds);
}
