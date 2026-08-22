using System.IO;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private static string LJobPathResolve(string pPath, string pSuffix)
    {
        string pFolder = Path.GetDirectoryName(pPath) ?? string.Empty;
        string pStem = Path.GetFileNameWithoutExtension(pPath);
        string pExtension = Path.GetExtension(pPath);
        string pSuffixText = string.IsNullOrEmpty(pSuffix) ? "_1" : pSuffix;

        for (int pIndex = 0; ; pIndex++)
        {
            string pName = pIndex == 0
                ? $"{pStem}{pSuffixText}{pExtension}"
                : $"{pStem}{pSuffixText} ({pIndex + 1}){pExtension}";
            string pCandidate = Path.Combine(pFolder, pName);
            if (!File.Exists(pCandidate))
            {
                return pCandidate;
            }
        }
    }

    private static void LJobTempClear(IReadOnlyList<LEncodeStage> pStages)
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
                    System.Threading.Thread.Sleep(200);
                }
            }

            if (!pRemoved)
            {
                LRunner.LRunnerRecord(
                    $"Could not delete the temporary file '{pPath}'; it may remain on disk.",
                    null);
            }
        }
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
}
