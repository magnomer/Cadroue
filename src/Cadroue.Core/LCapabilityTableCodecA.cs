namespace Cadroue.Core;

public static partial class LCapabilityTable
{
    private static IEnumerable<KeyValuePair<string, LCapabilityCodec>> LCapabilityCodecARead()
    {
        yield return new("libx264", LCapabilityX26xCreate("libx264", "23"));
        yield return new("h264_mf", LCapabilityMfCreate("h264_mf"));
        yield return new("libopenh264", new(
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
            "OpenH264 has no CRF, no QP and no preset. Every mode is bitrate driven via -rc_mode."));
        yield return new("h264_qsv", LCapabilityQsvCreate("h264_qsv"));
        yield return new("h264_amf", LCapabilityAmfCreate("h264_amf"));
        yield return new("h264_nvenc", LCapabilityNvencCreate("h264_nvenc"));

        yield return new("libx265", LCapabilityX26xCreate("libx265", "28"));
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
            new LCapabilitySpeed("Speed preset", "-preset", "medium", ["faster", "fast", "medium", "slow", "slower"]),
            [new LCapabilityExtra("Profile", "-profile", "main", ["main", "high"])],
            "VVenC has no CRF. Quantizer range is 0-63 with default 32."));

        yield return new("libaom-av1", new(
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
            "libaom has no -preset: speed is -cpu-used 0-8. Constant quality also requires -b:v 0."));
        yield return new("libsvtav1", new(
            "libsvtav1",
            [
                new("CRF (constant rate factor)", new("CRF", "-crf", "35", 0, 63)),
                new("CQP (constant quantizer)", new("QP", "-qp", "35", 0, 63)),
                new("Target bitrate", LCapabilityBitrateCreate("4M")),
                new("CBR", LCapabilityBitrateCreate("4M"))
            ],
            new LCapabilitySpeed("Speed preset", "-preset", "8", LCapabilityNumbersCreate(0, 13)),
            null,
            "SVT-AV1 preset is numeric 0-13, where 0 is slowest and 13 fastest - not an x264 word."));
        yield return new("librav1e", new(
            "librav1e",
            [
                new("Constant quantizer", new("Quantizer", "-qp", "100", 0, 255)),
                new("Target bitrate", LCapabilityBitrateCreate("4M"))
            ],
            new LCapabilitySpeed("Speed", "-speed", "6", LCapabilityNumbersCreate(0, 10)),
            null,
            "rav1e quantizer runs 0-255, not 0-51 or 0-63. There is no CRF and no preset."));
        yield return new("av1_qsv", LCapabilityQsvCreate("av1_qsv"));
        yield return new("av1_amf", new(
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
            "AV1 AMF differs from H.264/HEVC AMF: quantizer range is 0-255, there is no -qp_b, and -quality adds high_quality."));
        yield return new("av1_nvenc", LCapabilityNvencCreate("av1_nvenc"));
    }
}
