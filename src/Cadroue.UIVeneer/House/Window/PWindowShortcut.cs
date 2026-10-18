using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PWindowShortcut(Window pShortcutWindow, PStrip pShortcutStrip, QWindowShortcut lShortcut)
{
    private static readonly Type[] PShortcutInputTypes = [typeof(TextBoxBase), typeof(PasswordBox)];

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int virtualKey);

    public void PShortcutAttach() => ComponentDispatcher.ThreadPreprocessMessage += PShortcutMessageHandle;

    public void PShortcutDetach() => ComponentDispatcher.ThreadPreprocessMessage -= PShortcutMessageHandle;

    private void PShortcutMessageHandle(ref MSG pShortcutMessage, ref bool pShortcutHandled)
    {
        pShortcutHandled = lShortcut.QWindowShortcutHandle(new QWindowPress(
            pShortcutHandled,
            pShortcutMessage.message,
            (int)pShortcutMessage.wParam,
            ComponentDispatcher.IsThreadModal,
            new WindowInteropHelper(pShortcutWindow).Handle,
            GetForegroundWindow,
            PShortcutViewerMatch,
            PShortcutInputCheck,
            QShortcutKey.QShortcutKeyFormat,
            GetKeyState));
    }

    private bool? PShortcutViewerMatch(nint pForeground) =>
        pShortcutStrip.PStripSelected?.PTabViewer?.PViewerSurfaceMatch(pForeground);

    private static bool PShortcutInputCheck() =>
        QWalk.QWalkParentCheck(Keyboard.FocusedElement as DependencyObject, PShortcutInputMatch);

    private static bool PShortcutInputMatch(DependencyObject pNode) =>
        QWalkType.QWalkTypeMatch(pNode, PShortcutInputTypes);
}
