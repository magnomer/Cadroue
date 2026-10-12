namespace Cadroue.UIDeportment;

public sealed record QCursorLine(double QCursorLineTop, double QCursorLineBottom);

public static class QCursor
{
    public const double QCursorVolumeStep = 5;
    public const double QCursorChipGap = 2;
    private const int QCursorWheelNotch = 120;

    public static string QCursorTimeFormat(TimeSpan lTime) => lTime.TotalHours >= 1
        ? $"{(int)lTime.TotalHours}:{lTime.Minutes:D2}:{lTime.Seconds:D2}"
        : $"{lTime.Minutes}:{lTime.Seconds:D2}";

    public static int QCursorWheelResolve(int lDelta)
    {
        int lSteps = lDelta / QCursorWheelNotch;
        if (lSteps != 0)
        {
            return lSteps;
        }

        return lDelta > 0 ? 1 : -1;
    }

    public static (double QCursorLeft, double QCursorRight) QCursorGuideResolve(double lCursorX, double lThickness) =>
        (lCursorX - lThickness / 2, lCursorX + lThickness / 2);

    public static (double QCursorLeft, double QCursorTop) QCursorChipResolve(
        double lCursorX,
        double lChipWidth,
        double lChipHeight,
        double lLineTop,
        double lLineBottom,
        double lActualWidth) =>
        (Math.Clamp(lCursorX - lChipWidth / 2, 0, Math.Max(0, lActualWidth - lChipWidth)),
            (lLineTop + lLineBottom) / 2 - lChipHeight / 2);

    public static IReadOnlyList<QCursorLine> QCursorLinesResolve(
        double lLineTop,
        double lLineBottom,
        bool lChipEmpty,
        double lChipTop,
        double lChipBottom,
        double lChipHeight)
    {
        var lLines = new List<QCursorLine>(2);
        if (lChipEmpty || lChipHeight <= 0)
        {
            QCursorLineAdd(lLines, lLineTop, lLineBottom);
            return lLines;
        }

        QCursorLineAdd(lLines, lLineTop, lChipTop - QCursorChipGap);
        QCursorLineAdd(lLines, lChipBottom + QCursorChipGap, lLineBottom);
        return lLines;
    }

    private static void QCursorLineAdd(List<QCursorLine> lLines, double lTop, double lBottom)
    {
        if (lBottom > lTop)
        {
            lLines.Add(new QCursorLine(lTop, lBottom));
        }
    }
}
