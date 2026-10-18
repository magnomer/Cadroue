using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LJobFailure
{
    private const int LJobTailLimit = 240;

    internal static string LJobTailRead(string pJobError)
    {
        if (string.IsNullOrWhiteSpace(pJobError))
        {
            return "FFmpeg reported nothing.";
        }

        string[] pJobLines = pJobError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(" | ", pJobLines[^Math.Min(3, pJobLines.Length)..]);
    }

    private static string LJobTailShorten(string pJobTail) =>
        pJobTail.Length <= LJobTailLimit ? pJobTail : pJobTail[..LJobTailLimit].TrimEnd() + "...";

    internal static void LJobFailureRecord(
        LWorkItem pWorkItem, LAutopsyResult pAutopsy, string pTail, TimeSpan pElapsed)
    {
        string pSymbol = pAutopsy.LAutopsyResultSymbol is { Length: > 0 } pAutopsySymbol
            ? $" {pAutopsySymbol}"
            : string.Empty;

        LRunnerTrace.LRunnerRecord(
            $"Encode failed '{pWorkItem.LWorkOutputName}': {pAutopsy.LAutopsyResultTechnical} " +
            $"(exit {pAutopsy.LAutopsyResultCode}{pSymbol}) " +
            $"after {pElapsed:hh\\:mm\\:ss\\.fff}. {pTail}");
    }

    internal static string LJobFailureDescribe(LAutopsyResult pAutopsy, int pExitCode, string pTail) =>
        pAutopsy.LAutopsyResultVisible
            ? pAutopsy.LAutopsyResultAction is { Length: > 0 } pAutopsyAction
                ? $"{pAutopsy.LAutopsyResultSimple} {pAutopsyAction}"
                : pAutopsy.LAutopsyResultSimple
            : $"FFmpeg exited with code {pExitCode}. {LJobTailShorten(pTail)}";
}
