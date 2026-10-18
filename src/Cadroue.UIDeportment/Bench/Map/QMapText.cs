namespace Cadroue.UIDeportment;

public static class QMapText
{
    private const double QMapBadgeHorizontal = 6;
    private const double QMapBadgeVertical = 1;
    private const double QMapBadgeMargin = 2;

    public static IReadOnlyList<QMapBadge> QMapBadgeResolve(QMapBand lBand, double lTextWidth, double lTextHeight)
    {
        double lBandWidth = lBand.QMapBandRight - lBand.QMapBandLeft;
        double lBandHeight = lBand.QMapBandBottom - lBand.QMapBandTop;
        double lBadgeHeight = lTextHeight + QMapBadgeVertical * 2;
        double lBadgeWidth = Math.Max(lBadgeHeight, lTextWidth + QMapBadgeHorizontal * 2);
        if (lBadgeWidth > lBandWidth - QMapBadgeMargin * 2 || lBadgeHeight > lBandHeight - QMapBadgeMargin * 2)
        {
            return [];
        }

        double lLeft = lBand.QMapBandLeft + (lBandWidth - lBadgeWidth) / 2;
        double lTop = lBand.QMapBandTop + (lBandHeight - lBadgeHeight) / 2;
        return
        [
            new QMapBadge(
                lLeft,
                lTop,
                lLeft + lBadgeWidth,
                lTop + lBadgeHeight,
                lBadgeHeight / 2,
                lLeft + (lBadgeWidth - lTextWidth) / 2,
                lTop + QMapBadgeVertical)
        ];
    }
}
