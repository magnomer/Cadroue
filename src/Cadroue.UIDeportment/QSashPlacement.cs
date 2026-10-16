using System.Windows;

namespace Cadroue.UIDeportment;

// A window's stored placement restored inside the work area of its monitor, once the window has a handle.
public static class QSashPlacement
{
    public static void QSashPlacementRestore(
        Window lWindow,
        double? lLeft,
        double? lTop,
        double lWidth,
        double lHeight) =>
        QSashSource.QSashSourceRun(lWindow, lTarget => QSashPlacementApply(lTarget, lLeft, lTop, lWidth, lHeight));

    // A stored value that is missing or not finite gives way to the window's own, then to the work area corner.
    public static QSashBounds QSashPlacementResolve(
        double? lSavedLeft,
        double? lSavedTop,
        double lSavedWidth,
        double lSavedHeight,
        QSashBounds lCurrent,
        double lMinimumWidth,
        double lMinimumHeight,
        double lWorkLeft,
        double lWorkTop)
    {
        double lLeft = lSavedLeft is double lFiniteLeft && double.IsFinite(lFiniteLeft)
            ? lFiniteLeft
            : double.IsFinite(lCurrent.QSashLeft) ? lCurrent.QSashLeft : lWorkLeft;
        double lTop = lSavedTop is double lFiniteTop && double.IsFinite(lFiniteTop)
            ? lFiniteTop
            : double.IsFinite(lCurrent.QSashTop) ? lCurrent.QSashTop : lWorkTop;
        double lWidth = double.IsFinite(lSavedWidth) && lSavedWidth > 0
            ? lSavedWidth
            : Math.Max(lCurrent.QSashWidth, lMinimumWidth);
        double lHeight = double.IsFinite(lSavedHeight) && lSavedHeight > 0
            ? lSavedHeight
            : Math.Max(lCurrent.QSashHeight, lMinimumHeight);
        return new QSashBounds(lLeft, lTop, lWidth, lHeight);
    }

    private static void QSashPlacementApply(
        Window lWindow,
        double? lLeft,
        double? lTop,
        double lWidth,
        double lHeight)
    {
        QSashBounds lDesired = QSashPlacementResolve(
            lLeft,
            lTop,
            lWidth,
            lHeight,
            QSashWindow.QSashWindowRead(lWindow),
            lWindow.MinWidth,
            lWindow.MinHeight,
            SystemParameters.WorkArea.Left,
            SystemParameters.WorkArea.Top);
        QSashBounds lBounds = QSashBounds.QSashBoundsClamp(
            lDesired,
            QSashScreen.QSashScreenRead(lWindow, lDesired),
            lWindow.MinWidth,
            lWindow.MinHeight,
            lWindow.MaxWidth,
            lWindow.MaxHeight);

        lWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        lWindow.Width = lBounds.QSashWidth;
        lWindow.Height = lBounds.QSashHeight;
        lWindow.Left = lBounds.QSashLeft;
        lWindow.Top = lBounds.QSashTop;
    }
}
