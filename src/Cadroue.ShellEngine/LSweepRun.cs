using System.Diagnostics;
using System.Globalization;

using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

public static partial class LSweep
{
    public static async Task<IReadOnlyList<(TimeSpan Start, TimeSpan End)>> LSweepScan(
        string lSweepSource,
        LDetectorBlank lSweepBlank,
        TimeSpan lSweepDuration,
        CancellationToken lSweepToken,
        IProgress<double>? lSweepProgress = null)
    {
        if (string.IsNullOrWhiteSpace(lSweepSource))
        {
            return Array.Empty<(TimeSpan, TimeSpan)>();
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
            LSweepArgsFormat(lSweepSource, lSweepBlank),
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

        lSweepProgress?.Report(1);
        return LSweepOutputParse(lSweepLines);
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
            LSweepSceneFormat(lSweepSource, lSweepThreshold),
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

        lSweepProgress?.Report(1);
        return LSweepSceneParse(lSweepLines);
    }

    public static async Task<IReadOnlyList<(TimeSpan Start, TimeSpan End)>> LSweepStillScan(
        string lSweepSource,
        double lSweepTolerance,
        double lSweepMinimum,
        TimeSpan lSweepDuration,
        CancellationToken lSweepToken,
        IProgress<double>? lSweepProgress = null)
    {
        if (string.IsNullOrWhiteSpace(lSweepSource))
        {
            return Array.Empty<(TimeSpan, TimeSpan)>();
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
            LSweepStillFormat(lSweepSource, lSweepTolerance, lSweepMinimum),
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

        lSweepProgress?.Report(1);
        return LSweepStillParse(lSweepLines);
    }

    private static double? LSweepTimeRead(string lSweepLine)
    {
        const string lSweepKey = "time=";
        int lSweepAt = lSweepLine.IndexOf(lSweepKey, StringComparison.Ordinal);
        if (lSweepAt < 0)
        {
            return null;
        }

        int lSweepFrom = lSweepAt + lSweepKey.Length;
        int lSweepTo = lSweepFrom;
        while (lSweepTo < lSweepLine.Length && !char.IsWhiteSpace(lSweepLine[lSweepTo]))
        {
            lSweepTo++;
        }

        return TimeSpan.TryParse(
            lSweepLine.AsSpan(lSweepFrom, lSweepTo - lSweepFrom),
            CultureInfo.InvariantCulture,
            out TimeSpan lSweepValue)
            ? lSweepValue.TotalSeconds
            : null;
    }
}
