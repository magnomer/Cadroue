namespace Cadroue.UIDeportment;

public sealed record LSMonitorLabel(string LSMonitorLabelText, double LSMonitorLabelTop);

public sealed record LSMonitorFrame(
    LFlowWaveformOutline LSMonitorFrameOutline,
    IReadOnlyList<double> LSMonitorFrameLines,
    IReadOnlyList<LSMonitorLabel> LSMonitorFrameLabels);

public sealed record LSMonitorHead(bool LSMonitorHeadShown, double LSMonitorHeadLeft);

public static class LSMonitorPlan
{
    public const double LSMonitorGutter = 46;

    private const double LSMonitorLabelHeight = 14;
    private const double LSMonitorLabelLift = 7;
    private static readonly double[] LSMonitorFractions = { 1.0, 0.5 };

    public static double LSMonitorColumnResolve(
        double[] lEnvelope, int lColumn, int lColumns, double lScale, double lOffset)
    {
        double lViewport = 1.0 / lScale;
        int lLength = lEnvelope.Length;
        double lFromF = (lOffset + (double)lColumn / lColumns * lViewport) * lLength;
        double lToF = (lOffset + (double)(lColumn + 1) / lColumns * lViewport) * lLength;
        int lFrom = Math.Clamp((int)Math.Floor(lFromF), 0, lLength - 1);
        int lTo = Math.Clamp((int)Math.Ceiling(lToF), lFrom + 1, lLength);

        double lPeak = 0;
        for (int lIndex = lFrom; lIndex < lTo; lIndex++)
        {
            if (lEnvelope[lIndex] > lPeak)
            {
                lPeak = lEnvelope[lIndex];
            }
        }

        return lPeak;
    }

    public static LFlowWaveformOutline LSMonitorOutlineResolve(
        double[] lEnvelope, double lWidth, double lHeight, double lScale, double lOffset)
    {
        double lMid = lHeight / 2;
        double lPlotWidth = lWidth - LSMonitorGutter;
        if (lWidth <= 0 || lHeight <= 0 || lEnvelope.Length == 0 || lPlotWidth <= 1)
        {
            return new LFlowWaveformOutline(LSMonitorGutter, lMid, Array.Empty<LFlowWaveformPoint>());
        }

        int lColumns = Math.Max(1, (int)lPlotWidth);
        var lPoints = new List<LFlowWaveformPoint>(lColumns * 2);
        for (int lColumn = 0; lColumn < lColumns; lColumn++)
        {
            double lLevel = LSMonitorLevelClamp(LSMonitorColumnResolve(lEnvelope, lColumn, lColumns, lScale, lOffset));
            lPoints.Add(new LFlowWaveformPoint(LSMonitorGutter + lColumn, lMid - lLevel * lMid));
        }

        for (int lColumn = lColumns - 1; lColumn >= 0; lColumn--)
        {
            double lLevel = LSMonitorLevelClamp(LSMonitorColumnResolve(lEnvelope, lColumn, lColumns, lScale, lOffset));
            lPoints.Add(new LFlowWaveformPoint(LSMonitorGutter + lColumn, lMid + lLevel * lMid));
        }

        return new LFlowWaveformOutline(LSMonitorGutter, lMid, lPoints);
    }

    public static IReadOnlyList<double> LSMonitorLinesResolve(double lHeight)
    {
        double lMid = lHeight / 2;
        var lLines = new List<double>();
        foreach (double lFraction in LSMonitorFractions)
        {
            lLines.Add(lMid - lFraction * lMid);
            lLines.Add(lMid + lFraction * lMid);
        }

        lLines.Add(lMid);
        return lLines;
    }

    public static IReadOnlyList<LSMonitorLabel> LSMonitorLabelsResolve(double lHeight)
    {
        double lMid = lHeight / 2;
        var lLabels = new List<LSMonitorLabel>();
        foreach (double lFraction in LSMonitorFractions)
        {
            double lDb = 20.0 * Math.Log10(lFraction);
            lLabels.Add(new LSMonitorLabel($"{lDb:0} dB", LSMonitorTopResolve(lMid - lFraction * lMid, lHeight)));
        }

        lLabels.Add(new LSMonitorLabel("-∞", LSMonitorTopResolve(lMid, lHeight)));
        return lLabels;
    }

    public static LSMonitorHead LSMonitorHeadResolve(
        double lCursor, double lDuration, double lWidth, double lScale, double lOffset)
    {
        double lPlot = lWidth - LSMonitorGutter;
        if (lPlot <= 1 || lDuration <= 0)
        {
            return new LSMonitorHead(false, 0);
        }

        double lFraction = Math.Clamp(lCursor / lDuration, 0, 1);
        double lLocal = (lFraction - lOffset) * lScale;
        return lLocal < 0 || lLocal > 1
            ? new LSMonitorHead(false, 0)
            : new LSMonitorHead(true, LSMonitorGutter + lLocal * lPlot);
    }

    public static double? LSMonitorSeekResolve(
        double lX, double lWidth, double lDuration, double lScale, double lOffset)
    {
        double lPlot = lWidth - LSMonitorGutter;
        if (lPlot <= 1 || lDuration <= 0)
        {
            return null;
        }

        double lLocal = Math.Clamp((lX - LSMonitorGutter) / lPlot, 0, 1);
        return Math.Clamp(lOffset + lLocal / lScale, 0, 1) * lDuration;
    }

    private static double LSMonitorLevelClamp(double lPeak) => Math.Clamp(lPeak, 0, 1);

    private static double LSMonitorTopResolve(double lY, double lHeight) =>
        Math.Clamp(lY - LSMonitorLabelLift, 0, Math.Max(0, lHeight - LSMonitorLabelHeight));
}
