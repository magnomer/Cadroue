using Cadroue.Core;

namespace Cadroue.Application;

public sealed record LColor(
    double LColorBrightness,
    double LColorContrast,
    double LColorSaturation,
    double LColorHue)
{
    public const double LColorBrightnessFactor = 2.5;

    public double LColorGamma { get; init; } = 1;

    public double LColorGammaRed { get; init; } = 1;

    public double LColorGammaGreen { get; init; } = 1;

    public double LColorGammaBlue { get; init; } = 1;

    public double LColorHighlightProtection { get; init; }

    public double LColorExposure { get; init; }

    public LWorkWhitebalanceSettings? LColorWhitebalance { get; init; }

    public LWorkCurveSettings? LColorCurve { get; init; }

    public static LColor LColorDefaultCreate()
    {
        return new LColor(0, 1, 1, 0);
    }

    public static LColor LColorVideoResolve(LWorkVideo lVideo)
    {
        LWorkVideoStep? lBrightnessStep = lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindBrightness
                && lStep.LWorkStepActive);
        double lBrightness = (lBrightnessStep is null ? 0 : LWorkVideoFilter.LWorkFfmpegResolve(lBrightnessStep))
            * LColorBrightnessFactor;
        LWorkVideoStep? lContrastStep = lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindContrast
                && lStep.LWorkStepActive);
        double lContrast = lContrastStep is null ? 1 : LWorkVideoFilter.LWorkFfmpegResolve(lContrastStep);
        LWorkVideoStep? lSaturationStep = lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindSaturation
                && lStep.LWorkStepActive);
        double lSaturation = lSaturationStep is null ? 1 : LWorkVideoFilter.LWorkFfmpegResolve(lSaturationStep);
        LWorkVideoStep? lGammaStep = lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindGamma
                && lStep.LWorkStepActive);
        LWorkGammaSettings? lGamma = lGammaStep?.LWorkGammaRead();
        LWorkVideoStep? lWhitebalanceStep = lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindWhitebalance
                && lStep.LWorkStepActive);
        LWorkVideoStep? lExposureStep = lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindExposure
                && lStep.LWorkStepActive);
        double lExposure = lExposureStep is null ? 0 : LWorkVideoFilter.LWorkFfmpegResolve(lExposureStep);
        LWorkVideoStep? lCurveStep = lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindCurve
                && lStep.LWorkStepActive);
        return new LColor(lBrightness, lContrast, lSaturation, 0)
        {
            LColorGamma = lGamma is null ? 1 : LWorkVideoFilter.LWorkGammaResolve(lGamma.LWorkGammaGlobal),
            LColorGammaRed = lGamma is null ? 1 : LWorkVideoFilter.LWorkGammaResolve(lGamma.LWorkGammaRed),
            LColorGammaGreen = lGamma is null ? 1 : LWorkVideoFilter.LWorkGammaResolve(lGamma.LWorkGammaGreen),
            LColorGammaBlue = lGamma is null ? 1 : LWorkVideoFilter.LWorkGammaResolve(lGamma.LWorkGammaBlue),
            LColorHighlightProtection = lGamma?.LWorkGammaHighlight ?? 0,
            LColorExposure = lExposure,
            LColorWhitebalance = lWhitebalanceStep?.LWorkWhitebalanceRead(),
            LColorCurve = lCurveStep?.LWorkCurveRead()
        };
    }
}

public enum LRotateKind
{
    LRotateNone,
    LRotate90,
    LRotate180,
    LRotate270
}

public sealed record LRotateFlip(LRotateKind LRotateKind, bool LRotateFlipHorizontal, bool LRotateFlipVertical)
{
    public static LRotateFlip LRotateDefaultCreate()
    {
        return new LRotateFlip(LRotateKind.LRotateNone, false, false);
    }

    public static LRotateFlip LRotateCropResolve(LWorkCrop lCrop) => new(
        lCrop.LWorkCropRotation switch
        {
            90 => LRotateKind.LRotate90,
            180 => LRotateKind.LRotate180,
            270 => LRotateKind.LRotate270,
            _ => LRotateKind.LRotateNone
        },
        lCrop.LWorkFlipHorizontal,
        lCrop.LWorkFlipVertical);
}

public sealed record LPlaybackState(bool LPlaybackStatePlaying, TimeSpan LPlaybackPosition)
{
    public static LPlaybackState LPlaybackStoppedCreate()
    {
        return new LPlaybackState(false, TimeSpan.Zero);
    }
}

public sealed record LPreviewState(
    LCropbox? LCropbox,
    LColor LColor,
    LRotateFlip LRotateFlip,
    LPlaybackState LPlaybackState)
{
    public static LPreviewState LPreviewDefaultCreate()
    {
        return new LPreviewState(
            null,
            LColor.LColorDefaultCreate(),
            LRotateFlip.LRotateDefaultCreate(),
            LPlaybackState.LPlaybackStoppedCreate());
    }

    public LPreviewState LCropboxChange(LCropbox? lCropbox)
    {
        return this with { LCropbox = lCropbox };
    }

    public LPreviewState LColorChange(LColor lColor)
    {
        return this with { LColor = lColor };
    }

    public LPreviewState LRotateFlipChange(LRotateFlip lRotateFlip)
    {
        return this with { LRotateFlip = lRotateFlip };
    }

    public LPreviewState LPlaybackStateChange(LPlaybackState lPlaybackState)
    {
        return this with { LPlaybackState = lPlaybackState };
    }
}
