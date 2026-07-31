namespace Cadroue.Core;

public static partial class LCapabilityTable
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
        [new LCapabilityExtra("Content preset", "-preset", "default", ["none", "default", "picture", "photo", "drawing", "icon", "text"])],
        "WebP quality is 0-100 and HIGHER is better. Its -preset picks a content type (photo, drawing, icon...), not an encoding speed.");
}
