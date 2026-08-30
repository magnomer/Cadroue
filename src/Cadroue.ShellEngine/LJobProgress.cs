using System.Globalization;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private void LJobOutputRead(string pLine)
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
                        lJobItem.LWorkProgress = pFraction;
                        lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeProgress);
                    });
                }

                if (string.Equals(pValue, "end", StringComparison.Ordinal))
                {
                    lJobOwner.LRunnerDispatch(() =>
                    {
                        lJobItem.LWorkProgress = 1;
                        lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeProgress);
                    });
                }

                if (lJobProgressBlock is not null)
                {
                    LRunner.LRunnerFfmpegRecord(
                        $"stdout progress '{lJobItem.LWorkOutputName}'",
                        lJobProgressBlock.ToString());
                    lJobProgressBlock.Clear();
                }

                lJobBlockMicroseconds = -1;
                break;
        }
    }

    private void LJobStderrRead(string pLine)
    {
        if (pLine.Length > 0 && LRunner.LRunnerVerboseCheck())
        {
            LRunner.LRunnerFfmpegRecord($"stderr '{lJobItem.LWorkOutputName}'", pLine);
        }
    }
}
