using Cadroue.Core;

namespace Cadroue.Application;

public static class LFixName
{
    internal static string LFixNameCreate(
        LEncoding lFixOutput,
        string lFixSourcePath,
        string lFixFolder,
        TimeSpan lFixDuration)
    {
        string lFixSourceStem = Path.GetFileNameWithoutExtension(lFixSourcePath);
        string lFixPattern = string.IsNullOrWhiteSpace(lFixOutput.LEncodingNamePattern)
            ? "{OriginalName}"
            : lFixOutput.LEncodingNamePattern;

        DateTimeOffset lFixStamp = DateTimeOffset.Now;
        string lFixOrigin = LEncodingPath.LEncodingTimeFormat(TimeSpan.Zero);
        string lFixLength = LEncodingPath.LEncodingTimeFormat(lFixDuration);
        string lFixStem = lFixPattern
            .Replace("{Prefix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lFixSourceStem, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", "01", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", "Fix", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionStart}", lFixOrigin, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionEnd}", lFixLength, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionDuration}", lFixLength, StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lFixStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lFixStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        lFixStem = LEncodingPath.LEncodingShorten(lFixStem);

        string lFixBaseName = LFixNameNormalize(lFixStem);
        string lFixFileName = LFixNameFormat(lFixBaseName, lFixSourcePath);
        return LFixSourceMatch(Path.Combine(lFixFolder, lFixFileName), lFixSourcePath)
            ? LFixNameFormat($"{lFixBaseName}_fix", lFixSourcePath)
            : lFixFileName;
    }

    private static string LFixNameFormat(string lFixBaseName, string lFixSourcePath)
    {
        string lFixExtension = Path.GetExtension(lFixSourcePath).TrimStart('.');
        return string.IsNullOrWhiteSpace(lFixExtension)
            ? lFixBaseName
            : $"{lFixBaseName}.{lFixExtension}";
    }

    private static bool LFixSourceMatch(string lFixOutputPath, string lFixSourcePath)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(lFixOutputPath),
                Path.GetFullPath(lFixSourcePath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception lFixError) when (lFixError is ArgumentException or IOException or NotSupportedException)
        {
            return string.Equals(lFixOutputPath, lFixSourcePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string LFixNameNormalize(string lFixName)
    {
        char[] lFixInvalidChars = Path.GetInvalidFileNameChars();
        var lFixBuilder = new System.Text.StringBuilder(lFixName.Length);
        foreach (char lFixChar in lFixName)
        {
            lFixBuilder.Append(Array.IndexOf(lFixInvalidChars, lFixChar) >= 0 ? '_' : lFixChar);
        }

        string lFixTrimmed = lFixBuilder.ToString().Trim();
        return lFixTrimmed.Length == 0 ? "output" : lFixTrimmed;
    }
}
