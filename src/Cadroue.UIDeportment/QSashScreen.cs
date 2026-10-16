using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

// The Win32 monitor reads: the work area of the monitor nearest a window's bounds, and the maximize extent kept
// to the work area of the window's monitor.
internal static class QSashScreen
{
    private const int QSashScreenMessage = 0x0024;
    private const uint QSashScreenNearest = 0x00000002;

    internal static void QSashScreenAttach(Window lWindow) =>
        HwndSource.FromHwnd(new WindowInteropHelper(lWindow).Handle)?.AddHook(QSashScreenHandle);

    // The system work area stands in when no monitor answers.
    internal static QSashBounds QSashScreenRead(Window lWindow, QSashBounds lBounds)
    {
        Matrix lToDevice = (PresentationSource.FromVisual(lWindow) as HwndSource)!.CompositionTarget.TransformToDevice;
        Point lTopLeft = lToDevice.Transform(new Point(lBounds.QSashLeft, lBounds.QSashTop));
        Point lBottomRight = lToDevice.Transform(new Point(lBounds.QSashRight, lBounds.QSashBottom));
        var lNativeBounds = new QSashRect
        {
            QSashLeft = (int)Math.Floor(lTopLeft.X),
            QSashTop = (int)Math.Floor(lTopLeft.Y),
            QSashRight = (int)Math.Ceiling(lBottomRight.X),
            QSashBottom = (int)Math.Ceiling(lBottomRight.Y)
        };
        IntPtr lMonitor = MonitorFromRect(ref lNativeBounds, QSashScreenNearest);
        if (lMonitor == IntPtr.Zero)
        {
            lMonitor = MonitorFromWindow(new WindowInteropHelper(lWindow).Handle, QSashScreenNearest);
        }

        var lMonitorInfo = new QSashMonitor { QSashSize = Marshal.SizeOf<QSashMonitor>() };
        if (lMonitor == IntPtr.Zero || !GetMonitorInfo(lMonitor, ref lMonitorInfo))
        {
            Rect lWorkArea = SystemParameters.WorkArea;
            return new QSashBounds(lWorkArea.Left, lWorkArea.Top, lWorkArea.Width, lWorkArea.Height);
        }

        Point lWorkTopLeft = QSashPointer.QSashPointerResolve(
            lWindow, new Point(lMonitorInfo.QSashWork.QSashLeft, lMonitorInfo.QSashWork.QSashTop));
        Point lWorkBottomRight = QSashPointer.QSashPointerResolve(
            lWindow, new Point(lMonitorInfo.QSashWork.QSashRight, lMonitorInfo.QSashWork.QSashBottom));
        return QSashBounds.QSashCornersCreate(
            lWorkTopLeft.X,
            lWorkTopLeft.Y,
            lWorkBottomRight.X,
            lWorkBottomRight.Y);
    }

    private static IntPtr QSashScreenHandle(
        IntPtr lWindowHandle,
        int lMessage,
        IntPtr lWParam,
        IntPtr lLParam,
        ref bool lHandled)
    {
        if (lMessage != QSashScreenMessage)
        {
            return IntPtr.Zero;
        }

        IntPtr lMonitor = MonitorFromWindow(lWindowHandle, QSashScreenNearest);
        var lMonitorInfo = new QSashMonitor { QSashSize = Marshal.SizeOf<QSashMonitor>() };
        if (lMonitor == IntPtr.Zero || !GetMonitorInfo(lMonitor, ref lMonitorInfo))
        {
            return IntPtr.Zero;
        }

        QSashLimits lLimits = QSash.QSashLimitsResolve(
            lMonitorInfo.QSashArea.QSashLeft,
            lMonitorInfo.QSashArea.QSashTop,
            lMonitorInfo.QSashWork.QSashLeft,
            lMonitorInfo.QSashWork.QSashTop,
            lMonitorInfo.QSashWork.QSashRight,
            lMonitorInfo.QSashWork.QSashBottom);
        QSashExtent lWindowLimits = Marshal.PtrToStructure<QSashExtent>(lLParam);
        lWindowLimits.QSashPosition.QSashX = (int)lLimits.QSashX;
        lWindowLimits.QSashPosition.QSashY = (int)lLimits.QSashY;
        lWindowLimits.QSashSize.QSashX = (int)lLimits.QSashWidth;
        lWindowLimits.QSashSize.QSashY = (int)lLimits.QSashHeight;
        Marshal.StructureToPtr(lWindowLimits, lLParam, false);
        lHandled = true;
        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct QSashPoint
    {
        internal int QSashX;
        internal int QSashY;
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

    [StructLayout(LayoutKind.Sequential)]
    private struct QSashExtent
    {
        internal QSashPoint QSashReserved;
        internal QSashPoint QSashSize;
        internal QSashPoint QSashPosition;
        internal QSashPoint QSashMinimumTrack;
        internal QSashPoint QSashMaximumTrack;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref QSashRect rectangle, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr windowHandle, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitorHandle, ref QSashMonitor monitorInfo);
}
