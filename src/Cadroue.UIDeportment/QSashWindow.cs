using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Cadroue.UIDeportment;

// A window's frame: its bounds read and applied, the drag out of maximized, and the placement restored to the
// monitor work area once the window has a handle.
public static class QSashWindow
{
    public static QSashBounds QSashWindowRead(Window lWindow) =>
        new(lWindow.Left, lWindow.Top, lWindow.ActualWidth, lWindow.ActualHeight);

    public static void QSashWindowApply(Window lWindow, QSashBounds lBounds)
    {
        lWindow.Left = lBounds.QSashLeft;
        lWindow.Top = lBounds.QSashTop;
        lWindow.Width = lBounds.QSashWidth;
        lWindow.Height = lBounds.QSashHeight;
    }

    // A maximized window is restored under the pointer before the drag starts.
    public static void QSashWindowMove(Window lWindow, MouseEventArgs lEvent, double lBandHeight)
    {
        Point lPointer = lEvent.GetPosition(lWindow);
        Point lPointerDip = QSashPointer.QSashPointerRead(lWindow, lPointer);
        Rect lRestoreBounds = lWindow.RestoreBounds;
        double lActualWidth = lWindow.ActualWidth;
        bool lMaximized = lWindow.WindowState == WindowState.Maximized;
        lWindow.WindowState = lMaximized ? WindowState.Normal : lWindow.WindowState;
        (lWindow.Left, lWindow.Top) = QSash.QSashDragResolve(
            lMaximized,
            lWindow.Left,
            lWindow.Top,
            lPointer.X,
            lPointer.Y,
            lPointerDip.X,
            lPointerDip.Y,
            lActualWidth,
            lRestoreBounds.Width,
            lWindow.Width,
            lBandHeight);
        lWindow.DragMove();
    }

    // The maximize hook is attached first; both steps wait for the window's handle.
    public static void QSashWindowRestore(
        Window lWindow,
        double? lLeft,
        double? lTop,
        double lWidth,
        double lHeight)
    {
        QSashWindowRun(lWindow, QSashScreen.QSashScreenAttach);
        QSashWindowRun(lWindow, lTarget => QSashWindowPlace(lTarget, lLeft, lTop, lWidth, lHeight));
    }

    private static void QSashWindowRun(Window lWindow, Action<Window> lStep)
    {
        if (new WindowInteropHelper(lWindow).Handle == IntPtr.Zero)
        {
            QSashWindowDefer(lWindow, lStep);
            return;
        }

        lStep(lWindow);
    }

    private static void QSashWindowDefer(Window lWindow, Action<Window> lStep)
    {
        EventHandler? lSourceHandle = null;
        lSourceHandle = (_, _) =>
        {
            lWindow.SourceInitialized -= lSourceHandle;
            lStep(lWindow);
        };
        lWindow.SourceInitialized += lSourceHandle;
    }

    private static void QSashWindowPlace(
        Window lWindow,
        double? lLeft,
        double? lTop,
        double lWidth,
        double lHeight)
    {
        QSashBounds lDesired = QSash.QSashPlacementResolve(
            lLeft,
            lTop,
            lWidth,
            lHeight,
            QSashWindowRead(lWindow),
            lWindow.MinWidth,
            lWindow.MinHeight,
            SystemParameters.WorkArea.Left,
            SystemParameters.WorkArea.Top);
        QSashBounds lBounds = QSash.QSashBoundsClamp(
            lDesired,
            QSashScreen.QSashScreenRead(lWindow, lDesired),
            lWindow.MinWidth,
            lWindow.MinHeight,
            lWindow.MaxWidth,
            lWindow.MaxHeight);

        lWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        lWindow.Width = lBounds.QSashWidth;
        lWindow.Height = lBounds.QSashHeight;
        lWindow.Left = lBounds.QSashLeft;
        lWindow.Top = lBounds.QSashTop;
    }
}
