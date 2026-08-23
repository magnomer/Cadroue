using System.Globalization;

using Cadroue.Application;

namespace Cadroue.ShellEngine;

public static partial class LSweep
{
    public static string LSweepSilenceFormat(string lSweepSource, double lSweepThresholdDb, double lSweepMinimum)
    {
        string lSweepFilter = string.Create(CultureInfo.InvariantCulture,
            $"silencedetect=noise={lSweepThresholdDb:0.###}dB:d={lSweepMinimum:0.###}");
        return $"-hide_banner -stats -i {LEncode.LEncodeFormat(lSweepSource)} -map 0:a:0 -af {LEncode.LEncodeFormat(lSweepFilter)} -vn -f null -";
    }

    public static IReadOnlyList<(TimeSpan Start, TimeSpan End)> LSweepSilenceParse(
        IEnumerable<string> lSweepLines, TimeSpan lSweepDuration)
    {
        var lSweepIntervals = new List<(TimeSpan, TimeSpan)>();
        double? lSweepPending = null;
        foreach (string lSweepLine in lSweepLines)
        {
            if (lSweepLine is null)
            {
                continue;
            }

            double? lSweepStart = LSweepFieldRead(lSweepLine, "silence_start: ");
            if (lSweepStart is { } lSweepFrom)
            {
                lSweepPending = lSweepFrom;
                continue;
            }

            double? lSweepEnd = LSweepFieldRead(lSweepLine, "silence_end: ");
            if (lSweepEnd is { } lSweepTo && lSweepPending is { } lSweepAt)
            {
                if (lSweepTo > lSweepAt)
                {
                    lSweepIntervals.Add((TimeSpan.FromSeconds(lSweepAt), TimeSpan.FromSeconds(lSweepTo)));
                }

                lSweepPending = null;
            }
        }

        if (lSweepPending is { } lSweepDangling)
        {
            TimeSpan lSweepStartSpan = TimeSpan.FromSeconds(lSweepDangling);
            if (lSweepDuration > lSweepStartSpan)
            {
                lSweepIntervals.Add((lSweepStartSpan, lSweepDuration));
            }
        }

        return lSweepIntervals;
    }
}
