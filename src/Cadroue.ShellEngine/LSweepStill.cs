using System.Globalization;

namespace Cadroue.ShellEngine;

public static partial class LSweep
{
    public static string LSweepStillFormat(string lSweepSource, double lSweepTolerance, double lSweepMinimum)
    {
        string lSweepFilter = string.Create(CultureInfo.InvariantCulture,
            $"freezedetect=n={lSweepTolerance / 100:0.#####}:d={lSweepMinimum:0.###}");
        return $"-hide_banner -stats -i {LEncode.LEncodeFormat(lSweepSource)} -map 0:v:0 -vf {LEncode.LEncodeFormat(lSweepFilter)} -an -f null -";
    }

    public static IReadOnlyList<(TimeSpan Start, TimeSpan End)> LSweepStillParse(IEnumerable<string> lSweepLines)
    {
        var lSweepIntervals = new List<(TimeSpan, TimeSpan)>();
        double? lSweepPending = null;
        foreach (string lSweepLine in lSweepLines)
        {
            if (lSweepLine is null)
            {
                continue;
            }

            double? lSweepStart = LSweepFieldRead(lSweepLine, "freeze_start: ");
            if (lSweepStart is { } lSweepFrom)
            {
                lSweepPending = lSweepFrom;
                continue;
            }

            double? lSweepEnd = LSweepFieldRead(lSweepLine, "freeze_end: ");
            if (lSweepEnd is { } lSweepTo && lSweepPending is { } lSweepOpen && lSweepTo > lSweepOpen)
            {
                lSweepIntervals.Add((TimeSpan.FromSeconds(lSweepOpen), TimeSpan.FromSeconds(lSweepTo)));
                lSweepPending = null;
            }
        }

        return lSweepIntervals;
    }
}
