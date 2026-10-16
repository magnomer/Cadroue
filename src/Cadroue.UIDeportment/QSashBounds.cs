namespace Cadroue.UIDeportment;

// A window's bounds in device-independent pixels, and their clamp to a work area.
public readonly record struct QSashBounds(double QSashLeft, double QSashTop, double QSashWidth, double QSashHeight)
{
    public double QSashRight => QSashLeft + QSashWidth;

    public double QSashBottom => QSashTop + QSashHeight;

    public static QSashBounds QSashCornersCreate(double lLeft, double lTop, double lRight, double lBottom) =>
        new(lLeft, lTop, lRight - lLeft, lBottom - lTop);

    public static QSashBounds QSashBoundsClamp(
        QSashBounds lBounds,
        QSashBounds lWorkArea,
        double lMinimumWidth,
        double lMinimumHeight,
        double lMaximumWidth,
        double lMaximumHeight)
    {
        double lWidthLimit = double.IsFinite(lMaximumWidth)
            ? Math.Min(lMaximumWidth, lWorkArea.QSashWidth)
            : lWorkArea.QSashWidth;
        double lHeightLimit = double.IsFinite(lMaximumHeight)
            ? Math.Min(lMaximumHeight, lWorkArea.QSashHeight)
            : lWorkArea.QSashHeight;
        double lWidth = Math.Min(Math.Max(lBounds.QSashWidth, lMinimumWidth), lWidthLimit);
        double lHeight = Math.Min(Math.Max(lBounds.QSashHeight, lMinimumHeight), lHeightLimit);
        double lLeft = Math.Clamp(lBounds.QSashLeft, lWorkArea.QSashLeft, lWorkArea.QSashRight - lWidth);
        double lTop = Math.Clamp(lBounds.QSashTop, lWorkArea.QSashTop, lWorkArea.QSashBottom - lHeight);
        return new QSashBounds(lLeft, lTop, lWidth, lHeight);
    }
}
