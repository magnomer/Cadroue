using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PViewerOverlay
{
    private const int PViewerOwnerIndex = -8;
    private const int PViewerStyleIndex = -20;
    private const nint PViewerInertStyle = 0x08000000;
    private const uint PViewerPositionFlags = 0x0001 | 0x0002 | 0x0010;
    private const double PViewerOverlayNudge = 0.5;
    private static readonly nint pViewerNotTopmost = new(-2);

    private static readonly IReadOnlyDictionary<bool, Action<PViewerOverlay>> pViewerOverlayApplies =
        new Dictionary<bool, Action<PViewerOverlay>>
        {
            [true] = pOverlay => pOverlay.PViewerOverlayOpen(),
            [false] = pOverlay => pOverlay.pViewerOverlayPopup.IsOpen = false,
        };

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint PViewerWindowLongSet(nint pWindow, int pIndex, nint pValue);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint PViewerWindowLongRead(nint pWindow, int pIndex);

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

    private readonly Popup pViewerOverlayPopup;
    private readonly PViewerMpvHost pViewerOverlayHost;
    private readonly Window pViewerOverlayWindow;
    private readonly LViewerRenderer lViewerRenderer;

    public PViewerOverlay(PViewerMpvHost pHost, LViewerRenderer lRenderer)
    {
        pViewerOverlayHost = pHost;
        lViewerRenderer = lRenderer;
        pViewerOverlayWindow = System.Windows.Application.Current.MainWindow;
        pViewerOverlayPopup = new Popup
        {
            PlacementTarget = pHost,
            Placement = PlacementMode.Relative,
            AllowsTransparency = true,
            StaysOpen = true,
            IsOpen = false
        };
        pViewerOverlayHost.SizeChanged += PViewerOverlayHandle;
        pViewerOverlayHost.IsVisibleChanged += PViewerVisibleHandle;
        pViewerOverlayWindow.LocationChanged += PViewerOverlayHandle;
        pViewerOverlayWindow.SizeChanged += PViewerOverlayHandle;
    }

    public void PViewerChildSet(FrameworkElement? pChild) => pViewerOverlayPopup.Child = pChild;

    public void PViewerOverlayDispose()
    {
        pViewerOverlayHost.SizeChanged -= PViewerOverlayHandle;
        pViewerOverlayHost.IsVisibleChanged -= PViewerVisibleHandle;
        pViewerOverlayWindow.LocationChanged -= PViewerOverlayHandle;
        pViewerOverlayWindow.SizeChanged -= PViewerOverlayHandle;
        pViewerOverlayPopup.IsOpen = false;
        pViewerOverlayPopup.Child = null;
    }

    private void PViewerOverlayHandle(object? pSender, EventArgs pEvent) => PViewerOverlayPlace();

    private void PViewerVisibleHandle(object pSender, DependencyPropertyChangedEventArgs pEvent) =>
        PViewerOverlayPlace();

    public void PViewerOverlayPlace() =>
        pViewerOverlayApplies[lViewerRenderer.LViewerOverlayHandle(
            pViewerOverlayHost.IsVisible,
            pViewerOverlayHost.ActualWidth,
            pViewerOverlayHost.ActualHeight)](this);

    private void PViewerOverlayOpen()
    {
        var pChild = (FrameworkElement)pViewerOverlayPopup.Child;
        pChild.Width = pViewerOverlayHost.ActualWidth;
        pChild.Height = pViewerOverlayHost.ActualHeight;
        pViewerOverlayPopup.IsOpen = true;
        PViewerOrderApply(pChild);
        pViewerOverlayPopup.HorizontalOffset = PViewerOverlayNudge;
        pViewerOverlayPopup.HorizontalOffset = 0;
    }

    private void PViewerOrderApply(FrameworkElement pChild)
    {
        nint pOverlayHandle = PViewerSourceRead(PresentationSource.FromVisual(pChild) as HwndSource);
        nint pOwnerHandle = new WindowInteropHelper(pViewerOverlayWindow).Handle;
        PViewerInertApply(pOverlayHandle);
        _ = PViewerWindowLongSet(pOverlayHandle, PViewerOwnerIndex, pOwnerHandle);
        _ = SetWindowPos(pOverlayHandle, pViewerNotTopmost, 0, 0, 0, 0, PViewerPositionFlags);
    }

    private static nint PViewerSourceRead(HwndSource? pSource)
    {
        try
        {
            return pSource!.Handle;
        }
        catch (NullReferenceException)
        {
            return nint.Zero;
        }
    }

    public static void PViewerInertApply(nint pWindowHandle)
    {
        nint pExStyle = PViewerWindowLongRead(pWindowHandle, PViewerStyleIndex);
        _ = PViewerWindowLongSet(pWindowHandle, PViewerStyleIndex, pExStyle | PViewerInertStyle);
    }
}
