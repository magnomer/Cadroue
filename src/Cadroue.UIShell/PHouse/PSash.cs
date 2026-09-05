using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Cadroue.UIShell.PHouse;

internal static class PSash
{
    private const int PSashLimitMessage = 0x0024;
    private const uint PSashMonitorNearest = 0x00000002;

    internal static bool PSashInteractiveCheck(DependencyObject? pSource)
    {
        while (pSource is not null)
        {
            if (pSource is ButtonBase
                or TextBoxBase
                or PasswordBox
                or Selector
                or RangeBase
                or Thumb
                or MenuItem
                or Hyperlink)
            {
                return true;
            }

            pSource = pSource is Visual or Visual3D
                ? VisualTreeHelper.GetParent(pSource)
                : LogicalTreeHelper.GetParent(pSource);
        }

        return false;
    }

    internal static void PSashAttach(Window pWindow)
    {
        IntPtr pWindowHandle = new WindowInteropHelper(pWindow).Handle;
        if (pWindowHandle == IntPtr.Zero)
        {
            EventHandler? pSourceHandle = null;
            pSourceHandle = (_, _) =>
            {
                pWindow.SourceInitialized -= pSourceHandle;
                PSashAttach(pWindow);
            };
            pWindow.SourceInitialized += pSourceHandle;
            return;
        }

        HwndSource.FromHwnd(pWindowHandle)?.AddHook(PSashMessageHandle);
    }

    internal static void PSashPlacementRestore(
        Window pWindow,
        double? pLeft,
        double? pTop,
        double pWidth,
        double pHeight)
    {
        PSashAttach(pWindow);
        if (new WindowInteropHelper(pWindow).Handle != IntPtr.Zero)
        {
            PSashPlacementApply(pWindow, pLeft, pTop, pWidth, pHeight);
            return;
        }

        EventHandler? pSourceHandle = null;
        pSourceHandle = (_, _) =>
        {
            pWindow.SourceInitialized -= pSourceHandle;
            PSashPlacementApply(pWindow, pLeft, pTop, pWidth, pHeight);
        };
        pWindow.SourceInitialized += pSourceHandle;
    }

    internal static void PSashDragMove(Window pWindow, MouseEventArgs pEvent, double pBandHeight)
    {
        if (pWindow.WindowState == WindowState.Maximized)
        {
            Point pPointer = pEvent.GetPosition(pWindow);
            Point pPointerScreen = pWindow.PointToScreen(pPointer);
            Rect pRestoreBounds = pWindow.RestoreBounds;
            double pHorizontalRatio = pWindow.ActualWidth > 0
                ? Math.Clamp(pPointer.X / pWindow.ActualWidth, 0, 1)
                : 0.5;
            Matrix pFromDevice = PresentationSource.FromVisual(pWindow)?.CompositionTarget?.TransformFromDevice
                ?? Matrix.Identity;
            Point pPointerDip = pFromDevice.Transform(pPointerScreen);

            pWindow.WindowState = WindowState.Normal;
            double pRestoreWidth = pRestoreBounds.Width > 0 ? pRestoreBounds.Width : pWindow.Width;
            pWindow.Left = pPointerDip.X - (pRestoreWidth * pHorizontalRatio);
            pWindow.Top = pPointerDip.Y - Math.Min(pPointer.Y, pBandHeight / 2);
        }

        pWindow.DragMove();
    }

    private static void PSashPlacementApply(
        Window pWindow,
        double? pLeft,
        double? pTop,
        double pWidth,
        double pHeight)
    {
        HwndSource? pSource = PresentationSource.FromVisual(pWindow) as HwndSource;
        if (pSource?.CompositionTarget is not { } pCompositionTarget)
        {
            return;
        }

        double pDesiredLeft = pLeft is double pSavedLeft && double.IsFinite(pSavedLeft)
            ? pSavedLeft
            : double.IsFinite(pWindow.Left) ? pWindow.Left : SystemParameters.WorkArea.Left;
        double pDesiredTop = pTop is double pSavedTop && double.IsFinite(pSavedTop)
            ? pSavedTop
            : double.IsFinite(pWindow.Top) ? pWindow.Top : SystemParameters.WorkArea.Top;
        double pDesiredWidth = double.IsFinite(pWidth) && pWidth > 0 ? pWidth : Math.Max(pWindow.ActualWidth, pWindow.MinWidth);
        double pDesiredHeight = double.IsFinite(pHeight) && pHeight > 0 ? pHeight : Math.Max(pWindow.ActualHeight, pWindow.MinHeight);
        var pDesiredBounds = new Rect(pDesiredLeft, pDesiredTop, pDesiredWidth, pDesiredHeight);
        Rect pWorkArea = PSashAreaRead(
            pSource.Handle,
            pDesiredBounds,
            pCompositionTarget.TransformToDevice,
            pCompositionTarget.TransformFromDevice);
        Rect pBounds = PSashBoundsClamp(
            pDesiredBounds,
            pWorkArea,
            pWindow.MinWidth,
            pWindow.MinHeight,
            pWindow.MaxWidth,
            pWindow.MaxHeight);

        pWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        pWindow.Width = pBounds.Width;
        pWindow.Height = pBounds.Height;
        pWindow.Left = pBounds.Left;
        pWindow.Top = pBounds.Top;
    }

    private static Rect PSashBoundsClamp(
        Rect pBounds,
        Rect pWorkArea,
        double pMinimumWidth,
        double pMinimumHeight,
        double pMaximumWidth,
        double pMaximumHeight)
    {
        double pWidthLimit = double.IsFinite(pMaximumWidth) ? Math.Min(pMaximumWidth, pWorkArea.Width) : pWorkArea.Width;
        double pHeightLimit = double.IsFinite(pMaximumHeight) ? Math.Min(pMaximumHeight, pWorkArea.Height) : pWorkArea.Height;
        double pWidth = Math.Min(Math.Max(pBounds.Width, pMinimumWidth), pWidthLimit);
        double pHeight = Math.Min(Math.Max(pBounds.Height, pMinimumHeight), pHeightLimit);
        double pLeft = Math.Clamp(pBounds.Left, pWorkArea.Left, pWorkArea.Right - pWidth);
        double pTop = Math.Clamp(pBounds.Top, pWorkArea.Top, pWorkArea.Bottom - pHeight);
        return new Rect(pLeft, pTop, pWidth, pHeight);
    }

    private static Rect PSashAreaRead(
        IntPtr pWindowHandle,
        Rect pBounds,
        Matrix pToDevice,
        Matrix pFromDevice)
    {
        Point pTopLeft = pToDevice.Transform(pBounds.TopLeft);
        Point pBottomRight = pToDevice.Transform(pBounds.BottomRight);
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
            pMonitor = MonitorFromWindow(pWindowHandle, PSashMonitorNearest);
        }

        var pMonitorInfo = new PSashMonitor { PSashSize = Marshal.SizeOf<PSashMonitor>() };
        if (pMonitor == IntPtr.Zero || !GetMonitorInfo(pMonitor, ref pMonitorInfo))
        {
            return SystemParameters.WorkArea;
        }

        Point pWorkTopLeft = pFromDevice.Transform(
            new Point(pMonitorInfo.PSashWork.PSashLeft, pMonitorInfo.PSashWork.PSashTop));
        Point pWorkBottomRight = pFromDevice.Transform(
            new Point(pMonitorInfo.PSashWork.PSashRight, pMonitorInfo.PSashWork.PSashBottom));
        return new Rect(pWorkTopLeft, pWorkBottomRight);
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

        PSashLimits pWindowLimits = Marshal.PtrToStructure<PSashLimits>(pLParam);
        pWindowLimits.PSashPosition.PSashX =
            pMonitorInfo.PSashWork.PSashLeft - pMonitorInfo.PSashBounds.PSashLeft;
        pWindowLimits.PSashPosition.PSashY =
            pMonitorInfo.PSashWork.PSashTop - pMonitorInfo.PSashBounds.PSashTop;
        pWindowLimits.PSashSize.PSashX =
            pMonitorInfo.PSashWork.PSashRight - pMonitorInfo.PSashWork.PSashLeft;
        pWindowLimits.PSashSize.PSashY =
            pMonitorInfo.PSashWork.PSashBottom - pMonitorInfo.PSashWork.PSashTop;
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
