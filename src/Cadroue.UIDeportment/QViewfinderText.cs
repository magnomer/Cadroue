namespace Cadroue.UIDeportment;

public sealed record QViewfinderPoint(double QViewfinderPointX, double QViewfinderPointY);

public sealed record QViewfinderLabel(string QViewfinderLabelName, double QViewfinderLabelRoom);

public sealed record QViewfinderBadge(
    double QViewfinderBadgeLeft,
    double QViewfinderBadgeTop,
    double QViewfinderBadgeRight,
    double QViewfinderBadgeBottom,
    double QViewfinderBadgeRadius,
    QViewfinderPoint QViewfinderBadgeText,
    QViewfinderPoint QViewfinderBadgeName);

public sealed record QViewfinderChip(
    double QViewfinderChipLeft,
    double QViewfinderChipTop,
    double QViewfinderChipRight,
    double QViewfinderChipBottom,
    QViewfinderPoint QViewfinderChipText);

public static class QViewfinderText
{
    private const double QViewfinderChipHorizontal = 4;
    private const double QViewfinderChipVertical = 2;
    private const double QViewfinderSectionPadding = 5;
    private const double QViewfinderSectionLeast = 18;
    private const double QViewfinderHeightLeast = 16;
    private const double QViewfinderBadgeHorizontal = 6;
    private const double QViewfinderBadgeVertical = 1;
    private const double QViewfinderBadgeGap = 6;

    public static QViewfinderPoint QViewfinderTickResolve(QViewfinderGraduation lTick, double lTextHeight) =>
        new(lTick.QViewfinderTickX + 2, QViewfinder.QViewfinderLaneHeight * 0.5 - lTextHeight / 2);

    public static IReadOnlyList<QViewfinderLabel> QViewfinderLabelResolve(
        QViewfinderBand lBand, double lBadgeTextWidth, double lBadgeTextHeight)
    {
        double lBandWidth = lBand.QViewfinderBandRight - lBand.QViewfinderBandLeft;
        double lBandHeight = lBand.QViewfinderBandBottom - lBand.QViewfinderBandTop;
        double lLabelRoom = lBandWidth - QViewfinderSectionPadding * 2;
        if (lLabelRoom <= 0 || lBandHeight < QViewfinderHeightLeast)
        {
            return [];
        }

        double lBadgeHeight = lBadgeTextHeight + QViewfinderBadgeVertical * 2;
        double lBadgeWidth = Math.Max(lBadgeHeight, lBadgeTextWidth + QViewfinderBadgeHorizontal * 2);
        if (lBadgeWidth > lLabelRoom || lBadgeHeight > lBandHeight - 2)
        {
            return [];
        }

        double lNameRoom = lLabelRoom - lBadgeWidth - QViewfinderBadgeGap;
        bool lNamed = !string.IsNullOrEmpty(lBand.QViewfinderBandName) && lNameRoom >= QViewfinderSectionLeast;
        string lName = lNamed ? lBand.QViewfinderBandName : string.Empty;
        return [new QViewfinderLabel(lName, Math.Max(1, Math.Round(lNameRoom)))];
    }

    public static QViewfinderBadge QViewfinderBadgeResolve(
        QViewfinderBand lBand,
        QViewfinderLabel lLabel,
        double lBadgeTextWidth,
        double lBadgeTextHeight,
        double lNameWidth,
        double lNameHeight)
    {
        double lBandWidth = lBand.QViewfinderBandRight - lBand.QViewfinderBandLeft;
        double lBandHeight = lBand.QViewfinderBandBottom - lBand.QViewfinderBandTop;
        double lBadgeHeight = lBadgeTextHeight + QViewfinderBadgeVertical * 2;
        double lBadgeWidth = Math.Max(lBadgeHeight, lBadgeTextWidth + QViewfinderBadgeHorizontal * 2);
        double lLabelWidth = string.IsNullOrEmpty(lLabel.QViewfinderLabelName)
            ? lBadgeWidth
            : lBadgeWidth + QViewfinderBadgeGap + lNameWidth;
        double lLeft = lBand.QViewfinderBandLeft + (lBandWidth - lLabelWidth) / 2;
        double lTop = lBand.QViewfinderBandTop + (lBandHeight - lBadgeHeight) / 2;
        return new QViewfinderBadge(
            lLeft,
            lTop,
            lLeft + lBadgeWidth,
            lTop + lBadgeHeight,
            lBadgeHeight / 2,
            new QViewfinderPoint(lLeft + (lBadgeWidth - lBadgeTextWidth) / 2, lTop + QViewfinderBadgeVertical),
            new QViewfinderPoint(
                lLeft + lBadgeWidth + QViewfinderBadgeGap,
                lBand.QViewfinderBandTop + (lBandHeight - lNameHeight) / 2));
    }

    public static QViewfinderChip QViewfinderChipResolve(
        QViewfinderCursor lCursor, double lTextWidth, double lTextHeight, double lWidth, double lHeight)
    {
        double lChipWidth = Math.Min(lTextWidth + QViewfinderChipHorizontal * 2, lWidth);
        double lChipHeight = lTextHeight + QViewfinderChipVertical * 2;
        (double lLeft, double lTop) = QCursor.QCursorChipResolve(
            lCursor.QViewfinderCursorX, lChipWidth, lChipHeight, QViewfinder.QViewfinderLaneHeight, lHeight, lWidth);
        return new QViewfinderChip(
            lLeft,
            lTop,
            lLeft + lChipWidth,
            lTop + lChipHeight,
            new QViewfinderPoint(lLeft + QViewfinderChipHorizontal, lTop + QViewfinderChipVertical));
    }
}
