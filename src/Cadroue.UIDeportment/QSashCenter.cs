namespace Cadroue.UIDeportment;

// A window centred on its owner, unless the window is placed by hand.
public static class QSashCenter
{
    public static (double QSashLeft, double QSashTop) QSashCenterResolve(
        bool lManual,
        QSashBounds lWindow,
        QSashBounds lOwner) =>
        lManual
            ? (lWindow.QSashLeft, lWindow.QSashTop)
            : (
                lOwner.QSashLeft + ((lOwner.QSashWidth - lWindow.QSashWidth) / 2),
                lOwner.QSashTop + ((lOwner.QSashHeight - lWindow.QSashHeight) / 2));
}
