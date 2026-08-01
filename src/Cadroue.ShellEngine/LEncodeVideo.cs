using System.Globalization;
using System.Text;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LEncodeVideo
{
    private const string LEncodeBitrateOption = "-b:v";

    internal static void LEncodeVideoAppend(StringBuilder lArguments, LWorkItem lWorkItem, LWorkOutput lOutput)
    {
        if (string.Equals(lOutput.LWorkOutputVideoStream, "Exclude", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lOutput.LWorkOutputVideoMode, "Exclude", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -vn");
            return;
        }

        if (string.Equals(lOutput.LWorkOutputVideoMode, "Copy", StringComparison.OrdinalIgnoreCase)
            && !LEncodeVideoCheck(lWorkItem, lOutput))
        {
            lArguments.Append(" -c:v copy");
            return;
        }

        LEncodeEncoderAppend(lArguments, lWorkItem, lOutput);
    }

    internal static void LEncodeEncoderAppend(StringBuilder lArguments, LWorkItem lWorkItem, LWorkOutput lOutput)
    {
        string lEncoderName = LCapability.LCapabilityNameRead(lOutput.LWorkOutputVideoEncoder);
        if (string.IsNullOrWhiteSpace(lEncoderName))
        {
            LEncodeFilterAppend(lArguments, lWorkItem, lOutput);
            return;
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" -c:v {lEncoderName}");

        LCapabilityCodec lCodec = LCapability.LCapabilityRead(lEncoderName);
        LCapabilityMode lMode = lCodec.LCapabilityModeFind(lOutput.LWorkOutputRateControl);
        LEncodeQualityAppend(lArguments, lEncoderName, lMode, lOutput.LWorkOutputQuality);

        if (lCodec.CapabilitySpeed is LCapabilitySpeed lSpeed && !string.IsNullOrWhiteSpace(lOutput.LWorkOutputSpeedPreset))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" {lSpeed.CapabilitySpeedOption} {lOutput.LWorkOutputSpeedPreset}");
        }

        foreach (var lExtra in lOutput.LWorkOutputVideoExtras)
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

    private static void LEncodeFilterAppend(StringBuilder lArguments, LWorkItem lWorkItem, LWorkOutput lOutput)
    {
        var lFilters = new List<string>();
        LWorkCrop lCrop = lWorkItem.LWorkCrop;

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

        if (lCrop.LWorkCropFlipHorizontal)
        {
            lFilters.Add("hflip");
        }

        if (lCrop.LWorkCropFlipVertical)
        {
            lFilters.Add("vflip");
        }

        if (lCrop.LWorkEdgeActive)
        {
            lFilters.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"crop=in_w-{lCrop.LWorkCropLeft}-{lCrop.LWorkCropRight}:in_h-{lCrop.LWorkCropTop}-{lCrop.LWorkCropBottom}:{lCrop.LWorkCropLeft}:{lCrop.LWorkCropTop}"));
        }

        LEncodeFiltersAppend(lFilters, lWorkItem.LWorkVideo);

        string? lSize = LEncodeSizeRead(lOutput.LWorkOutputVideoSize);
        if (lSize is not null)
        {
            lFilters.Add(LEncodeScaleResolve(lSize, lOutput.LWorkSizeReactive));
            lFilters.Add("setsar=1");
        }

        if (lFilters.Count > 0)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -vf {LEncode.LEncodeFormat(string.Join(',', lFilters))}");
        }

        if (!LEncode.LEncodeSourceCheck(lOutput.LWorkOutputVideoFps)
            && double.TryParse(lOutput.LWorkOutputVideoFps, NumberStyles.Float, CultureInfo.InvariantCulture, out double lFps))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -r {lFps.ToString(CultureInfo.InvariantCulture)}");
        }

        if (!string.IsNullOrWhiteSpace(lOutput.LWorkOutputPixelFormat)
            && !string.Equals(lOutput.LWorkOutputPixelFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -pix_fmt {lOutput.LWorkOutputPixelFormat}");
        }
    }

    private static void LEncodeFiltersAppend(List<string> lFilters, LWorkVideo lWorkVideo)
    {
        var lEqParts = new List<string>();
        foreach (LWorkVideoStep lStep in lWorkVideo.LWorkVideoSteps)
        {
            if (!lStep.LWorkStepActive)
            {
                continue;
            }

            switch (lStep.LWorkStepKind)
            {
                case LWorkVideoKind.LWorkVideoKindBrightness:
                    lEqParts.Add(
                        $"brightness={lStep.LWorkFfmpegValue.ToString("0.###", CultureInfo.InvariantCulture)}");
                    break;
                case LWorkVideoKind.LWorkVideoKindContrast:
                    lEqParts.Add(
                        $"contrast={lStep.LWorkFfmpegValue.ToString("0.###", CultureInfo.InvariantCulture)}");
                    break;
            }
        }

        if (lEqParts.Count > 0)
        {
            lFilters.Add("eq=" + string.Join(':', lEqParts));
        }
    }

    internal static bool LEncodeVideoCheck(LWorkItem lWorkItem, LWorkOutput lOutput) =>
        lWorkItem.LWorkCrop.LWorkCropActive
        || lWorkItem.LWorkVideo.LWorkVideoActive
        || LEncodeSizeRead(lOutput.LWorkOutputVideoSize) is not null;

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
