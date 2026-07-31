using System.Diagnostics;
using System.Globalization;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed partial class LRunner
{
    private async Task LRunnerProgressRead(Process pProcess, LWorkItem pWorkItem, double pTotalSeconds, CancellationToken lRunnerToken)
    {
        long pBlockMicroseconds = -1;
        bool pRunnerVerbose = LRunnerVerboseCheck();
        var pRunnerBlock = pRunnerVerbose ? new System.Text.StringBuilder() : null;

        while (await pProcess.StandardOutput.ReadLineAsync(lRunnerToken).ConfigureAwait(false) is string pLine)
        {
            int pSeparator = pLine.IndexOf('=');
            if (pSeparator <= 0)
            {
                continue;
            }

            string pKey = pLine[..pSeparator];
            string pValue = pLine[(pSeparator + 1)..].Trim();
            pRunnerBlock?.AppendLine(pLine);

            switch (pKey)
            {
                case "out_time_us":
                case "out_time_ms":
                    if (long.TryParse(pValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long pParsed))
                    {
                        pBlockMicroseconds = pParsed;
                    }
                    break;

                case "progress":
                    LRunnerDispatch(() => LRunnerPhaseSet(pWorkItem, LWorkPhase.LWorkPhaseEncoding));
                    if (pBlockMicroseconds >= 0 && pTotalSeconds > 0)
                    {
                        double pFraction = pBlockMicroseconds / 1_000_000d / pTotalSeconds;
                        LRunnerDispatch(() => pWorkItem.LWorkProgress = pFraction);
                    }

                    if (string.Equals(pValue, "end", StringComparison.Ordinal))
                    {
                        LRunnerDispatch(() => pWorkItem.LWorkProgress = 1);
                    }

                    if (pRunnerBlock is not null)
                    {
                        LRunnerFfmpegRecord(
                            $"stdout progress '{pWorkItem.LWorkOutputName}'",
                            pRunnerBlock.ToString());
                        pRunnerBlock.Clear();
                    }

                    pBlockMicroseconds = -1;
                    break;
            }
        }
    }

    private static void LRunnerPartialRemove(LWorkItem? pWorkItem)
    {
        if (pWorkItem is null || string.IsNullOrWhiteSpace(pWorkItem.LWorkOutputPath))
        {
            return;
        }

        try
        {
            if (File.Exists(pWorkItem.LWorkOutputPath))
            {
                File.Delete(pWorkItem.LWorkOutputPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void LRunnerMessageSet(LWorkItem? pWorkItem, string pMessage)
    {
        if (pWorkItem is null)
        {
            return;
        }

        LRunnerDispatch(() => pWorkItem.LWorkMessage = pMessage);
    }

    private void LRunnerDispatch(Action pAction) => lRunnerPost(pAction);
}
