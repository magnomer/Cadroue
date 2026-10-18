namespace Cadroue.UIDeportment;

public static class QViewfinderRuler
{
    private const double QViewfinderTickPixels = 100;

    private static readonly double[] qViewfinderTickSteps =
        [0.1, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 1800, 3600];

    internal static IReadOnlyList<QViewfinderGraduation> QViewfinderTicksResolve(
        double lWidth, TimeSpan lRangeStart, double lRangeSeconds)
    {
        double lInterval = lRangeSeconds / (lWidth / QViewfinderTickPixels);
        double lStep = qViewfinderTickSteps.FirstOrDefault(
            lCandidate => lCandidate >= lInterval, qViewfinderTickSteps[^1]);
        double lStartSeconds = lRangeStart.TotalSeconds;
        var lTicks = new List<QViewfinderGraduation>();
        for (double lSeconds = Math.Ceiling(lStartSeconds / lStep) * lStep;
            lSeconds <= lStartSeconds + lRangeSeconds + 1e-9;
            lSeconds += lStep)
        {
            lTicks.Add(new QViewfinderGraduation(
                (lSeconds - lStartSeconds) / lRangeSeconds * lWidth,
                QViewfinder.QViewfinderLaneHeight * 0.5,
                QViewfinder.QViewfinderLaneHeight,
                QCursor.QCursorTimeFormat(TimeSpan.FromSeconds(lSeconds))));
        }

        return lTicks;
    }
}
