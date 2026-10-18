namespace Cadroue.ShellEngine;

internal sealed class LJobReservation
{
    private readonly List<string> lJobReserved = new();

    internal string LJobPathResolve(string pPath, string pSuffix)
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
            if (LJobClaim(pCandidate))
            {
                return pCandidate;
            }
        }
    }

    internal bool LJobClaim(string pPath)
    {
        try
        {
            using (new FileStream(pPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
            }

            lJobReserved.Add(pPath);
            return true;
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    internal void LJobReservedClear()
    {
        foreach (string pPath in lJobReserved)
        {
            try
            {
                if (File.Exists(pPath) && new FileInfo(pPath).Length == 0)
                {
                    File.Delete(pPath);
                }
            }
            catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
            {
            }
        }

        lJobReserved.Clear();
    }
}
