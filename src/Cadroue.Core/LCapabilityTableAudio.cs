namespace Cadroue.Core;

public static partial class LCapabilityTable
{
    private const string LCapabilityAudioBitrateLabel = "Target bitrate";

    private static LCapabilityQuality LCapabilityAudioBitrateCreate(string lDefault = "192k") =>
        new(LCapabilityAudioBitrateLabel, "-b:a", lDefault);

    public static LCapabilityCodec LCapabilityAudioFallback { get; } = new(
        "audio",
        [new("Target bitrate", LCapabilityAudioBitrateCreate())],
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
        LCapabilityAudioTableRead()
            .ToDictionary(pEntry => pEntry.Key, pEntry => pEntry.Value, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<KeyValuePair<string, LCapabilityCodec>> LCapabilityAudioTableRead()
    {
        yield return new("aac", new(
            "aac",
            [
                new("Target bitrate", LCapabilityAudioBitrateCreate()),
                new("VBR quality (experimental)", new("VBR quality", "-q:a", "1", 0.1, 2, true))
            ],
            null,
            null,
            "Native AAC. -q:a VBR is experimental and its bitrate varies; a target bitrate is the reliable control."));

        yield return new("libfdk_aac", new(
            "libfdk_aac",
            [
                new("Target bitrate (CBR)", LCapabilityAudioBitrateCreate()),
                new("VBR", new("VBR level", "-vbr", "4", 1, 5, true))
            ],
            null,
            [
                new LCapabilityExtra("Profile", "-profile:a", "aac_low", ["aac_low", "aac_he", "aac_he_v2", "aac_ld", "aac_eld"]),
                new LCapabilityExtra("Afterburner", "-afterburner", "1", ["1", "0"])
            ],
            "Fraunhofer FDK AAC. Requires a special (often non-free) FFmpeg build; HE/HEv2 suit low bitrates."));

        yield return new("aac_mf", new(
            "aac_mf",
            [new("Target bitrate", LCapabilityAudioBitrateCreate())],
            null,
            null,
            "Media Foundation AAC (Windows system encoder)."));

        yield return new("libmp3lame", new(
            "libmp3lame",
            [
                new("VBR (quality)", new("VBR quality", "-q:a", "2", 0, 9)),
                new("ABR (average bitrate)", LCapabilityAudioBitrateCreate()),
                new("CBR", LCapabilityAudioBitrateCreate())
            ],
            null,
            null,
            "LAME MP3. VBR quality runs 0-9 where 0 is best (about V0); ABR and CBR use a target bitrate."));

        yield return new("libshine", new(
            "libshine",
            [new("CBR", LCapabilityAudioBitrateCreate("128k"))],
            null,
            null,
            "Shine is a fixed-point CBR-only MP3 encoder, lower quality than LAME. Use only where fixed-point speed matters."));

        yield return new("mp3_mf", new(
            "mp3_mf",
            [new("CBR", LCapabilityAudioBitrateCreate("192k"))],
            null,
            null,
            "Media Foundation MP3 (Windows system encoder)."));

        yield return new("libopus", LCapabilityOpusCreate("libopus"));
        yield return new("opus", LCapabilityOpusCreate("opus"));

        yield return new("libvorbis", new(
            "libvorbis",
            [
                new("VBR (quality)", new("VBR quality", "-q:a", "5", -1, 10, true)),
                new("Target bitrate", LCapabilityAudioBitrateCreate())
            ],
            null,
            null,
            "Vorbis quality -q:a runs -1 to 10 where higher is better; 5 is a common transparent setting."));

        yield return new("vorbis", new(
            "vorbis",
            [new("Target bitrate", LCapabilityAudioBitrateCreate())],
            null,
            null,
            "Native Vorbis is experimental and lower quality than libvorbis."));

        yield return new("ac3", LCapabilityAc3Create("ac3"));
        yield return new("ac3_fixed", LCapabilityAc3Create("ac3_fixed"));
        yield return new("ac3_mf", LCapabilityAc3Create("ac3_mf"));

        yield return new("eac3", new(
            "eac3",
            [new("Target bitrate (CBR)", LCapabilityAudioBitrateCreate("384k"))],
            null,
            null,
            "E-AC-3 (Dolby Digital Plus). Valid bitrates and channel counts are codec-constrained."));

        yield return new("mp2", LCapabilityMp2Create("mp2"));
        yield return new("mp2fixed", LCapabilityMp2Create("mp2fixed"));
        yield return new("libtwolame", LCapabilityMp2Create("libtwolame"));

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
        [new("Target bitrate", LCapabilityAudioBitrateCreate("128k"))],
        new LCapabilitySpeed("Complexity", "-compression_level", "10", LCapabilityNumbersCreate(0, 10)),
        [
            new LCapabilityExtra("Rate control", "-vbr", "on", ["on", "constrained", "off"]),
            new LCapabilityExtra("Application", "-application", "audio", ["audio", "voip", "lowdelay"]),
            new LCapabilityExtra("Frame duration", "-frame_duration", "20", ["2.5", "5", "10", "20", "40", "60"]),
            new LCapabilityExtra("Forward error correction", "-fec", "0", ["0", "1"])
        ],
        "Opus. -vbr picks VBR, constrained VBR or CBR; complexity 0-10 trades speed for quality; the bitrate is a target.");

    private static LCapabilityCodec LCapabilityAc3Create(string lEncoder) => new(
        lEncoder,
        [new("Target bitrate (CBR)", LCapabilityAudioBitrateCreate("448k"))],
        null,
        null,
        "AC-3 (Dolby Digital) is CBR; valid bitrates and channel counts are codec-constrained.");

    private static LCapabilityCodec LCapabilityMp2Create(string lEncoder) => new(
        lEncoder,
        [new("Target bitrate (CBR)", LCapabilityAudioBitrateCreate("384k"))],
        null,
        null,
        "MPEG-1 Audio Layer II is CBR, used for broadcast and legacy delivery.");
}
