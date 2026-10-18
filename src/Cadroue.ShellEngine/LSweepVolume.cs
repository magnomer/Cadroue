using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LSweepVolume
{
    private const double LSweepVolumeFloor = -70.0;

    public static string LSweepVolumeFormat(string lSweepSource, LDetectorMetricMode lSweepMode)
    {
        string lSweepFilter = lSweepMode == LDetectorMetricMode.LDetectorMetricLufs
            ? "ebur128=metadata=1,ametadata=print:key=lavfi.r128.M"
            : "astats=metadata=1:reset=1,ametadata=print:key=lavfi.astats.Overall.RMS_level";
        return $"-hide_banner -stats -i {LEncode.LEncodeFormat(lSweepSource)} " +
            $"-map 0:a:0 -af {LEncode.LEncodeFormat(lSweepFilter)} -vn -f null -";
    }

    public static IReadOnlyList<LSweepSample> LSweepVolumeParse(IEnumerable<string> lSweepLines)
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

            double? lSweepLoudness = LSweepField.LSweepFieldRead(lSweepLine, "lavfi.r128.M=")
                ?? LSweepField.LSweepFieldRead(lSweepLine, "lavfi.astats.Overall.RMS_level=");
            if (lSweepLoudness is { } lSweepValue && lSweepValue > LSweepVolumeFloor && lSweepTime is { } lSweepStamp)
            {
                lSweepSamples.Add(new LSweepSample(TimeSpan.FromSeconds(lSweepStamp), lSweepValue));
            }
        }

        return lSweepSamples;
    }

    public static async Task<IReadOnlyList<TimeSpan>> LSweepVolumeScan(
        string lSweepSource,
        double lSweepWindow,
        double lSweepThreshold,
        double lSweepMinimum,
        LDetectorMetricMode lSweepMode,
        TimeSpan lSweepDuration,
        CancellationToken lSweepToken,
        IProgress<double>? lSweepProgress = null)
    {
        if (string.IsNullOrWhiteSpace(lSweepSource))
        {
            return Array.Empty<TimeSpan>();
        }

        IReadOnlyList<LSweepSample> lSweepSamples = LSweepVolumeParse(
            await LSweepProcess.LSweepLinesRead(
                LSweepVolumeFormat(lSweepSource, lSweepMode),
                lSweepDuration,
                lSweepToken,
                lSweepProgress)
                .ConfigureAwait(false));
        return LSweepDelta.LSweepMinimumResolve(
            LSweepDelta.LSweepBoundaryResolve(lSweepSamples, lSweepWindow, lSweepThreshold), lSweepMinimum);
    }
}
