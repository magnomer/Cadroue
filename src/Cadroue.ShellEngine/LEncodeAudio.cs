using System.Globalization;
using System.Text;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LEncodeAudio
{
    internal static void LEncodeAudioAppend(StringBuilder lArguments, LEncoding lOutput)
    {
        if (string.Equals(lOutput.LEncodingAudio.LEncodingStream, "Exclude", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lOutput.LEncodingAudio.LEncodingMode, "Exclude", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -an");
            return;
        }

        if (string.Equals(lOutput.LEncodingAudio.LEncodingStream, "Include all audio tracks", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -map 0:v:0? -map 0:a");
        }

        if (string.Equals(lOutput.LEncodingAudio.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -c:a copy");
            return;
        }

        LEncodeSettingsAppend(lArguments, lOutput, LEncodeTrackRead(lOutput.LEncodingAudio.LEncodingEncoder));
    }

    internal static void LEncodeMuxAppend(StringBuilder lArguments, LEncoding lOutput)
    {
        string lAudioName = LEncodeTrackRead(lOutput.LEncodingAudio.LEncodingEncoder);
        if (string.IsNullOrWhiteSpace(lAudioName))
        {
            lAudioName = "aac";
        }

        LEncodeSettingsAppend(lArguments, lOutput, lAudioName);
    }

    private static void LEncodeSettingsAppend(StringBuilder lArguments, LEncoding lOutput, string lAudioName)
    {
        if (!string.IsNullOrWhiteSpace(lAudioName))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -c:a {lAudioName}");
        }

        LCapabilityCodec lCodec = LCapability.LCapabilityAudioRead(lAudioName);
        LCapabilityMode lMode = lCodec.LCapabilityModeFind(lOutput.LEncodingAudio.LEncodingRateControl);
        if (lMode.LCapabilityModeQuality is LCapabilityQuality lQuality)
        {
            string lValue = string.IsNullOrWhiteSpace(lOutput.LEncodingAudio.LEncodingQuality)
                ? lQuality.LCapabilityQualityDefault
                : lOutput.LEncodingAudio.LEncodingQuality;
            if (!string.IsNullOrWhiteSpace(lValue)
                && !string.Equals(lValue, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                lArguments.Append(CultureInfo.InvariantCulture, $" {lQuality.LCapabilityQualityOption} {lValue}");
            }
        }

        if (lCodec.LCapabilitySpeed is LCapabilitySpeed lSpeed && !string.IsNullOrWhiteSpace(lOutput.LEncodingAudio.LEncodingSpeed))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" {lSpeed.LCapabilitySpeedOption} {lOutput.LEncodingAudio.LEncodingSpeed}");
        }

        foreach (var lExtra in lOutput.LEncodingAudio.LEncodingExtras)
        {
            if (string.IsNullOrWhiteSpace(lExtra.Value) || string.Equals(lExtra.Value, "none", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            lArguments.Append(CultureInfo.InvariantCulture, $" {lExtra.Key} {lExtra.Value}");
        }

        if (!LEncode.LEncodeSourceCheck(lOutput.LEncodingAudio.LEncodingSampleRate)
            && int.TryParse(lOutput.LEncodingAudio.LEncodingSampleRate, out int lSampleRate))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -ar {lSampleRate}");
        }

        LEncodeChannelAppend(lArguments, lOutput.LEncodingAudio.LEncodingChannels);
    }

    private static void LEncodeChannelAppend(StringBuilder lArguments, string lChannels)
    {
        if (LEncode.LEncodeSourceCheck(lChannels) || string.IsNullOrWhiteSpace(lChannels))
        {
            return;
        }

        if (int.TryParse(lChannels, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lCount) && lCount > 0)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -ac {lCount}");
            return;
        }

        string lLayout = lChannels switch
        {
            "Mono" => "mono",
            "Stereo" => "stereo",
            _ => lChannels
        };
        lArguments.Append(CultureInfo.InvariantCulture, $" -channel_layout {lLayout}");
    }

    private static string LEncodeTrackRead(string lAudioEncoder) => lAudioEncoder switch
    {
        "AAC" => "aac",
        "FLAC" => "flac",
        _ => LCapability.LCapabilityNameRead(lAudioEncoder)
    };
}
