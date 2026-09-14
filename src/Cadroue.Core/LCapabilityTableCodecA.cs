namespace Cadroue.Core;

public static partial class LCapabilityTable
{
    private static IEnumerable<KeyValuePair<string, LCapabilityCodec>> LCapabilityFirstRead()
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
    }
}
