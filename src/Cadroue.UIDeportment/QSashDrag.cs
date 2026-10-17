using System.Windows;
using System.Windows.Input;

namespace Cadroue.UIDeportment;

// The window drag: a maximized window is restored under the pointer before the drag starts.
public static class QSashDrag
{
    public static void QSashDragMove(Window lWindow, MouseEventArgs lEvent, double lBandHeight)
    {
        Point lPointer = lEvent.GetPosition(lWindow);
        Point lPointerDip = QSashPointer.QSashPointerRead(lWindow, lPointer);
        (double lRestoreWidth, double lActualWidth, bool lMaximized) = QSashDragRead(lWindow);
        QSashNormalApply(lWindow, lMaximized);
        QSashOriginApply(lWindow, lMaximized, lPointer, lPointerDip, lActualWidth, lRestoreWidth, lBandHeight);
        lWindow.DragMove();
    }

    // The window's extent before the restore changes it.
    private static (double, double, bool) QSashDragRead(Window lWindow)
    {
        Rect lRestoreBounds = lWindow.RestoreBounds;
        double lActualWidth = lWindow.ActualWidth;
        bool lMaximized = lWindow.WindowState == WindowState.Maximized;
        return (lRestoreBounds.Width, lActualWidth, lMaximized);
    }

    private static void QSashNormalApply(Window lWindow, bool lMaximized) =>
        lWindow.WindowState = lMaximized ? WindowState.Normal : lWindow.WindowState;

    private static void QSashOriginApply(
        Window lWindow,
        bool lMaximized,
        Point lPointer,
        Point lPointerDip,
        double lActualWidth,
        double lRestoreWidth,
        double lBandHeight) =>
        (lWindow.Left, lWindow.Top) = QSashDragResolve(
            lMaximized,
            lWindow.Left,
            lWindow.Top,
            lPointer.X,
            lPointer.Y,
            lPointerDip.X,
            lPointerDip.Y,
            lActualWidth,
            lRestoreWidth,
            lWindow.Width,
            lBandHeight);

    // The restored window keeps the pointer at the same share of its width, and at most half the band down.
    public static (double QSashLeft, double QSashTop) QSashDragResolve(
        bool lMaximized,
        double lLeft,
        double lTop,
        double lPointerX,
        double lPointerY,
        double lPointerDipX,
        double lPointerDipY,
        double lActualWidth,
        double lRestoreWidth,
        double lWindowWidth,
        double lBandHeight)
    {
        if (!lMaximized)
        {
            return (lLeft, lTop);
        }

        double lRatio = lActualWidth > 0 ? Math.Clamp(lPointerX / lActualWidth, 0, 1) : 0.5;
        double lWidth = lRestoreWidth > 0 ? lRestoreWidth : lWindowWidth;
        return (lPointerDipX - (lWidth * lRatio), lPointerDipY - Math.Min(lPointerY, lBandHeight / 2));
    }
}
