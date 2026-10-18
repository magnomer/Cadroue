using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJobFile
{
    private readonly LWorkItem lJobItem;
    private readonly LJobCollision lJobCollision;

    internal LJobFile(LWorkItem lJobWorkItem, LJobCollision lJobWorkCollision)
    {
        lJobItem = lJobWorkItem;
        lJobCollision = lJobWorkCollision;
    }

    internal async Task LJobOutputClear()
    {
        string pOutput = lJobItem.LWorkOutputPath;
        if (string.IsNullOrWhiteSpace(pOutput))
        {
            return;
        }

        if (lJobCollision.LJobExistingCheck(pOutput))
        {
            LRunnerTrace.LRunnerRecord(
                $"Preserved '{Path.GetFileName(pOutput)}'; " +
                $"the unresolved Fix output is a pre-existing file, not this job's own output");
            return;
        }

        for (int pAttempt = 0; pAttempt < 5; pAttempt++)
        {
            try
            {
                if (!File.Exists(pOutput))
                {
                    return;
                }

                File.Delete(pOutput);
                LRunnerTrace.LRunnerRecord($"Discarded the unresolved Fix output '{Path.GetFileName(pOutput)}'");
                return;
            }
            catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
            {
                await Task.Delay(200).ConfigureAwait(false);
            }
        }

        LRunnerTrace.LRunnerRecord(
            $"Could not delete the unresolved Fix output '{pOutput}'; it may remain on disk.", null);
    }

    internal static async Task LJobTempClear(IReadOnlyList<LEncodeStage> pStages)
    {
        foreach (LEncodeStage pStage in pStages)
        {
            if (!pStage.LEncodeStageTemporary || string.IsNullOrWhiteSpace(pStage.LEncodeStagePath))
            {
                continue;
            }

            string pPath = pStage.LEncodeStagePath;
            bool pRemoved = false;
            for (int pAttempt = 0; pAttempt < 5 && !pRemoved; pAttempt++)
            {
                try
                {
                    if (Directory.Exists(pPath))
                    {
                        Directory.Delete(pPath, true);
                        pRemoved = true;
                        break;
                    }

                    if (!File.Exists(pPath))
                    {
                        pRemoved = true;
                        break;
                    }

                    File.Delete(pPath);
                    pRemoved = true;
                }
                catch (Exception pException)
                    when (pException is IOException or UnauthorizedAccessException)
                {
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }

            if (!pRemoved)
            {
                LRunnerTrace.LRunnerRecord(
                    $"Could not delete the temporary file '{pPath}'; it may remain on disk.",
                    null);
            }
        }
    }
}
