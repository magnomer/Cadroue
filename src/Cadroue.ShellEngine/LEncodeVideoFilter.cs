using System.Globalization;
using System.Linq;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static partial class LEncodeVideo
{
    private static void LEncodeFilterAppend(StringBuilder lArguments, LWorkItem lWorkItem, LEncoding lOutput)
    {
        var lFilters = new List<string>(LEncodeGeometryRead(lWorkItem.LWorkCrop, lWorkItem.LWorkSourceMedia));

        bool lRgbDomain = LEncodeFiltersAppend(lFilters, lWorkItem.LWorkVideo);

        if (LEncodeSizeCheck(lOutput.LEncodingVideo)
            && LEncodeSizeRead(lOutput.LEncodingVideo.LEncodingSize) is { } lSize)
        {
            lFilters.Add(LEncodeScaleResolve(lSize, lOutput.LEncodingVideo.LEncodingSizeReactive));
            lFilters.Add("setsar=1");
        }

        if (lRgbDomain)
        {
            lFilters.AddRange(LEncodeColorNormalize(lWorkItem, lOutput));
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

        string lPixel = LEncodePixelResolve(lWorkItem, lOutput.LEncodingVideo);
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

            switch (lStep.LWorkStepKind)
            {
                case LColorKind.LColorKindBrightness:
                    lEqParts.Add(
                        $"brightness={lStep.LWorkFfmpegValue.ToString("0.###", CultureInfo.InvariantCulture)}");
                    break;
                case LColorKind.LColorKindContrast:
                    lEqParts.Add(
                        $"contrast={lStep.LWorkFfmpegValue.ToString("0.###", CultureInfo.InvariantCulture)}");
                    break;
                case LColorKind.LColorKindSaturation:
                    lEqParts.Add(
                        $"saturation={lStep.LWorkFfmpegValue.ToString("0.###", CultureInfo.InvariantCulture)}");
                    break;
                case LColorKind.LColorKindGamma:
                    LWorkGammaSettings lGamma = lStep.LWorkGammaRead();
                    lEqParts.Add(
                        $"gamma={LEncodeGammaFormat(LWorkVideoStep.LWorkGammaResolve(lGamma.LWorkGammaGlobal))}");
                    if (lGamma.LWorkGammaRed != 0)
                    {
                        lEqParts.Add(
                            $"gamma_r={LEncodeGammaFormat(LWorkVideoStep.LWorkGammaResolve(lGamma.LWorkGammaRed))}");
                    }
                    if (lGamma.LWorkGammaGreen != 0)
                    {
                        lEqParts.Add(
                            $"gamma_g={LEncodeGammaFormat(LWorkVideoStep.LWorkGammaResolve(lGamma.LWorkGammaGreen))}");
                    }
                    if (lGamma.LWorkGammaBlue != 0)
                    {
                        lEqParts.Add(
                            $"gamma_b={LEncodeGammaFormat(LWorkVideoStep.LWorkGammaResolve(lGamma.LWorkGammaBlue))}");
                    }
                    if (lGamma.LWorkGammaHighlight != 0)
                    {
                        lEqParts.Add($"gamma_weight={LEncodeGammaFormat(1d - lGamma.LWorkGammaHighlight / 100d)}");
                    }
                    break;
                case LColorKind.LColorKindWhitebalance:
                    LEncodeEqAppend();
                    lFilters.AddRange(lStep.LWorkWhitebalanceRead().LWorkWhitebalanceFormat());
                    lRgbDomain = true;
                    break;
                case LColorKind.LColorKindExposure:
                    LEncodeEqAppend();
                    lFilters.Add(
                        $"exposure=exposure={lStep.LWorkFfmpegValue.ToString("0.###", CultureInfo.InvariantCulture)}");
                    lRgbDomain = true;
                    break;
                case LColorKind.LColorKindCurve:
                    LEncodeEqAppend();
                    string lCurve = lStep.LWorkCurveRead().LWorkCurveFormat();
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
