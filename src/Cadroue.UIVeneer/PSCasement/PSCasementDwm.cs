using System.Windows;

namespace Cadroue.UIVeneer;

internal static class PSCasementDwm
{
    private const int PSCasementDwmPreference = 33;
    private const int PSCasementDwmRound = 2;
    private const int PSCasementDwmCaption = 35;

    internal static void PSCasementDwmApply(Window pWindow, int pCaption)
    {
        IntPtr pCasementHandle = new System.Windows.Interop.WindowInteropHelper(pWindow).Handle;
        if (pCasementHandle == IntPtr.Zero)
        {
            return;
        }

        int pCasementCorner = PSCasementDwmRound;
        _ = DwmSetWindowAttribute(
            pCasementHandle,
            PSCasementDwmPreference,
            ref pCasementCorner,
            System.Runtime.InteropServices.Marshal.SizeOf<int>());

        int pCasementCaption = pCaption;
        _ = DwmSetWindowAttribute(
            pCasementHandle,
            PSCasementDwmCaption,
            ref pCasementCaption,
            System.Runtime.InteropServices.Marshal.SizeOf<int>());
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int windowAttribute,
        ref int attributeValue,
        int attributeByteSize);
}
