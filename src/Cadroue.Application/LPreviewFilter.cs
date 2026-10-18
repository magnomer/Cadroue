using Cadroue.Core;

namespace Cadroue.Application;

// The preview state written as one lavfi filter chain: flips, rotation, white balance, exposure, the eq terms and the
// curve, in that order; empty when nothing departs from neutral.
public static class LPreviewFilter
{
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
        if (lColor.LColorWhitebalance is { } lWhitebalance)
        {
            lFilters.AddRange(LWorkVideoFilter.LWorkWhitebalanceFormat(lWhitebalance));
        }

        if (lColor.LColorExposure != 0)
        {
            lFilters.Add("exposure=exposure=" + LPreviewFilterFormat(lColor.LColorExposure));
        }

        var lEq = new List<string>();
        double lBrightness = lColor.LColorBrightness / LColor.LColorBrightnessFactor;
        if (lBrightness != 0)
        {
            lEq.Add("brightness=" + LPreviewFilterFormat(lBrightness));
        }

        if (lColor.LColorContrast != 1)
        {
            lEq.Add("contrast=" + LPreviewFilterFormat(lColor.LColorContrast));
        }

        if (lColor.LColorGamma != 1)
        {
            lEq.Add("gamma=" + LPreviewFilterFormat(lColor.LColorGamma));
        }

        if (lColor.LColorGammaRed != 1)
        {
            lEq.Add("gamma_r=" + LPreviewFilterFormat(lColor.LColorGammaRed));
        }

        if (lColor.LColorGammaGreen != 1)
        {
            lEq.Add("gamma_g=" + LPreviewFilterFormat(lColor.LColorGammaGreen));
        }

        if (lColor.LColorGammaBlue != 1)
        {
            lEq.Add("gamma_b=" + LPreviewFilterFormat(lColor.LColorGammaBlue));
        }

        if (lColor.LColorHighlightProtection != 0)
        {
            lEq.Add("gamma_weight=" + LPreviewFilterFormat(1 - lColor.LColorHighlightProtection / 100d));
        }

        if (lColor.LColorSaturation != 1)
        {
            lEq.Add("saturation=" + LPreviewFilterFormat(lColor.LColorSaturation));
        }

        if (lEq.Count > 0)
        {
            lFilters.Add("eq=" + string.Join(':', lEq));
        }

        if (lColor.LColorCurve is { } lCurve)
        {
            string lCurveFilter = LWorkVideoFilter.LWorkCurveFormat(lCurve);
            if (lCurveFilter.Length > 0)
            {
                lFilters.Add(lCurveFilter);
            }
        }

        return lFilters.Count > 0 ? "lavfi=[" + string.Join(',', lFilters) + "]" : string.Empty;
    }

    private static string LPreviewFilterFormat(double lValue) =>
        lValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}
