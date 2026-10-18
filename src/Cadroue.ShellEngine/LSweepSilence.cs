using System.Globalization;

using Cadroue.Application;

namespace Cadroue.ShellEngine;

public static class LSweepSilence
{
    public static string LSweepSilenceFormat(string lSweepSource, double lSweepThresholdDb, double lSweepMinimum)
    {
        string lSweepFilter = string.Create(CultureInfo.InvariantCulture,
            $"silencedetect=noise={lSweepThresholdDb:0.###}dB:d={lSweepMinimum:0.###}");
        return $"-hide_banner -stats -i {LEncode.LEncodeFormat(lSweepSource)} " +
            $"-map 0:a:0 -af {LEncode.LEncodeFormat(lSweepFilter)} -vn -f null -";
    }

    public static IReadOnlyList<LSweepSpan> LSweepSilenceParse(
        IEnumerable<string> lSweepLines, TimeSpan lSweepDuration)
    {
        var lSweepIntervals = new List<LSweepSpan>();
        double? lSweepPending = null;
        foreach (string lSweepLine in lSweepLines)
        {
            if (lSweepLine is null)
            {
                continue;
            }

            double? lSweepStart = LSweepField.LSweepFieldRead(lSweepLine, "silence_start: ");
            if (lSweepStart is { } lSweepFrom)
            {
                lSweepPending = lSweepFrom;
                continue;
            }

            double? lSweepEnd = LSweepField.LSweepFieldRead(lSweepLine, "silence_end: ");
            if (lSweepEnd is { } lSweepTo && lSweepPending is { } lSweepAt)
            {
                if (lSweepTo > lSweepAt)
                {
                    lSweepIntervals.Add(new LSweepSpan(TimeSpan.FromSeconds(lSweepAt), TimeSpan.FromSeconds(lSweepTo)));
                }

                lSweepPending = null;
            }
        }

        if (lSweepPending is { } lSweepDangling)
        {
            TimeSpan lSweepStartSpan = TimeSpan.FromSeconds(lSweepDangling);
            if (lSweepDuration > lSweepStartSpan)
            {
                lSweepIntervals.Add(new LSweepSpan(lSweepStartSpan, lSweepDuration));
            }
        }

        return lSweepIntervals;
    }

    public static async Task<IReadOnlyList<LSweepSpan>> LSweepSilenceScan(
        string lSweepSource,
        double lSweepThresholdDb,
        double lSweepMinimum,
        TimeSpan lSweepDuration,
        CancellationToken lSweepToken,
        IProgress<double>? lSweepProgress = null)
    {
        if (string.IsNullOrWhiteSpace(lSweepSource))
        {
            return Array.Empty<LSweepSpan>();
        }

        return LSweepSilenceParse(
            await LSweepProcess.LSweepLinesRead(
                    LSweepSilenceFormat(lSweepSource, lSweepThresholdDb, lSweepMinimum),
                    lSweepDuration,
                    lSweepToken,
                    lSweepProgress)
                .ConfigureAwait(false),
            lSweepDuration);
    }
}
