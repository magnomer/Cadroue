using Cadroue.Core;

namespace Cadroue.Application;

public sealed record LPreviewApplication(
    int LPreviewBrightness,
    int LPreviewContrast,
    int LPreviewSaturation,
    int LPreviewHue,
    uint LPreviewRotation,
    bool LPreviewFlipHorizontal,
    bool LPreviewFlipVertical,
    string LPreviewReason);

public sealed record LPreviewMpvEqualizer(
    int LPreviewMpvBrightness,
    int LPreviewMpvContrast,
    int LPreviewMpvSaturation,
    int LPreviewMpvHue,
    double LPreviewMpvGamma);

public static class LPreview
{
    public const double LPreviewBrightnessFactor = 2.5;
    public const double LPreviewGammaMaximum = 2;

    public static Action<object, LPreviewApplication>? LPreviewApplySeam;

    public static LColor LPreviewColorResolve(LWorkVideo lVideo)
    {
        double lBrightness = (lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindBrightness
                && lStep.LWorkStepActive)
            ?.LWorkFfmpegValue ?? 0) * LPreviewBrightnessFactor;
        double lContrast = lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindContrast
                && lStep.LWorkStepActive)
            ?.LWorkFfmpegValue ?? 1;
        LWorkVideoStep? lGammaStep = lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindGamma
                && lStep.LWorkStepActive);
        LWorkGammaSettings? lGamma = lGammaStep?.LWorkGammaRead();
        LWorkVideoStep? lWhitebalanceStep = lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindWhitebalance
                && lStep.LWorkStepActive);
        return new LColor(lBrightness, lContrast, 1, 0)
        {
            LColorGamma = lGamma is null ? 1 : LWorkVideoStep.LWorkGammaResolve(lGamma.LWorkGammaGlobal),
            LColorGammaRed = lGamma is null ? 1 : LWorkVideoStep.LWorkGammaResolve(lGamma.LWorkGammaRed),
            LColorGammaGreen = lGamma is null ? 1 : LWorkVideoStep.LWorkGammaResolve(lGamma.LWorkGammaGreen),
            LColorGammaBlue = lGamma is null ? 1 : LWorkVideoStep.LWorkGammaResolve(lGamma.LWorkGammaBlue),
            LColorHighlightProtection = lGamma?.LWorkGammaHighlight ?? 0,
            LColorWhitebalance = lWhitebalanceStep?.LWorkWhitebalanceRead()
        };
    }

    public static void LPreviewApply(object? lPreviewTarget, LPreviewState lPreviewState)
    {
        if (lPreviewTarget is null)
        {
            return;
        }

        LPreviewApplySeam?.Invoke(lPreviewTarget, LPreviewApplicationResolve(lPreviewState, "preview color/geometry"));
    }

    public static void LPreviewRestore(object? lPreviewTarget, LPreviewState lPreviewState)
    {
        if (lPreviewTarget is null)
        {
            return;
        }

        LPreviewApplySeam?.Invoke(lPreviewTarget, LPreviewApplicationResolve(lPreviewState, "preview restored"));
    }

    public static LPreviewMpvEqualizer LPreviewEqualizerResolve(LPreviewState lPreviewState)
    {
        LColor lColor = lPreviewState.LColor;
        double lContrast = lColor.LColorContrast;
        // FFmpeg eq pivots luma contrast at 0.5 and leaves chroma unchanged;
        // MPV applies contrast as a black-anchored gain to both luma and chroma.
        double lBrightness = lColor.LColorBrightness / LPreviewBrightnessFactor
            + (1 - lContrast) / 2;
        double lSaturation = lContrast == 0
            ? lColor.LColorSaturation
            : lColor.LColorSaturation / lContrast;
        return new LPreviewMpvEqualizer(
            LPreviewValueClamp(lBrightness * 100, -100, 100),
            LPreviewValueClamp((lContrast - 1) * 100, -100, 100),
            LPreviewValueClamp((lSaturation - 1) * 100, -100, 100),
            LPreviewValueClamp(lColor.LColorHue / 180 * 100, -100, 100),
            LPreviewGammaCheck(lColor) ? 1 : lColor.LColorGamma);
    }

    public static string LPreviewFilterResolve(LPreviewState lPreviewState)
    {
        var lFilters = new List<string>();

        LRotateFlip lRotateFlip = lPreviewState.LRotateFlip;

        if (lRotateFlip.LRotateFlipHorizontal)
        {
            lFilters.Add("hflip");
        }

        if (lRotateFlip.LRotateFlipVertical)
        {
            lFilters.Add("vflip");
        }

        string? lRotate = lRotateFlip.LRotateKind switch
        {
            LRotateKind.LRotate90 => "transpose=1",
            LRotateKind.LRotate180 => "transpose=1,transpose=1",
            LRotateKind.LRotate270 => "transpose=2",
            _ => null
        };

        if (lRotate is not null)
        {
            lFilters.Add(lRotate);
        }

        LColor lColor = lPreviewState.LColor;
        if (LPreviewGammaCheck(lColor))
        {
            double lGammaWeight = 1 - lColor.LColorHighlightProtection / 100d;
            lFilters.Add(
                "lutyuv=y=" + LPreviewLutFormat(
                    lColor.LColorGamma * lColor.LColorGammaGreen,
                    lGammaWeight)
                + ":u=" + LPreviewLutFormat(
                    Math.Sqrt(lColor.LColorGammaBlue / lColor.LColorGammaGreen),
                    lGammaWeight)
                + ":v=" + LPreviewLutFormat(
                    Math.Sqrt(lColor.LColorGammaRed / lColor.LColorGammaGreen),
                    lGammaWeight));
        }

        if (lColor.LColorWhitebalance is { } lWhitebalance)
        {
            lFilters.AddRange(lWhitebalance.LWorkWhitebalanceFormat());
        }

        return lFilters.Count > 0 ? "lavfi=[" + string.Join(',', lFilters) + "]" : string.Empty;
    }

    public static bool LPreviewGammaCheck(LColor lColor) =>
        lColor.LColorGammaAdvanced
        || lColor.LColorGamma > LPreviewGammaMaximum;

    private static LPreviewApplication LPreviewApplicationResolve(LPreviewState lPreviewState, string lPreviewReason)
    {
        LColor lColor = lPreviewState.LColor;
        LRotateFlip lRotateFlip = lPreviewState.LRotateFlip;
        return new LPreviewApplication(
            LPreviewValueClamp(lColor.LColorBrightness * 100, -100, 100),
            LPreviewValueClamp((lColor.LColorContrast - 1) * 100, -100, 100),
            LPreviewValueClamp((lColor.LColorSaturation - 1) * 100, -100, 100),
            LPreviewValueClamp(lColor.LColorHue, -180, 180),
            LPreviewRotationRead(lRotateFlip.LRotateKind),
            lRotateFlip.LRotateFlipHorizontal,
            lRotateFlip.LRotateFlipVertical,
            lPreviewReason);
    }

    private static uint LPreviewRotationRead(LRotateKind lRotateKind)
    {
        return lRotateKind switch
        {
            LRotateKind.LRotate90 => 90u,
            LRotateKind.LRotate180 => 180u,
            LRotateKind.LRotate270 => 270u,
            _ => 0u
        };
    }

    private static int LPreviewValueClamp(double lPreviewValue, int lPreviewMinimum, int lPreviewMaximum)
    {
        return Math.Clamp((int)Math.Round(lPreviewValue), lPreviewMinimum, lPreviewMaximum);
    }

    private static string LPreviewLutFormat(double lGamma, double lGammaWeight)
    {
        string lLinearWeight = LPreviewNumberFormat(1 - lGammaWeight);
        string lCurveWeight = LPreviewNumberFormat(lGammaWeight);
        string lExponent = LPreviewNumberFormat(1 / lGamma);
        return $"'val*{lLinearWeight}+maxval*pow(val/maxval\\,{lExponent})*{lCurveWeight}'";
    }

    private static string LPreviewNumberFormat(double lValue) =>
        lValue.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
}
