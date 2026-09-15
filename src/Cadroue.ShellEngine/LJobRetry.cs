namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private const int LJobTailLimit = 240;

    private bool LJobRetryStart(string pJobReason)
    {
        if (!lJobOwner.LRunnerRetryAllowed
            || lJobOwner.LRunnerRetryMaximum <= 0
            || lJobItem.LWorkRetryCount >= lJobOwner.LRunnerRetryMaximum)
        {
            return false;
        }

        LJobAttemptClear();
        int pRetry = 0;
        lJobOwner.LRunnerDispatch(() => pRetry = lJobOwner.lRunnerSchedule.LScheduleRetryRelease(
            lJobItem.LWorkId, lJobOwner.LRunnerIdentity, lJobOwner.LRunnerRetryMaximum, pJobReason));
        if (pRetry == 0)
        {
            return false;
        }

        LRunner.LRunnerRecord(
            $"Encode requeued '{lJobItem.LWorkOutputName}': {pJobReason} " +
            $"Retry {pRetry} of {lJobOwner.LRunnerRetryMaximum}.");
        return true;
    }

    private static string LJobTailRead(string pJobError)
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
}
