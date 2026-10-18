using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Cadroue.UIVeneer.PHouse;

internal static class PWindowDwm
{
    private const int PWindowMessageErase = 0x0014;
    private const int PWindowCornerPreference = 33;
    private const int PWindowCornerRound = 2;
    private const int PWindowCaptionColor = 35;
    private const int PWindowColorBackground = 0x00F7E8DC;
    private const int PWindowMessageIcon = 0x0080;
    private const int PWindowIconSmall = 0;
    private const int PWindowIconBig = 1;

    public static void PWindowMessageAttach(Window pWindow)
    {
        HwndSource? pWindowSource = HwndSource.FromHwnd(new WindowInteropHelper(pWindow).Handle);
        pWindowSource?.AddHook(PWindowMessageHook);
    }

    public static void PWindowIconApply(Window pWindow)
    {
        IntPtr pWindowHandle = new WindowInteropHelper(pWindow).Handle;
        _ = ExtractIconEx(Environment.ProcessPath!, 0, out IntPtr pIconLarge, out IntPtr pIconSmall, 1);
        _ = SendMessage(pWindowHandle, PWindowMessageIcon, new IntPtr(PWindowIconSmall), pIconSmall);
        _ = SendMessage(pWindowHandle, PWindowMessageIcon, new IntPtr(PWindowIconBig), pIconLarge);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(
        string file,
        int index,
        out IntPtr iconLarge,
        out IntPtr iconSmall,
        uint iconCount);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam);

    private static IntPtr PWindowMessageHook(
        IntPtr windowHandle,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message == PWindowMessageErase)
        {
            handled = true;
            return new IntPtr(1);
        }

        return IntPtr.Zero;
    }

    public static void PWindowDwmApply(Window pWindow)
    {
        IntPtr pWindowHandle = new WindowInteropHelper(pWindow).Handle;
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
