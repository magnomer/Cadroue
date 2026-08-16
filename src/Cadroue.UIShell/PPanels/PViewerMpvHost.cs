using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Cadroue.UIShell.PPanels;

internal sealed class PViewerMpvHost : HwndHost
{
    private const int PViewerMpvStyle = 0x40000000 | 0x10000000;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        int lExStyle,
        string lClassName,
        string? lWindowName,
        int lStyle,
        int lX,
        int lY,
        int lWidth,
        int lHeight,
        nint lParent,
        nint lMenu,
        nint lInstance,
        nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint lWindow);

    public nint PViewerMpvHwnd { get; private set; }

    protected override HandleRef BuildWindowCore(HandleRef lParent)
    {
        PViewerMpvHwnd = CreateWindowExW(
            0,
            "static",
            null,
            PViewerMpvStyle,
            0,
            0,
            0,
            0,
            lParent.Handle,
            nint.Zero,
            nint.Zero,
            nint.Zero);
        return new HandleRef(this, PViewerMpvHwnd);
    }

    protected override void DestroyWindowCore(HandleRef lWindow)
    {
        if (lWindow.Handle != nint.Zero)
        {
            DestroyWindow(lWindow.Handle);
        }

        PViewerMpvHwnd = nint.Zero;
    }
}
