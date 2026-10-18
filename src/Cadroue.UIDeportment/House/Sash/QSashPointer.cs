using System.Windows;
using System.Windows.Interop;

namespace Cadroue.UIDeportment;

// A window's points as device-independent screen points.
public static class QSashPointer
{
    // A point in the window, as a point on the screen.
    public static Point QSashPointerRead(Window lWindow, Point lWindowPoint) =>
        QSashPointerResolve(lWindow, lWindow.PointToScreen(lWindowPoint));

    // A point in device pixels, as device-independent pixels.
    public static Point QSashPointerResolve(Window lWindow, Point lDevicePoint) =>
        (PresentationSource.FromVisual(lWindow) as HwndSource)!.CompositionTarget.TransformFromDevice
            .Transform(lDevicePoint);
}
