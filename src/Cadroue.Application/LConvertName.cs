using Cadroue.Core;

namespace Cadroue.Application;

public static class LConvertName
{
    internal static string LConvertNameCreate(
        LEncoding lConvertOutput,
        string lConvertSourcePath,
        string lConvertFolder,
        TimeSpan lConvertDuration,
        DateTimeOffset lConvertStamp,
        HashSet<string> lConvertTakenPaths)
    {
        string lConvertSourceStem = Path.GetFileNameWithoutExtension(lConvertSourcePath);
        string lConvertPattern = string.IsNullOrWhiteSpace(lConvertOutput.LEncodingNamePattern)
            ? "{OriginalName}"
            : lConvertOutput.LEncodingNamePattern;

        string lConvertStem = LEncodingPath.LEncodingNameFormat(
            lConvertPattern,
            lConvertMarker => LConvertMarkerRead(lConvertMarker, lConvertSourceStem, lConvertDuration, lConvertStamp));

        string lConvertBaseName = LEncodingPath.LEncodingNameNormalize(lConvertStem);
        string lConvertExtension = LEncodingPath.LEncodingExtensionResolve(lConvertOutput, lConvertSourcePath);
        if (LConvertSourceMatch(
                Path.Combine(lConvertFolder, LConvertNameFormat(lConvertBaseName, lConvertExtension)),
                lConvertSourcePath))
        {
            lConvertBaseName = $"{lConvertBaseName}_convert";
        }

        return LEncodingPath.LEncodingNameClaim(
            lConvertTakenPaths,
            lConvertFolder,
            lConvertBaseName,
            lConvertExtension);
    }

    private static string? LConvertMarkerRead(
        string lConvertMarker,
        string lConvertSourceStem,
        TimeSpan lConvertDuration,
        DateTimeOffset lConvertStamp)
    {
        if (lConvertMarker.Equals("Prefix", StringComparison.OrdinalIgnoreCase)
            || lConvertMarker.Equals("Suffix", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (lConvertMarker.Equals("OriginalName", StringComparison.OrdinalIgnoreCase))
        {
            return lConvertSourceStem;
        }

        if (lConvertMarker.Equals("SectionNumber", StringComparison.OrdinalIgnoreCase))
        {
            return "01";
        }

        if (lConvertMarker.Equals("SectionName", StringComparison.OrdinalIgnoreCase))
        {
            return "Convert";
        }

        if (lConvertMarker.Equals("SectionStart", StringComparison.OrdinalIgnoreCase))
        {
            return LEncodingPath.LEncodingTimeFormat(TimeSpan.Zero);
        }

        if (lConvertMarker.Equals("SectionEnd", StringComparison.OrdinalIgnoreCase)
            || lConvertMarker.Equals("SectionDuration", StringComparison.OrdinalIgnoreCase))
        {
            return LEncodingPath.LEncodingTimeFormat(lConvertDuration);
        }

        if (lConvertMarker.Equals("Date", StringComparison.OrdinalIgnoreCase))
        {
            return lConvertStamp.ToString("yyyy-MM-dd");
        }

        return lConvertMarker.Equals("Time", StringComparison.OrdinalIgnoreCase)
            ? lConvertStamp.ToString("HHmmss")
            : null;
    }

    private static string LConvertNameFormat(string lConvertBaseName, string lConvertExtension) =>
        string.IsNullOrWhiteSpace(lConvertExtension)
            ? lConvertBaseName
            : $"{lConvertBaseName}.{lConvertExtension}";

    private static bool LConvertSourceMatch(string lConvertOutputPath, string lConvertSourcePath)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(lConvertOutputPath),
                Path.GetFullPath(lConvertSourcePath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception lConvertError) when (lConvertError is ArgumentException
            or IOException
            or NotSupportedException)
        {
            return string.Equals(lConvertOutputPath, lConvertSourcePath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
