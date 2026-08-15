using System.IO;

namespace Cadroue.Core;

public sealed record LEncoding(
    string LEncodingNamePattern,
    string LEncodingContainer,
    string LEncodingExtension,
    string LEncodingLocation,
    string LEncodingLocationFolder,
    string LEncodingExportMode,
    LEncodingVideo LEncodingVideo,
    LEncodingAudio LEncodingAudio,
    string LEncodingPresetName,
    string LEncodingCollision,
    string LEncodingCollisionSuffix)
{
    public string LEncodingFolderRead(string lEncodingSourcePath)
    {
        string lEncodingSourceFolder = Path.GetDirectoryName(lEncodingSourcePath) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(LEncodingLocationFolder))
        {
            return lEncodingSourceFolder;
        }

        if (string.Equals(LEncodingLocation, "Custom location", StringComparison.Ordinal)
            || string.Equals(LEncodingLocation, "Custom folder", StringComparison.Ordinal))
        {
            return LEncodingLocationFolder;
        }

        if (string.Equals(LEncodingLocation, "Subfolder", StringComparison.Ordinal))
        {
            string lEncodingSubfolder = LEncodingFolderNormalize(LEncodingLocationFolder);
            return string.IsNullOrEmpty(lEncodingSubfolder)
                ? lEncodingSourceFolder
                : Path.Combine(lEncodingSourceFolder, lEncodingSubfolder);
        }

        if (string.Equals(LEncodingLocation, "Sibling", StringComparison.Ordinal))
        {
            string lEncodingSiblingFolder = LEncodingFolderNormalize(LEncodingLocationFolder);
            if (string.IsNullOrEmpty(lEncodingSiblingFolder))
            {
                return lEncodingSourceFolder;
            }

            string lEncodingParentFolder = Path.GetDirectoryName(lEncodingSourceFolder) ?? lEncodingSourceFolder;
            return Path.Combine(lEncodingParentFolder, lEncodingSiblingFolder);
        }

        return lEncodingSourceFolder;
    }

    public string LEncodingExtensionResolve(string lEncodingSourcePath)
    {
        if (string.Equals(LEncodingContainer, "Same as source", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetExtension(lEncodingSourcePath).TrimStart('.');
        }

        return LEncodingExtension.TrimStart('.');
    }

    public static string LEncodingShorten(string lEncodingStem)
    {
        if (string.IsNullOrEmpty(lEncodingStem) || lEncodingStem.IndexOf('{') < 0)
        {
            return lEncodingStem;
        }

        var lEncodingText = new System.Text.StringBuilder();
        var lEncodingOperators = new List<(int lEncodingOffset, bool lEncodingForward, int lEncodingCount)>();
        int lEncodingIndex = 0;
        while (lEncodingIndex < lEncodingStem.Length)
        {
            int lEncodingOpen = lEncodingStem.IndexOf('{', lEncodingIndex);
            if (lEncodingOpen < 0)
            {
                lEncodingText.Append(lEncodingStem, lEncodingIndex, lEncodingStem.Length - lEncodingIndex);
                break;
            }

            int lEncodingClose = lEncodingStem.IndexOf('}', lEncodingOpen + 1);
            if (lEncodingClose < 0)
            {
                lEncodingText.Append(lEncodingStem, lEncodingIndex, lEncodingStem.Length - lEncodingIndex);
                break;
            }

            lEncodingText.Append(lEncodingStem, lEncodingIndex, lEncodingOpen - lEncodingIndex);
            string lEncodingMarker = lEncodingStem[(lEncodingOpen + 1)..lEncodingClose];
            if (LEncodingOperatorParse(lEncodingMarker, out bool lEncodingForward, out int lEncodingCount))
            {
                lEncodingOperators.Add((lEncodingText.Length, lEncodingForward, lEncodingCount));
            }
            else
            {
                lEncodingText.Append(lEncodingStem, lEncodingOpen, lEncodingClose - lEncodingOpen + 1);
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

    private static bool LEncodingOperatorParse(string lEncodingMarker, out bool lEncodingForward, out int lEncodingCount)
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

        if (lEncodingColon >= 0 && int.TryParse(lEncodingMarker[(lEncodingColon + 1)..], out int lEncodingParsed) && lEncodingParsed > 0)
        {
            lEncodingCount = lEncodingParsed;
        }

        return true;
    }

    private static string LEncodingFolderNormalize(string lEncodingFolderName)
    {
        char[] lEncodingInvalidChars = Path.GetInvalidFileNameChars()
            .Where(lEncodingChar => lEncodingChar != Path.DirectorySeparatorChar && lEncodingChar != Path.AltDirectorySeparatorChar)
            .ToArray();

        string lEncodingCleaned = new(lEncodingFolderName
            .Trim()
            .Select(lEncodingChar => lEncodingInvalidChars.Contains(lEncodingChar) ? '_' : lEncodingChar)
            .ToArray());

        return lEncodingCleaned.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}

public sealed record LEncodingVideo(
    string LEncodingStream,
    string LEncodingMode,
    string LEncodingEncoder,
    string LEncodingRateControl,
    string LEncodingQuality,
    string LEncodingSpeedPreset,
    string LEncodingSize,
    bool LEncodingSizeReactive,
    string LEncodingFps,
    string LEncodingPixel,
    IReadOnlyDictionary<string, string> LEncodingExtras);

public sealed record LEncodingAudio(
    string LEncodingStream,
    string LEncodingMode,
    string LEncodingEncoder,
    string LEncodingRateControl,
    string LEncodingQuality,
    string LEncodingSpeed,
    IReadOnlyDictionary<string, string> LEncodingExtras,
    string LEncodingSampleRate,
    string LEncodingChannels);
