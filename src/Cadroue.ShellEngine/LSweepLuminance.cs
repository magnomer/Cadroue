using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LSweepLuminance
{
    private const string LSweepLuminanceScale = "scale=-2:108,";

    public static string LSweepLuminanceFormat(string lSweepSource, LDetectorLuminanceMode lSweepMode)
    {
        string lSweepScale = lSweepMode == LDetectorLuminanceMode.LDetectorLuminanceFull
            ? string.Empty
            : LSweepLuminanceScale;
        string lSweepFilter = $"{lSweepScale}format=yuv420p,signalstats,metadata=print:key=lavfi.signalstats.YAVG";
        string lSweepSkip = lSweepMode == LDetectorLuminanceMode.LDetectorLuminanceFast
            ? "-skip_frame nokey "
            : string.Empty;
        return $"-hide_banner -stats {lSweepSkip}-i {LEncode.LEncodeFormat(lSweepSource)} " +
            $"-map 0:v:0 -vf {LEncode.LEncodeFormat(lSweepFilter)} -an -f null -";
    }

    public static IReadOnlyList<LSweepSample> LSweepLuminanceParse(IEnumerable<string> lSweepLines)
    {
        var lSweepSamples = new List<LSweepSample>();
        double? lSweepTime = null;
        foreach (string lSweepLine in lSweepLines)
        {
            if (lSweepLine is null)
            {
                continue;
            }

            double? lSweepAt = LSweepField.LSweepFieldRead(lSweepLine, "pts_time:");
            if (lSweepAt is { } lSweepPts)
            {
                lSweepTime = lSweepPts;
                continue;
            }

            double? lSweepLuma = LSweepField.LSweepFieldRead(lSweepLine, "lavfi.signalstats.YAVG=");
            if (lSweepLuma is { } lSweepValue && lSweepTime is { } lSweepStamp)
            {
                lSweepSamples.Add(new LSweepSample(TimeSpan.FromSeconds(lSweepStamp), lSweepValue));
            }
        }

        return lSweepSamples;
    }

    public static IReadOnlyList<TimeSpan> LSweepLuminanceResolve(
        IReadOnlyList<LSweepSample> lSweepSamples,
        double lSweepWindow,
        double lSweepThreshold)
    {
        return LSweepDelta.LSweepBoundaryResolve(lSweepSamples, lSweepWindow, lSweepThreshold / 100.0 * 255.0);
    }

    public static async Task<IReadOnlyList<TimeSpan>> LSweepLuminanceScan(
        string lSweepSource,
        double lSweepWindow,
        double lSweepThreshold,
        double lSweepMinimum,
        LDetectorLuminanceMode lSweepMode,
        TimeSpan lSweepDuration,
        CancellationToken lSweepToken,
        IProgress<double>? lSweepProgress = null)
    {
        if (string.IsNullOrWhiteSpace(lSweepSource))
        {
            return Array.Empty<TimeSpan>();
        }

        IReadOnlyList<LSweepSample> lSweepSamples = LSweepLuminanceParse(
            await LSweepProcess.LSweepLinesRead(
                LSweepLuminanceFormat(lSweepSource, lSweepMode),
                lSweepDuration,
                lSweepToken,
                lSweepProgress)
                .ConfigureAwait(false));
        return LSweepDelta.LSweepMinimumResolve(
            LSweepLuminanceResolve(lSweepSamples, lSweepWindow, lSweepThreshold), lSweepMinimum);
    }
}
