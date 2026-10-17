using System.Globalization;
using System.IO;

namespace Cadroue.Core;

public static class LEncodingPath
{
    public static string LEncodingFolderRead(LEncoding lEncoding, string lEncodingSourcePath)
    {
        string lEncodingSourceFolder = Path.GetDirectoryName(lEncodingSourcePath) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(lEncoding.LEncodingLocationFolder))
        {
            return lEncodingSourceFolder;
        }

        if (string.Equals(lEncoding.LEncodingLocation, "Custom location", StringComparison.Ordinal)
            || string.Equals(lEncoding.LEncodingLocation, "Custom folder", StringComparison.Ordinal))
        {
            return lEncoding.LEncodingLocationFolder;
        }

        if (string.Equals(lEncoding.LEncodingLocation, "Subfolder", StringComparison.Ordinal))
        {
            string lEncodingSubfolder = LEncodingFolderNormalize(lEncoding.LEncodingLocationFolder);
            return string.IsNullOrEmpty(lEncodingSubfolder)
                ? lEncodingSourceFolder
                : Path.Combine(lEncodingSourceFolder, lEncodingSubfolder);
        }

        if (string.Equals(lEncoding.LEncodingLocation, "Sibling", StringComparison.Ordinal))
        {
            string lEncodingSiblingFolder = LEncodingFolderNormalize(lEncoding.LEncodingLocationFolder);
            if (string.IsNullOrEmpty(lEncodingSiblingFolder))
            {
                return lEncodingSourceFolder;
            }

            string lEncodingParentFolder = Path.GetDirectoryName(lEncodingSourceFolder) ?? lEncodingSourceFolder;
            return Path.Combine(lEncodingParentFolder, lEncodingSiblingFolder);
        }

        return lEncodingSourceFolder;
    }

    public static string LEncodingExtensionResolve(LEncoding lEncoding, string lEncodingSourcePath)
    {
        if (string.Equals(lEncoding.LEncodingContainer, "Same as source", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetExtension(lEncodingSourcePath).TrimStart('.');
        }

        return lEncoding.LEncodingExtension.TrimStart('.');
    }

    public static string LEncodingTimeFormat(TimeSpan lEncodingSpan)
    {
        TimeSpan lEncodingClamped = lEncodingSpan < TimeSpan.Zero ? TimeSpan.Zero : lEncodingSpan;
        int lEncodingHours = (int)lEncodingClamped.TotalHours;
        return string.Concat(
            lEncodingHours.ToString("D2", CultureInfo.InvariantCulture),
            "-",
            lEncodingClamped.Minutes.ToString("D2", CultureInfo.InvariantCulture),
            "-",
            lEncodingClamped.Seconds.ToString("D2", CultureInfo.InvariantCulture),
            ".",
            lEncodingClamped.Milliseconds.ToString("D3", CultureInfo.InvariantCulture));
    }

    public static string LEncodingShorten(string lEncodingStem) =>
        LEncodingNameFormat(lEncodingStem, _ => null);

    public static string LEncodingNameFormat(string lEncodingPattern, Func<string, string?> lEncodingValueRead)
    {
        if (string.IsNullOrEmpty(lEncodingPattern) || lEncodingPattern.IndexOf('{') < 0)
        {
            return lEncodingPattern;
        }

        var lEncodingText = new System.Text.StringBuilder();
        var lEncodingOperators = new List<(int lEncodingOffset, bool lEncodingForward, int lEncodingCount)>();
        int lEncodingIndex = 0;
        while (lEncodingIndex < lEncodingPattern.Length)
        {
            int lEncodingOpen = lEncodingPattern.IndexOf('{', lEncodingIndex);
            if (lEncodingOpen < 0)
            {
                lEncodingText.Append(lEncodingPattern, lEncodingIndex, lEncodingPattern.Length - lEncodingIndex);
                break;
            }

            int lEncodingClose = lEncodingPattern.IndexOf('}', lEncodingOpen + 1);
            if (lEncodingClose < 0)
            {
                lEncodingText.Append(lEncodingPattern, lEncodingIndex, lEncodingPattern.Length - lEncodingIndex);
                break;
            }

            lEncodingText.Append(lEncodingPattern, lEncodingIndex, lEncodingOpen - lEncodingIndex);
            string lEncodingMarker = lEncodingPattern[(lEncodingOpen + 1)..lEncodingClose];
            if (LEncodingOperatorParse(lEncodingMarker, out bool lEncodingForward, out int lEncodingCount))
            {
                lEncodingOperators.Add((lEncodingText.Length, lEncodingForward, lEncodingCount));
            }
            else if (lEncodingValueRead(lEncodingMarker) is { } lEncodingValue)
            {
                lEncodingText.Append(lEncodingValue);
            }
            else
            {
                lEncodingText.Append(lEncodingPattern, lEncodingOpen, lEncodingClose - lEncodingOpen + 1);
            }

            lEncodingIndex = lEncodingClose + 1;
        }

        string lEncodingResolved = lEncodingText.ToString();
        if (lEncodingOperators.Count == 0)
        {
            return lEncodingResolved;
        }

        var lEncodingDeleted = new bool[lEncodingResolved.Length];
        foreach ((int lEncodingOffset, bool lEncodingForward, int lEncodingCount) in lEncodingOperators)
        {
            int lEncodingRemaining = lEncodingCount;
            int lEncodingPosition = lEncodingForward ? lEncodingOffset : lEncodingOffset - 1;
            int lEncodingStep = lEncodingForward ? 1 : -1;
            while (lEncodingRemaining > 0 && lEncodingPosition >= 0 && lEncodingPosition < lEncodingDeleted.Length)
            {
                if (!lEncodingDeleted[lEncodingPosition])
                {
                    lEncodingDeleted[lEncodingPosition] = true;
                    lEncodingRemaining--;
                }

                lEncodingPosition += lEncodingStep;
            }
        }

        var lEncodingResult = new System.Text.StringBuilder(lEncodingResolved.Length);
        for (int lEncodingChar = 0; lEncodingChar < lEncodingResolved.Length; lEncodingChar++)
        {
            if (!lEncodingDeleted[lEncodingChar])
            {
                lEncodingResult.Append(lEncodingResolved[lEncodingChar]);
            }
        }

        return lEncodingResult.Length == 0 ? lEncodingResolved : lEncodingResult.ToString();
    }

    public static string LEncodingNameNormalize(string lEncodingName)
    {
        char[] lEncodingInvalidChars = Path.GetInvalidFileNameChars();
        var lEncodingBuilder = new System.Text.StringBuilder(lEncodingName.Length);
        foreach (char lEncodingChar in lEncodingName)
        {
            lEncodingBuilder.Append(Array.IndexOf(lEncodingInvalidChars, lEncodingChar) >= 0 ? '_' : lEncodingChar);
        }

        string lEncodingTrimmed = lEncodingBuilder.ToString().Trim();
        return lEncodingTrimmed.Length == 0 ? "output" : lEncodingTrimmed;
    }

    public static string LEncodingNameClaim(
        ISet<string> lEncodingTakenPaths,
        string lEncodingFolder,
        string lEncodingBaseName,
        string lEncodingExtension)
    {
        int lEncodingAttempt = 1;
        while (true)
        {
            string lEncodingUniqueName = lEncodingAttempt == 1
                ? lEncodingBaseName
                : $"{lEncodingBaseName}_{lEncodingAttempt}";
            string lEncodingFileName = string.IsNullOrWhiteSpace(lEncodingExtension)
                ? lEncodingUniqueName
                : $"{lEncodingUniqueName}.{lEncodingExtension}";

            if (lEncodingTakenPaths.Add(LEncodingPathResolve(Path.Combine(lEncodingFolder, lEncodingFileName))))
            {
                return lEncodingFileName;
            }

            lEncodingAttempt++;
        }
    }

    private static string LEncodingPathResolve(string lEncodingPath)
    {
        try
        {
            return Path.GetFullPath(lEncodingPath).ToUpperInvariant();
        }
        catch (Exception lEncodingError)
            when (lEncodingError is ArgumentException
                or IOException
                or NotSupportedException
                or System.Security.SecurityException)
        {
            return lEncodingPath.ToUpperInvariant();
        }
    }

    private static bool LEncodingOperatorParse(
        string lEncodingMarker,
        out bool lEncodingForward,
        out int lEncodingCount)
    {
        lEncodingForward = false;
        lEncodingCount = 1;
        int lEncodingColon = lEncodingMarker.IndexOf(':');
        string lEncodingName = lEncodingColon < 0 ? lEncodingMarker : lEncodingMarker[..lEncodingColon];
        if (lEncodingName.Equals("Delete", StringComparison.OrdinalIgnoreCase))
        {
            lEncodingForward = true;
        }
        else if (!lEncodingName.Equals("Backspace", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (lEncodingColon >= 0
            && int.TryParse(lEncodingMarker[(lEncodingColon + 1)..], out int lEncodingParsed)
            && lEncodingParsed > 0)
        {
            lEncodingCount = lEncodingParsed;
        }

        return true;
    }

    private static string LEncodingFolderNormalize(string lEncodingFolderName)
    {
        char[] lEncodingInvalidChars = Path.GetInvalidFileNameChars()
            .Where(lEncodingChar => lEncodingChar != Path.DirectorySeparatorChar
                && lEncodingChar != Path.AltDirectorySeparatorChar)
            .ToArray();

        string lEncodingCleaned = new(lEncodingFolderName
            .Trim()
            .Select(lEncodingChar => lEncodingInvalidChars.Contains(lEncodingChar) ? '_' : lEncodingChar)
            .ToArray());

        return lEncodingCleaned.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
