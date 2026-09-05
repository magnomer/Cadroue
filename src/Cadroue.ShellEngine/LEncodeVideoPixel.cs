using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static partial class LEncodeVideo
{
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
}
