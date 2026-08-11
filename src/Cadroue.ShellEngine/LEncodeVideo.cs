using System.Globalization;
using System.Text;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LEncodeVideo
{
    private const string LEncodeBitrateOption = "-b:v";

    internal static void LEncodeVideoAppend(StringBuilder lArguments, LWorkItem lWorkItem, LEncoding lOutput)
    {
        if (string.Equals(lOutput.LEncodingVideo.LEncodingStream, "Exclude", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lOutput.LEncodingVideo.LEncodingMode, "Exclude", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -vn");
            return;
        }

        if (string.Equals(lOutput.LEncodingVideo.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase)
            && !LEncodeVideoCheck(lWorkItem, lOutput))
        {
            lArguments.Append(" -c:v copy");
            return;
        }

        LEncodeEncoderAppend(lArguments, lWorkItem, lOutput);
    }

    internal static void LEncodeEncoderAppend(StringBuilder lArguments, LWorkItem lWorkItem, LEncoding lOutput)
    {
        string lEncoderName = LCapability.LCapabilityNameRead(lOutput.LEncodingVideo.LEncodingEncoder);
        if (string.IsNullOrWhiteSpace(lEncoderName))
        {
            LEncodeFilterAppend(lArguments, lWorkItem, lOutput);
            return;
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" -c:v {lEncoderName}");

        LCapabilityCodec lCodec = LCapability.LCapabilityRead(lEncoderName);
        LCapabilityMode lMode = lCodec.LCapabilityModeFind(lOutput.LEncodingVideo.LEncodingRateControl);
        LEncodeQualityAppend(lArguments, lEncoderName, lMode, lOutput.LEncodingVideo.LEncodingQuality);

        if (lCodec.CapabilitySpeed is LCapabilitySpeed lSpeed && !string.IsNullOrWhiteSpace(lOutput.LEncodingVideo.LEncodingSpeedPreset))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" {lSpeed.CapabilitySpeedOption} {lOutput.LEncodingVideo.LEncodingSpeedPreset}");
        }

        foreach (var lExtra in lOutput.LEncodingVideo.LEncodingExtras)
        {
            if (string.IsNullOrWhiteSpace(lExtra.Value) || string.Equals(lExtra.Value, "none", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            lArguments.Append(CultureInfo.InvariantCulture, $" {lExtra.Key} {lExtra.Value}");
        }

        LEncodeFilterAppend(lArguments, lWorkItem, lOutput);
    }

    private static void LEncodeQualityAppend(
        StringBuilder lArguments,
        string lEncoderName,
        LCapabilityMode lMode,
        string lQualityValue)
    {
        if (lMode.CapabilityModeQuality is not LCapabilityQuality lQuality)
        {
            LEncodeLosslessAppend(lArguments, lEncoderName);
            return;
        }

        if (string.IsNullOrWhiteSpace(lQualityValue))
        {
            lQualityValue = lQuality.CapabilityQualityDefault;
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" {lQuality.CapabilityQualityOption} {lQualityValue}");

        bool lNeedsZeroBitrate = lEncoderName is "libaom-av1" or "libvpx" or "libvpx-vp9";
        if (lNeedsZeroBitrate && !string.Equals(lQuality.CapabilityQualityOption, LEncodeBitrateOption, StringComparison.Ordinal))
        {
            lArguments.Append(" -b:v 0");
        }
    }

    private static void LEncodeLosslessAppend(StringBuilder lArguments, string lEncoderName)
    {
        switch (lEncoderName)
        {
            case "libx264":
                lArguments.Append(" -crf 0");
                break;
            case "libx265":
                lArguments.Append(" -x265-params lossless=1");
                break;
            case "libvpx-vp9":
                lArguments.Append(" -lossless 1");
                break;
            case "libwebp":
            case "libwebp_anim":
                lArguments.Append(" -lossless 1");
                break;
            case "h264_nvenc":
            case "hevc_nvenc":
            case "av1_nvenc":
                lArguments.Append(" -tune lossless");
                break;
            case "libaom-av1":
                lArguments.Append(" -aom-params lossless=1");
                break;
            case "ffv1":
                break;
        }
    }

    private static void LEncodeFilterAppend(StringBuilder lArguments, LWorkItem lWorkItem, LEncoding lOutput)
    {
        var lFilters = new List<string>(LEncodeGeometryRead(lWorkItem.LWorkCrop));

        LEncodeFiltersAppend(lFilters, lWorkItem.LWorkVideo);

        string? lSize = LEncodeSizeRead(lOutput.LEncodingVideo.LEncodingSize);
        if (lSize is not null)
        {
            lFilters.Add(LEncodeScaleResolve(lSize, lOutput.LEncodingVideo.LEncodingSizeReactive));
            lFilters.Add("setsar=1");
        }

        if (lFilters.Count > 0)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -vf {LEncode.LEncodeFormat(string.Join(',', lFilters))}");
        }

        if (!LEncode.LEncodeSourceCheck(lOutput.LEncodingVideo.LEncodingFps)
            && double.TryParse(lOutput.LEncodingVideo.LEncodingFps, NumberStyles.Float, CultureInfo.InvariantCulture, out double lFps))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -r {lFps.ToString(CultureInfo.InvariantCulture)}");
        }

        if (!string.IsNullOrWhiteSpace(lOutput.LEncodingVideo.LEncodingPixelFormat)
            && !string.Equals(lOutput.LEncodingVideo.LEncodingPixelFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -pix_fmt {lOutput.LEncodingVideo.LEncodingPixelFormat}");
        }
    }

    internal static IReadOnlyList<string> LEncodeGeometryRead(LWorkCrop lCrop)
    {
        var lFilters = new List<string>();

        // Flyleaf applies its flip flags in source space, before Rotation. Keep FFmpeg's
        // sequential filter graph in that same order so combined transforms match preview.
        if (lCrop.LWorkFlipHorizontal)
        {
            lFilters.Add("hflip");
        }

        if (lCrop.LWorkFlipVertical)
        {
            lFilters.Add("vflip");
        }

        string? lRotate = lCrop.LWorkCropRotation switch
        {
            90 => "transpose=1",
            180 => "transpose=1,transpose=1",
            270 => "transpose=2",
            _ => null
        };

        if (lRotate is not null)
        {
            lFilters.Add(lRotate);
        }

        if (lCrop.LWorkEdgeActive)
        {
            lFilters.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"crop=in_w-{lCrop.LWorkCropLeft}-{lCrop.LWorkCropRight}:in_h-{lCrop.LWorkCropTop}-{lCrop.LWorkCropBottom}:{lCrop.LWorkCropLeft}:{lCrop.LWorkCropTop}"));
        }

        return lFilters;
    }

    private static void LEncodeFiltersAppend(List<string> lFilters, LWorkVideo lWorkVideo)
    {
        var lEqParts = new List<string>();
        void lEqFlush()
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
                case LColorKind.LColorKindGamma:
                    LWorkGammaSettings lGamma = lStep.LWorkGammaRead();
                    lEqParts.Add($"gamma={LEncodeGammaFormat(LWorkVideoStep.LWorkGammaFactorRead(lGamma.LWorkGammaGlobal))}");
                    if (lGamma.LWorkGammaRed != 0)
                    {
                        lEqParts.Add($"gamma_r={LEncodeGammaFormat(LWorkVideoStep.LWorkGammaFactorRead(lGamma.LWorkGammaRed))}");
                    }
                    if (lGamma.LWorkGammaGreen != 0)
                    {
                        lEqParts.Add($"gamma_g={LEncodeGammaFormat(LWorkVideoStep.LWorkGammaFactorRead(lGamma.LWorkGammaGreen))}");
                    }
                    if (lGamma.LWorkGammaBlue != 0)
                    {
                        lEqParts.Add($"gamma_b={LEncodeGammaFormat(LWorkVideoStep.LWorkGammaFactorRead(lGamma.LWorkGammaBlue))}");
                    }
                    if (lGamma.LWorkGammaHighlightProtection != 0)
                    {
                        lEqParts.Add($"gamma_weight={LEncodeGammaFormat(1d - lGamma.LWorkGammaHighlightProtection / 100d)}");
                    }
                    break;
                default:
                    lEqFlush();
                    break;
            }
        }

        lEqFlush();
    }

    private static string LEncodeGammaFormat(double lValue) =>
        lValue.ToString("0.###", CultureInfo.InvariantCulture);

    internal static bool LEncodeVideoCheck(LWorkItem lWorkItem, LEncoding lOutput) =>
        lWorkItem.LWorkCrop.LWorkCropActive
        || lWorkItem.LWorkVideo.LWorkVideoActive
        || LEncodeSizeRead(lOutput.LEncodingVideo.LEncodingSize) is not null;

    private static string LEncodeScaleResolve(string lSize, bool lReactive)
    {
        string[] lParts = lSize.Split('x');
        int lWidth = int.Parse(lParts[0], CultureInfo.InvariantCulture);
        int lHeight = int.Parse(lParts[1], CultureInfo.InvariantCulture);

        int lShortEdge = Math.Min(lWidth, lHeight);
        int lLongEdge = Math.Max(lWidth, lHeight);
        if (!lReactive || lShortEdge == lLongEdge)
        {
            return string.Create(CultureInfo.InvariantCulture, $"scale={lWidth}:{lHeight}");
        }

        int lEdgeSpan = lLongEdge - lShortEdge;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"scale=w={lShortEdge}+{lEdgeSpan}*gte(iw\\,ih):h={lLongEdge}-{lEdgeSpan}*gte(iw\\,ih)");
    }

    private static string? LEncodeSizeRead(string lSize)
    {
        if (LEncode.LEncodeSourceCheck(lSize) || string.Equals(lSize, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string[] lParts = lSize.Split(['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lParts.Length != 2 || !int.TryParse(lParts[0], out int lWidth) || !int.TryParse(lParts[1], out int lHeight))
        {
            return null;
        }

        return $"{lWidth}x{lHeight}";
    }
}
