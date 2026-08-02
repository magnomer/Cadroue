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
        if (lMode.CapabilityModeQuality is LCapabilityQuality lQuality)
        {
            string lValue = string.IsNullOrWhiteSpace(lOutput.LEncodingAudio.LEncodingQuality)
                ? lQuality.CapabilityQualityDefault
                : lOutput.LEncodingAudio.LEncodingQuality;
            if (!string.IsNullOrWhiteSpace(lValue)
                && !string.Equals(lValue, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                lArguments.Append(CultureInfo.InvariantCulture, $" {lQuality.CapabilityQualityOption} {lValue}");
            }
        }

        if (lCodec.CapabilitySpeed is LCapabilitySpeed lSpeed && !string.IsNullOrWhiteSpace(lOutput.LEncodingAudio.LEncodingSpeed))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" {lSpeed.CapabilitySpeedOption} {lOutput.LEncodingAudio.LEncodingSpeed}");
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

        int? lChannels = LEncodeChannelRead(lOutput.LEncodingAudio.LEncodingChannels);
        if (lChannels is int lChannelCount)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -ac {lChannelCount}");
        }
    }

    private static string LEncodeTrackRead(string lAudioEncoder) => lAudioEncoder switch
    {
        "AAC" => "aac",
        "FLAC" => "flac",
        _ => LCapability.LCapabilityNameRead(lAudioEncoder)
    };

    private static int? LEncodeChannelRead(string lChannels) => lChannels switch
    {
        "Mono" => 1,
        "Stereo" => 2,
        "5.1" => 6,
        _ => int.TryParse(lChannels, out int lCount) && lCount > 0 ? lCount : null
    };
}
