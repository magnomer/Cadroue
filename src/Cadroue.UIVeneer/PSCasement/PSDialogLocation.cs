using System.Windows;

namespace Cadroue.UIVeneer;

internal static class PSDialogLocation
{
    private const string PSDialogPlacementKey = "PSDialog";

    internal static void PSDialogLocationAttach(Window pWindow)
    {
        PSDialogLocationRestore(pWindow);
        PSDialogClosedAttach(pWindow);
    }

    private static void PSDialogClosedAttach(Window pWindow) =>
        pWindow.Closed += (_, _) => PSDialogLocationSave(pWindow);

    private static void PSDialogLocationRestore(Window pWindow)
    {
        if (!Cadroue.UIDeportment.QPlacement.QPlacementRead(
                PSDialogPlacementKey, out double pLeft, out double pTop, out _, out _))
        {
            return;
        }

        PSDialogLocationPlace(pWindow, pLeft, pTop);
    }

    private static void PSDialogLocationPlace(Window pWindow, double pLeft, double pTop)
    {
        pWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        pWindow.Left = pLeft;
        pWindow.Top = pTop;
    }

    private static void PSDialogLocationSave(Window pWindow)
    {
        Rect pBounds = PSDialogBoundsResolve(pWindow);
        Cadroue.UIDeportment.QPlacement.QPlacementSave(
            PSDialogPlacementKey, pBounds.Left, pBounds.Top, pBounds.Width, pBounds.Height);
    }

    private static Rect PSDialogBoundsResolve(Window pWindow) =>
        pWindow.WindowState == WindowState.Normal
            ? new Rect(pWindow.Left, pWindow.Top, pWindow.ActualWidth, pWindow.ActualHeight)
            : pWindow.RestoreBounds;
}
