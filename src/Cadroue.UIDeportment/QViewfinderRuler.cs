namespace Cadroue.UIDeportment;

public static class QViewfinderRuler
{
    private const double LViewfinderTickPixels = 100;

    private static readonly double[] lViewfinderTickSteps =
        [0.1, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 1800, 3600];

    internal static IReadOnlyList<LViewfinderGraduation> LViewfinderTicksResolve(
        double lWidth, TimeSpan lRangeStart, double lRangeSeconds)
    {
        double lInterval = lRangeSeconds / (lWidth / LViewfinderTickPixels);
        double lStep = lViewfinderTickSteps.FirstOrDefault(
            lCandidate => lCandidate >= lInterval, lViewfinderTickSteps[^1]);
        double lStartSeconds = lRangeStart.TotalSeconds;
        var lTicks = new List<LViewfinderGraduation>();
        for (double lSeconds = Math.Ceiling(lStartSeconds / lStep) * lStep;
            lSeconds <= lStartSeconds + lRangeSeconds + 1e-9;
            lSeconds += lStep)
        {
            lTicks.Add(new LViewfinderGraduation(
                (lSeconds - lStartSeconds) / lRangeSeconds * lWidth,
                LViewfinder.LViewfinderLaneHeight * 0.5,
                LViewfinder.LViewfinderLaneHeight,
                LCursor.LCursorTimeFormat(TimeSpan.FromSeconds(lSeconds))));
        }

        return lTicks;
    }
}
