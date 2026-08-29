using System.Globalization;
using System.IO;

using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

internal static class LSalvageScan
{
    // Two decodable packets separated by more than this many seconds are treated as
    // straddling a dead region rather than one continuous span.
    private const double LSalvageGapSeconds = 1.0;

    internal static async Task<IReadOnlyList<LSalvageSpan>> LSalvageScanRun(
        string lSalvageSource, CancellationToken lSalvageToken)
    {
        if (string.IsNullOrWhiteSpace(lSalvageSource) || !File.Exists(lSalvageSource))
        {
            return Array.Empty<LSalvageSpan>();
        }

        try
        {
            var lSalvageLines = new List<string>();
            var lSalvageProbe = new LEmployer(LTool.LToolFfprobeRead());
            _ = await lSalvageProbe.LEmployerRun(
                "-hide_banner -v error -select_streams v:0 -show_packets "
                + "-show_entries packet=pts_time,dts_time,duration_time,flags "
                + $"-of csv=p=0 -i {LEncode.LEncodeFormat(lSalvageSource)}",
                lSalvageToken,
                static _ => { },
                lSalvageLines.Add,
                static _ => { }).ConfigureAwait(false);

            IReadOnlyList<LSalvageSpan> lSalvageSpans = LSalvageSpansResolve(lSalvageLines);
            if (lSalvageSpans.Count > 0)
            {
                return lSalvageSpans;
            }

            // The packet probe found no usable span (no video stream, or a container
            // the demuxer could not walk). Fall back to the whole measured duration so
            // the run still copies every readable byte rather than salvaging nothing.
            return LSalvageWholeResolve(lSalvageSource, lSalvageToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception lSalvageException)
        {
            LRunner.LRunnerRecord(
                $"Salvage scan could not examine '{Path.GetFileName(lSalvageSource)}'", lSalvageException);
            return Array.Empty<LSalvageSpan>();
        }
    }

    private static IReadOnlyList<LSalvageSpan> LSalvageSpansResolve(IReadOnlyList<string> lSalvageLines)
    {
        var lSalvageSpans = new List<LSalvageSpan>();
        double lSalvageStart = 0;
        double lSalvageEnd = 0;
        bool lSalvageOpen = false;

        foreach (string lSalvageLine in lSalvageLines)
        {
            string[] lSalvageFields = lSalvageLine.Split(',');
            if (lSalvageFields.Length < 4)
            {
                continue;
            }

            // The demuxer marks a torn packet corrupt; end the current span at it and
            // resume a fresh span only once clean packets return.
            if (lSalvageFields[3].Contains('C', StringComparison.OrdinalIgnoreCase))
            {
                LSalvageSpanClose(lSalvageSpans, lSalvageStart, lSalvageEnd, ref lSalvageOpen);
                continue;
            }

            double? lSalvagePts = LSalvageTimeRead(lSalvageFields[0]) ?? LSalvageTimeRead(lSalvageFields[1]);
            if (lSalvagePts is not { } lSalvageTime)
            {
                continue;
            }

            double lSalvageDuration = LSalvageTimeRead(lSalvageFields[2]) ?? 0;
            if (!lSalvageOpen)
            {
                lSalvageStart = lSalvageTime < 0 ? 0 : lSalvageTime;
                lSalvageEnd = lSalvageStart + Math.Max(0, lSalvageDuration);
                lSalvageOpen = true;
                continue;
            }

            if (lSalvageTime - lSalvageEnd > LSalvageGapSeconds)
            {
                LSalvageSpanClose(lSalvageSpans, lSalvageStart, lSalvageEnd, ref lSalvageOpen);
                lSalvageStart = lSalvageTime < 0 ? 0 : lSalvageTime;
                lSalvageEnd = lSalvageStart + Math.Max(0, lSalvageDuration);
                lSalvageOpen = true;
                continue;
            }

            lSalvageEnd = Math.Max(lSalvageEnd, lSalvageTime + Math.Max(0, lSalvageDuration));
        }

        LSalvageSpanClose(lSalvageSpans, lSalvageStart, lSalvageEnd, ref lSalvageOpen);
        return lSalvageSpans;
    }

    private static void LSalvageSpanClose(
        List<LSalvageSpan> lSalvageSpans, double lSalvageStart, double lSalvageEnd, ref bool lSalvageOpen)
    {
        if (lSalvageOpen && lSalvageEnd > lSalvageStart)
        {
            lSalvageSpans.Add(new LSalvageSpan(
                TimeSpan.FromSeconds(lSalvageStart), TimeSpan.FromSeconds(lSalvageEnd)));
        }

        lSalvageOpen = false;
    }

    private static double? LSalvageTimeRead(string lSalvageField)
    {
        string lSalvageValue = lSalvageField.Trim();
        return double.TryParse(lSalvageValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double lSalvageParsed)
            ? lSalvageParsed
            : null;
    }

    private static IReadOnlyList<LSalvageSpan> LSalvageWholeResolve(string lSalvageSource, CancellationToken lSalvageToken)
    {
        TimeSpan lSalvageDuration = LScout.LScoutMediaRead(lSalvageSource, lSalvageToken)?.LWorkMediaDuration ?? TimeSpan.Zero;
        return lSalvageDuration > TimeSpan.Zero
            ? new[] { new LSalvageSpan(TimeSpan.Zero, lSalvageDuration) }
            : Array.Empty<LSalvageSpan>();
    }
}
