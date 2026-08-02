namespace Cadroue.Core;

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
