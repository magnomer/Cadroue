namespace Cadroue.UIDeportment;

public sealed record LCursorLine(double LCursorLineTop, double LCursorLineBottom);

public static class LCursor
{
    public const double LCursorVolumeStep = 5;
    public const double LCursorChipGap = 2;
    private const int LCursorWheelNotch = 120;

    public static string LCursorTimeFormat(TimeSpan lTime) => lTime.TotalHours >= 1
        ? $"{(int)lTime.TotalHours}:{lTime.Minutes:D2}:{lTime.Seconds:D2}"
        : $"{lTime.Minutes}:{lTime.Seconds:D2}";

    public static int LCursorWheelResolve(int lDelta)
    {
        int lSteps = lDelta / LCursorWheelNotch;
        if (lSteps != 0)
        {
            return lSteps;
        }

        return lDelta > 0 ? 1 : -1;
    }

    public static (double LCursorLeft, double LCursorRight) LCursorGuideResolve(double lCursorX, double lThickness) =>
        (lCursorX - lThickness / 2, lCursorX + lThickness / 2);

    public static (double LCursorLeft, double LCursorTop) LCursorChipResolve(
        double lCursorX,
        double lChipWidth,
        double lChipHeight,
        double lLineTop,
        double lLineBottom,
        double lActualWidth) =>
        (Math.Clamp(lCursorX - lChipWidth / 2, 0, Math.Max(0, lActualWidth - lChipWidth)),
            (lLineTop + lLineBottom) / 2 - lChipHeight / 2);

    public static IReadOnlyList<LCursorLine> LCursorLinesResolve(
        double lLineTop,
        double lLineBottom,
        bool lChipEmpty,
        double lChipTop,
        double lChipBottom,
        double lChipHeight)
    {
        var lLines = new List<LCursorLine>(2);
        if (lChipEmpty || lChipHeight <= 0)
        {
            LCursorLineAdd(lLines, lLineTop, lLineBottom);
            return lLines;
        }

        LCursorLineAdd(lLines, lLineTop, lChipTop - LCursorChipGap);
        LCursorLineAdd(lLines, lChipBottom + LCursorChipGap, lLineBottom);
        return lLines;
    }

    private static void LCursorLineAdd(List<LCursorLine> lLines, double lTop, double lBottom)
    {
        if (lBottom > lTop)
        {
            lLines.Add(new LCursorLine(lTop, lBottom));
        }
    }
}
