using System.Globalization;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static partial class LEncodeVideo
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

        if (lCodec.LCapabilitySpeed is LCapabilitySpeed lSpeed && !string.IsNullOrWhiteSpace(lOutput.LEncodingVideo.LEncodingSpeedPreset))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" {lSpeed.LCapabilitySpeedOption} {lOutput.LEncodingVideo.LEncodingSpeedPreset}");
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
        if (lMode.LCapabilityModeQuality is not LCapabilityQuality lQuality)
        {
            LEncodeLosslessAppend(lArguments, lEncoderName);
            return;
        }

        if (string.IsNullOrWhiteSpace(lQualityValue))
        {
            lQualityValue = lQuality.LCapabilityQualityDefault;
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" {lQuality.LCapabilityQualityOption} {lQualityValue}");

        bool lNeedsZeroBitrate = lEncoderName is "libaom-av1" or "libvpx" or "libvpx-vp9";
        if (lNeedsZeroBitrate && !string.Equals(lQuality.LCapabilityQualityOption, LEncodeBitrateOption, StringComparison.Ordinal))
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

    internal static bool LEncodeVideoCheck(LWorkItem lWorkItem, LEncoding lOutput) =>
        lWorkItem.LWorkCrop.LWorkCropActive
        || lWorkItem.LWorkVideo.LWorkVideoActive
        || LEncodeSizeRead(lOutput.LEncodingVideo.LEncodingSize) is not null;
}
