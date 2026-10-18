using System.Globalization;
using System.Linq;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LEncodeVideoFilter
{
    internal static void LEncodeFilterAppend(StringBuilder lArguments, LWorkItem lWorkItem, LEncoding lOutput)
    {
        var lFilters = new List<string>(
            LEncodeVideoGeometry.LEncodeGeometryRead(lWorkItem.LWorkCrop, lWorkItem.LWorkSourceMedia));

        bool lRgbDomain = LEncodeFiltersAppend(lFilters, lWorkItem.LWorkVideo);

        if (LEncodeVideoGeometry.LEncodeSizeCheck(lOutput.LEncodingVideo)
            && LEncodeVideoGeometry.LEncodeSizeRead(lOutput.LEncodingVideo.LEncodingSize) is { } lSize)
        {
            lFilters.Add(LEncodeVideoGeometry.LEncodeScaleResolve(lSize, lOutput.LEncodingVideo.LEncodingSizeReactive));
            lFilters.Add("setsar=1");
        }

        if (lRgbDomain)
        {
            lFilters.AddRange(LEncodeVideoColor.LEncodeColorNormalize(lWorkItem, lOutput));
        }

        if (lFilters.Count > 0)
        {
            lArguments.Append(
                CultureInfo.InvariantCulture, $" -vf {LEncode.LEncodeFormat(string.Join(',', lFilters))}");
        }

        string lFps = lOutput.LEncodingVideo.LEncodingFps?.Trim() ?? string.Empty;
        if (!LEncode.LEncodeSourceCheck(lFps) && LEncodeFpsCheck(lFps))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -r {lFps}");
        }

        string lPixel = LEncodeVideoPixel.LEncodePixelResolve(lWorkItem, lOutput.LEncodingVideo);
        if (lPixel.Length > 0)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -pix_fmt {LEncode.LEncodeValueFormat(lPixel)}");
        }
    }

    private static bool LEncodeFpsCheck(string lFps)
    {
        if (lFps.Length == 0)
        {
            return false;
        }

        if (double.TryParse(lFps, NumberStyles.Float, CultureInfo.InvariantCulture, out double lRate))
        {
            return double.IsFinite(lRate) && lRate > 0;
        }

        string[] lParts = lFps.Split('/');
        return lParts.Length == 2
            && int.TryParse(lParts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lNumerator)
            && int.TryParse(lParts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lDenominator)
            && lNumerator > 0 && lDenominator > 0;
    }

    private static bool LEncodeFiltersAppend(List<string> lFilters, LWorkVideo lWorkVideo)
    {
        bool lRgbDomain = false;
        var lEqParts = new List<string>();
        void LEncodeEqAppend()
        {
            if (lEqParts.Count == 0)
            {
                return;
            }

            lFilters.Add("eq=" + string.Join(':', lEqParts));
            lEqParts.Clear();
        }

        foreach (LWorkVideoStep lStep in lWorkVideo.LWorkVideoSteps)
        {
            if (!lStep.LWorkStepActive)
            {
                continue;
            }

            string lEncodeStepText =
                LWorkVideoFilter.LWorkFfmpegResolve(lStep).ToString("0.###", CultureInfo.InvariantCulture);
            switch (lStep.LWorkStepKind)
            {
                case LColorKind.LColorKindBrightness:
                    lEqParts.Add($"brightness={lEncodeStepText}");
                    break;
                case LColorKind.LColorKindContrast:
                    lEqParts.Add($"contrast={lEncodeStepText}");
                    break;
                case LColorKind.LColorKindSaturation:
                    lEqParts.Add($"saturation={lEncodeStepText}");
                    break;
                case LColorKind.LColorKindGamma:
                    LWorkGammaSettings lGamma = lStep.LWorkGammaRead();
                    Func<double, string> lGammaText =
                        lValue => LEncodeGammaFormat(LWorkVideoFilter.LWorkGammaResolve(lValue));
                    lEqParts.Add($"gamma={lGammaText(lGamma.LWorkGammaGlobal)}");
                    if (lGamma.LWorkGammaRed != 0)
                    {
                        lEqParts.Add($"gamma_r={lGammaText(lGamma.LWorkGammaRed)}");
                    }
                    if (lGamma.LWorkGammaGreen != 0)
                    {
                        lEqParts.Add($"gamma_g={lGammaText(lGamma.LWorkGammaGreen)}");
                    }
                    if (lGamma.LWorkGammaBlue != 0)
                    {
                        lEqParts.Add($"gamma_b={lGammaText(lGamma.LWorkGammaBlue)}");
                    }
                    if (lGamma.LWorkGammaHighlight != 0)
                    {
                        lEqParts.Add($"gamma_weight={LEncodeGammaFormat(1d - lGamma.LWorkGammaHighlight / 100d)}");
                    }
                    break;
                case LColorKind.LColorKindWhitebalance:
                    LEncodeEqAppend();
                    lFilters.AddRange(LWorkVideoFilter.LWorkWhitebalanceFormat(lStep.LWorkWhitebalanceRead()));
                    lRgbDomain = true;
                    break;
                case LColorKind.LColorKindExposure:
                    LEncodeEqAppend();
                    lFilters.Add($"exposure=exposure={lEncodeStepText}");
                    lRgbDomain = true;
                    break;
                case LColorKind.LColorKindCurve:
                    LEncodeEqAppend();
                    string lCurve = LWorkVideoFilter.LWorkCurveFormat(lStep.LWorkCurveRead());
                    if (lCurve.Length > 0)
                    {
                        lFilters.Add(lCurve);
                        lRgbDomain = true;
                    }
                    break;
                default:
                    LEncodeEqAppend();
                    break;
            }
        }

        LEncodeEqAppend();
        return lRgbDomain;
    }

    private static string LEncodeGammaFormat(double lValue) =>
        lValue.ToString("0.###", CultureInfo.InvariantCulture);
}
