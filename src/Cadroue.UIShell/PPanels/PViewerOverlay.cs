using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

using Cadroue.Core;
using Cadroue.Media;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private const int PViewerOwnerIndex = -8;
    private const int PViewerStyleIndex = -20;
    private const int PViewerInertStyle = 0x08000000;
    private const uint PViewerPositionFlags = 0x0001 | 0x0002 | 0x0010;
    private static readonly nint pViewerNotTopmost = new(-2);

    private Popup? pViewerMpvOverlay;
    private Window? pViewerMpvWindow;

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int PViewerWindowLongSet(nint pWindow, int pIndex, int pValue);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint PViewerWindowLongPtrSet(nint pWindow, int pIndex, nint pValue);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int PViewerWindowLongRead(nint pWindow, int pIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint PViewerWindowLongPtrRead(nint pWindow, int pIndex);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint pWindow,
        nint pInsertAfter,
        int pX,
        int pY,
        int pWidth,
        int pHeight,
        uint pFlags);

    private void PViewerOverlayHandle(object? sender, EventArgs eventArgs) => PViewerOverlayPlace();

    private void PViewerVisibleHandle(object sender, DependencyPropertyChangedEventArgs eventArgs) =>
        PViewerOverlayPlace();

    private void PViewerOverlayPlace()
    {
        if (pViewerMpvOverlay is null || pViewerMpvHost is null)
        {
            return;
        }

        PViewerWindowAttach();

        bool pViewerOverlayShow = pViewerMpvHost.Visibility == Visibility.Visible
            && pViewerMpvHost.IsVisible
            && pViewerMpvHost.ActualWidth > 0
            && pViewerMpvHost.ActualHeight > 0;

        if (!pViewerOverlayShow)
        {
            if (pViewerMpvOverlay.IsOpen)
            {
                LTraceLog.LTraceInfoRecord(
                    $"mpv overlay closed (host {(pViewerMpvHost.IsVisible ? "visible" : "hidden")}, "
                    + $"vis={pViewerMpvHost.Visibility}, size {pViewerMpvHost.ActualWidth:0}x{pViewerMpvHost.ActualHeight:0})");
            }

            pViewerMpvOverlay.IsOpen = false;
            return;
        }

        if (pViewerMpvOverlay.Child is FrameworkElement pViewerOverlayChild)
        {
            pViewerOverlayChild.Width = pViewerMpvHost.ActualWidth;
            pViewerOverlayChild.Height = pViewerMpvHost.ActualHeight;
        }

        if (!pViewerMpvOverlay.IsOpen)
        {
            LTraceLog.LTraceInfoRecord(
                "mpv overlay opened",
                $"top-level transparent window over {pViewerMpvHost.ActualWidth:0}x{pViewerMpvHost.ActualHeight:0}");
        }

        pViewerMpvOverlay.IsOpen = true;
        PViewerOrderApply();
        double pViewerOverlayOffset = pViewerMpvOverlay.HorizontalOffset;
        pViewerMpvOverlay.HorizontalOffset = pViewerOverlayOffset + 0.5;
        pViewerMpvOverlay.HorizontalOffset = pViewerOverlayOffset;
    }

    private void PViewerOrderApply()
    {
        if (pViewerMpvOverlay?.Child is not Visual pViewerOverlayChild
            || PresentationSource.FromVisual(pViewerOverlayChild) is not HwndSource pViewerOverlaySource)
        {
            return;
        }

        nint pViewerOwnerHandle = PViewerWindowHandle(pViewerMpvWindow);
        if (pViewerOwnerHandle == nint.Zero)
        {
            return;
        }

        PViewerInertApply(pViewerOverlaySource.Handle);

        if (Environment.Is64BitProcess)
        {
            _ = PViewerWindowLongPtrSet(
                pViewerOverlaySource.Handle,
                PViewerOwnerIndex,
                pViewerOwnerHandle);
        }
        else
        {
            _ = PViewerWindowLongSet(
                pViewerOverlaySource.Handle,
                PViewerOwnerIndex,
                pViewerOwnerHandle.ToInt32());
        }

        _ = SetWindowPos(
            pViewerOverlaySource.Handle,
            pViewerNotTopmost,
            0,
            0,
            0,
            0,
            PViewerPositionFlags);
    }

    private static void PViewerInertApply(nint pViewerOverlayHandle)
    {
        if (Environment.Is64BitProcess)
        {
            nint pViewerExStyle = PViewerWindowLongPtrRead(pViewerOverlayHandle, PViewerStyleIndex);
            if ((pViewerExStyle & PViewerInertStyle) == PViewerInertStyle)
            {
                return;
            }

            _ = PViewerWindowLongPtrSet(
                pViewerOverlayHandle,
                PViewerStyleIndex,
                pViewerExStyle | PViewerInertStyle);
            return;
        }

        int pViewerExStyleValue = PViewerWindowLongRead(pViewerOverlayHandle, PViewerStyleIndex);
        if ((pViewerExStyleValue & PViewerInertStyle) == PViewerInertStyle)
        {
            return;
        }

        _ = PViewerWindowLongSet(
            pViewerOverlayHandle,
            PViewerStyleIndex,
            pViewerExStyleValue | PViewerInertStyle);
    }

    private void PViewerWindowAttach()
    {
        Window? pViewerMpvHostWindow = Window.GetWindow(this);
        if (ReferenceEquals(pViewerMpvHostWindow, pViewerMpvWindow))
        {
            return;
        }

        PViewerWindowDetach();
        pViewerMpvWindow = pViewerMpvHostWindow;
        if (pViewerMpvWindow is null)
        {
            return;
        }

        pViewerMpvWindow.LocationChanged += PViewerOverlayHandle;
        pViewerMpvWindow.SizeChanged += PViewerOverlayHandle;
    }

    private void PViewerWindowDetach()
    {
        if (pViewerMpvWindow is null)
        {
            return;
        }

        pViewerMpvWindow.LocationChanged -= PViewerOverlayHandle;
        pViewerMpvWindow.SizeChanged -= PViewerOverlayHandle;
        pViewerMpvWindow = null;
    }

    private void PViewerOverlayDetach()
    {
        (pViewerOverlay.Parent as Panel)?.Children.Remove(pViewerOverlay);
        (pViewerCloseButton.Parent as Panel)?.Children.Remove(pViewerCloseButton);
        (pViewerPreviewButton.Parent as Panel)?.Children.Remove(pViewerPreviewButton);
        (pViewerAudioSwitch.Parent as Panel)?.Children.Remove(pViewerAudioSwitch);
        PViewerEngineDetach();
    }
}
