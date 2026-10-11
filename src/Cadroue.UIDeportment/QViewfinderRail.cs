namespace Cadroue.UIDeportment;

public static class QViewfinderRail
{
    private const double LViewfinderRailGap = 2;

    internal static (double, double) LViewfinderRailResolve(double lHeight)
    {
        double lRailTop = LViewfinder.LViewfinderLaneHeight + LViewfinderRailGap;
        double lCoverageTop = Math.Max(0, Math.Max(0, lHeight - 1) - LViewfinder.LViewfinderCoverageHeight);
        return (lRailTop, Math.Max(lRailTop, lCoverageTop - LViewfinderRailGap));
    }
}
