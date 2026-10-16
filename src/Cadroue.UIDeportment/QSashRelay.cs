using System.Windows;

namespace Cadroue.UIDeportment;

// A relayed window placed near the drop point, kept far enough inside the screen to stay grabbable.
public static class QSashRelay
{
    // The drop point's offset from the window corner, and the window part kept on the screen.
    private static readonly Vector QSashRelayOffset = new(120, 16);
    private static readonly Vector QSashRelayMargin = new(200, 120);

    public static (double QSashLeft, double QSashTop) QSashRelayResolve(
        double lDropLeft,
        double lDropTop,
        double lScreenLeft,
        double lScreenTop,
        double lScreenWidth,
        double lScreenHeight) =>
        (
            Math.Clamp(
                lDropLeft - QSashRelayOffset.X, lScreenLeft, lScreenLeft + lScreenWidth - QSashRelayMargin.X),
            Math.Clamp(
                lDropTop - QSashRelayOffset.Y, lScreenTop, lScreenTop + lScreenHeight - QSashRelayMargin.Y));
}
