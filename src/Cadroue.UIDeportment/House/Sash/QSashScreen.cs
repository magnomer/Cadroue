using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

// The Win32 monitor reads: the work area of the monitor nearest a window's bounds, and the monitor and work
// areas of the monitor nearest a window handle.
internal static class QSashScreen
{
    private const uint QSashScreenNearest = 0x00000002;

    // The system work area stands in when no monitor answers.
    internal static QSashBounds QSashScreenRead(Window lWindow, QSashBounds lBounds)
    {
        QSashBounds lNear = QSashDeviceResolve(lWindow, lBounds);
        if (!QSashScreenFind(new WindowInteropHelper(lWindow).Handle, lNear, out _, out QSashBounds lWork))
        {
            return QSashAreaRead();
        }

        return QSashLogicalResolve(lWindow, lWork);
    }

    // The bounds in device pixels, widened outward to whole pixels.
    private static QSashBounds QSashDeviceResolve(Window lWindow, QSashBounds lBounds)
    {
        Matrix lToDevice = (PresentationSource.FromVisual(lWindow) as HwndSource)!.CompositionTarget.TransformToDevice;
        Point lTopLeft = lToDevice.Transform(new Point(lBounds.QSashLeft, lBounds.QSashTop));
        Point lBottomRight = lToDevice.Transform(new Point(lBounds.QSashRight, lBounds.QSashBottom));
        return QSashBounds.QSashCornersCreate(
            Math.Floor(lTopLeft.X),
            Math.Floor(lTopLeft.Y),
            Math.Ceiling(lBottomRight.X),
            Math.Ceiling(lBottomRight.Y));
    }

    private static QSashBounds QSashAreaRead()
    {
        Rect lWorkArea = SystemParameters.WorkArea;
        return new QSashBounds(lWorkArea.Left, lWorkArea.Top, lWorkArea.Width, lWorkArea.Height);
    }

    // A device-pixel work area as device-independent pixels.
    private static QSashBounds QSashLogicalResolve(Window lWindow, QSashBounds lWork)
    {
        Point lWorkTopLeft = QSashPointer.QSashPointerResolve(lWindow, new Point(lWork.QSashLeft, lWork.QSashTop));
        Point lWorkBottomRight = QSashPointer.QSashPointerResolve(
            lWindow, new Point(lWork.QSashRight, lWork.QSashBottom));
        return QSashBounds.QSashCornersCreate(
            lWorkTopLeft.X,
            lWorkTopLeft.Y,
            lWorkBottomRight.X,
            lWorkBottomRight.Y);
    }

    // The monitor nearest the device-pixel bounds when given, else the one nearest the window. Both areas are in
    // device pixels; false when no monitor answers.
    internal static bool QSashScreenFind(
        IntPtr lWindowHandle,
        QSashBounds? lNear,
        out QSashBounds lArea,
        out QSashBounds lWork)
    {
        IntPtr lMonitor = IntPtr.Zero;
        if (lNear is QSashBounds lBounds)
        {
            var lNativeBounds = new QSashRect
            {
                QSashLeft = (int)lBounds.QSashLeft,
                QSashTop = (int)lBounds.QSashTop,
                QSashRight = (int)lBounds.QSashRight,
                QSashBottom = (int)lBounds.QSashBottom
            };
            lMonitor = MonitorFromRect(ref lNativeBounds, QSashScreenNearest);
        }

        if (lMonitor == IntPtr.Zero)
        {
            lMonitor = MonitorFromWindow(lWindowHandle, QSashScreenNearest);
        }

        var lMonitorInfo = new QSashMonitor { QSashSize = Marshal.SizeOf<QSashMonitor>() };
        if (lMonitor == IntPtr.Zero || !GetMonitorInfo(lMonitor, ref lMonitorInfo))
        {
            lArea = default;
            lWork = default;
            return false;
        }

        lArea = QSashBounds.QSashCornersCreate(
            lMonitorInfo.QSashArea.QSashLeft,
            lMonitorInfo.QSashArea.QSashTop,
            lMonitorInfo.QSashArea.QSashRight,
            lMonitorInfo.QSashArea.QSashBottom);
        lWork = QSashBounds.QSashCornersCreate(
            lMonitorInfo.QSashWork.QSashLeft,
            lMonitorInfo.QSashWork.QSashTop,
            lMonitorInfo.QSashWork.QSashRight,
            lMonitorInfo.QSashWork.QSashBottom);
        return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct QSashRect
    {
        internal int QSashLeft;
        internal int QSashTop;
        internal int QSashRight;
        internal int QSashBottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct QSashMonitor
    {
        internal int QSashSize;
        internal QSashRect QSashArea;
        internal QSashRect QSashWork;
        internal uint QSashFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref QSashRect rectangle, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr windowHandle, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitorHandle, ref QSashMonitor monitorInfo);
}
