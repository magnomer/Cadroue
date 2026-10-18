using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSCasementWindow
{
    internal static void PSCasementEscapeAttach(Window pWindow) =>
        pWindow.KeyDown += (_, e) => PSCasementEscapeHandle(pWindow, e);

    private static void PSCasementEscapeHandle(Window pWindow, KeyEventArgs e)
    {
        if (!PSCasementEscapeAccept(e))
        {
            return;
        }

        pWindow.Close();
    }

    private static bool PSCasementEscapeAccept(KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return false;
        }

        e.Handled = true;
        return true;
    }

    internal static void PSCasementDragAttach(Border pArea, Window pWindow, bool pCloseOnly, double pBandHeight) =>
        pArea.MouseLeftButtonDown += (_, e) => PSCasementDragHandle(pWindow, e, pCloseOnly, pBandHeight);

    internal static void PSCasementMinimizeApply(Window pWindow) => pWindow.WindowState = WindowState.Minimized;

    private static void PSCasementDragHandle(
        Window pWindow,
        MouseButtonEventArgs e,
        bool pCloseOnly,
        double pBandHeight)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (e.ClickCount > 1)
        {
            if (pCloseOnly)
            {
                return;
            }

            PSCasementMaximizeToggle(pWindow);
            return;
        }

        QSashDrag.QSashDragMove(pWindow, e, pBandHeight);
    }

    internal static void PSCasementMaximizeToggle(Window pWindow) =>
        pWindow.WindowState = PSCasementStateResolve(pWindow.WindowState);

    private static WindowState PSCasementStateResolve(WindowState pState) =>
        pState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
}
