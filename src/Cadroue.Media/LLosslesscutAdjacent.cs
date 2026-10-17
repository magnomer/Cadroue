using Newtonsoft.Json;

namespace Cadroue.Media;

public static class LLosslesscutAdjacent
{
    public static IReadOnlyList<string> LLosslesscutAdjacentRead(string lLosslesscutSourcePath)
    {
        string lLosslesscutFullPath = Path.GetFullPath(lLosslesscutSourcePath);
        string? lLosslesscutFolderPath = Path.GetDirectoryName(lLosslesscutFullPath);
        if (string.IsNullOrWhiteSpace(lLosslesscutFolderPath) || !Directory.Exists(lLosslesscutFolderPath))
        {
            return Array.Empty<string>();
        }

        IEnumerable<string> lLosslesscutCandidates;
        try
        {
            lLosslesscutCandidates = Directory
                .EnumerateFiles(
                    lLosslesscutFolderPath,
                    $"*{LLosslesscut.LLosslesscutExtension}",
                    SearchOption.TopDirectoryOnly)
                .ToArray();
        }
        catch (Exception lLosslesscutException) when (
            lLosslesscutException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return Array.Empty<string>();
        }

        var lLosslesscutMatches = new List<string>();
        foreach (string lLosslesscutCandidatePath in lLosslesscutCandidates)
        {
            try
            {
                LLosslesscutProject lLosslesscutProject = LLosslesscut.LLosslesscutRead(lLosslesscutCandidatePath);
                if (!string.IsNullOrWhiteSpace(lLosslesscutProject.LLosslesscutProjectMedia)
                    && LLosslesscut.LLosslesscutMediaMatch(
                        lLosslesscutFullPath,
                        lLosslesscutProject.LLosslesscutProjectMedia))
                {
                    lLosslesscutMatches.Add(lLosslesscutCandidatePath);
                }
            }
            catch (Exception lLosslesscutException) when (
                lLosslesscutException is IOException
                    or UnauthorizedAccessException
                    or JsonException
                    or ArgumentException)
            {
            }
        }

        return lLosslesscutMatches
            .OrderByDescending(lLosslesscutPath => File.GetLastWriteTimeUtc(lLosslesscutPath))
            .ThenBy(lLosslesscutPath => Path.GetFileName(lLosslesscutPath), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
