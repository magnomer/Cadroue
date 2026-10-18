using Cadroue.Core;

namespace Cadroue.Application;

public static class LEditName
{
    internal static string LEditNameCreate(
        LEncoding lEditOutput,
        string lEditSourcePath,
        string lEditFolder,
        TimeSpan lEditDuration)
    {
        string lEditSourceStem = Path.GetFileNameWithoutExtension(lEditSourcePath);
        string lEditPattern = string.IsNullOrWhiteSpace(lEditOutput.LEncodingNamePattern)
            ? "{OriginalName}"
            : lEditOutput.LEncodingNamePattern;

        DateTimeOffset lEditStamp = DateTimeOffset.Now;
        string lEditOrigin = LEncodingPath.LEncodingTimeFormat(TimeSpan.Zero);
        string lEditLength = LEncodingPath.LEncodingTimeFormat(lEditDuration);
        string lEditStem = lEditPattern
            .Replace("{Prefix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lEditSourceStem, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", "01", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", "Edit", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionStart}", lEditOrigin, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionEnd}", lEditLength, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionDuration}", lEditLength, StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lEditStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lEditStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        lEditStem = LEncodingPath.LEncodingShorten(lEditStem);

        string lEditBaseName = LEditNameNormalize(lEditStem);
        string lEditFileName = LEditNameFormat(lEditOutput, lEditBaseName, lEditSourcePath);
        return LEditSourceMatch(Path.Combine(lEditFolder, lEditFileName), lEditSourcePath)
            ? LEditNameFormat(lEditOutput, $"{lEditBaseName}_edit", lEditSourcePath)
            : lEditFileName;
    }

    private static string LEditNameFormat(LEncoding lEditOutput, string lEditBaseName, string lEditSourcePath)
    {
        string lEditExtension = LEncodingPath.LEncodingExtensionResolve(lEditOutput, lEditSourcePath);
        return string.IsNullOrWhiteSpace(lEditExtension)
            ? lEditBaseName
            : $"{lEditBaseName}.{lEditExtension}";
    }

    private static bool LEditSourceMatch(string lEditOutputPath, string lEditSourcePath)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(lEditOutputPath),
                Path.GetFullPath(lEditSourcePath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception lEditError) when (lEditError is ArgumentException or IOException or NotSupportedException)
        {
            return string.Equals(lEditOutputPath, lEditSourcePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string LEditNameNormalize(string lEditName)
    {
        char[] lEditInvalidChars = Path.GetInvalidFileNameChars();
        var lEditBuilder = new System.Text.StringBuilder(lEditName.Length);
        foreach (char lEditChar in lEditName)
        {
            lEditBuilder.Append(Array.IndexOf(lEditInvalidChars, lEditChar) >= 0 ? '_' : lEditChar);
        }

        string lEditTrimmed = lEditBuilder.ToString().Trim();
        return lEditTrimmed.Length == 0 ? "output" : lEditTrimmed;
    }
}
