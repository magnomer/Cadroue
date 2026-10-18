using System.Windows;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSGrabberPlacement
{
    internal static void PSGrabberPlacementRestore(Window pWindow, string pPlacementKey)
    {
        bool pStored = Cadroue.UIDeportment.QPlacement.QPlacementRead(
            pPlacementKey, out double pLeftStored, out double pTopStored,
            out double pWidthStored, out double pHeightStored);
        (double? pLeft, double? pTop, double pWidth, double pHeight) = PSGrabberStoredResolve(
            pWindow, pStored, pLeftStored, pTopStored, pWidthStored, pHeightStored);
        QSashMaximize.QSashMaximizeAttach(pWindow);
        QSashPlacement.QSashPlacementRestore(pWindow, pLeft, pTop, pWidth, pHeight);
    }

    private static (double?, double?, double, double) PSGrabberStoredResolve(
        Window pWindow,
        bool pStored,
        double pLeft,
        double pTop,
        double pWidthStored,
        double pHeightStored)
    {
        double pWidth = Math.Max(pStored ? pWidthStored : pWindow.Width, pWindow.MinWidth);
        double pHeight = Math.Max(pStored ? pHeightStored : pWindow.Height, pWindow.MinHeight);
        return (pStored ? pLeft : null, pStored ? pTop : null, pWidth, pHeight);
    }

    internal static void PSGrabberPlacementSave(Window pWindow, string pPlacementKey)
    {
        Rect pBounds = PSGrabberRectResolve(pWindow);
        Cadroue.UIDeportment.QPlacement.QPlacementSave(
            pPlacementKey,
            pBounds.Left,
            pBounds.Top,
            pBounds.Width,
            pBounds.Height);
    }

    private static Rect PSGrabberRectResolve(Window pWindow) =>
        pWindow.WindowState == WindowState.Normal
            ? new Rect(pWindow.Left, pWindow.Top, pWindow.Width, pWindow.Height)
            : pWindow.RestoreBounds;
}
