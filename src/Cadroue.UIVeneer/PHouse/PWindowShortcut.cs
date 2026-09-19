using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PHouse;

public partial class PWindow
{
    private static readonly Type[] PShortcutInputTypes = [typeof(TextBoxBase), typeof(PasswordBox)];

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int virtualKey);

    private void PShortcutMessageHandle(ref MSG pShortcutMessage, ref bool pShortcutHandled)
    {
        pShortcutHandled = lWindow.LWindowShortcut.LWindowShortcutHandle(new LWindowPress(
            pShortcutHandled,
            pShortcutMessage.message,
            (int)pShortcutMessage.wParam,
            ComponentDispatcher.IsThreadModal,
            new WindowInteropHelper(this).Handle,
            GetForegroundWindow,
            PShortcutViewerMatch,
            PShortcutInputCheck,
            PShortcutKeyRead,
            GetKeyState));
    }

    private bool? PShortcutViewerMatch(nint pForeground) =>
        pStrip.PStripSelected?.PWorkspaceViewer?.PViewerSurfaceMatch(pForeground);

    private static bool PShortcutInputCheck() =>
        PWalk.PWalkParentCheck(Keyboard.FocusedElement as DependencyObject, PShortcutInputMatch);

    private static string PShortcutKeyRead(int pVirtualKey) =>
        PShortcut.PShortcutKeyFormat(KeyInterop.KeyFromVirtualKey(pVirtualKey));

    private static bool PShortcutInputMatch(DependencyObject pNode) => PWalk.PWalkTypeCheck(pNode, PShortcutInputTypes);
}
