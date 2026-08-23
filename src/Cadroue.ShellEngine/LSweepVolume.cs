using System.Diagnostics;

using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

public static partial class LSweep
{
    public static string LSweepVolumeFormat(string lSweepSource, LDetectorMetricMode lSweepMode)
    {
        string lSweepFilter = lSweepMode == LDetectorMetricMode.LDetectorMetricLufs
            ? "ebur128=metadata=1,ametadata=print:key=lavfi.r128.M"
            : "astats=metadata=1:reset=1,ametadata=print:key=lavfi.astats.Overall.RMS_level";
        return $"-hide_banner -stats -i {LEncode.LEncodeFormat(lSweepSource)} -map 0:a:0 -af {LEncode.LEncodeFormat(lSweepFilter)} -vn -f null -";
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

            double? lSweepAt = LSweepFieldRead(lSweepLine, "pts_time:");
            if (lSweepAt is { } lSweepPts)
            {
                lSweepTime = lSweepPts;
                continue;
            }

            double? lSweepLoudness = LSweepFieldRead(lSweepLine, "lavfi.r128.M=")
                ?? LSweepFieldRead(lSweepLine, "lavfi.astats.Overall.RMS_level=");
            if (lSweepLoudness is { } lSweepValue && lSweepTime is { } lSweepStamp)
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

        var lSweepLines = new List<string>();
        var lSweepEmployer = new LEmployer(LTool.LToolFfmpegRead());
        Process? lSweepProcess = null;
        using CancellationTokenRegistration lSweepKill = lSweepToken.Register(() =>
        {
            try
            {
                lSweepProcess?.Kill(true);
            }
            catch (Exception lSweepException)
                when (lSweepException is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException)
            {
            }
        });
        await lSweepEmployer.LEmployerRun(
            LSweepVolumeFormat(lSweepSource, lSweepMode),
            lSweepToken,
            lSweepAttach => lSweepProcess = lSweepAttach,
            _ => { },
            lSweepLine =>
            {
                lSweepLines.Add(lSweepLine);
                if (lSweepProgress is not null
                    && lSweepDuration > TimeSpan.Zero
                    && LSweepTimeRead(lSweepLine) is { } lSweepElapsed)
                {
                    lSweepProgress.Report(Math.Clamp(lSweepElapsed / lSweepDuration.TotalSeconds, 0, 1));
                }
            }).ConfigureAwait(false);

        IReadOnlyList<LSweepSample> lSweepSamples = LSweepVolumeParse(lSweepLines);
        lSweepProgress?.Report(1);
        return LSweepMinimumResolve(
            LSweepBoundaryResolve(lSweepSamples, lSweepWindow, lSweepThreshold), lSweepMinimum);
    }
}
