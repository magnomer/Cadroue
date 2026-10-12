namespace Cadroue.UIDeportment;

public sealed record QSMonitorLabel(string QSMonitorLabelText, double QSMonitorLabelTop);

public sealed record QSMonitorFrame(
    QFlowWaveformOutline QSMonitorFrameOutline,
    IReadOnlyList<double> QSMonitorFrameLines,
    IReadOnlyList<QSMonitorLabel> QSMonitorFrameLabels);

public sealed record QSMonitorHead(bool QSMonitorHeadShown, double QSMonitorHeadLeft);

public static class QSMonitorPlan
{
    public const double QSMonitorGutter = 46;

    private const double QSMonitorLabelHeight = 14;
    private const double QSMonitorLabelLift = 7;
    private static readonly double[] QSMonitorFractions = { 1.0, 0.5 };

    public static double QSMonitorColumnResolve(
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

    public static QFlowWaveformOutline QSMonitorOutlineResolve(
        double[] lEnvelope, double lWidth, double lHeight, double lScale, double lOffset)
    {
        double lMid = lHeight / 2;
        double lPlotWidth = lWidth - QSMonitorGutter;
        if (lWidth <= 0 || lHeight <= 0 || lEnvelope.Length == 0 || lPlotWidth <= 1)
        {
            return new QFlowWaveformOutline(QSMonitorGutter, lMid, Array.Empty<QFlowWaveformPoint>());
        }

        int lColumns = Math.Max(1, (int)lPlotWidth);
        var lPoints = new List<QFlowWaveformPoint>(lColumns * 2);
        for (int lColumn = 0; lColumn < lColumns; lColumn++)
        {
            double lLevel = QSMonitorLevelClamp(QSMonitorColumnResolve(lEnvelope, lColumn, lColumns, lScale, lOffset));
            lPoints.Add(new QFlowWaveformPoint(QSMonitorGutter + lColumn, lMid - lLevel * lMid));
        }

        for (int lColumn = lColumns - 1; lColumn >= 0; lColumn--)
        {
            double lLevel = QSMonitorLevelClamp(QSMonitorColumnResolve(lEnvelope, lColumn, lColumns, lScale, lOffset));
            lPoints.Add(new QFlowWaveformPoint(QSMonitorGutter + lColumn, lMid + lLevel * lMid));
        }

        return new QFlowWaveformOutline(QSMonitorGutter, lMid, lPoints);
    }

    public static IReadOnlyList<double> QSMonitorLinesResolve(double lHeight)
    {
        double lMid = lHeight / 2;
        var lLines = new List<double>();
        foreach (double lFraction in QSMonitorFractions)
        {
            lLines.Add(lMid - lFraction * lMid);
            lLines.Add(lMid + lFraction * lMid);
        }

        lLines.Add(lMid);
        return lLines;
    }

    public static IReadOnlyList<QSMonitorLabel> QSMonitorLabelsResolve(double lHeight)
    {
        double lMid = lHeight / 2;
        var lLabels = new List<QSMonitorLabel>();
        foreach (double lFraction in QSMonitorFractions)
        {
            double lDb = 20.0 * Math.Log10(lFraction);
            lLabels.Add(new QSMonitorLabel($"{lDb:0} dB", QSMonitorTopResolve(lMid - lFraction * lMid, lHeight)));
        }

        lLabels.Add(new QSMonitorLabel("-∞", QSMonitorTopResolve(lMid, lHeight)));
        return lLabels;
    }

    public static QSMonitorHead QSMonitorHeadResolve(
        double lCursor, double lDuration, double lWidth, double lScale, double lOffset)
    {
        double lPlot = lWidth - QSMonitorGutter;
        if (lPlot <= 1 || lDuration <= 0)
        {
            return new QSMonitorHead(false, 0);
        }

        double lFraction = Math.Clamp(lCursor / lDuration, 0, 1);
        double lLocal = (lFraction - lOffset) * lScale;
        return lLocal < 0 || lLocal > 1
            ? new QSMonitorHead(false, 0)
            : new QSMonitorHead(true, QSMonitorGutter + lLocal * lPlot);
    }

    public static double? QSMonitorSeekResolve(
        double lX, double lWidth, double lDuration, double lScale, double lOffset)
    {
        double lPlot = lWidth - QSMonitorGutter;
        if (lPlot <= 1 || lDuration <= 0)
        {
            return null;
        }

        double lLocal = Math.Clamp((lX - QSMonitorGutter) / lPlot, 0, 1);
        return Math.Clamp(lOffset + lLocal / lScale, 0, 1) * lDuration;
    }

    private static double QSMonitorLevelClamp(double lPeak) => Math.Clamp(lPeak, 0, 1);

    private static double QSMonitorTopResolve(double lY, double lHeight) =>
        Math.Clamp(lY - QSMonitorLabelLift, 0, Math.Max(0, lHeight - QSMonitorLabelHeight));
}
