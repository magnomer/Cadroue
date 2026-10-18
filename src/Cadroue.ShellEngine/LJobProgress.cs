using System.Globalization;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJobProgress
{
    private readonly LRunner lJobOwner;
    private readonly LWorkItem lJobItem;
    private readonly double lJobTotalSeconds;
    private readonly System.Text.StringBuilder? lJobProgressBlock;
    private long lJobBlockMicroseconds = -1;

    internal LJobProgress(LRunner lJobRunner, LWorkItem lJobWorkItem, double lJobStageSeconds)
    {
        lJobOwner = lJobRunner;
        lJobItem = lJobWorkItem;
        lJobTotalSeconds = lJobStageSeconds;
        lJobProgressBlock = LRunnerTrace.LRunnerVerboseCheck() ? new System.Text.StringBuilder() : null;
    }

    internal void LJobOutputRead(string pLine)
    {
        int pSeparator = pLine.IndexOf('=');
        if (pSeparator <= 0)
        {
            return;
        }

        string pKey = pLine[..pSeparator];
        string pValue = pLine[(pSeparator + 1)..].Trim();
        lJobProgressBlock?.AppendLine(pLine);

        switch (pKey)
        {
            case "out_time_us":
            case "out_time_ms":
                if (long.TryParse(pValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long pParsed))
                {
                    lJobBlockMicroseconds = pParsed;
                }
                break;

            case "progress":
                lJobOwner.LRunnerDispatch(() => lJobOwner.LRunnerPhaseSet(lJobItem, LWorkPhase.LWorkPhaseEncoding));
                if (lJobBlockMicroseconds >= 0 && lJobTotalSeconds > 0)
                {
                    double pFraction = lJobBlockMicroseconds / 1_000_000d / lJobTotalSeconds;
                    lJobOwner.LRunnerDispatch(() =>
                    {
                        lJobItem.LWorkRunState.LWorkProgress = pFraction;
                        lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeProgress);
                    });
                }

                if (string.Equals(pValue, "end", StringComparison.Ordinal))
                {
                    lJobOwner.LRunnerDispatch(() =>
                    {
                        lJobItem.LWorkRunState.LWorkProgress = 1;
                        lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeProgress);
                    });
                }

                if (lJobProgressBlock is not null)
                {
                    LRunnerTrace.LRunnerFfmpegRecord(
                        $"stdout progress '{lJobItem.LWorkOutputName}'",
                        lJobProgressBlock.ToString());
                    lJobProgressBlock.Clear();
                }

                lJobBlockMicroseconds = -1;
                break;
        }
    }

    internal void LJobStderrRead(string pLine)
    {
        if (pLine.Length > 0 && LRunnerTrace.LRunnerVerboseCheck())
        {
            LRunnerTrace.LRunnerFfmpegRecord($"stderr '{lJobItem.LWorkOutputName}'", pLine);
        }
    }
}
