namespace Cadroue.UIDeportment;

public static class QMapBadge
{
    private const double LMapBadgeHorizontal = 6;
    private const double LMapBadgeVertical = 1;
    private const double LMapBadgeMargin = 2;

    public static IReadOnlyList<LMapBadge> LMapBadgeResolve(LMapBand lBand, double lTextWidth, double lTextHeight)
    {
        double lBandWidth = lBand.LMapBandRight - lBand.LMapBandLeft;
        double lBandHeight = lBand.LMapBandBottom - lBand.LMapBandTop;
        double lBadgeHeight = lTextHeight + LMapBadgeVertical * 2;
        double lBadgeWidth = Math.Max(lBadgeHeight, lTextWidth + LMapBadgeHorizontal * 2);
        if (lBadgeWidth > lBandWidth - LMapBadgeMargin * 2 || lBadgeHeight > lBandHeight - LMapBadgeMargin * 2)
        {
            return [];
        }

        double lLeft = lBand.LMapBandLeft + (lBandWidth - lBadgeWidth) / 2;
        double lTop = lBand.LMapBandTop + (lBandHeight - lBadgeHeight) / 2;
        return
        [
            new LMapBadge(
                lLeft,
                lTop,
                lLeft + lBadgeWidth,
                lTop + lBadgeHeight,
                lBadgeHeight / 2,
                lLeft + (lBadgeWidth - lTextWidth) / 2,
                lTop + LMapBadgeVertical)
        ];
    }
}
