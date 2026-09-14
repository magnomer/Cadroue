using System.Globalization;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

internal static partial class LEncodeVideo
{
    internal static void LEncodeVideoAppend(
        StringBuilder lArguments, LWorkItem lWorkItem, LEncoding lOutput, int lPass = 0)
    {
        if (string.Equals(lOutput.LEncodingVideo.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase)
            && !LEncodeVideoCheck(lWorkItem, lOutput))
        {
            lArguments.Append(" -c:v copy -avoid_negative_ts make_zero");
            return;
        }

        LEncodeEncoderAppend(lArguments, lWorkItem, lOutput, lPass);
    }

    internal static void LEncodeEncoderAppend(
        StringBuilder lArguments, LWorkItem lWorkItem, LEncoding lOutput, int lPass = 0)
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
        LEncodeModeAppend(lArguments, lMode, lOutput.LEncodingVideo.LEncodingQuality);
        if (lPass > 0)
        {
            lArguments.Append(
                CultureInfo.InvariantCulture,
                $" -pass {lPass} -passlogfile {LEncode.LEncodeFormat(LEncodePassFormat(lWorkItem))}");
        }

        if (lCodec.LCapabilitySpeed is LCapabilitySpeed lSpeed
            && !string.IsNullOrWhiteSpace(lOutput.LEncodingVideo.LEncodingSpeedPreset))
        {
            lArguments.Append(
                CultureInfo.InvariantCulture,
                $" {lSpeed.LCapabilitySpeedOption}"
                + $" {LEncode.LEncodeValueFormat(lOutput.LEncodingVideo.LEncodingSpeedPreset)}");
        }

        foreach (var lExtra in lOutput.LEncodingVideo.LEncodingExtras)
        {
            if (string.IsNullOrWhiteSpace(lExtra.Value)
                || string.Equals(lExtra.Value, "none", StringComparison.OrdinalIgnoreCase)
                || lMode.LCapabilityConflictCheck(lExtra.Key))
            {
                continue;
            }

            lArguments.Append(
                CultureInfo.InvariantCulture,
                $" {LEncode.LEncodeValueFormat(lExtra.Key)} {LEncode.LEncodeValueFormat(lExtra.Value)}");
        }

        LEncodeFilterAppend(lArguments, lWorkItem, lOutput);
    }

    private static void LEncodeModeAppend(StringBuilder lArguments, LCapabilityMode lMode, string lQualityValue)
    {
        if (lMode.LCapabilityModeQuality is LCapabilityQuality lQuality)
        {
            if (string.IsNullOrWhiteSpace(lQualityValue))
            {
                lQualityValue = lQuality.LCapabilityQualityDefault;
            }

            lArguments.Append(
                CultureInfo.InvariantCulture,
                $" {lQuality.LCapabilityQualityOption} {LEncode.LEncodeValueFormat(lQualityValue)}");
        }

        if (string.IsNullOrWhiteSpace(lMode.LCapabilityModeArgument))
        {
            return;
        }

        string lModeArgument = lMode.LCapabilityModeArgument.Replace(
            LCapabilityMode.LCapabilityModeToken,
            LEncode.LEncodeValueFormat(lQualityValue),
            StringComparison.Ordinal);
        lArguments.Append(CultureInfo.InvariantCulture, $" {lModeArgument}");
    }

    internal static int LEncodePassRead(LWorkItem lWorkItem, LEncoding lOutput)
    {
        if (string.Equals(lOutput.LEncodingVideo.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase)
            && !LEncodeVideoCheck(lWorkItem, lOutput))
        {
            return 1;
        }

        string lEncoderName = LCapability.LCapabilityNameRead(lOutput.LEncodingVideo.LEncodingEncoder);
        if (string.IsNullOrWhiteSpace(lEncoderName))
        {
            return 1;
        }

        return LCapability.LCapabilityRead(lEncoderName)
            .LCapabilityModeFind(lOutput.LEncodingVideo.LEncodingRateControl)
            .LCapabilityModePass;
    }

    internal static string LEncodePassFormat(LWorkItem lWorkItem)
    {
        string lPassFolder = Path.Combine(LDepot.LDepotPassRead(), $"{lWorkItem.LWorkId:N}");
        Directory.CreateDirectory(lPassFolder);
        return Path.Combine(lPassFolder, "pass");
    }

    internal static bool LEncodeVideoCheck(LWorkItem lWorkItem, LEncoding lOutput) =>
        lWorkItem.LWorkCrop.LWorkCropActive
        || lWorkItem.LWorkVideo.LWorkVideoActive
        || LEncodeSizeCheck(lOutput.LEncodingVideo);

    private static bool LEncodeSizeCheck(LEncodingVideo lVideo) =>
        string.Equals(lVideo.LEncodingMode, "Encode", StringComparison.OrdinalIgnoreCase)
        && LEncodeSizeRead(lVideo.LEncodingSize) is not null;
}
