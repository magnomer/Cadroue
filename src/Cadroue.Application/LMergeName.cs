using Cadroue.Core;

namespace Cadroue.Application;

public static class LMergeName
{
    internal static string LMergeNameCreate(
        LEncoding lMergeOutput,
        string lMergeBasePath,
        string lMergeGroupName,
        DateTimeOffset lMergeStamp,
        HashSet<string> lMergeTakenNames)
    {
        string lMergePattern = string.IsNullOrWhiteSpace(lMergeOutput.LEncodingNamePattern)
            ? "{OriginalName}"
            : lMergeOutput.LEncodingNamePattern;

        string lMergeStemName = lMergePattern
            .Replace("{Prefix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lMergeGroupName, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionStart}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionEnd}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionDuration}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lMergeStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lMergeStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        lMergeStemName = LEncodingPath.LEncodingShorten(lMergeStemName);

        string lMergeBaseName = LMergeNameNormalize(lMergeStemName);
        string lMergeUniqueName = lMergeBaseName;
        int lMergeAttempt = 2;
        while (!lMergeTakenNames.Add(lMergeUniqueName))
        {
            lMergeUniqueName = $"{lMergeBaseName}_{lMergeAttempt}";
            lMergeAttempt++;
        }

        string lMergeExtension = LEncodingPath.LEncodingExtensionResolve(lMergeOutput, lMergeBasePath);
        return string.IsNullOrWhiteSpace(lMergeExtension)
            ? lMergeUniqueName
            : $"{lMergeUniqueName}.{lMergeExtension}";
    }

    private static string LMergeNameNormalize(string lMergeName)
    {
        char[] lMergeInvalidChars = Path.GetInvalidFileNameChars();
        var lMergeBuilder = new System.Text.StringBuilder(lMergeName.Length);
        foreach (char lMergeChar in lMergeName)
        {
            lMergeBuilder.Append(Array.IndexOf(lMergeInvalidChars, lMergeChar) >= 0 ? '_' : lMergeChar);
        }

        string lMergeTrimmed = lMergeBuilder.ToString().Trim();
        return lMergeTrimmed.Length == 0 ? "merged" : lMergeTrimmed;
    }
}
