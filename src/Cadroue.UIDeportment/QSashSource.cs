using System.Windows;
using System.Windows.Interop;

namespace Cadroue.UIDeportment;

// A step that needs the window's handle: run now when the handle exists, otherwise once the source is initialized.
internal static class QSashSource
{
    internal static void QSashSourceRun(Window lWindow, Action<Window> lStep)
    {
        if (new WindowInteropHelper(lWindow).Handle == IntPtr.Zero)
        {
            QSashSourceDefer(lWindow, lStep);
            return;
        }

        lStep(lWindow);
    }

    private static void QSashSourceDefer(Window lWindow, Action<Window> lStep)
    {
        EventHandler? lSourceHandle = null;
        lSourceHandle = (_, _) =>
        {
            lWindow.SourceInitialized -= lSourceHandle;
            lStep(lWindow);
        };
        lWindow.SourceInitialized += lSourceHandle;
    }
}
