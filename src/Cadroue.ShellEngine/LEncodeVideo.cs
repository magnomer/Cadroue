using System.Globalization;
using System.Linq;
using System.Text;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LEncodeVideo
{
    private const string LEncodeBitrateOption = "-b:v";

    internal static void LEncodeVideoAppend(StringBuilder lArguments, LWorkItem lWorkItem, LEncoding lOutput)
    {
        if (string.Equals(lOutput.LEncodingVideo.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase)
            && !LEncodeVideoCheck(lWorkItem, lOutput))
        {
            lArguments.Append(" -c:v copy -avoid_negative_ts make_zero");
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

        bool lRgbDomain = LEncodeFiltersAppend(lFilters, lWorkItem.LWorkVideo);

        string? lSize = LEncodeSizeRead(lOutput.LEncodingVideo.LEncodingSize);
        if (lSize is not null)
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
            lArguments.Append(CultureInfo.InvariantCulture, $" -vf {LEncode.LEncodeFormat(string.Join(',', lFilters))}");
        }

        string lFps = lOutput.LEncodingVideo.LEncodingFps?.Trim() ?? string.Empty;
        if (!LEncode.LEncodeSourceCheck(lFps) && LEncodeFpsCheck(lFps))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -r {lFps}");
        }

        string lPixel = LEncodePixelResolve(lWorkItem, lOutput.LEncodingVideo);
        if (lPixel.Length > 0)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -pix_fmt {lPixel}");
        }
    }

    private static string LEncodePixelResolve(LWorkItem lWorkItem, LEncodingVideo lVideo)
    {
        string lPixel = lVideo.LEncodingPixel?.Trim() ?? string.Empty;
        if (lPixel.Length > 0 && !string.Equals(lPixel, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return lPixel;
        }

        string lEncoder = LCapability.LCapabilityNameRead(lVideo.LEncodingEncoder);
        string lSourcePixel = lWorkItem.LWorkSourceMedia?.LWorkMediaPixel ?? string.Empty;
        bool lHighDepth = LEncodeDepthCheck(lSourcePixel);
        bool lTwelveBit = LEncodeTwelveCheck(lSourcePixel);
        bool lAlpha = LEncodeAlphaCheck(lSourcePixel);
        return lEncoder switch
        {
            // Delivery formats stay within their broadly decoded 4:2:0 profiles. Preserve
            // received high bit depth only where the codec and encoder have a mainstream
            // 10-bit profile; never recover properties from an earlier lineage source.
            "h264_qsv" => "nv12",
            "libx264" or "libopenh264" or "h264_amf" or "h264_mf" or "h264_nvenc" => "yuv420p",

            "hevc_qsv" or "av1_qsv" or "vp9_qsv" => lHighDepth ? "p010le" : "nv12",
            "hevc_amf" or "hevc_nvenc" or "av1_amf" or "av1_nvenc" =>
                lHighDepth ? "p010le" : "yuv420p",
            "libx265" or "libaom-av1" or "libsvtav1" or "librav1e"
                or "libvpx-vp9" or "libxeve" => lHighDepth ? "yuv420p10le" : "yuv420p",
            "hevc_mf" => "yuv420p",
            "libvvenc" => "yuv420p10le",

            "libvpx" or "libvpx-vp8" or "libxvid" or "mpeg4" or "libtheora"
                or "libxavs2" => "yuv420p",

            // Professional intermediates use their native baseline rather than inheriting
            // an arbitrary decoder layout. Preserve alpha when the received source has it.
            "prores" or "prores_aw" or "prores_ks" => lAlpha ? "yuva444p10le" : "yuv422p10le",
            "liboapv" => (lAlpha, lTwelveBit) switch
            {
                (true, true) => "yuva444p12le",
                (true, false) => "yuva444p10le",
                (false, true) => "yuv422p12le",
                _ => "yuv422p10le"
            },
            "mjpeg" => "yuvj420p",
            "libwebp" or "libwebp_anim" => lAlpha ? "yuva420p" : "yuv420p",

            // FFV1 and JPEG 2000 intentionally retain FFmpeg's received-format negotiation:
            // their purpose is archival fidelity and their supported layouts are extensive.
            "ffv1" or "jpeg2000" or "libopenjpeg" => string.Empty,
            _ => string.Empty
        };
    }

    private static bool LEncodeDepthCheck(string lPixel) =>
        lPixel.Contains("p9", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p10", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p12", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p14", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p16", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p010", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p012", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p016", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("gray9", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("gray10", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("gray12", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("gray14", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("gray16", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("rgb48", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("rgba64", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("f16", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("f32", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("nv20", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p210", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p212", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p216", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("x2rgb10", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("x2bgr10", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("y210", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("xv30", StringComparison.OrdinalIgnoreCase);

    private static bool LEncodeTwelveCheck(string lPixel) =>
        lPixel.Contains("p12", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p14", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p16", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p012", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p016", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("gray12", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("gray14", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("gray16", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("rgb48", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("rgba64", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("f16", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("f32", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p212", StringComparison.OrdinalIgnoreCase)
        || lPixel.Contains("p216", StringComparison.OrdinalIgnoreCase);

    private static bool LEncodeAlphaCheck(string lPixel) =>
        lPixel.StartsWith("yuva", StringComparison.OrdinalIgnoreCase)
        || lPixel.StartsWith("gbrap", StringComparison.OrdinalIgnoreCase)
        || lPixel.StartsWith("rgba", StringComparison.OrdinalIgnoreCase)
        || lPixel.StartsWith("bgra", StringComparison.OrdinalIgnoreCase)
        || lPixel.StartsWith("argb", StringComparison.OrdinalIgnoreCase)
        || lPixel.StartsWith("abgr", StringComparison.OrdinalIgnoreCase)
        || lPixel.StartsWith("ya", StringComparison.OrdinalIgnoreCase);

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

    private static IReadOnlyList<string> LEncodeColorNormalize(LWorkItem lWorkItem, LEncoding lOutput)
    {
        LWorkMedia? lMedia = lWorkItem.LWorkSourceMedia;
        string lSourcePixel = lMedia?.LWorkMediaPixel ?? string.Empty;
        string lSourceRange = lMedia?.LWorkMediaRange ?? string.Empty;
        bool lFullRange = string.Equals(lSourceRange, "pc", StringComparison.OrdinalIgnoreCase)
            || lSourcePixel.StartsWith("yuvj", StringComparison.OrdinalIgnoreCase);

        string lPixel = lOutput.LEncodingVideo.LEncodingPixel;
        string lTargetPixel = !string.IsNullOrWhiteSpace(lPixel)
            && !string.Equals(lPixel, "Auto", StringComparison.OrdinalIgnoreCase)
                ? lPixel
                : string.IsNullOrWhiteSpace(lSourcePixel) ? "yuv420p" : lSourcePixel;

        return new[]
        {
            "scale=in_range=full:out_range=" + (lFullRange ? "pc" : "tv"),
            "format=" + lTargetPixel
        };
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

        foreach (LWorkVideoStep lStep in lWorkVideo.LWorkVideoSteps.OrderBy(lStep => lStep.LWorkStepKind))
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
                    lEqParts.Add($"gamma={LEncodeGammaFormat(LWorkVideoStep.LWorkGammaResolve(lGamma.LWorkGammaGlobal))}");
                    if (lGamma.LWorkGammaRed != 0)
                    {
                        lEqParts.Add($"gamma_r={LEncodeGammaFormat(LWorkVideoStep.LWorkGammaResolve(lGamma.LWorkGammaRed))}");
                    }
                    if (lGamma.LWorkGammaGreen != 0)
                    {
                        lEqParts.Add($"gamma_g={LEncodeGammaFormat(LWorkVideoStep.LWorkGammaResolve(lGamma.LWorkGammaGreen))}");
                    }
                    if (lGamma.LWorkGammaBlue != 0)
                    {
                        lEqParts.Add($"gamma_b={LEncodeGammaFormat(LWorkVideoStep.LWorkGammaResolve(lGamma.LWorkGammaBlue))}");
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
                    lFilters.Add($"exposure=exposure={lStep.LWorkFfmpegValue.ToString("0.###", CultureInfo.InvariantCulture)}");
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
