using System.Globalization;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LSweepProcess
{
    internal static async Task<IReadOnlyList<string>> LSweepLinesRead(
        string lSweepArguments,
        TimeSpan lSweepDuration,
        CancellationToken lSweepToken,
        IProgress<double>? lSweepProgress)
    {
        var lSweepLines = new List<string>();
        string lSweepProgram = LToolEngine.LToolFfmpegSeam();
        LEmployerResult lSweepResult;
        await LSweepEngine.LSweepClaimSeam(lSweepToken).ConfigureAwait(false);
        try
        {
            lSweepResult = await LEmployerEngine.LEmployerRunSeam(
            lSweepProgram,
            string.Empty,
            LCustodyFamily.LCustodyFamilyEncode,
            null,
            null,
            lSweepArguments,
            lSweepToken,
            null,
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
        }
        finally
        {
            LSweepEngine.LSweepReleaseSeam();
        }

        lSweepToken.ThrowIfCancellationRequested();
        if (lSweepResult.LEmployerExit != 0)
        {
            throw new InvalidOperationException(LSweepFaultFormat(lSweepResult));
        }

        lSweepProgress?.Report(1);
        return lSweepLines;
    }

    public static bool LSweepFaultCheck(Exception lSweepException) =>
        lSweepException is System.ComponentModel.Win32Exception or System.IO.IOException or InvalidOperationException;

    private static string LSweepFaultFormat(LEmployerResult lSweepResult)
    {
        LAutopsyResult lSweepAutopsy = LAutopsy.LAutopsyResolve(lSweepResult.LEmployerExit);
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
