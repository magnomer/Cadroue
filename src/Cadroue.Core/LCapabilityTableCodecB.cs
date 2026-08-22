namespace Cadroue.Core;

public static partial class LCapabilityTable
{
    private static IEnumerable<KeyValuePair<string, LCapabilityCodec>> LCapabilitySecondRead()
    {
        yield return new("libvpx", new(
            "libvpx",
            [
                new("Constant quality (CQ)", new("CRF", "-crf", "10", 0, 63)),
                new("Constrained quality", new("CRF", "-crf", "10", 0, 63)),
                new("Target bitrate", LCapabilityBitrateCreate("2M")),
                new("CBR", LCapabilityBitrateCreate("2M"))
            ],
            new LCapabilitySpeed("Speed (cpu-used)", "-cpu-used", "1", LCapabilityNumbersCreate(0, 16, true)),
            [new LCapabilityExtra("Deadline", "-deadline", "good",
                [new("best", "Best"), new("good", "Good"), new("realtime", "Real-time")])],
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
            new LCapabilitySpeed("Speed (cpu-used)", "-cpu-used", "1", LCapabilityNumbersCreate(0, 8, true)),
            [new LCapabilityExtra("Deadline", "-deadline", "good",
                [new("best", "Best"), new("good", "Good"), new("realtime", "Real-time")])],
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
            new LCapabilitySpeed("Speed level", "-speed_level", "1", LCapabilityNumbersCreate(0, 3, true)),
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
            [new LCapabilityExtra("Profile", "-profile:v", "auto",
                [new("auto", "Auto"), new("proxy", "Proxy"), new("lt", "LT"), new("standard", "Standard"),
                 new("hq", "HQ"), new("4444", "4444"), new("4444xq", "4444 XQ")])],
            "ProRes quality is chosen mainly by profile, not by a quantizer. prores_ks is the only ProRes encoder with -profile:v."));

        yield return new("ffv1", new(
            "ffv1",
            [new("Lossless (only mode)")],
            null,
            [
                new LCapabilityExtra("Coder", "-coder", "rice",
                    [new("rice", "Rice"), new("range_def", "Range (default table)"), new("range_tab", "Range (custom table)")]),
                new LCapabilityExtra("Context", "-context", "0", [new("0", "Small"), new("1", "Large")]),
                new LCapabilityExtra("Slice CRC", "-slicecrc", "-1", [new("-1", "Auto"), new("0", "Off"), new("1", "On")])
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
                new LCapabilityExtra("Format", "-format", "jp2", [new("jp2", "JP2"), new("j2k", "J2K codestream")]),
                new LCapabilityExtra("DWT type", "-pred", "dwt97int",
                    [new("dwt97int", "9/7 integer (lossy)"), new("dwt53", "5/3 (lossless)")])
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
                new LCapabilityExtra("Format", "-format", "jp2", [new("jp2", "JP2"), new("j2k", "J2K codestream"), new("jpt", "JPT")]),
                new LCapabilityExtra("Profile", "-profile", "jpeg2000",
                    [new("jpeg2000", "JPEG 2000"), new("cinema2k", "Digital Cinema 2K"), new("cinema4k", "Digital Cinema 4K")])
            ],
            "OpenJPEG defaults to lossless; -irreversible 1 switches to the lossy DWT. No preset."));

        yield return new("libwebp", LCapabilityWebpCreate("libwebp"));
        yield return new("libwebp_anim", LCapabilityWebpCreate("libwebp_anim"));
    }
}
