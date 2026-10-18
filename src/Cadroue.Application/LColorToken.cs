using Cadroue.Core;

namespace Cadroue.Application;

// A colour kind and the token that names it in a sidecar step or a processing order; an unknown token reads as no
// kind and an unnamed kind writes as brightness.
public static class LColorToken
{
    public static LColorKind? LColorTokenParse(string lKindToken) => lKindToken switch
    {
        "Contrast" => LColorKind.LColorKindContrast,
        "Brightness" => LColorKind.LColorKindBrightness,
        "Gamma" => LColorKind.LColorKindGamma,
        "Whitebalance" => LColorKind.LColorKindWhitebalance,
        "Saturation" => LColorKind.LColorKindSaturation,
        "Exposure" => LColorKind.LColorKindExposure,
        "Curve" => LColorKind.LColorKindCurve,
        _ => null
    };

    public static string LColorTokenFormat(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindContrast => "Contrast",
        LColorKind.LColorKindGamma => "Gamma",
        LColorKind.LColorKindWhitebalance => "Whitebalance",
        LColorKind.LColorKindSaturation => "Saturation",
        LColorKind.LColorKindExposure => "Exposure",
        LColorKind.LColorKindCurve => "Curve",
        _ => "Brightness"
    };
}
