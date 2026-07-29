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
    string LWorkOutputAudioBitrate,
    string LWorkOutputAudioSampleRate,
    string LWorkOutputAudioChannels)
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
    double LWorkMediaFrameRate,
    long LWorkMediaDurationMilliseconds,
    bool LWorkMediaVideoPresent)
{
    public TimeSpan LWorkMediaDuration => TimeSpan.FromMilliseconds(LWorkMediaDurationMilliseconds);
}

public sealed record LWorkCrop(
    int LWorkCropLeft,
    int LWorkCropTop,
    int LWorkCropRight,
    int LWorkCropBottom,
    int LWorkCropRotation,
    bool LWorkCropFlipHorizontal,
    bool LWorkCropFlipVertical)
{
    public static LWorkCrop LWorkCropNoneCreate() => new(0, 0, 0, 0, 0, false, false);

    public bool LWorkCropEdgeActive =>
        LWorkCropLeft > 0 || LWorkCropTop > 0 || LWorkCropRight > 0 || LWorkCropBottom > 0;

    public bool LWorkCropActive =>
        LWorkCropEdgeActive
        || LWorkCropRotation != 0
        || LWorkCropFlipHorizontal
        || LWorkCropFlipVertical;
}

public enum LWorkVideoKind
{
    LWorkVideoKindBrightness,
    LWorkVideoKindContrast
}

public sealed record LWorkVideoStep(
    LWorkVideoKind LWorkVideoStepKind,
    bool LWorkVideoStepActive,
    double LWorkVideoStepValue)
{
    public static LWorkVideoStep LWorkVideoBrightnessCreate(bool lStepActive, double lStepValue) =>
        new(LWorkVideoKind.LWorkVideoKindBrightness, lStepActive, lStepValue);

    public static LWorkVideoStep LWorkVideoContrastCreate(bool lStepActive, double lStepValue) =>
        new(LWorkVideoKind.LWorkVideoKindContrast, lStepActive, Math.Clamp(lStepValue, 0, 200));

    public double LWorkVideoFfmpegValue => LWorkVideoStepKind switch
    {
        LWorkVideoKind.LWorkVideoKindBrightness => Math.Clamp(LWorkVideoStepValue * 0.0025d, -1, 1),
        _ => LWorkVideoStepValue / 100d
    };
}

public sealed record LWorkVideo(IReadOnlyList<LWorkVideoStep> LWorkVideoSteps)
{
    public static LWorkVideo LWorkVideoNoneCreate() => new(Array.Empty<LWorkVideoStep>());

    public bool LWorkVideoActive => LWorkVideoSteps.Any(lStep => lStep.LWorkVideoStepActive);
}
