using System.IO;

namespace Cadroue.Core;

public sealed record LWorkOutput(
    string LWorkOutputNamePattern,
    string LWorkOutputContainer,
    string LWorkOutputExtension,
    string LWorkOutputLocation,
    string LWorkOutputLocationFolder,
    string LWorkOutputExportMode,
    string LWorkOutputVideoStream,
    string LWorkOutputVideoMode,
    string LWorkOutputVideoEncoder,
    string LWorkOutputRateControl,
    string LWorkOutputQuality,
    string LWorkOutputSpeedPreset,
    string LWorkOutputVideoSize,
    bool LWorkSizeReactive,
    string LWorkOutputVideoFps,
    string LWorkOutputPixelFormat,
    IReadOnlyDictionary<string, string> LWorkOutputVideoExtras,
    string LWorkOutputAudioStream,
    string LWorkOutputAudioMode,
    string LWorkOutputAudioEncoder,
    string LWorkOutputAudioRateControl,
    string LWorkOutputAudioQuality,
    string LWorkOutputAudioSpeed,
    IReadOnlyDictionary<string, string> LWorkOutputAudioExtras,
    string LWorkOutputAudioSampleRate,
    string LWorkOutputAudioChannels,
    string LWorkOutputPresetName,
    string LWorkOutputCollision,
    string LWorkOutputCollisionSuffix)
{
    public string LWorkFolderRead(string lWorkSourcePath)
    {
        string lWorkSourceFolder = Path.GetDirectoryName(lWorkSourcePath) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(LWorkOutputLocationFolder))
        {
            return lWorkSourceFolder;
        }

        if (string.Equals(LWorkOutputLocation, "Custom location", StringComparison.Ordinal)
            || string.Equals(LWorkOutputLocation, "Custom folder", StringComparison.Ordinal))
        {
            return LWorkOutputLocationFolder;
        }

        if (string.Equals(LWorkOutputLocation, "Subfolder", StringComparison.Ordinal))
        {
            string lWorkSubfolder = LWorkFolderNormalize(LWorkOutputLocationFolder);
            return string.IsNullOrEmpty(lWorkSubfolder)
                ? lWorkSourceFolder
                : Path.Combine(lWorkSourceFolder, lWorkSubfolder);
        }

        if (string.Equals(LWorkOutputLocation, "Sibling", StringComparison.Ordinal))
        {
            string lWorkSiblingFolder = LWorkFolderNormalize(LWorkOutputLocationFolder);
            if (string.IsNullOrEmpty(lWorkSiblingFolder))
            {
                return lWorkSourceFolder;
            }

            string lWorkParentFolder = Path.GetDirectoryName(lWorkSourceFolder) ?? lWorkSourceFolder;
            return Path.Combine(lWorkParentFolder, lWorkSiblingFolder);
        }

        return lWorkSourceFolder;
    }

    public string LWorkExtensionResolve(string lWorkSourcePath)
    {
        if (string.Equals(LWorkOutputContainer, "Same as source", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetExtension(lWorkSourcePath).TrimStart('.');
        }

        return LWorkOutputExtension.TrimStart('.');
    }

    public static string LWorkOutputShorten(string lWorkStem)
    {
        if (string.IsNullOrEmpty(lWorkStem) || lWorkStem.IndexOf('{') < 0)
        {
            return lWorkStem;
        }

        var lWorkText = new System.Text.StringBuilder();
        var lWorkOperators = new List<(int lWorkOffset, bool lWorkForward, int lWorkCount)>();
        int lWorkIndex = 0;
        while (lWorkIndex < lWorkStem.Length)
        {
            int lWorkOpen = lWorkStem.IndexOf('{', lWorkIndex);
            if (lWorkOpen < 0)
            {
                lWorkText.Append(lWorkStem, lWorkIndex, lWorkStem.Length - lWorkIndex);
                break;
            }

            int lWorkClose = lWorkStem.IndexOf('}', lWorkOpen + 1);
            if (lWorkClose < 0)
            {
                lWorkText.Append(lWorkStem, lWorkIndex, lWorkStem.Length - lWorkIndex);
                break;
            }

            lWorkText.Append(lWorkStem, lWorkIndex, lWorkOpen - lWorkIndex);
            string lWorkMarker = lWorkStem[(lWorkOpen + 1)..lWorkClose];
            if (LWorkOperatorParse(lWorkMarker, out bool lWorkForward, out int lWorkCount))
            {
                lWorkOperators.Add((lWorkText.Length, lWorkForward, lWorkCount));
            }
            else
            {
                lWorkText.Append(lWorkStem, lWorkOpen, lWorkClose - lWorkOpen + 1);
            }

            lWorkIndex = lWorkClose + 1;
        }

        string lWorkResolved = lWorkText.ToString();
        if (lWorkOperators.Count == 0)
        {
            return lWorkResolved;
        }

        var lWorkDeleted = new bool[lWorkResolved.Length];
        foreach ((int lWorkOffset, bool lWorkForward, int lWorkCount) in lWorkOperators)
        {
            int lWorkRemaining = lWorkCount;
            int lWorkPosition = lWorkForward ? lWorkOffset : lWorkOffset - 1;
            int lWorkStep = lWorkForward ? 1 : -1;
            while (lWorkRemaining > 0 && lWorkPosition >= 0 && lWorkPosition < lWorkDeleted.Length)
            {
                if (!lWorkDeleted[lWorkPosition])
                {
                    lWorkDeleted[lWorkPosition] = true;
                    lWorkRemaining--;
                }

                lWorkPosition += lWorkStep;
            }
        }

        var lWorkResult = new System.Text.StringBuilder(lWorkResolved.Length);
        for (int lWorkChar = 0; lWorkChar < lWorkResolved.Length; lWorkChar++)
        {
            if (!lWorkDeleted[lWorkChar])
            {
                lWorkResult.Append(lWorkResolved[lWorkChar]);
            }
        }

        return lWorkResult.Length == 0 ? lWorkResolved : lWorkResult.ToString();
    }

    private static bool LWorkOperatorParse(string lWorkMarker, out bool lWorkForward, out int lWorkCount)
    {
        lWorkForward = false;
        lWorkCount = 1;
        int lWorkColon = lWorkMarker.IndexOf(':');
        string lWorkName = lWorkColon < 0 ? lWorkMarker : lWorkMarker[..lWorkColon];
        if (lWorkName.Equals("Delete", StringComparison.OrdinalIgnoreCase))
        {
            lWorkForward = true;
        }
        else if (!lWorkName.Equals("Backspace", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (lWorkColon >= 0 && int.TryParse(lWorkMarker[(lWorkColon + 1)..], out int lWorkParsed) && lWorkParsed > 0)
        {
            lWorkCount = lWorkParsed;
        }

        return true;
    }

    private static string LWorkFolderNormalize(string lWorkFolderName)
    {
        char[] lWorkInvalidChars = Path.GetInvalidFileNameChars()
            .Where(lWorkChar => lWorkChar != Path.DirectorySeparatorChar && lWorkChar != Path.AltDirectorySeparatorChar)
            .ToArray();

        string lWorkCleaned = new(lWorkFolderName
            .Trim()
            .Select(lWorkChar => lWorkInvalidChars.Contains(lWorkChar) ? '_' : lWorkChar)
            .ToArray());

        return lWorkCleaned.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}

public sealed record LWorkMedia(
    int LWorkMediaWidth,
    int LWorkMediaHeight,
    double LWorkMediaFramerate,
    long LWorkMediaMilliseconds,
    bool LWorkMediaVideo)
{
    public TimeSpan LWorkMediaDuration => TimeSpan.FromMilliseconds(LWorkMediaMilliseconds);

    public double? LWorkKeyframeInterval { get; init; }

    public string LWorkMediaCodec { get; init; } = "";

    public int LWorkMediaBitrate { get; init; }

    public int LWorkMediaSamplerate { get; init; }
}

public sealed record LWorkCrop(
    int LWorkCropLeft,
    int LWorkCropTop,
    int LWorkCropRight,
    int LWorkCropBottom,
    int LWorkCropRotation,
    bool LWorkFlipHorizontal,
    bool LWorkFlipVertical)
{
    public static LWorkCrop LWorkCropCreate() => new(0, 0, 0, 0, 0, false, false);

    public bool LWorkEdgeActive =>
        LWorkCropLeft > 0 || LWorkCropTop > 0 || LWorkCropRight > 0 || LWorkCropBottom > 0;

    public bool LWorkCropActive =>
        LWorkEdgeActive
        || LWorkCropRotation != 0
        || LWorkFlipHorizontal
        || LWorkFlipVertical;
}

public enum LColorKind
{
    LColorKindBrightness,
    LColorKindContrast
}

public sealed record LWorkVideoStep(
    LColorKind LWorkStepKind,
    bool LWorkStepActive,
    double LWorkStepValue)
{
    public static LWorkVideoStep LWorkBrightnessCreate(bool lStepActive, double lStepValue) =>
        new(LColorKind.LColorKindBrightness, lStepActive, lStepValue);

    public static LWorkVideoStep LWorkContrastCreate(bool lStepActive, double lStepValue) =>
        new(LColorKind.LColorKindContrast, lStepActive, Math.Clamp(lStepValue, 0, 200));

    public double LWorkFfmpegValue => LWorkStepKind switch
    {
        LColorKind.LColorKindBrightness => Math.Clamp(LWorkStepValue * 0.0025d, -1, 1),
        _ => LWorkStepValue / 100d
    };
}

public sealed record LWorkVideo(IReadOnlyList<LWorkVideoStep> LWorkVideoSteps)
{
    public static LWorkVideo LWorkVideoCreate() => new(Array.Empty<LWorkVideoStep>());

    public bool LWorkVideoActive => LWorkVideoSteps.Any(lStep => lStep.LWorkStepActive);
}
