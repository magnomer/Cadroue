namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private bool LJobRetryStart(string pJobReason)
    {
        if (!lJobOwner.LRunnerRetryAllowed || lJobOwner.LRunnerRetryMaximum <= 0)
        {
            return false;
        }

        int pAttempt = lJobOwner.lRunnerAttempts.AddOrUpdate(lJobItem.LWorkId, 1, (_, pPrevious) => pPrevious + 1);
        if (pAttempt > lJobOwner.LRunnerRetryMaximum)
        {
            return false;
        }

        string pJobMessage = $"{pJobReason} Retry {pAttempt} of {lJobOwner.LRunnerRetryMaximum}.";
        bool pReleased = false;
        lJobOwner.LRunnerDispatch(() => pReleased = lJobOwner.lRunnerSchedule.LScheduleItemRelease(lJobItem.LWorkId, lJobOwner.LRunnerIdentity, pJobMessage));
        if (!pReleased)
        {
            return false;
        }

        LRunner.LRunnerRecord($"Encode requeued '{lJobItem.LWorkOutputName}': {pJobMessage}");
        return true;
    }
}
