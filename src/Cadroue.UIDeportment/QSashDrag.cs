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
        Rect lRestoreBounds = lWindow.RestoreBounds;
        double lActualWidth = lWindow.ActualWidth;
        bool lMaximized = lWindow.WindowState == WindowState.Maximized;
        lWindow.WindowState = lMaximized ? WindowState.Normal : lWindow.WindowState;
        (lWindow.Left, lWindow.Top) = QSashDragResolve(
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
