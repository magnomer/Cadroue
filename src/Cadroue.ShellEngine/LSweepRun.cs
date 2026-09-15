using System.Diagnostics;
using System.Globalization;

using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

public static partial class LSweep
{
    public static async Task<IReadOnlyList<LSweepSpan>> LSweepScan(
        string lSweepSource,
        LDetectorBlank lSweepBlank,
        TimeSpan lSweepDuration,
        CancellationToken lSweepToken,
        IProgress<double>? lSweepProgress = null)
    {
        if (string.IsNullOrWhiteSpace(lSweepSource))
        {
            return Array.Empty<LSweepSpan>();
        }

        return LSweepOutputParse(
            await LSweepLinesRead(
                LSweepArgsFormat(lSweepSource, lSweepBlank),
                lSweepDuration,
                lSweepToken,
                lSweepProgress)
                .ConfigureAwait(false));
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
            await LSweepLinesRead(
                LSweepSceneFormat(lSweepSource, lSweepThreshold),
                lSweepDuration,
                lSweepToken,
                lSweepProgress)
                .ConfigureAwait(false));
    }

    public static async Task<IReadOnlyList<LSweepSpan>> LSweepStillScan(
        string lSweepSource,
        double lSweepTolerance,
        double lSweepMinimum,
        TimeSpan lSweepDuration,
        CancellationToken lSweepToken,
        IProgress<double>? lSweepProgress = null)
    {
        if (string.IsNullOrWhiteSpace(lSweepSource))
        {
            return Array.Empty<LSweepSpan>();
        }

        return LSweepStillParse(
            await LSweepLinesRead(
                    LSweepStillFormat(lSweepSource, lSweepTolerance, lSweepMinimum),
                    lSweepDuration,
                    lSweepToken,
                    lSweepProgress)
                .ConfigureAwait(false),
            lSweepDuration);
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
            await LSweepLinesRead(
                LSweepLuminanceFormat(lSweepSource, lSweepMode),
                lSweepDuration,
                lSweepToken,
                lSweepProgress)
                .ConfigureAwait(false));
        return LSweepMinimumResolve(
            LSweepLuminanceResolve(lSweepSamples, lSweepWindow, lSweepThreshold), lSweepMinimum);
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
            await LSweepLinesRead(
                    LSweepSilenceFormat(lSweepSource, lSweepThresholdDb, lSweepMinimum),
                    lSweepDuration,
                    lSweepToken,
                    lSweepProgress)
                .ConfigureAwait(false),
            lSweepDuration);
    }

    private static async Task<IReadOnlyList<string>> LSweepLinesRead(
        string lSweepArguments,
        TimeSpan lSweepDuration,
        CancellationToken lSweepToken,
        IProgress<double>? lSweepProgress)
    {
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
                when (lSweepException is System.ComponentModel.Win32Exception
                    or InvalidOperationException
                    or NotSupportedException)
            {
            }
        });
        LEmployerResult lSweepResult = await lSweepEmployer.LEmployerRun(
            lSweepArguments,
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

        lSweepToken.ThrowIfCancellationRequested();
        if (lSweepResult.LEmployerExit != 0)
        {
            throw new InvalidOperationException(LSweepFaultFormat(lSweepResult));
        }

        lSweepProgress?.Report(1);
        return lSweepLines;
    }

    private static string LSweepFaultFormat(LEmployerResult lSweepResult)
    {
        string[] lSweepTail = lSweepResult.LEmployerError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        LAutopsyResult lSweepAutopsy = LAutopsy.LAutopsyResolve(
            lSweepResult.LEmployerExit,
            string.Join(" | ", lSweepTail[^Math.Min(3, lSweepTail.Length)..]));
        return lSweepAutopsy.LAutopsyResultVisible
            ? $"{lSweepAutopsy.LAutopsyResultSimple} (exit {lSweepAutopsy.LAutopsyResultCode})"
            : $"{lSweepAutopsy.LAutopsyResultTechnical} (exit {lSweepAutopsy.LAutopsyResultCode})";
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
