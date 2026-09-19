using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PHouse;

internal static class PSash
{
    private const int PSashLimitMessage = 0x0024;
    private const uint PSashMonitorNearest = 0x00000002;

    private static readonly Type[] PSashInteractiveTypes =
    [
        typeof(ButtonBase),
        typeof(TextBoxBase),
        typeof(PasswordBox),
        typeof(Selector),
        typeof(RangeBase),
        typeof(Thumb),
        typeof(MenuItem),
        typeof(Hyperlink),
    ];

    internal static bool PSashInteractiveCheck(DependencyObject? pSource) =>
        PWalk.PWalkParentCheck(pSource, PSashInteractiveMatch);

    private static bool PSashInteractiveMatch(DependencyObject pNode) =>
        PWalk.PWalkTypeCheck(pNode, PSashInteractiveTypes);

    internal static void PSashAttach(Window pWindow) =>
        PSashSourceRun(pWindow, PSashHookApply);

    internal static void PSashPlacementRestore(
        Window pWindow,
        double? pLeft,
        double? pTop,
        double pWidth,
        double pHeight)
    {
        PSashAttach(pWindow);
        PSashSourceRun(pWindow, pTarget => PSashPlacementApply(pTarget, pLeft, pTop, pWidth, pHeight));
    }

    internal static void PSashDragMove(Window pWindow, MouseEventArgs pEvent, double pBandHeight)
    {
        Point pPointer = pEvent.GetPosition(pWindow);
        Point pPointerDip = PSashPointerRead(pWindow, pPointer);
        Rect pRestoreBounds = pWindow.RestoreBounds;
        double pActualWidth = pWindow.ActualWidth;
        bool pMaximized = PLook.PLookMaximized[pWindow.WindowState];
        pWindow.WindowState = PLook.PLookUnmaximized[pWindow.WindowState];
        (pWindow.Left, pWindow.Top) = LSash.LSashDragResolve(
            pMaximized,
            pWindow.Left,
            pWindow.Top,
            pPointer.X,
            pPointer.Y,
            pPointerDip.X,
            pPointerDip.Y,
            pActualWidth,
            pRestoreBounds.Width,
            pWindow.Width,
            pBandHeight);
        pWindow.DragMove();
    }

    internal static Point PSashPointerRead(Window pWindow, Point pWindowPoint)
    {
        Point pScreenPoint = pWindow.PointToScreen(pWindowPoint);
        Matrix pFromDevice = PSashDipRead(pWindow);
        return pFromDevice.Transform(pScreenPoint);
    }

    internal static LSashBounds PSashBoundsRead(Window pWindow) =>
        new(pWindow.Left, pWindow.Top, pWindow.ActualWidth, pWindow.ActualHeight);

    internal static void PSashBoundsApply(Window pWindow, LSashBounds lBounds)
    {
        pWindow.Left = lBounds.LSashLeft;
        pWindow.Top = lBounds.LSashTop;
        pWindow.Width = lBounds.LSashWidth;
        pWindow.Height = lBounds.LSashHeight;
    }

    private static void PSashSourceRun(Window pWindow, Action<Window> pStep)
    {
        if (PSashHandleRead(pWindow) == IntPtr.Zero)
        {
            PSashSourceDefer(pWindow, pStep);
            return;
        }

        pStep(pWindow);
    }

    private static void PSashSourceDefer(Window pWindow, Action<Window> pStep)
    {
        EventHandler? pSourceHandle = null;
        pSourceHandle = (_, _) =>
        {
            pWindow.SourceInitialized -= pSourceHandle;
            pStep(pWindow);
        };
        pWindow.SourceInitialized += pSourceHandle;
    }

    private static IntPtr PSashHandleRead(Window pWindow) => new WindowInteropHelper(pWindow).Handle;

    private static void PSashHookApply(Window pWindow) =>
        HwndSource.FromHwnd(PSashHandleRead(pWindow))?.AddHook(PSashMessageHandle);

    private static Matrix PSashDipRead(Window pWindow) =>
        (PresentationSource.FromVisual(pWindow) as HwndSource)!.CompositionTarget.TransformFromDevice;

    private static Matrix PSashDeviceRead(Window pWindow) =>
        (PresentationSource.FromVisual(pWindow) as HwndSource)!.CompositionTarget.TransformToDevice;

    private static void PSashPlacementApply(
        Window pWindow,
        double? pLeft,
        double? pTop,
        double pWidth,
        double pHeight)
    {
        LSashBounds lDesired = LSash.LSashPlacementResolve(
            pLeft,
            pTop,
            pWidth,
            pHeight,
            PSashBoundsRead(pWindow),
            pWindow.MinWidth,
            pWindow.MinHeight,
            SystemParameters.WorkArea.Left,
            SystemParameters.WorkArea.Top);
        LSashBounds lBounds = LSash.LSashBoundsClamp(
            lDesired,
            PSashAreaRead(pWindow, lDesired),
            pWindow.MinWidth,
            pWindow.MinHeight,
            pWindow.MaxWidth,
            pWindow.MaxHeight);

        pWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        pWindow.Width = lBounds.LSashWidth;
        pWindow.Height = lBounds.LSashHeight;
        pWindow.Left = lBounds.LSashLeft;
        pWindow.Top = lBounds.LSashTop;
    }

    private static LSashBounds PSashAreaRead(Window pWindow, LSashBounds lBounds)
    {
        Matrix pToDevice = PSashDeviceRead(pWindow);
        Point pTopLeft = pToDevice.Transform(new Point(lBounds.LSashLeft, lBounds.LSashTop));
        Point pBottomRight = pToDevice.Transform(new Point(lBounds.LSashRight, lBounds.LSashBottom));
        var pNativeBounds = new PSashRect
        {
            PSashLeft = (int)Math.Floor(pTopLeft.X),
            PSashTop = (int)Math.Floor(pTopLeft.Y),
            PSashRight = (int)Math.Ceiling(pBottomRight.X),
            PSashBottom = (int)Math.Ceiling(pBottomRight.Y)
        };
        IntPtr pMonitor = MonitorFromRect(ref pNativeBounds, PSashMonitorNearest);
        if (pMonitor == IntPtr.Zero)
        {
            pMonitor = MonitorFromWindow(PSashHandleRead(pWindow), PSashMonitorNearest);
        }

        var pMonitorInfo = new PSashMonitor { PSashSize = Marshal.SizeOf<PSashMonitor>() };
        if (pMonitor == IntPtr.Zero || !GetMonitorInfo(pMonitor, ref pMonitorInfo))
        {
            Rect pWorkArea = SystemParameters.WorkArea;
            return new LSashBounds(pWorkArea.Left, pWorkArea.Top, pWorkArea.Width, pWorkArea.Height);
        }

        Matrix pFromDevice = PSashDipRead(pWindow);
        Point pWorkTopLeft = pFromDevice.Transform(
            new Point(pMonitorInfo.PSashWork.PSashLeft, pMonitorInfo.PSashWork.PSashTop));
        Point pWorkBottomRight = pFromDevice.Transform(
            new Point(pMonitorInfo.PSashWork.PSashRight, pMonitorInfo.PSashWork.PSashBottom));
        return LSashBounds.LSashCornersCreate(
            pWorkTopLeft.X,
            pWorkTopLeft.Y,
            pWorkBottomRight.X,
            pWorkBottomRight.Y);
    }

    private static IntPtr PSashMessageHandle(
        IntPtr pWindowHandle,
        int pMessage,
        IntPtr pWParam,
        IntPtr pLParam,
        ref bool pHandled)
    {
        if (pMessage != PSashLimitMessage)
        {
            return IntPtr.Zero;
        }

        IntPtr pMonitor = MonitorFromWindow(pWindowHandle, PSashMonitorNearest);
        var pMonitorInfo = new PSashMonitor { PSashSize = Marshal.SizeOf<PSashMonitor>() };
        if (pMonitor == IntPtr.Zero || !GetMonitorInfo(pMonitor, ref pMonitorInfo))
        {
            return IntPtr.Zero;
        }

        LSashLimits lLimits = LSash.LSashLimitsResolve(
            pMonitorInfo.PSashBounds.PSashLeft,
            pMonitorInfo.PSashBounds.PSashTop,
            pMonitorInfo.PSashWork.PSashLeft,
            pMonitorInfo.PSashWork.PSashTop,
            pMonitorInfo.PSashWork.PSashRight,
            pMonitorInfo.PSashWork.PSashBottom);
        PSashLimits pWindowLimits = Marshal.PtrToStructure<PSashLimits>(pLParam);
        pWindowLimits.PSashPosition.PSashX = (int)lLimits.LSashX;
        pWindowLimits.PSashPosition.PSashY = (int)lLimits.LSashY;
        pWindowLimits.PSashSize.PSashX = (int)lLimits.LSashWidth;
        pWindowLimits.PSashSize.PSashY = (int)lLimits.LSashHeight;
        Marshal.StructureToPtr(pWindowLimits, pLParam, false);
        pHandled = true;
        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PSashPoint
    {
        internal int PSashX;
        internal int PSashY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PSashRect
    {
        internal int PSashLeft;
        internal int PSashTop;
        internal int PSashRight;
        internal int PSashBottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PSashMonitor
    {
        internal int PSashSize;
        internal PSashRect PSashBounds;
        internal PSashRect PSashWork;
        internal uint PSashFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PSashLimits
    {
        internal PSashPoint PSashReserved;
        internal PSashPoint PSashSize;
        internal PSashPoint PSashPosition;
        internal PSashPoint PSashMinimumTrack;
        internal PSashPoint PSashMaximumTrack;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref PSashRect rectangle, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr windowHandle, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitorHandle, ref PSashMonitor monitorInfo);
}
