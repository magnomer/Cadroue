namespace Cadroue.Core;

public static partial class LCapabilityTable
{
    private const string LCapabilityBitrateLabel = "Target bitrate";

    private static LCapabilityQuality LCapabilityBitrateCreate(
        string lDefault = "8M", string lOption = "-b:v", double lMinimum = 100, double lMaximum = 100000) =>
        new(LCapabilityBitrateLabel, lOption, lDefault, lMinimum, lMaximum);

    private static LCapabilityChoice[] LCapabilityNumbersCreate(int lFrom, int lTo) =>
        Enumerable.Range(lFrom, lTo - lFrom + 1).Select(lValue => (LCapabilityChoice)lValue.ToString()).ToArray();

    private static readonly LCapabilityChoice[] LCapabilityLibxPresets =
    [
        new("ultrafast", "Ultrafast"), new("superfast", "Superfast"), new("veryfast", "Very fast"),
        new("faster", "Faster"), new("fast", "Fast"), new("medium", "Medium"), new("slow", "Slow"),
        new("slower", "Slower"), new("veryslow", "Very slow"), new("placebo", "Placebo (slowest)")
    ];

    private static readonly LCapabilityChoice[] LCapabilityQsvPresets =
    [
        new("veryfast", "Very fast"), new("faster", "Faster"), new("fast", "Fast"), new("medium", "Medium"),
        new("slow", "Slow"), new("slower", "Slower"), new("veryslow", "Very slow")
    ];

    private static readonly LCapabilityChoice[] LCapabilityNvencPresets =
    [
        new("p1", "P1 (fastest)"), new("p2", "P2"), new("p3", "P3"), new("p4", "P4 (default)"),
        new("p5", "P5"), new("p6", "P6"), new("p7", "P7 (slowest)")
    ];

    private static LCapabilityCodec LCapabilityLibxCreate(string lEncoder, string lCrfDefault) => new(
        lEncoder,
        [
            new("CRF (constant quality)", new("CRF", "-crf", lCrfDefault, 0, 51)),
            new("CQP (constant quantizer)", new("QP", "-qp", lCrfDefault, 0, 51)),
            new("Target bitrate (ABR)", LCapabilityBitrateCreate()),
            new("Two-pass bitrate", LCapabilityBitrateCreate()),
            new("CBR", LCapabilityBitrateCreate()),
            new("Lossless")
        ],
        new LCapabilitySpeed("Speed preset", "-preset", "medium", LCapabilityLibxPresets),
        [new LCapabilityExtra("Tune", "-tune", "none",
            [new("none", "None"), new("film", "Film"), new("animation", "Animation"), new("grain", "Grain"),
             new("stillimage", "Still image"), new("fastdecode", "Fast decode"), new("zerolatency", "Zero latency")])],
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
        [new LCapabilityExtra("Low power", "-low_power", "auto",
            [new("auto", "Auto"), new("1", "On"), new("0", "Off")])],
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
        [new LCapabilityExtra("Tune", "-tune", "hq",
            [new("hq", "High quality"), new("uhq", "Ultra-high quality"), new("ll", "Low latency"),
             new("ull", "Ultra-low latency"), new("lossless", "Lossless")])],
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
        new LCapabilitySpeed("Quality preset", "-quality", "balanced",
            [new("balanced", "Balanced"), new("speed", "Speed"), new("quality", "Quality")]),
        [new LCapabilityExtra("Usage", "-usage", "transcoding",
            [new("transcoding", "Transcoding"), new("ultralowlatency", "Ultra-low latency"), new("lowlatency", "Low latency"),
             new("webcam", "Webcam"), new("high_quality", "High quality"), new("lowlatency_high_quality", "Low latency, high quality")])],
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
        [new LCapabilityExtra("Scenario", "-scenario", "default",
            [new("default", "Default"), new("display_remoting", "Display remoting"), new("video_conference", "Video conference"),
             new("archive", "Archive"), new("live_streaming", "Live streaming"), new("camera_record", "Camera record")])],
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
        LCapabilityFirstRead()
            .Concat(LCapabilitySecondRead())
            .Concat(LCapabilityThirdRead())
            .ToDictionary(pEntry => pEntry.Key, pEntry => pEntry.Value, StringComparer.OrdinalIgnoreCase);

    private static LCapabilityCodec LCapabilityWebpCreate(string lEncoder) => new(
        lEncoder,
        [
            new("Lossy quality", new("Quality", "-quality", "75", 0, 100, true)),
            new("Lossless")
        ],
        null,
        [new LCapabilityExtra("Content preset", "-preset", "default",
            [new("none", "None"), new("default", "Default"), new("picture", "Picture"), new("photo", "Photo"),
             new("drawing", "Drawing"), new("icon", "Icon"), new("text", "Text")])],
        "WebP quality is 0-100 and HIGHER is better. Its -preset picks a content type (photo, drawing, icon...), not an encoding speed.");
}
