using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Cadroue.UIShell.PSShared;

internal static class PSWindowManagement
{
    private const int PSWindowLimitMessage = 0x0024;
    private const uint PSMonitorDefaultNearest = 0x00000002;

    internal static void PSWindowAttach(Window pWindow)
    {
        IntPtr pWindowHandle = new WindowInteropHelper(pWindow).Handle;
        if (pWindowHandle == IntPtr.Zero)
        {
            EventHandler? pSourceHandle = null;
            pSourceHandle = (_, _) =>
            {
                pWindow.SourceInitialized -= pSourceHandle;
                PSWindowAttach(pWindow);
            };
            pWindow.SourceInitialized += pSourceHandle;
            return;
        }

        HwndSource.FromHwnd(pWindowHandle)?.AddHook(PSWindowMessageHandle);
    }

    internal static void PSWindowPlacementRestore(
        Window pWindow,
        double? pLeft,
        double? pTop,
        double pWidth,
        double pHeight)
    {
        PSWindowAttach(pWindow);
        if (new WindowInteropHelper(pWindow).Handle != IntPtr.Zero)
        {
            PSWindowPlacementApply(pWindow, pLeft, pTop, pWidth, pHeight);
            return;
        }

        EventHandler? pSourceHandle = null;
        pSourceHandle = (_, _) =>
        {
            pWindow.SourceInitialized -= pSourceHandle;
            PSWindowPlacementApply(pWindow, pLeft, pTop, pWidth, pHeight);
        };
        pWindow.SourceInitialized += pSourceHandle;
    }

    internal static void PSWindowDragMove(Window pWindow, MouseEventArgs pEvent)
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
            pWindow.Top = pPointerDip.Y - Math.Min(pPointer.Y, PSCasement.PSCasementBandHeight / 2);
        }

        pWindow.DragMove();
    }

    private static void PSWindowPlacementApply(
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
        Rect pWorkArea = PSWindowAreaRead(
            pSource.Handle,
            pDesiredBounds,
            pCompositionTarget.TransformToDevice,
            pCompositionTarget.TransformFromDevice);
        Rect pBounds = PSWindowBoundsClamp(
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

    private static Rect PSWindowBoundsClamp(
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

    private static Rect PSWindowAreaRead(
        IntPtr pWindowHandle,
        Rect pBounds,
        Matrix pToDevice,
        Matrix pFromDevice)
    {
        Point pTopLeft = pToDevice.Transform(pBounds.TopLeft);
        Point pBottomRight = pToDevice.Transform(pBounds.BottomRight);
        var pNativeBounds = new PSWindowRect
        {
            PSRectLeft = (int)Math.Floor(pTopLeft.X),
            PSRectTop = (int)Math.Floor(pTopLeft.Y),
            PSRectRight = (int)Math.Ceiling(pBottomRight.X),
            PSRectBottom = (int)Math.Ceiling(pBottomRight.Y)
        };
        IntPtr pMonitor = MonitorFromRect(ref pNativeBounds, PSMonitorDefaultNearest);
        if (pMonitor == IntPtr.Zero)
        {
            pMonitor = MonitorFromWindow(pWindowHandle, PSMonitorDefaultNearest);
        }

        var pMonitorInfo = new PSMonitorInfo { PSMonitorSize = Marshal.SizeOf<PSMonitorInfo>() };
        if (pMonitor == IntPtr.Zero || !GetMonitorInfo(pMonitor, ref pMonitorInfo))
        {
            return SystemParameters.WorkArea;
        }

        Point pWorkTopLeft = pFromDevice.Transform(
            new Point(pMonitorInfo.PSMonitorWork.PSRectLeft, pMonitorInfo.PSMonitorWork.PSRectTop));
        Point pWorkBottomRight = pFromDevice.Transform(
            new Point(pMonitorInfo.PSMonitorWork.PSRectRight, pMonitorInfo.PSMonitorWork.PSRectBottom));
        return new Rect(pWorkTopLeft, pWorkBottomRight);
    }

    private static IntPtr PSWindowMessageHandle(
        IntPtr pWindowHandle,
        int pMessage,
        IntPtr pWParam,
        IntPtr pLParam,
        ref bool pHandled)
    {
        if (pMessage != PSWindowLimitMessage)
        {
            return IntPtr.Zero;
        }

        IntPtr pMonitor = MonitorFromWindow(pWindowHandle, PSMonitorDefaultNearest);
        var pMonitorInfo = new PSMonitorInfo { PSMonitorSize = Marshal.SizeOf<PSMonitorInfo>() };
        if (pMonitor == IntPtr.Zero || !GetMonitorInfo(pMonitor, ref pMonitorInfo))
        {
            return IntPtr.Zero;
        }

        PSWindowLimits pWindowLimits = Marshal.PtrToStructure<PSWindowLimits>(pLParam);
        pWindowLimits.PSWindowPosition.PSWindowX =
            pMonitorInfo.PSMonitorWork.PSRectLeft - pMonitorInfo.PSMonitorBounds.PSRectLeft;
        pWindowLimits.PSWindowPosition.PSWindowY =
            pMonitorInfo.PSMonitorWork.PSRectTop - pMonitorInfo.PSMonitorBounds.PSRectTop;
        pWindowLimits.PSWindowSize.PSWindowX =
            pMonitorInfo.PSMonitorWork.PSRectRight - pMonitorInfo.PSMonitorWork.PSRectLeft;
        pWindowLimits.PSWindowSize.PSWindowY =
            pMonitorInfo.PSMonitorWork.PSRectBottom - pMonitorInfo.PSMonitorWork.PSRectTop;
        Marshal.StructureToPtr(pWindowLimits, pLParam, false);
        pHandled = true;
        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PSWindowPoint
    {
        internal int PSWindowX;
        internal int PSWindowY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PSWindowRect
    {
        internal int PSRectLeft;
        internal int PSRectTop;
        internal int PSRectRight;
        internal int PSRectBottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PSMonitorInfo
    {
        internal int PSMonitorSize;
        internal PSWindowRect PSMonitorBounds;
        internal PSWindowRect PSMonitorWork;
        internal uint PSMonitorFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PSWindowLimits
    {
        internal PSWindowPoint PSWindowReserved;
        internal PSWindowPoint PSWindowSize;
        internal PSWindowPoint PSWindowPosition;
        internal PSWindowPoint PSWindowMinimumTrack;
        internal PSWindowPoint PSWindowMaximumTrack;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref PSWindowRect rectangle, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr windowHandle, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitorHandle, ref PSMonitorInfo monitorInfo);
}
