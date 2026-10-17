using static Cadroue.Core.LCapabilityFamily;

namespace Cadroue.Core;

public static class LCapabilityTable
{
    public static LCapabilityCodec LCapabilityFallback { get; } = new(
        "unknown",
        [
            new("Constant quantizer (qscale)", new("qscale", "-q:v", "5", 1, 31)),
            new("Target bitrate", LCapabilityBitrateCreate("4M"))
        ],
        null,
        null,
        "Unrecognised encoder: falling back to the generic qscale controls.");

    public static IReadOnlyDictionary<string, LCapabilityCodec> LCapabilityMap { get; } =
        LCapabilityVideoRead()
            .ToDictionary(pEntry => pEntry.Key, pEntry => pEntry.Value, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<KeyValuePair<string, LCapabilityCodec>> LCapabilityVideoRead()
    {
        yield return new("libx264", LCapabilityLibxCreate("libx264", "23", "-crf 0"));
        yield return new("h264_mf", LCapabilityMfCreate("h264_mf"));
        yield return new("libopenh264", new(
            "libopenh264",
            [
                new("Quality mode (bitrate targeted)", LCapabilityBitrateCreate(), "-rc_mode quality"),
                new("Bitrate mode", LCapabilityBitrateCreate(), "-rc_mode bitrate"),
                new("Buffer mode", LCapabilityBitrateCreate(), "-rc_mode buffer"),
                new("Timestamp mode", LCapabilityBitrateCreate(), "-rc_mode timestamp"),
                new("Rate control off", null, "-rc_mode off")
            ],
            null,
            [new LCapabilityExtra("Profile", "-profile", "constrained_baseline",
                [new("constrained_baseline", "Constrained baseline"), new("main", "Main"), new("high", "High")])],
            "OpenH264 has no CRF, no QP and no preset. Every mode is bitrate driven via -rc_mode."));
        yield return new("h264_qsv", LCapabilityQsvCreate("h264_qsv"));
        yield return new("h264_amf", LCapabilityAmfCreate("h264_amf"));
        yield return new("h264_nvenc", LCapabilityNvencCreate("h264_nvenc"));

        yield return new("libx265", LCapabilityLibxCreate("libx265", "28", "-x265-params lossless=1"));
        yield return new("hevc_qsv", LCapabilityQsvCreate("hevc_qsv"));
        yield return new("hevc_amf", LCapabilityAmfCreate("hevc_amf"));
        yield return new("hevc_mf", LCapabilityMfCreate("hevc_mf"));
        yield return new("hevc_nvenc", LCapabilityNvencCreate("hevc_nvenc"));

        yield return new("libvvenc", new(
            "libvvenc",
            [
                new("Constant QP", new("QP", "-qp", "32", 0, 63)),
                new("Target bitrate", LCapabilityBitrateCreate())
            ],
            new LCapabilitySpeed("Speed preset", "-preset", "medium",
                [new("faster", "Faster"), new("fast", "Fast"), new("medium", "Medium"), new("slow", "Slow"),
                 new("slower", "Slower")]),
            [new LCapabilityExtra("Profile", "-profile", "main", [new("main", "Main"), new("high", "High")])],
            "VVenC has no CRF. Quantizer range is 0-63 with default 32."));

        yield return new("libaom-av1", new(
            "libaom-av1",
            [
                new("Constant quality (CQ)", new("CRF", "-crf", "32", 0, 63), "-b:v 0"),
                new("Constrained quality", new("CRF", "-crf", "32", 0, 63), "-b:v 4M"),
                new("Target bitrate", LCapabilityBitrateCreate("4M")),
                new("Two-pass bitrate", LCapabilityBitrateCreate("4M"), "", 2),
                new("Lossless", null, "-aom-params lossless=1")
            ],
            new LCapabilitySpeed("Speed (cpu-used)", "-cpu-used", "1", LCapabilityNumbersCreate(0, 8, true)),
            [
                new LCapabilityExtra("Usage", "-usage", "good",
                    [new("good", "Good"), new("realtime", "Real-time"), new("allintra", "All-intra")]),
                new LCapabilityExtra("Tune", "-tune", "psnr", [new("psnr", "PSNR"), new("ssim", "SSIM")])
            ],
            "libaom has no -preset: speed is -cpu-used 0-8. Constant quality sets -b:v 0; " +
            "constrained quality caps the CRF target at 4M."));
        yield return new("libsvtav1", new(
            "libsvtav1",
            [
                new("CRF (constant rate factor)", new("CRF", "-crf", "35", 0, 63)),
                new("CQP (constant quantizer)", new("QP", "-qp", "35", 0, 63)),
                new("Target bitrate", LCapabilityBitrateCreate("4M")),
                new("CBR", LCapabilityBitrateCreate("4M"), "-svtav1-params rc=2")
            ],
            new LCapabilitySpeed("Speed preset", "-preset", "8", LCapabilityNumbersCreate(0, 13, true)),
            null,
            "SVT-AV1 preset is numeric 0-13, where 0 is slowest and 13 fastest - not an x264 word."));
        yield return new("librav1e", new(
            "librav1e",
            [
                new("Constant quantizer", new("Quantizer", "-qp", "100", 0, 255)),
                new("Target bitrate", LCapabilityBitrateCreate("4M"))
            ],
            new LCapabilitySpeed("Speed", "-speed", "6", LCapabilityNumbersCreate(0, 10, true)),
            null,
            "rav1e quantizer runs 0-255, not 0-51 or 0-63. There is no CRF and no preset."));
        yield return new("av1_qsv", LCapabilityQsvCreate("av1_qsv"));
        yield return new("av1_amf", new(
            "av1_amf",
            [
                new("CQP (constant quantizer)", new("QP (I-frame)", "-qp_i", "22", 0, 255), "-rc cqp"),
                new("QVBR (quality VBR)",
                    new("QVBR quality level", "-qvbr_quality_level", "23", 0, 51), "-rc qvbr"),
                new("Latency-constrained VBR", LCapabilityBitrateCreate(), "-rc vbr_latency"),
                new("Peak-constrained VBR", LCapabilityBitrateCreate(), "-rc vbr_peak"),
                new("CBR", LCapabilityBitrateCreate(), "-rc cbr"),
                new("High-quality VBR", LCapabilityBitrateCreate(), "-rc hqvbr"),
                new("High-quality CBR", LCapabilityBitrateCreate(), "-rc hqcbr")
            ],
            new LCapabilitySpeed("Quality preset", "-quality", "balanced",
                [new("speed", "Speed"), new("balanced", "Balanced"), new("quality", "Quality"),
                 new("high_quality", "High quality")]),
            [
                new LCapabilityExtra("Usage", "-usage", "transcoding",
                    [new("transcoding", "Transcoding"), new("ultralowlatency", "Ultra-low latency"),
                     new("lowlatency", "Low latency"), new("webcam", "Webcam"), new("high_quality", "High quality"),
                     new("lowlatency_high_quality", "Low latency, high quality")]),
                new LCapabilityExtra("Latency", "-latency", "none",
                    [new("none", "None"), new("power_saving_real_time", "Power-saving real-time"),
                     new("real_time", "Real-time"), new("lowest_latency", "Lowest latency")])
            ],
            "AV1 AMF differs from H.264/HEVC AMF: quantizer range is 0-255, there is no -qp_b, " +
            "and -quality adds high_quality."));
        yield return new("av1_nvenc", LCapabilityNvencCreate("av1_nvenc"));

        yield return new("libvpx", new(
            "libvpx",
            [
                new("Constant quality (CQ)", new("CRF", "-crf", "10", 0, 63), "-b:v 0"),
                new("Constrained quality", new("CRF", "-crf", "10", 0, 63), "-b:v 2M"),
                new("Target bitrate", LCapabilityBitrateCreate("2M")),
                new("CBR", LCapabilityBitrateCreate("2M"), LCapabilityCbrArgument)
            ],
            new LCapabilitySpeed("Speed (cpu-used)", "-cpu-used", "1", LCapabilityNumbersCreate(0, 16, true)),
            [new LCapabilityExtra("Deadline", "-deadline", "good",
                [new("best", "Best"), new("good", "Good"), new("realtime", "Real-time")])],
            "VP8 constant quality sets -b:v 0 alongside -crf; constrained quality caps it at 2M. " +
            "Speed is -cpu-used, not a preset."));
        yield return new("libvpx-vp9", new(
            "libvpx-vp9",
            [
                new("Constant quality (CQ)", new("CRF", "-crf", "31", 0, 63), "-b:v 0"),
                new("Constrained quality", new("CRF", "-crf", "31", 0, 63), "-b:v 2M"),
                new("Target bitrate", LCapabilityBitrateCreate("2M")),
                new("CBR", LCapabilityBitrateCreate("2M"), LCapabilityCbrArgument),
                new("Lossless", null, "-lossless 1")
            ],
            new LCapabilitySpeed("Speed (cpu-used)", "-cpu-used", "1", LCapabilityNumbersCreate(0, 8, true)),
            [new LCapabilityExtra("Deadline", "-deadline", "good",
                [new("best", "Best"), new("good", "Good"), new("realtime", "Real-time")])],
            "VP9 constant quality sets -b:v 0 alongside -crf; constrained quality caps it at 2M. " +
            "Lossless is -lossless 1."));
        yield return new("vp9_qsv", LCapabilityQsvCreate("vp9_qsv"));

        yield return new("libxvid", LCapabilityQscaleCreate("libxvid", "5", 1, 31,
            "Xvid uses the 1-31 quantizer scale (lower is better). No CRF, no preset."));
        yield return new("mpeg4", LCapabilityQscaleCreate("mpeg4", "5", 1, 31,
            "Native MPEG-4 uses the 1-31 quantizer scale (lower is better). No CRF, no preset."));

        yield return new("libtheora", new(
            "libtheora",
            [
                new("Constant quality (qscale)", new("qscale", "-q:v", "7", 0, 10, true)),
                new("Target bitrate", LCapabilityBitrateCreate("2M"))
            ],
            new LCapabilitySpeed("Speed level", "-speed_level", "1", LCapabilityNumbersCreate(0, 3, true)),
            null,
            "Theora quality is 0-10 and HIGHER is better - the opposite of CRF."));

        yield return new("prores", LCapabilityQscaleCreate("prores", "11", 1, 32,
            "Native prores exposes no encoder-specific options; -profile:v is rejected. " +
            "Use prores_ks for profile control."));
        yield return new("prores_aw", LCapabilityQscaleCreate("prores_aw", "11", 1, 32,
            "prores_aw exposes no encoder-specific options; -profile:v is rejected. " +
            "Use prores_ks for profile control."));
        yield return new("prores_ks", new(
            "prores_ks",
            [
                new("Constant quantizer (qscale)", new("qscale", "-q:v", "11", 1, 32)),
                new("Bits per macroblock", new("Bits per MB", "-bits_per_mb", "8000", 0, 8192)),
                new("Target bitrate", LCapabilityBitrateCreate("50M"))
            ],
            null,
            [new LCapabilityExtra("Profile", "-profile:v", "auto",
                [new("auto", "Auto"), new("proxy", "Proxy"), new("lt", "LT"), new("standard", "Standard"),
                 new("hq", "HQ"), new("4444", "4444"), new("4444xq", "4444 XQ")])],
            "ProRes quality is chosen mainly by profile, not by a quantizer. " +
            "prores_ks is the only ProRes encoder with -profile:v."));

        yield return new("ffv1", new(
            "ffv1",
            [new("Lossless (only mode)")],
            null,
            [
                new LCapabilityExtra("Coder", "-coder", "rice",
                    [new("rice", "Rice"), new("range_def", "Range (default table)"),
                     new("range_tab", "Range (custom table)")]),
                new LCapabilityExtra("Context", "-context", "0", [new("0", "Small"), new("1", "Large")]),
                new LCapabilityExtra("Slice CRC", "-slicecrc", "-1",
                    [new("-1", "Auto"), new("0", "Off"), new("1", "On")])
            ],
            "FFV1 is mathematically lossless. There is no quality control and no preset; " +
            "-q:v is accepted but ignored."));

        yield return new("mjpeg", LCapabilityQscaleCreate("mjpeg", "5", 2, 31,
            "MJPEG uses the 2-31 quantizer scale (lower is better). No CRF, no preset."));

        yield return new("jpeg2000", new(
            "jpeg2000",
            [
                new("Lossy (qscale)", new("qscale", "-q:v", "7", 1, 31)),
                new("Lossless (reversible DWT)", null, "-pred dwt53", 1, ["-pred"])
            ],
            null,
            [
                new LCapabilityExtra("Format", "-format", "jp2", [new("jp2", "JP2"), new("j2k", "J2K codestream")]),
                new LCapabilityExtra("DWT type", "-pred", "dwt97int",
                    [new("dwt97int", "9/7 integer (lossy)"), new("dwt53", "5/3 (lossless)")])
            ],
            "Lossless JPEG 2000 requires the reversible transform -pred dwt53."));
        yield return new("libopenjpeg", new(
            "libopenjpeg",
            [
                new("Lossless (reversible)"),
                new("Lossy (irreversible DWT)", new("qscale", "-q:v", "7", 1, 31), "-irreversible 1")
            ],
            null,
            [
                new LCapabilityExtra("Format", "-format", "jp2",
                    [new("jp2", "JP2"), new("j2k", "J2K codestream"), new("jpt", "JPT")]),
                new LCapabilityExtra("Profile", "-profile", "jpeg2000",
                    [new("jpeg2000", "JPEG 2000"), new("cinema2k", "Digital Cinema 2K"),
                     new("cinema4k", "Digital Cinema 4K")])
            ],
            "OpenJPEG defaults to lossless; -irreversible 1 switches to the lossy DWT. No preset."));

        yield return new("libwebp", LCapabilityWebpCreate("libwebp"));
        yield return new("libwebp_anim", LCapabilityWebpCreate("libwebp_anim"));

        yield return new("libxeve", new(
            "libxeve",
            [
                new("CQP (constant quantizer)", new("QP", "-qp", "32", 0, 51)),
                new("CRF (constant rate factor)", new("CRF", "-crf", "32", 10, 49)),
                new("ABR (target bitrate)", LCapabilityBitrateCreate("4M"))
            ],
            new LCapabilitySpeed("Speed preset", "-preset", "medium",
                [new("default", "Default"), new("fast", "Fast"), new("medium", "Medium"), new("slow", "Slow"),
                 new("placebo", "Placebo (slowest)")]),
            [
                new LCapabilityExtra("Tune", "-tune", "none",
                    [new("none", "None"), new("zerolatency", "Zero latency"), new("psnr", "PSNR")]),
                new LCapabilityExtra("Profile", "-profile", "baseline",
                    [new("baseline", "Baseline"), new("main", "Main")])
            ],
            "XEVE CRF range is 10-49, not the 0-51 of x264/x265."));
        yield return new("libxavs2", new(
            "libxavs2",
            [
                new("Constant QP", new("QP", "-qp", "34", 1, 63)),
                new("Target bitrate", LCapabilityBitrateCreate("4M"))
            ],
            new LCapabilitySpeed("Speed level", "-speed_level", "0", LCapabilityNumbersCreate(0, 9)),
            null,
            "xavs2 has no CRF and no named preset. Higher -speed_level is slower and better."));
        yield return new("liboapv", new(
            "liboapv",
            [
                new("CQP (constant quantizer)", new("QP", "-qp", "32", 0, 63)),
                new("Target bitrate", LCapabilityBitrateCreate("50M"))
            ],
            new LCapabilitySpeed("Speed preset", "-preset", "medium",
                [new("fastest", "Fastest"), new("fast", "Fast"), new("medium", "Medium"), new("slow", "Slow"),
                 new("placebo", "Placebo (slowest)")]),
            null,
            "APV is an intra-only professional codec. Quantizer range is 0-63; there is no CRF."));
    }
}
