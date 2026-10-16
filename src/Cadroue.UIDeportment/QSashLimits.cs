namespace Cadroue.UIDeportment;

// The maximized position, relative to the monitor, and size, in device pixels.
public readonly record struct QSashLimits(double QSashX, double QSashY, double QSashWidth, double QSashHeight)
{
    public static QSashLimits QSashLimitsResolve(
        double lBoundsLeft,
        double lBoundsTop,
        double lWorkLeft,
        double lWorkTop,
        double lWorkRight,
        double lWorkBottom) =>
        new(lWorkLeft - lBoundsLeft, lWorkTop - lBoundsTop, lWorkRight - lWorkLeft, lWorkBottom - lWorkTop);
}
