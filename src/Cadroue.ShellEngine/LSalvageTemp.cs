namespace Cadroue.ShellEngine;

internal static class LSalvageTemp
{
    internal static string LSalvageTempResolve(string lSalvageOutputPath)
    {
        string lSalvageFolder = Path.GetDirectoryName(lSalvageOutputPath) ?? string.Empty;
        string lSalvageStem = Path.GetFileNameWithoutExtension(lSalvageOutputPath);
        string lSalvageExtension = Path.GetExtension(lSalvageOutputPath);

        for (int lSalvageIndex = 0; ; lSalvageIndex++)
        {
            string lSalvageName = lSalvageIndex == 0
                ? $"{lSalvageStem}.cadsalvage{lSalvageExtension}"
                : $"{lSalvageStem}.cadsalvage ({lSalvageIndex + 1}){lSalvageExtension}";
            string lSalvageCandidate = Path.Combine(lSalvageFolder, lSalvageName);
            if (!File.Exists(lSalvageCandidate))
            {
                return lSalvageCandidate;
            }
        }
    }

    internal static async Task LSalvageTempClear(string lSalvageTemp)
    {
        for (int lSalvageAttempt = 0; lSalvageAttempt < 5; lSalvageAttempt++)
        {
            try
            {
                if (!File.Exists(lSalvageTemp))
                {
                    return;
                }

                File.Delete(lSalvageTemp);
                return;
            }
            catch (Exception lSalvageException) when (lSalvageException is IOException or UnauthorizedAccessException)
            {
                await Task.Delay(200).ConfigureAwait(false);
            }
        }

        LRunnerTrace.LRunnerRecord(
            $"Could not delete the salvage temporary '{lSalvageTemp}'; it may remain on disk.", null);
    }
}
