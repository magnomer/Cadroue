namespace Cadroue.Core;

public static partial class LCapabilityTable
{
    public static LCapabilityCodec LCapabilityAudioFallback { get; } = new(
        "audio",
        [new("Target bitrate", LCapabilityBitrateCreate("192k", "-b:a", 6, 512))],
        null,
        null,
        "Generic encoder: only a target bitrate is offered.");

    public static LCapabilityCodec LCapabilityAudioUncompressed { get; } = new(
        "pcm",
        [new("Uncompressed")],
        null,
        null,
        "Uncompressed PCM: the encoder itself fixes the sample format, so no bitrate or quality applies.");

    public static IReadOnlyDictionary<string, LCapabilityCodec> LCapabilityAudioMap { get; } =
        LCapabilityAudioCreate()
            .ToDictionary(pEntry => pEntry.Key, pEntry => pEntry.Value, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<KeyValuePair<string, LCapabilityCodec>> LCapabilityAudioCreate()
    {
        yield return new("aac", new(
            "aac",
            [
                new("Target bitrate", LCapabilityBitrateCreate("192k", "-b:a", 6, 512)),
                new("VBR quality (experimental)", new("VBR quality", "-q:a", "1", 0.1, 2, true))
            ],
            null,
            null,
            "Native AAC. -q:a VBR is experimental and its bitrate varies; a target bitrate is the reliable control."));

        yield return new("libfdk_aac", new(
            "libfdk_aac",
            [
                new("Target bitrate (CBR)", LCapabilityBitrateCreate("192k", "-b:a", 6, 512)),
                new("VBR", new("VBR level", "-vbr", "4", 1, 5, true))
            ],
            null,
            [
                new LCapabilityExtra("Profile", "-profile:a", "aac_low",
                    [new("aac_low", "AAC-LC"), new("aac_he", "HE-AAC"), new("aac_he_v2", "HE-AAC v2"),
                     new("aac_ld", "AAC-LD"), new("aac_eld", "AAC-ELD")]),
                new LCapabilityExtra("Afterburner", "-afterburner", "1", [new("1", "On"), new("0", "Off")])
            ],
            "Fraunhofer FDK AAC. Requires a special (often non-free) FFmpeg build; HE/HEv2 suit low bitrates."));

        yield return new("aac_mf", new(
            "aac_mf",
            [new("Target bitrate", LCapabilityBitrateCreate("192k", "-b:a", 6, 512))],
            null,
            null,
            "Media Foundation AAC (Windows system encoder)."));

        yield return new("libmp3lame", new(
            "libmp3lame",
            [
                new("VBR (quality)", new("VBR quality", "-q:a", "2", 0, 9)),
                new("ABR (average bitrate)", LCapabilityBitrateCreate("192k", "-b:a", 8, 320)),
                new("CBR", LCapabilityBitrateCreate("192k", "-b:a", 8, 320))
            ],
            null,
            null,
            "LAME MP3. VBR quality runs 0-9 where 0 is best (about V0); ABR and CBR use a target bitrate."));

        yield return new("libshine", new(
            "libshine",
            [new("CBR", LCapabilityBitrateCreate("128k", "-b:a", 8, 320))],
            null,
            null,
            "Shine is a fixed-point CBR-only MP3 encoder, lower quality than LAME. Use only where fixed-point speed matters."));

        yield return new("mp3_mf", new(
            "mp3_mf",
            [new("CBR", LCapabilityBitrateCreate("192k", "-b:a", 8, 320))],
            null,
            null,
            "Media Foundation MP3 (Windows system encoder)."));

        yield return new("libopus", LCapabilityOpusCreate("libopus"));
        yield return new("opus", LCapabilityOpusCreate("opus"));

        yield return new("libvorbis", new(
            "libvorbis",
            [
                new("VBR (quality)", new("VBR quality", "-q:a", "5", -1, 10, true)),
                new("Target bitrate", LCapabilityBitrateCreate("192k", "-b:a", 6, 512))
            ],
            null,
            null,
            "Vorbis quality -q:a runs -1 to 10 where higher is better; 5 is a common transparent setting."));

        yield return new("vorbis", new(
            "vorbis",
            [new("Target bitrate", LCapabilityBitrateCreate("192k", "-b:a", 6, 512))],
            null,
            null,
            "Native Vorbis is experimental and lower quality than libvorbis."));

        yield return new("ac3", LCapabilityActhreeCreate("ac3"));
        yield return new("ac3_fixed", LCapabilityActhreeCreate("ac3_fixed"));
        yield return new("ac3_mf", LCapabilityActhreeCreate("ac3_mf"));

        yield return new("eac3", new(
            "eac3",
            [new("Target bitrate (CBR)", LCapabilityBitrateCreate("384k", "-b:a", 32, 6144))],
            null,
            null,
            "E-AC-3 (Dolby Digital Plus). Valid bitrates and channel counts are codec-constrained."));

        yield return new("mp2", LCapabilityMptwoCreate("mp2"));
        yield return new("mp2fixed", LCapabilityMptwoCreate("mp2fixed"));
        yield return new("libtwolame", LCapabilityMptwoCreate("libtwolame"));

        yield return new("flac", new(
            "flac",
            [new("Lossless compression")],
            new LCapabilitySpeed("Compression level", "-compression_level", "5", LCapabilityNumbersCreate(0, 12)),
            null,
            "FLAC is lossless; the compression level 0-12 trades encode time for file size, not quality."));

        yield return new("alac", new(
            "alac",
            [new("Lossless")],
            null,
            null,
            "Apple Lossless. Bit depth follows the source sample format."));

        yield return new("wavpack", new(
            "wavpack",
            [new("Lossless compression")],
            new LCapabilitySpeed("Compression level", "-compression_level", "1", LCapabilityNumbersCreate(0, 8)),
            null,
            "WavPack is lossless; the compression level 0-8 trades encode time for file size."));

        yield return new("tta", new("tta", [new("Lossless")], null, null, "TTA is a lossless codec with no rate control."));
        yield return new("truehd", new("truehd", [new("Lossless")], null, null, "Dolby TrueHD is lossless multichannel; there is no bitrate control."));
        yield return new("mlp", new("mlp", [new("Lossless")], null, null, "MLP is lossless; there is no bitrate control."));
    }

    private static LCapabilityCodec LCapabilityOpusCreate(string lEncoder) => new(
        lEncoder,
        [new("Target bitrate", LCapabilityBitrateCreate("128k", "-b:a", 6, 510))],
        new LCapabilitySpeed("Complexity", "-compression_level", "10", LCapabilityNumbersCreate(0, 10)),
        [
            new LCapabilityExtra("Rate control", "-vbr", "on",
                [new("on", "VBR"), new("constrained", "Constrained VBR"), new("off", "CBR")]),
            new LCapabilityExtra("Application", "-application", "audio",
                [new("audio", "Audio"), new("voip", "VoIP"), new("lowdelay", "Low delay")]),
            new LCapabilityExtra("Frame duration", "-frame_duration", "20",
                [new("2.5", "2.5 ms"), new("5", "5 ms"), new("10", "10 ms"), new("20", "20 ms"), new("40", "40 ms"), new("60", "60 ms")]),
            new LCapabilityExtra("Forward error correction", "-fec", "0", [new("0", "Off"), new("1", "On")])
        ],
        "Opus. -vbr picks VBR, constrained VBR or CBR; complexity 0-10 trades speed for quality; the bitrate is a target.");

    private static LCapabilityCodec LCapabilityActhreeCreate(string lEncoder) => new(
        lEncoder,
        [new("Target bitrate (CBR)", LCapabilityBitrateCreate("448k", "-b:a", 32, 640))],
        null,
        null,
        "AC-3 (Dolby Digital) is CBR; valid bitrates and channel counts are codec-constrained.");

    private static LCapabilityCodec LCapabilityMptwoCreate(string lEncoder) => new(
        lEncoder,
        [new("Target bitrate (CBR)", LCapabilityBitrateCreate("384k", "-b:a", 32, 384))],
        null,
        null,
        "MPEG-1 Audio Layer II is CBR, used for broadcast and legacy delivery.");
}
