namespace Cadroue.UIDeportment;

public static class QViewfinderRail
{
    private const double QViewfinderRailGap = 2;

    internal static (double, double) LViewfinderRailResolve(double lHeight)
    {
        double lRailTop = QViewfinder.QViewfinderLaneHeight + QViewfinderRailGap;
        double lCoverageTop = Math.Max(0, Math.Max(0, lHeight - 1) - QViewfinder.QViewfinderCoverageHeight);
        return (lRailTop, Math.Max(lRailTop, lCoverageTop - QViewfinderRailGap));
    }
}
