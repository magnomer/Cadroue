using Cadroue.Core;

namespace Cadroue.Application;

public sealed record LColor(
    double LColorBrightness,
    double LColorContrast,
    double LColorSaturation,
    double LColorHue)
{
    public double LColorGamma { get; init; } = 1;

    public double LColorGammaRed { get; init; } = 1;

    public double LColorGammaGreen { get; init; } = 1;

    public double LColorGammaBlue { get; init; } = 1;

    public double LColorHighlightProtection { get; init; }

    public LWorkWhitebalanceSettings? LColorWhitebalance { get; init; }

    public bool LColorGammaAdvanced =>
        LColorGammaRed != 1
        || LColorGammaGreen != 1
        || LColorGammaBlue != 1
        || LColorHighlightProtection != 0;

    public static LColor LColorDefaultCreate()
    {
        return new LColor(0, 1, 1, 0);
    }

    public static LColorKind? LColorKindParse(string lKindToken) => lKindToken switch
    {
        "Contrast" => LColorKind.LColorKindContrast,
        "Brightness" => LColorKind.LColorKindBrightness,
        "Gamma" => LColorKind.LColorKindGamma,
        "Whitebalance" => LColorKind.LColorKindWhitebalance,
        _ => null
    };

    public static string LColorKindFormat(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindContrast => "Contrast",
        LColorKind.LColorKindGamma => "Gamma",
        LColorKind.LColorKindWhitebalance => "Whitebalance",
        _ => "Brightness"
    };
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
