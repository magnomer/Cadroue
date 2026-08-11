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
    LColorKindContrast,
    LColorKindGamma
}

public sealed record LWorkGammaSettings(
    double LWorkGammaGlobal,
    double LWorkGammaRed,
    double LWorkGammaGreen,
    double LWorkGammaBlue,
    double LWorkGammaHighlightProtection);

public sealed record LWorkVideoStep(
    LColorKind LWorkStepKind,
    bool LWorkStepActive,
    double LWorkStepValue)
{
    public LWorkGammaSettings? LWorkStepGamma { get; init; }

    public static LWorkVideoStep LWorkBrightnessCreate(bool lStepActive, double lStepValue) =>
        new(LColorKind.LColorKindBrightness, lStepActive, lStepValue);

    public static LWorkVideoStep LWorkContrastCreate(bool lStepActive, double lStepValue) =>
        new(LColorKind.LColorKindContrast, lStepActive, Math.Clamp(lStepValue, 0, 200));

    public static LWorkVideoStep LWorkGammaCreate(
        bool lStepActive,
        double lStepGlobal,
        double lStepRed = 0,
        double lStepGreen = 0,
        double lStepBlue = 0,
        double lStepHighlightProtection = 0)
    {
        double lGlobal = Math.Clamp(lStepGlobal, -100, 100);
        return new(LColorKind.LColorKindGamma, lStepActive, lGlobal)
        {
            LWorkStepGamma = new LWorkGammaSettings(
                lGlobal,
                Math.Clamp(lStepRed, -100, 100),
                Math.Clamp(lStepGreen, -100, 100),
                Math.Clamp(lStepBlue, -100, 100),
                Math.Clamp(lStepHighlightProtection, 0, 100))
        };
    }

    public LWorkGammaSettings LWorkGammaRead() =>
        LWorkStepKind == LColorKind.LColorKindGamma && LWorkStepGamma is { } lGamma
            ? lGamma
            : new LWorkGammaSettings(
                Math.Clamp(LWorkStepValue, -100, 100), 0, 0, 0, 0);

    public string LWorkDiagnosticRead()
    {
        string lSummary = $"{LWorkStepKind} {LWorkStepValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}";
        if (LWorkStepKind != LColorKind.LColorKindGamma)
        {
            return lSummary;
        }

        LWorkGammaSettings lGamma = LWorkGammaRead();
        var lDetails = new List<string>();
        if (lGamma.LWorkGammaRed != 0)
        {
            lDetails.Add($"red {lGamma.LWorkGammaRed.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        if (lGamma.LWorkGammaGreen != 0)
        {
            lDetails.Add($"green {lGamma.LWorkGammaGreen.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        if (lGamma.LWorkGammaBlue != 0)
        {
            lDetails.Add($"blue {lGamma.LWorkGammaBlue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        if (lGamma.LWorkGammaHighlightProtection != 0)
        {
            lDetails.Add($"highlight {lGamma.LWorkGammaHighlightProtection.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        return lDetails.Count == 0 ? lSummary : $"{lSummary} ({string.Join(", ", lDetails)})";
    }

    public double LWorkFfmpegValue => LWorkStepKind switch
    {
        LColorKind.LColorKindBrightness => Math.Clamp(LWorkStepValue * 0.005d, -1, 1),
        LColorKind.LColorKindGamma => LWorkGammaFactorRead(LWorkStepValue),
        _ => LWorkStepValue / 100d
    };

    public static double LWorkGammaFactorRead(double lStepValue) =>
        Math.Pow(10d, Math.Clamp(lStepValue, -100, 100) / 100d);
}

public sealed record LWorkVideo(IReadOnlyList<LWorkVideoStep> LWorkVideoSteps)
{
    public static LWorkVideo LWorkVideoCreate() => new(Array.Empty<LWorkVideoStep>());

    public bool LWorkVideoActive => LWorkVideoSteps.Any(lStep => lStep.LWorkStepActive);
}
