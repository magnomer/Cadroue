using System;
using System.Collections.Generic;

namespace Cadroue.Core;

public sealed record LRepertoireAudio(string LRepertoireText, string LRepertoireName);

public static partial class LRepertoireCatalog
{
    public static readonly IReadOnlyList<LRepertoireAudio> LRepertoireAudioCandidates =
    [
        new("AAC, native / aac", "aac"),
        new("AAC, Fraunhofer FDK / libfdk_aac", "libfdk_aac"),
        new("AAC, Media Foundation / aac_mf", "aac_mf"),
        new("AAC, AudioToolbox / aac_at", "aac_at"),
        new("MP3, LAME / libmp3lame", "libmp3lame"),
        new("MP3, Shine / libshine", "libshine"),
        new("MP3, Media Foundation / mp3_mf", "mp3_mf"),
        new("MP2, native / mp2", "mp2"),
        new("MP2, fixed-point / mp2fixed", "mp2fixed"),
        new("MP2, TwoLAME / libtwolame", "libtwolame"),
        new("Opus, libopus / libopus", "libopus"),
        new("Opus, native / opus", "opus"),
        new("Vorbis, libvorbis / libvorbis", "libvorbis"),
        new("Vorbis, native / vorbis", "vorbis"),
        new("AC-3, native / ac3", "ac3"),
        new("AC-3, fixed-point / ac3_fixed", "ac3_fixed"),
        new("AC-3, Media Foundation / ac3_mf", "ac3_mf"),
        new("E-AC-3, native / eac3", "eac3"),
        new("DTS, native / dca", "dca"),
        new("TrueHD, native / truehd", "truehd"),
        new("MLP, native / mlp", "mlp"),
        new("WMA v1, native / wmav1", "wmav1"),
        new("WMA v2, native / wmav2", "wmav2"),
        new("FLAC, native / flac", "flac"),
        new("ALAC, native / alac", "alac"),
        new("ALAC, AudioToolbox / alac_at", "alac_at"),
        new("WavPack, native / wavpack", "wavpack"),
        new("TTA, native / tta", "tta"),
        new("Sonic, native / sonic", "sonic"),
        new("Sonic lossless, native / sonic_ls", "sonic_ls"),
        new("PCM signed 8-bit / pcm_s8", "pcm_s8"),
        new("PCM signed 8-bit planar / pcm_s8_planar", "pcm_s8_planar"),
        new("PCM signed 16-bit LE / pcm_s16le", "pcm_s16le"),
        new("PCM signed 16-bit BE / pcm_s16be", "pcm_s16be"),
        new("PCM signed 16-bit LE planar / pcm_s16le_planar", "pcm_s16le_planar"),
        new("PCM signed 16-bit BE planar / pcm_s16be_planar", "pcm_s16be_planar"),
        new("PCM signed 24-bit LE / pcm_s24le", "pcm_s24le"),
        new("PCM signed 24-bit BE / pcm_s24be", "pcm_s24be"),
        new("PCM signed 24-bit LE planar / pcm_s24le_planar", "pcm_s24le_planar"),
        new("PCM signed 24-bit D-Cinema / pcm_s24daud", "pcm_s24daud"),
        new("PCM signed 32-bit LE / pcm_s32le", "pcm_s32le"),
        new("PCM signed 32-bit BE / pcm_s32be", "pcm_s32be"),
        new("PCM signed 32-bit LE planar / pcm_s32le_planar", "pcm_s32le_planar"),
        new("PCM signed 64-bit LE / pcm_s64le", "pcm_s64le"),
        new("PCM signed 64-bit BE / pcm_s64be", "pcm_s64be"),
        new("PCM unsigned 8-bit / pcm_u8", "pcm_u8"),
        new("PCM unsigned 16-bit LE / pcm_u16le", "pcm_u16le"),
        new("PCM unsigned 16-bit BE / pcm_u16be", "pcm_u16be"),
        new("PCM unsigned 24-bit LE / pcm_u24le", "pcm_u24le"),
        new("PCM unsigned 24-bit BE / pcm_u24be", "pcm_u24be"),
        new("PCM unsigned 32-bit LE / pcm_u32le", "pcm_u32le"),
        new("PCM unsigned 32-bit BE / pcm_u32be", "pcm_u32be"),
        new("PCM float 32-bit LE / pcm_f32le", "pcm_f32le"),
        new("PCM float 32-bit BE / pcm_f32be", "pcm_f32be"),
        new("PCM float 64-bit LE / pcm_f64le", "pcm_f64le"),
        new("PCM float 64-bit BE / pcm_f64be", "pcm_f64be"),
        new("PCM A-law / pcm_alaw", "pcm_alaw"),
        new("PCM A-law, AudioToolbox / pcm_alaw_at", "pcm_alaw_at"),
        new("PCM mu-law / pcm_mulaw", "pcm_mulaw"),
        new("PCM mu-law, AudioToolbox / pcm_mulaw_at", "pcm_mulaw_at"),
        new("PCM VIDC / pcm_vidc", "pcm_vidc"),
        new("PCM Blu-ray / pcm_bluray", "pcm_bluray"),
        new("PCM DVD / pcm_dvd", "pcm_dvd"),
        new("PCM SMPTE 302M / s302m", "s302m"),
        new("ADPCM ADX / adpcm_adx", "adpcm_adx"),
        new("ADPCM Argonaut Games / adpcm_argo", "adpcm_argo"),
        new("ADPCM G.722 / adpcm_g722", "adpcm_g722"),
        new("ADPCM G.726 / adpcm_g726", "adpcm_g726"),
        new("ADPCM G.726 LE / adpcm_g726le", "adpcm_g726le"),
        new("ADPCM IMA ALP / adpcm_ima_alp", "adpcm_ima_alp"),
        new("ADPCM IMA AMV / adpcm_ima_amv", "adpcm_ima_amv"),
        new("ADPCM IMA APM / adpcm_ima_apm", "adpcm_ima_apm"),
        new("ADPCM IMA QuickTime / adpcm_ima_qt", "adpcm_ima_qt"),
        new("ADPCM IMA Simon & Schuster / adpcm_ima_ssi", "adpcm_ima_ssi"),
        new("ADPCM IMA WAV / adpcm_ima_wav", "adpcm_ima_wav"),
        new("ADPCM IMA Westwood / adpcm_ima_ws", "adpcm_ima_ws"),
        new("ADPCM Microsoft / adpcm_ms", "adpcm_ms"),
        new("ADPCM Shockwave Flash / adpcm_swf", "adpcm_swf"),
        new("ADPCM Yamaha / adpcm_yamaha", "adpcm_yamaha"),
        new("AMR-NB, OpenCORE / libopencore_amrnb", "libopencore_amrnb"),
        new("AMR-WB, VisualOn / libvo_amrwbenc", "libvo_amrwbenc"),
        new("Speex, libspeex / libspeex", "libspeex"),
        new("GSM, libgsm / libgsm", "libgsm"),
        new("GSM Microsoft, libgsm / libgsm_ms", "libgsm_ms"),
        new("iLBC, libilbc / libilbc", "libilbc"),
        new("iLBC, AudioToolbox / ilbc_at", "ilbc_at"),
        new("Codec2, libcodec2 / libcodec2", "libcodec2"),
        new("LC3, liblc3 / liblc3", "liblc3"),
        new("G.723.1, native / g723_1", "g723_1"),
        new("Comfort noise, native / comfortnoise", "comfortnoise"),
        new("Nellymoser, native / nellymoser", "nellymoser"),
        new("aptX, native / aptx", "aptx"),
        new("aptX HD, native / aptx_hd", "aptx_hd"),
        new("SBC, native / sbc", "sbc"),
        new("DFPWM, native / dfpwm", "dfpwm"),
        new("RealAudio 1.0, native / ra_144", "ra_144"),
        new("RoQ DPCM, native / roq_dpcm", "roq_dpcm"),
    ];

    public static string? LRepertoireAudioResolve(string lText)
    {
        foreach (LRepertoireAudio lCandidate in LRepertoireAudioCandidates)
        {
            if (string.Equals(lCandidate.LRepertoireText, lText, StringComparison.Ordinal))
            {
                return lCandidate.LRepertoireName;
            }
        }

        return null;
    }
}
