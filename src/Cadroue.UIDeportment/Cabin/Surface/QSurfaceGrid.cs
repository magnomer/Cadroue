namespace Cadroue.UIDeportment;

// The tab surface's grid tracks: panels sit on even columns with a splitter gutter on each odd one between them,
// and each track's width follows its panel's minimum and collapse.
public static class QSurfaceGrid
{
    public static IReadOnlyList<int> QSurfaceIndexesResolve(int lCount) => Enumerable.Range(0, lCount).ToArray();

    public static IReadOnlyList<int> QSurfaceSplittersResolve(int lCount) =>
        Enumerable.Range(0, Math.Max(0, lCount - 1)).ToArray();

    public static int QSurfaceColumnResolve(int lIndex) => lIndex * 2;

    public static int QSurfaceGutterResolve(int lIndex) => lIndex * 2 + 1;

    public static int QSurfaceLeftResolve(int lExportIndex) => lExportIndex - 1;

    public static double QSurfaceMinimumResolve(double lElementMinimum, double lTableMinimum) =>
        lElementMinimum > 0 ? lElementMinimum : lTableMinimum;

    public static double QSurfaceCollapseResolve(bool lCollapsed, double lStripWidth) => lCollapsed ? lStripWidth : 0;
}
