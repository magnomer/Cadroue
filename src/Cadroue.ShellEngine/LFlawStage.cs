using System.Globalization;
using System.Text;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed record LFlawOutput(string LFlawRunOutput, string LFlawRunError);

internal sealed class LFlawStage
{
    private const int LFlawStageCount = 12;

    private readonly TimeSpan lFlawStageDuration;

    private readonly CancellationToken lFlawStageToken;

    private readonly IProgress<double>? lFlawStageProgress;

    private int lFlawStageIndex;

    private LFlawStage(TimeSpan lFlawDuration, CancellationToken lFlawToken, IProgress<double>? lFlawProgress)
    {
        lFlawStageDuration = lFlawDuration;
        lFlawStageToken = lFlawToken;
        lFlawStageProgress = lFlawProgress;
    }

    internal static async Task<LFlawStage> LFlawStageCreate(
        string lFlawSource, CancellationToken lFlawToken, IProgress<double>? lFlawProgress)
    {
        TimeSpan lFlawDuration;
        try
        {
            lFlawDuration = (await LMediaEngine.LMediaProbeSeam(lFlawSource, lFlawToken, null)
                .ConfigureAwait(false)).LMediaInfoDuration;
        }
        catch (Exception lFlawDurationException) when (
            lFlawDurationException is InvalidOperationException
                or System.ComponentModel.Win32Exception
                or IOException)
        {
            lFlawDuration = TimeSpan.Zero;
        }

        return new LFlawStage(lFlawDuration, lFlawToken, lFlawProgress);
    }

    internal async Task<LFlawOutput> LFlawStageRun(string lFlawProgram, string lFlawArguments)
    {
        double lFlawStart = (double)lFlawStageIndex / LFlawStageCount;
        double lFlawEnd = (double)++lFlawStageIndex / LFlawStageCount;
        bool lFlawFfmpeg = string.Equals(
            lFlawProgram,
            LToolEngine.LToolFfmpegSeam(),
            StringComparison.OrdinalIgnoreCase);
        LFlawOutput lFlawResult = await LFlawRunRead(
            lFlawProgram,
            lFlawArguments,
            lFlawStageToken,
            lFlawStageProgress,
            lFlawStart,
            lFlawEnd,
            lFlawStageDuration,
            lFlawFfmpeg).ConfigureAwait(false);
        lFlawStageProgress?.Report(lFlawEnd);
        return lFlawResult;
    }

    private static async Task<LFlawOutput> LFlawRunRead(
        string lFlawProgram,
        string lFlawArguments,
        CancellationToken lFlawToken,
        IProgress<double>? lFlawProgress,
        double lFlawStart,
        double lFlawEnd,
        TimeSpan lFlawDuration,
        bool lFlawFfmpeg)
    {
        bool lFlawProgressEnabled = lFlawFfmpeg && lFlawProgress is not null && lFlawDuration > TimeSpan.Zero;
        var lFlawOutput = new StringBuilder();
        LEmployerResult lFlawResult = await LEmployerEngine.LEmployerRunSeam(
            lFlawProgram,
            string.Empty,
            null,
            null,
            null,
            lFlawProgressEnabled ? "-progress pipe:1 -nostats " + lFlawArguments : lFlawArguments,
            lFlawToken,
            null,
            lFlawLine =>
            {
                lFlawOutput.AppendLine(lFlawLine);
                if (lFlawProgressEnabled)
                {
                    LFlawProgressApply(lFlawLine, lFlawDuration, lFlawStart, lFlawEnd, lFlawProgress!);
                }
            },
            _ => { }).ConfigureAwait(false);
        lFlawToken.ThrowIfCancellationRequested();
        return new LFlawOutput(lFlawOutput.ToString(), lFlawResult.LEmployerError);
    }

    internal static void LFlawProgressApply(
        string lFlawLine,
        TimeSpan lFlawDuration,
        double lFlawStart,
        double lFlawEnd,
        IProgress<double> lFlawProgress)
    {
        int lFlawSeparator = lFlawLine.IndexOf('=');
        if (lFlawSeparator <= 0
            || lFlawLine[..lFlawSeparator] is not ("out_time_us" or "out_time_ms")
            || !long.TryParse(
                lFlawLine[(lFlawSeparator + 1)..],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long lFlawMicroseconds)
            || lFlawDuration <= TimeSpan.Zero)
        {
            return;
        }

        double lFlawFraction = lFlawMicroseconds / 1_000_000d / lFlawDuration.TotalSeconds;
        lFlawProgress.Report(lFlawStart + (lFlawEnd - lFlawStart) * Math.Clamp(lFlawFraction, 0, 1));
    }
}
