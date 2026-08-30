using System.Globalization;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static partial class LEncode
{
    private static string LEncodeMatchResolve(LWorkItem lWorkItem, LBridgeStream? lBridgeSource)
    {
        string lCodec = (lBridgeSource?.LBridgeCodec ?? lWorkItem.LWorkSourceMedia?.LWorkMediaCodec ?? string.Empty)
            .ToLowerInvariant();
        string lEncoder = LRepertoireCatalog.LRepertoireEncoderResolve(lCodec)
            ?? throw new InvalidOperationException(
                $"no smart-encoding boundary encoder maps to source codec '{lCodec}'");

        var lArguments = new StringBuilder();
        lArguments.Append(CultureInfo.InvariantCulture, $"-c:v {lEncoder}");

        if (LEncodeProfileResolve(lEncoder, lBridgeSource?.LBridgeProfile) is { } lProfile)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -profile:v {lProfile}");
        }

        string lPixel = lBridgeSource?.LBridgePixel ?? string.Empty;
        if (LEncodeValueCheck(lPixel))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -pix_fmt {lPixel}");
        }

        long lBitrate = lBridgeSource?.LBridgeBitrate ?? 0;
        if (lBitrate > 0)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -b:v {lBitrate}");
        }
        else
        {
            string lQuality = LEncodeQualityResolve(lEncoder);
            if (lQuality.Length > 0)
            {
                lArguments.Append(CultureInfo.InvariantCulture, $" {lQuality}");
            }
        }

        LEncodeColorAppend(lArguments, "-colorspace", lBridgeSource?.LBridgeColorSpace);
        LEncodeColorAppend(lArguments, "-color_primaries", lBridgeSource?.LBridgeColorPrimaries);
        LEncodeColorAppend(lArguments, "-color_trc", lBridgeSource?.LBridgeColorTransfer);
        LEncodeColorAppend(lArguments, "-color_range", lBridgeSource?.LBridgeColorRange);

        lArguments.Append(" -fps_mode passthrough");

        return lArguments.ToString();
    }

    private static string? LEncodeProfileResolve(string lEncoder, string? lProfile)
    {
        if (!LEncodeValueCheck(lProfile ?? string.Empty))
        {
            return null;
        }

        string lNormalized = lProfile!.ToLowerInvariant();
        return lEncoder switch
        {
            "libx264" => lNormalized switch
            {
                "baseline" or "constrained baseline" => "baseline",
                "main" => "main",
                "high" => "high",
                "high 10" => "high10",
                "high 4:2:2" => "high422",
                "high 4:4:4 predictive" or "high 4:4:4" => "high444",
                _ => null
            },
            "libx265" => lNormalized switch
            {
                "main" => "main",
                "main 10" => "main10",
                _ => null
            },
            _ => null
        };
    }

    private static string LEncodeQualityResolve(string lEncoder) => lEncoder switch
    {
        "libx265" => "-crf 18",
        "libvpx-vp9" => "-b:v 0 -crf 24",
        "libaom-av1" => "-crf 24",
        "ffv1" => string.Empty,
        _ => "-crf 18"
    };

    private static void LEncodeColorAppend(StringBuilder lArguments, string lFlag, string? lValue)
    {
        if (LEncodeValueCheck(lValue ?? string.Empty))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" {lFlag} {lValue}");
        }
    }

    private static bool LEncodeValueCheck(string lValue) =>
        !string.IsNullOrWhiteSpace(lValue)
        && !string.Equals(lValue, "unknown", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(lValue, "N/A", StringComparison.OrdinalIgnoreCase);
}
