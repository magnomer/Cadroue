using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Cadroue.UIVeneer.PHouse;

public partial class PWindow
{
    private const int PWindowMessageErase = 0x0014;
    private const int PWindowCornerPreference = 33;
    private const int PWindowCornerRound = 2;
    private const int PWindowCaptionColor = 35;
    private const int PWindowColorBackground = 0x00F7E8DC;
    private const int PWindowMessageIcon = 0x0080;
    private const int PWindowIconSmall = 0;
    private const int PWindowIconBig = 1;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        HwndSource? pWindowSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        pWindowSource?.AddHook(PWindowMessageHook);
        ComponentDispatcher.ThreadPreprocessMessage += PShortcutMessageHandle;
        PWindowDwmApply();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        PWindowIconApply();
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ApplicationIdle,
            new Action(PWindowIconApply));
    }

    private void PWindowIconApply()
    {
        IntPtr pWindowHandle = new WindowInteropHelper(this).Handle;
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
