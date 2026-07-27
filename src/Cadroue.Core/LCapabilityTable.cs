namespace Cadroue.Core;

/// <summary>
/// Per-encoder rate-control shapes, verified against FFmpeg 8.1.2 encoder help.
/// There is no shared "CRF / preset" pair: only 7 of these encoders have a real
/// -crf, ranges disagree, three invert the quality direction, and libwebp's
/// -preset selects content type rather than speed.
/// </summary>
public static class LCapabilityTable
{
    private const string LCapabilityBitrateLabel = "Target bitrate";

    private static LCapabilityQuality LCapabilityBitrateCreate(string lDefault = "8M") =>
        new(LCapabilityBitrateLabel, "-b:v", lDefault);

    private static string[] LCapabilityNumbersCreate(int lFrom, int lTo) =>
        Enumerable.Range(lFrom, lTo - lFrom + 1).Select(lValue => lValue.ToString()).ToArray();

    private static readonly string[] LCapabilityX26xPresets =
    [
        "ultrafast", "superfast", "veryfast", "faster", "fast",
        "medium", "slow", "slower", "veryslow", "placebo"
    ];

    private static readonly string[] LCapabilityQsvPresets =
        ["veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow"];

    private static readonly string[] LCapabilityNvencPresets =
        ["p1", "p2", "p3", "p4", "p5", "p6", "p7"];

    // ---- family factories -------------------------------------------------

    private static LCapabilityCodec LCapabilityX26xCreate(string lEncoder, string lCrfDefault) => new(
        lEncoder,
        [
            new("CRF (constant quality)", new("CRF", "-crf", lCrfDefault, 0, 51)),
            new("CQP (constant quantizer)", new("QP", "-qp", lCrfDefault, 0, 51)),
            new("Target bitrate (ABR)", LCapabilityBitrateCreate()),
            new("Two-pass bitrate", LCapabilityBitrateCreate()),
            new("CBR", LCapabilityBitrateCreate()),
            new("Lossless")
        ],
        new LCapabilitySpeed("Speed preset", "-preset", "medium", LCapabilityX26xPresets),
        [new LCapabilityExtra("Tune", "-tune", "none", ["none", "film", "animation", "grain", "stillimage", "fastdecode", "zerolatency"])],
        $"{lEncoder} CRF default is {lCrfDefault}. Lossless uses -crf 0 (x264) or -x265-params lossless=1 (x265).");

    private static LCapabilityCodec LCapabilityQsvCreate(string lEncoder) => new(
        lEncoder,
        [
            new("ICQ (constant quality)", new("Global quality", "-global_quality", "23", 1, 51)),
            new("LA-ICQ (lookahead quality)", new("Global quality", "-global_quality", "23", 1, 51)),
            new("CQP (constant quantizer)", new("QP", "-q", "23", 0, 51)),
            new("VBR (target bitrate)", LCapabilityBitrateCreate()),
            new("CBR", LCapabilityBitrateCreate())
        ],
        new LCapabilitySpeed("Speed preset", "-preset", "medium", LCapabilityQsvPresets),
        [new LCapabilityExtra("Low power", "-low_power", "auto", ["auto", "1", "0"])],
        "QSV has no -rc option: the mode follows from which of -b:v, -global_quality and -look_ahead is set. Preset numbering is inverted (veryslow=1, veryfast=7).");

    private static LCapabilityCodec LCapabilityNvencCreate(string lEncoder) => new(
        lEncoder,
        [
            new("Constant quality (VBR + CQ)", new("CQ", "-cq", "23", 0, 51)),
            new("CQP (constant quantizer)", new("QP", "-qp", "23", 0, 51)),
            new("VBR (target bitrate)", LCapabilityBitrateCreate()),
            new("CBR", LCapabilityBitrateCreate()),
            new("Lossless")
        ],
        new LCapabilitySpeed("Speed preset", "-preset", "p4", LCapabilityNvencPresets),
        [new LCapabilityExtra("Tune", "-tune", "hq", ["hq", "uhq", "ll", "ull", "lossless"])],
        "NVENC -cq 0 means automatic. Legacy presets (slow/medium/fast/hp/hq/ll/llhq/llhp/lossless) still parse but are deprecated in favour of p1-p7.");

    private static LCapabilityCodec LCapabilityAmfCreate(string lEncoder) => new(
        lEncoder,
        [
            new("CQP (constant quantizer)", new("QP (I-frame)", "-qp_i", "22", 0, 51)),
            new("QVBR (quality VBR)", new("QVBR quality level", "-qvbr_quality_level", "23", 0, 51)),
            new("Peak-constrained VBR", LCapabilityBitrateCreate()),
            new("Latency-constrained VBR", LCapabilityBitrateCreate()),
            new("CBR", LCapabilityBitrateCreate()),
            new("High-quality VBR", LCapabilityBitrateCreate()),
            new("High-quality CBR", LCapabilityBitrateCreate())
        ],
        new LCapabilitySpeed("Quality preset", "-quality", "balanced", ["balanced", "speed", "quality"]),
        [new LCapabilityExtra("Usage", "-usage", "transcoding", ["transcoding", "ultralowlatency", "lowlatency", "webcam", "high_quality", "lowlatency_high_quality"])],
        "AMF uses -qp_i/-qp_p/-qp_b rather than a single quantizer. There is no -preset in the x264 sense.");

    private static LCapabilityCodec LCapabilityMfCreate(string lEncoder) => new(
        lEncoder,
        [
            new("Quality", new("Quality", "-quality", "75", 0, 100, true)),
            new("CBR", LCapabilityBitrateCreate()),
            new("Peak-constrained VBR", LCapabilityBitrateCreate()),
            new("Unconstrained VBR", LCapabilityBitrateCreate()),
            new("Low-delay VBR", LCapabilityBitrateCreate()),
            new("Global VBR", LCapabilityBitrateCreate()),
            new("Global low-delay VBR", LCapabilityBitrateCreate()),
            new("Encoder default")
        ],
        null,
        [new LCapabilityExtra("Scenario", "-scenario", "default", ["default", "display_remoting", "video_conference", "archive", "live_streaming", "camera_record"])],
        "Media Foundation quality is 0-100 and HIGHER is better - the opposite of CRF. It has no speed preset.");

    private static LCapabilityCodec LCapabilityQscaleCreate(
        string lEncoder, string lDefault, double lMinimum, double lMaximum, string lNotice) => new(
        lEncoder,
        [
            new("Constant quantizer (qscale)", new("qscale", "-q:v", lDefault, lMinimum, lMaximum)),
            new("Target bitrate", LCapabilityBitrateCreate("4M"))
        ],
        null,
        null,
        lNotice);

    // ---- registry ---------------------------------------------------------

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
        new Dictionary<string, LCapabilityCodec>(StringComparer.OrdinalIgnoreCase)
        {
            // H.264
            ["libx264"] = LCapabilityX26xCreate("libx264", "23"),
            ["h264_mf"] = LCapabilityMfCreate("h264_mf"),
            ["libopenh264"] = new(
                "libopenh264",
                [
                    new("Quality mode (bitrate targeted)", LCapabilityBitrateCreate()),
                    new("Bitrate mode", LCapabilityBitrateCreate()),
                    new("Buffer mode", LCapabilityBitrateCreate()),
                    new("Timestamp mode", LCapabilityBitrateCreate()),
                    new("Rate control off")
                ],
                null,
                [new LCapabilityExtra("Profile", "-profile", "constrained_baseline", ["constrained_baseline", "main", "high"])],
                "OpenH264 has no CRF, no QP and no preset. Every mode is bitrate driven via -rc_mode."),
            ["h264_qsv"] = LCapabilityQsvCreate("h264_qsv"),
            ["h264_amf"] = LCapabilityAmfCreate("h264_amf"),
            ["h264_nvenc"] = LCapabilityNvencCreate("h264_nvenc"),

            // H.265
            ["libx265"] = LCapabilityX26xCreate("libx265", "28"),
            ["hevc_qsv"] = LCapabilityQsvCreate("hevc_qsv"),
            ["hevc_amf"] = LCapabilityAmfCreate("hevc_amf"),
            ["hevc_mf"] = LCapabilityMfCreate("hevc_mf"),
            ["hevc_nvenc"] = LCapabilityNvencCreate("hevc_nvenc"),

            // H.266
            ["libvvenc"] = new(
                "libvvenc",
                [
                    new("Constant QP", new("QP", "-qp", "32", 0, 63)),
                    new("Target bitrate", LCapabilityBitrateCreate())
                ],
                new LCapabilitySpeed("Speed preset", "-preset", "medium", ["faster", "fast", "medium", "slow", "slower"]),
                [new LCapabilityExtra("Profile", "-profile", "main", ["main", "high"])],
                "VVenC has no CRF. Quantizer range is 0-63 with default 32."),

            // AV1
            ["libaom-av1"] = new(
                "libaom-av1",
                [
                    new("Constant quality (CQ)", new("CRF", "-crf", "32", 0, 63)),
                    new("Constrained quality", new("CRF", "-crf", "32", 0, 63)),
                    new("Target bitrate", LCapabilityBitrateCreate("4M")),
                    new("Two-pass bitrate", LCapabilityBitrateCreate("4M")),
                    new("Lossless")
                ],
                new LCapabilitySpeed("Speed (cpu-used)", "-cpu-used", "1", LCapabilityNumbersCreate(0, 8)),
                [
                    new LCapabilityExtra("Usage", "-usage", "good", ["good", "realtime", "allintra"]),
                    new LCapabilityExtra("Tune", "-tune", "psnr", ["psnr", "ssim"])
                ],
                "libaom has no -preset: speed is -cpu-used 0-8. Constant quality also requires -b:v 0."),
            ["libsvtav1"] = new(
                "libsvtav1",
                [
                    new("CRF (constant rate factor)", new("CRF", "-crf", "35", 0, 63)),
                    new("CQP (constant quantizer)", new("QP", "-qp", "35", 0, 63)),
                    new("Target bitrate", LCapabilityBitrateCreate("4M")),
                    new("CBR", LCapabilityBitrateCreate("4M"))
                ],
                new LCapabilitySpeed("Speed preset", "-preset", "8", LCapabilityNumbersCreate(0, 13)),
                null,
                "SVT-AV1 preset is numeric 0-13, where 0 is slowest and 13 fastest - not an x264 word."),
            ["librav1e"] = new(
                "librav1e",
                [
                    new("Constant quantizer", new("Quantizer", "-qp", "100", 0, 255)),
                    new("Target bitrate", LCapabilityBitrateCreate("4M"))
                ],
                new LCapabilitySpeed("Speed", "-speed", "6", LCapabilityNumbersCreate(0, 10)),
                null,
                "rav1e quantizer runs 0-255, not 0-51 or 0-63. There is no CRF and no preset."),
            ["av1_qsv"] = LCapabilityQsvCreate("av1_qsv"),
            ["av1_amf"] = new(
                "av1_amf",
                [
                    new("CQP (constant quantizer)", new("QP (I-frame)", "-qp_i", "22", 0, 255)),
                    new("QVBR (quality VBR)", new("QVBR quality level", "-qvbr_quality_level", "23", 0, 51)),
                    new("Latency-constrained VBR", LCapabilityBitrateCreate()),
                    new("Peak-constrained VBR", LCapabilityBitrateCreate()),
                    new("CBR", LCapabilityBitrateCreate()),
                    new("High-quality VBR", LCapabilityBitrateCreate()),
                    new("High-quality CBR", LCapabilityBitrateCreate())
                ],
                new LCapabilitySpeed("Quality preset", "-quality", "balanced", ["high_quality", "quality", "balanced", "speed"]),
                [
                    new LCapabilityExtra("Usage", "-usage", "transcoding", ["transcoding", "ultralowlatency", "lowlatency", "webcam", "high_quality", "lowlatency_high_quality"]),
                    new LCapabilityExtra("Latency", "-latency", "none", ["none", "power_saving_real_time", "real_time", "lowest_latency"])
                ],
                "AV1 AMF differs from H.264/HEVC AMF: quantizer range is 0-255, there is no -qp_b, and -quality adds high_quality."),
            ["av1_nvenc"] = LCapabilityNvencCreate("av1_nvenc"),

            // VP8 / VP9
            ["libvpx"] = new(
                "libvpx",
                [
                    new("Constant quality (CQ)", new("CRF", "-crf", "10", 0, 63)),
                    new("Constrained quality", new("CRF", "-crf", "10", 0, 63)),
                    new("Target bitrate", LCapabilityBitrateCreate("2M")),
                    new("CBR", LCapabilityBitrateCreate("2M"))
                ],
                new LCapabilitySpeed("Speed (cpu-used)", "-cpu-used", "1", LCapabilityNumbersCreate(0, 16)),
                [new LCapabilityExtra("Deadline", "-deadline", "good", ["best", "good", "realtime"])],
                "VP8 constant quality requires -b:v 0 alongside -crf. Speed is -cpu-used, not a preset."),
            ["libvpx-vp9"] = new(
                "libvpx-vp9",
                [
                    new("Constant quality (CQ)", new("CRF", "-crf", "31", 0, 63)),
                    new("Constrained quality", new("CRF", "-crf", "31", 0, 63)),
                    new("Target bitrate", LCapabilityBitrateCreate("2M")),
                    new("CBR", LCapabilityBitrateCreate("2M")),
                    new("Lossless")
                ],
                new LCapabilitySpeed("Speed (cpu-used)", "-cpu-used", "1", LCapabilityNumbersCreate(0, 8)),
                [new LCapabilityExtra("Deadline", "-deadline", "good", ["best", "good", "realtime"])],
                "VP9 constant quality requires -b:v 0 alongside -crf. Lossless is -lossless 1."),
            ["vp9_qsv"] = LCapabilityQsvCreate("vp9_qsv"),

            // MPEG-4 Part 2
            ["libxvid"] = LCapabilityQscaleCreate("libxvid", "5", 1, 31,
                "Xvid uses the 1-31 quantizer scale (lower is better). No CRF, no preset."),
            ["mpeg4"] = LCapabilityQscaleCreate("mpeg4", "5", 1, 31,
                "Native MPEG-4 uses the 1-31 quantizer scale (lower is better). No CRF, no preset."),

            // Theora
            ["libtheora"] = new(
                "libtheora",
                [
                    new("Constant quality (qscale)", new("qscale", "-q:v", "7", 0, 10, true)),
                    new("Target bitrate", LCapabilityBitrateCreate("2M"))
                ],
                new LCapabilitySpeed("Speed level", "-speed_level", "1", LCapabilityNumbersCreate(0, 3)),
                null,
                "Theora quality is 0-10 and HIGHER is better - the opposite of CRF."),

            // ProRes
            ["prores"] = LCapabilityQscaleCreate("prores", "11", 1, 32,
                "Native prores exposes no encoder-specific options; -profile:v is rejected. Use prores_ks for profile control."),
            ["prores_aw"] = LCapabilityQscaleCreate("prores_aw", "11", 1, 32,
                "prores_aw exposes no encoder-specific options; -profile:v is rejected. Use prores_ks for profile control."),
            ["prores_ks"] = new(
                "prores_ks",
                [
                    new("Constant quantizer (qscale)", new("qscale", "-q:v", "11", 1, 32)),
                    new("Bits per macroblock", new("Bits per MB", "-bits_per_mb", "8000", 0, 8192)),
                    new("Target bitrate", LCapabilityBitrateCreate("50M"))
                ],
                null,
                [new LCapabilityExtra("Profile", "-profile:v", "auto", ["auto", "proxy", "lt", "standard", "hq", "4444", "4444xq"])],
                "ProRes quality is chosen mainly by profile, not by a quantizer. prores_ks is the only ProRes encoder with -profile:v."),

            // Lossless / intra
            ["ffv1"] = new(
                "ffv1",
                [new("Lossless (only mode)")],
                null,
                [
                    new LCapabilityExtra("Coder", "-coder", "rice", ["rice", "range_def", "range_tab"]),
                    new LCapabilityExtra("Context", "-context", "0", ["0", "1"]),
                    new LCapabilityExtra("Slice CRC", "-slicecrc", "-1", ["-1", "0", "1"])
                ],
                "FFV1 is mathematically lossless. There is no quality control and no preset; -q:v is accepted but ignored."),

            // MJPEG
            ["mjpeg"] = LCapabilityQscaleCreate("mjpeg", "5", 2, 31,
                "MJPEG uses the 2-31 quantizer scale (lower is better). No CRF, no preset."),

            // JPEG 2000
            ["jpeg2000"] = new(
                "jpeg2000",
                [
                    new("Lossy (qscale)", new("qscale", "-q:v", "7", 1, 31)),
                    new("Lossless (reversible DWT)")
                ],
                null,
                [
                    new LCapabilityExtra("Format", "-format", "jp2", ["jp2", "j2k"]),
                    new LCapabilityExtra("DWT type", "-pred", "dwt97int", ["dwt97int", "dwt53"])
                ],
                "Lossless JPEG 2000 requires the reversible transform -pred dwt53."),
            ["libopenjpeg"] = new(
                "libopenjpeg",
                [
                    new("Lossless (reversible)"),
                    new("Lossy (irreversible DWT)", new("qscale", "-q:v", "7", 1, 31))
                ],
                null,
                [
                    new LCapabilityExtra("Format", "-format", "jp2", ["jp2", "j2k", "jpt"]),
                    new LCapabilityExtra("Profile", "-profile", "jpeg2000", ["jpeg2000", "cinema2k", "cinema4k"])
                ],
                "OpenJPEG defaults to lossless; -irreversible 1 switches to the lossy DWT. No preset."),

            // WebP
            ["libwebp"] = LCapabilityWebpCreate("libwebp"),
            ["libwebp_anim"] = LCapabilityWebpCreate("libwebp_anim"),

            // EVC / AVS2 / APV
            ["libxeve"] = new(
                "libxeve",
                [
                    new("CQP (constant quantizer)", new("QP", "-qp", "32", 0, 51)),
                    new("CRF (constant rate factor)", new("CRF", "-crf", "32", 10, 49)),
                    new("ABR (target bitrate)", LCapabilityBitrateCreate("4M"))
                ],
                new LCapabilitySpeed("Speed preset", "-preset", "medium", ["default", "fast", "medium", "slow", "placebo"]),
                [
                    new LCapabilityExtra("Tune", "-tune", "none", ["none", "zerolatency", "psnr"]),
                    new LCapabilityExtra("Profile", "-profile", "baseline", ["baseline", "main"])
                ],
                "XEVE CRF range is 10-49, not the 0-51 of x264/x265."),
            ["libxavs2"] = new(
                "libxavs2",
                [
                    new("Constant QP", new("QP", "-qp", "34", 1, 63)),
                    new("Target bitrate", LCapabilityBitrateCreate("4M"))
                ],
                new LCapabilitySpeed("Speed level", "-speed_level", "0", LCapabilityNumbersCreate(0, 9)),
                null,
                "xavs2 has no CRF and no named preset. Higher -speed_level is slower and better."),
            ["liboapv"] = new(
                "liboapv",
                [
                    new("CQP (constant quantizer)", new("QP", "-qp", "32", 0, 63)),
                    new("Target bitrate", LCapabilityBitrateCreate("50M"))
                ],
                new LCapabilitySpeed("Speed preset", "-preset", "medium", ["fastest", "fast", "medium", "slow", "placebo"]),
                null,
                "APV is an intra-only professional codec. Quantizer range is 0-63; there is no CRF.")
        };

    private static LCapabilityCodec LCapabilityWebpCreate(string lEncoder) => new(
        lEncoder,
        [
            new("Lossy quality", new("Quality", "-quality", "75", 0, 100, true)),
            new("Lossless")
        ],
        null,
        [new LCapabilityExtra("Content preset", "-preset", "default", ["none", "default", "picture", "photo", "drawing", "icon", "text"])],
        "WebP quality is 0-100 and HIGHER is better. Its -preset picks a content type (photo, drawing, icon...), not an encoding speed.");
}
