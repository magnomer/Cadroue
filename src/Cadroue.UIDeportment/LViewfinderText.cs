namespace Cadroue.UIDeportment;

public sealed record LViewfinderLabel(string LViewfinderLabelName, double LViewfinderLabelRoom);

public sealed record LViewfinderBadge(
    double LViewfinderBadgeLeft,
    double LViewfinderBadgeTop,
    double LViewfinderBadgeRight,
    double LViewfinderBadgeBottom,
    double LViewfinderBadgeRadius,
    LViewfinderPoint LViewfinderBadgeText,
    LViewfinderPoint LViewfinderBadgeName);

public sealed record LViewfinderChip(
    double LViewfinderChipLeft,
    double LViewfinderChipTop,
    double LViewfinderChipRight,
    double LViewfinderChipBottom,
    LViewfinderPoint LViewfinderChipText);

public static class LViewfinderText
{
    private const double LViewfinderChipHorizontal = 4;
    private const double LViewfinderChipVertical = 2;
    private const double LViewfinderSectionPadding = 5;
    private const double LViewfinderSectionLeast = 18;
    private const double LViewfinderHeightLeast = 16;
    private const double LViewfinderBadgeHorizontal = 6;
    private const double LViewfinderBadgeVertical = 1;
    private const double LViewfinderBadgeGap = 6;

    public static LViewfinderPoint LViewfinderTickResolve(LViewfinderTick lTick, double lTextHeight) =>
        new(lTick.LViewfinderTickX + 2, LViewfinder.LViewfinderLaneHeight * 0.5 - lTextHeight / 2);

    public static IReadOnlyList<LViewfinderLabel> LViewfinderLabelResolve(
        LViewfinderBand lBand, double lBadgeTextWidth, double lBadgeTextHeight)
    {
        double lBandWidth = lBand.LViewfinderBandRight - lBand.LViewfinderBandLeft;
        double lBandHeight = lBand.LViewfinderBandBottom - lBand.LViewfinderBandTop;
        double lLabelRoom = lBandWidth - LViewfinderSectionPadding * 2;
        if (lLabelRoom <= 0 || lBandHeight < LViewfinderHeightLeast)
        {
            return [];
        }

        double lBadgeHeight = lBadgeTextHeight + LViewfinderBadgeVertical * 2;
        double lBadgeWidth = Math.Max(lBadgeHeight, lBadgeTextWidth + LViewfinderBadgeHorizontal * 2);
        if (lBadgeWidth > lLabelRoom || lBadgeHeight > lBandHeight - 2)
        {
            return [];
        }

        double lNameRoom = lLabelRoom - lBadgeWidth - LViewfinderBadgeGap;
        bool lNamed = !string.IsNullOrEmpty(lBand.LViewfinderBandName) && lNameRoom >= LViewfinderSectionLeast;
        string lName = lNamed ? lBand.LViewfinderBandName : string.Empty;
        return [new LViewfinderLabel(lName, Math.Max(1, Math.Round(lNameRoom)))];
    }

    public static LViewfinderBadge LViewfinderBadgeResolve(
        LViewfinderBand lBand,
        LViewfinderLabel lLabel,
        double lBadgeTextWidth,
        double lBadgeTextHeight,
        double lNameWidth,
        double lNameHeight)
    {
        double lBandWidth = lBand.LViewfinderBandRight - lBand.LViewfinderBandLeft;
        double lBandHeight = lBand.LViewfinderBandBottom - lBand.LViewfinderBandTop;
        double lBadgeHeight = lBadgeTextHeight + LViewfinderBadgeVertical * 2;
        double lBadgeWidth = Math.Max(lBadgeHeight, lBadgeTextWidth + LViewfinderBadgeHorizontal * 2);
        double lLabelWidth = string.IsNullOrEmpty(lLabel.LViewfinderLabelName)
            ? lBadgeWidth
            : lBadgeWidth + LViewfinderBadgeGap + lNameWidth;
        double lLeft = lBand.LViewfinderBandLeft + (lBandWidth - lLabelWidth) / 2;
        double lTop = lBand.LViewfinderBandTop + (lBandHeight - lBadgeHeight) / 2;
        return new LViewfinderBadge(
            lLeft,
            lTop,
            lLeft + lBadgeWidth,
            lTop + lBadgeHeight,
            lBadgeHeight / 2,
            new LViewfinderPoint(lLeft + (lBadgeWidth - lBadgeTextWidth) / 2, lTop + LViewfinderBadgeVertical),
            new LViewfinderPoint(
                lLeft + lBadgeWidth + LViewfinderBadgeGap,
                lBand.LViewfinderBandTop + (lBandHeight - lNameHeight) / 2));
    }

    public static LViewfinderChip LViewfinderChipResolve(
        LViewfinderCursor lCursor, double lTextWidth, double lTextHeight, double lWidth, double lHeight)
    {
        double lChipWidth = Math.Min(lTextWidth + LViewfinderChipHorizontal * 2, lWidth);
        double lChipHeight = lTextHeight + LViewfinderChipVertical * 2;
        (double lLeft, double lTop) = LCursor.LCursorChipResolve(
            lCursor.LViewfinderCursorX, lChipWidth, lChipHeight, LViewfinder.LViewfinderLaneHeight, lHeight, lWidth);
        return new LViewfinderChip(
            lLeft,
            lTop,
            lLeft + lChipWidth,
            lTop + lChipHeight,
            new LViewfinderPoint(lLeft + LViewfinderChipHorizontal, lTop + LViewfinderChipVertical));
    }
}
