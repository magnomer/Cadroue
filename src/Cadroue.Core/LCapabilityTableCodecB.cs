namespace Cadroue.Core;

public static partial class LCapabilityTable
{
    private static IEnumerable<KeyValuePair<string, LCapabilityCodec>> LCapabilityCodecBRead()
    {
        yield return new("libvpx", new(
            "libvpx",
            [
                new("Constant quality (CQ)", new("CRF", "-crf", "10", 0, 63)),
                new("Constrained quality", new("CRF", "-crf", "10", 0, 63)),
                new("Target bitrate", LCapabilityBitrateCreate("2M")),
                new("CBR", LCapabilityBitrateCreate("2M"))
            ],
            new LCapabilitySpeed("Speed (cpu-used)", "-cpu-used", "1", LCapabilityNumbersCreate(0, 16)),
            [new LCapabilityExtra("Deadline", "-deadline", "good", ["best", "good", "realtime"])],
            "VP8 constant quality requires -b:v 0 alongside -crf. Speed is -cpu-used, not a preset."));
        yield return new("libvpx-vp9", new(
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
            "VP9 constant quality requires -b:v 0 alongside -crf. Lossless is -lossless 1."));
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
            new LCapabilitySpeed("Speed level", "-speed_level", "1", LCapabilityNumbersCreate(0, 3)),
            null,
            "Theora quality is 0-10 and HIGHER is better - the opposite of CRF."));

        yield return new("prores", LCapabilityQscaleCreate("prores", "11", 1, 32,
            "Native prores exposes no encoder-specific options; -profile:v is rejected. Use prores_ks for profile control."));
        yield return new("prores_aw", LCapabilityQscaleCreate("prores_aw", "11", 1, 32,
            "prores_aw exposes no encoder-specific options; -profile:v is rejected. Use prores_ks for profile control."));
        yield return new("prores_ks", new(
            "prores_ks",
            [
                new("Constant quantizer (qscale)", new("qscale", "-q:v", "11", 1, 32)),
                new("Bits per macroblock", new("Bits per MB", "-bits_per_mb", "8000", 0, 8192)),
                new("Target bitrate", LCapabilityBitrateCreate("50M"))
            ],
            null,
            [new LCapabilityExtra("Profile", "-profile:v", "auto", ["auto", "proxy", "lt", "standard", "hq", "4444", "4444xq"])],
            "ProRes quality is chosen mainly by profile, not by a quantizer. prores_ks is the only ProRes encoder with -profile:v."));

        yield return new("ffv1", new(
            "ffv1",
            [new("Lossless (only mode)")],
            null,
            [
                new LCapabilityExtra("Coder", "-coder", "rice", ["rice", "range_def", "range_tab"]),
                new LCapabilityExtra("Context", "-context", "0", ["0", "1"]),
                new LCapabilityExtra("Slice CRC", "-slicecrc", "-1", ["-1", "0", "1"])
            ],
            "FFV1 is mathematically lossless. There is no quality control and no preset; -q:v is accepted but ignored."));

        yield return new("mjpeg", LCapabilityQscaleCreate("mjpeg", "5", 2, 31,
            "MJPEG uses the 2-31 quantizer scale (lower is better). No CRF, no preset."));

        yield return new("jpeg2000", new(
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
            "Lossless JPEG 2000 requires the reversible transform -pred dwt53."));
        yield return new("libopenjpeg", new(
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
            "OpenJPEG defaults to lossless; -irreversible 1 switches to the lossy DWT. No preset."));

        yield return new("libwebp", LCapabilityWebpCreate("libwebp"));
        yield return new("libwebp_anim", LCapabilityWebpCreate("libwebp_anim"));
    }
}
