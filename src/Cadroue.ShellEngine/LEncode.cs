using System.Globalization;
using System.Text;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LEncode
{
    private const string LEncodeBitrateOption = "-b:v";

    public const double LEncodeStatsPeriod = 0.5;

    public static string LEncodeArgumentBuild(LWorkItem lWorkItem)
    {
        LWorkOutput lOutput = lWorkItem.LWorkOutput;
        var lArguments = new StringBuilder();

        lArguments.Append("-hide_banner -nostdin -y");
        lArguments.Append(" -progress pipe:1 -nostats");
        lArguments.Append(CultureInfo.InvariantCulture,
            $" -stats_period {LEncodeStatsPeriod.ToString("0.###", CultureInfo.InvariantCulture)}");

        lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lWorkItem.LWorkStart)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -to {LEncodeTimeFormat(lWorkItem.LWorkEnd)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeQuote(lWorkItem.LWorkSourcePath)}");

        LEncodeVideoAppend(lArguments, lOutput);
        LEncodeAudioAppend(lArguments, lOutput);

        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeQuote(lWorkItem.LWorkOutputPath)}");
        return lArguments.ToString();
    }

    private static void LEncodeVideoAppend(StringBuilder lArguments, LWorkOutput lOutput)
    {
        if (string.Equals(lOutput.LWorkOutputVideoStream, "Exclude", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lOutput.LWorkOutputVideoMode, "Exclude", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -vn");
            return;
        }

        if (string.Equals(lOutput.LWorkOutputVideoMode, "Copy", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -c:v copy");
            return;
        }

        string lEncoderName = LCapability.LCapabilityNameRead(lOutput.LWorkOutputVideoEncoder);
        if (string.IsNullOrWhiteSpace(lEncoderName))
        {
            return;
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" -c:v {lEncoderName}");

        LCapabilityCodec lCodec = LCapability.LCapabilityRead(lEncoderName);
        LCapabilityMode lMode = lCodec.CapabilityModeFind(lOutput.LWorkOutputRateControl);
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

        LEncodeFilterAppend(lArguments, lOutput);
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

    private static void LEncodeFilterAppend(StringBuilder lArguments, LWorkOutput lOutput)
    {
        string? lSize = LEncodeSizeRead(lOutput.LWorkOutputVideoSize);
        if (lSize is not null)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -s {lSize}");
        }

        if (!LEncodeSameAsSource(lOutput.LWorkOutputVideoFps)
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

    private static void LEncodeAudioAppend(StringBuilder lArguments, LWorkOutput lOutput)
    {
        if (string.Equals(lOutput.LWorkOutputAudioStream, "Exclude", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lOutput.LWorkOutputAudioMode, "Exclude", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -an");
            return;
        }

        if (string.Equals(lOutput.LWorkOutputAudioStream, "Include all audio tracks", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -map 0:v:0 -map 0:a");
        }

        if (string.Equals(lOutput.LWorkOutputAudioMode, "Copy", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -c:a copy");
            return;
        }

        string lAudioName = LEncodeAudioNameRead(lOutput.LWorkOutputAudioEncoder);
        if (!string.IsNullOrWhiteSpace(lAudioName))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -c:a {lAudioName}");
        }

        if (!string.IsNullOrWhiteSpace(lOutput.LWorkOutputAudioBitrate)
            && !string.Equals(lOutput.LWorkOutputAudioBitrate, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -b:a {lOutput.LWorkOutputAudioBitrate}");
        }

        if (!LEncodeSameAsSource(lOutput.LWorkOutputAudioSampleRate)
            && int.TryParse(lOutput.LWorkOutputAudioSampleRate, out int lSampleRate))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -ar {lSampleRate}");
        }

        int? lChannels = LEncodeChannelRead(lOutput.LWorkOutputAudioChannels);
        if (lChannels is int lChannelCount)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -ac {lChannelCount}");
        }
    }

    private static string LEncodeAudioNameRead(string lAudioEncoder) => lAudioEncoder switch
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
        _ => null
    };

    private static string? LEncodeSizeRead(string lSize)
    {
        if (LEncodeSameAsSource(lSize) || string.Equals(lSize, "Custom", StringComparison.OrdinalIgnoreCase))
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

    private static bool LEncodeSameAsSource(string lValue) =>
        string.IsNullOrWhiteSpace(lValue) || string.Equals(lValue, "Same as source", StringComparison.OrdinalIgnoreCase);

    private static string LEncodeTimeFormat(TimeSpan lTime) =>
        lTime.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);

    private static string LEncodeQuote(string lPath) => $"\"{lPath}\"";
}
