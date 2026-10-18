using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Cadroue.UIDeportment;

// The maximize extent: a maximized window is kept to the work area of its monitor through the window's
// min-max message.
public static class QSashMaximize
{
    private const int QSashMaximizeMessage = 0x0024;

    public static void QSashMaximizeAttach(Window lWindow) =>
        QSashSource.QSashSourceRun(
            lWindow,
            lTarget => HwndSource.FromHwnd(new WindowInteropHelper(lTarget).Handle)?.AddHook(QSashMaximizeHandle));

    private static IntPtr QSashMaximizeHandle(
        IntPtr lWindowHandle,
        int lMessage,
        IntPtr lWParam,
        IntPtr lLParam,
        ref bool lHandled)
    {
        if (lMessage != QSashMaximizeMessage)
        {
            return IntPtr.Zero;
        }

        if (!QSashScreen.QSashScreenFind(lWindowHandle, null, out QSashBounds lArea, out QSashBounds lWork))
        {
            return IntPtr.Zero;
        }

        QSashLimits lLimits = QSashLimits.QSashLimitsResolve(
            lArea.QSashLeft,
            lArea.QSashTop,
            lWork.QSashLeft,
            lWork.QSashTop,
            lWork.QSashRight,
            lWork.QSashBottom);
        QSashExtentApply(lLParam, lLimits);
        lHandled = true;
        return IntPtr.Zero;
    }

    // The limits written into the min-max message's extent.
    private static void QSashExtentApply(IntPtr lLParam, QSashLimits lLimits)
    {
        QSashExtent lWindowLimits = Marshal.PtrToStructure<QSashExtent>(lLParam);
        lWindowLimits.QSashPosition.QSashX = (int)lLimits.QSashX;
        lWindowLimits.QSashPosition.QSashY = (int)lLimits.QSashY;
        lWindowLimits.QSashSize.QSashX = (int)lLimits.QSashWidth;
        lWindowLimits.QSashSize.QSashY = (int)lLimits.QSashHeight;
        Marshal.StructureToPtr(lWindowLimits, lLParam, false);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct QSashPoint
    {
        internal int QSashX;
        internal int QSashY;
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
}
