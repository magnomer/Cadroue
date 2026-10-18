using System.Globalization;

namespace Cadroue.ShellEngine;

public static class LSweepScene
{
    public static string LSweepSceneFormat(string lSweepSource, double lSweepThreshold)
    {
        string lSweepFilter = string.Create(CultureInfo.InvariantCulture,
            $"scdet=threshold={lSweepThreshold:0.###},metadata=print:key=lavfi.scd.time");
        return $"-hide_banner -stats -i {LEncode.LEncodeFormat(lSweepSource)} " +
            $"-map 0:v:0 -vf {LEncode.LEncodeFormat(lSweepFilter)} -an -f null -";
    }

    public static IReadOnlyList<TimeSpan> LSweepSceneParse(IEnumerable<string> lSweepLines)
    {
        var lSweepSeen = new SortedSet<double>();
        foreach (string lSweepLine in lSweepLines)
        {
            if (lSweepLine is null)
            {
                continue;
            }

            double? lSweepTime = LSweepField.LSweepFieldRead(lSweepLine, "lavfi.scd.time=");
            if (lSweepTime is { } lSweepAt)
            {
                lSweepSeen.Add(lSweepAt);
            }
        }

        return lSweepSeen.Select(TimeSpan.FromSeconds).ToList();
    }

    public static async Task<IReadOnlyList<TimeSpan>> LSweepSceneScan(
        string lSweepSource,
        double lSweepThreshold,
        TimeSpan lSweepDuration,
        CancellationToken lSweepToken,
        IProgress<double>? lSweepProgress = null)
    {
        if (string.IsNullOrWhiteSpace(lSweepSource))
        {
            return Array.Empty<TimeSpan>();
        }

        return LSweepSceneParse(
            await LSweepProcess.LSweepLinesRead(
                LSweepSceneFormat(lSweepSource, lSweepThreshold),
                lSweepDuration,
                lSweepToken,
                lSweepProgress)
                .ConfigureAwait(false));
    }
}
