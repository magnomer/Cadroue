using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Cadroue.UIShell.PMainWindow;

public partial class PWindow
{
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        HwndSource? pWindowSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        pWindowSource?.AddHook(PWindowMessageHook);
        ComponentDispatcher.ThreadPreprocessMessage += PShortcutMessageFilter;
        PWindowDwmApply();
    }

    private IntPtr PWindowMessageHook(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == PWindowMessageErase)
        {
            handled = true;
            return new IntPtr(1);
        }

        return IntPtr.Zero;
    }

    private void PWindowDwmApply()
    {
        IntPtr pWindowHandle = new WindowInteropHelper(this).Handle;
        if (pWindowHandle == IntPtr.Zero)
        {
            return;
        }

        int pWindowCornerPreference = PWindowCornerRound;
        _ = DwmSetWindowAttribute(
            pWindowHandle,
            PWindowCornerPreference,
            ref pWindowCornerPreference,
            Marshal.SizeOf<int>());
        int pWindowCaptionColor = PWindowColorBackground;
        _ = DwmSetWindowAttribute(
            pWindowHandle,
            PWindowCaptionColor,
            ref pWindowCaptionColor,
            Marshal.SizeOf<int>());
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int windowAttribute,
        ref int attributeValue,
        int attributeByteSize);
}
