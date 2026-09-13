using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanel;

internal sealed partial class PSEncoder
{
    private static readonly (string PSAudioText, string PSAudioName)[] PSAudioCandidates =
    [
        ("AAC, native / aac", "aac"),
        ("AAC, Fraunhofer FDK / libfdk_aac", "libfdk_aac"),
        ("AAC, Media Foundation / aac_mf", "aac_mf"),
        ("AAC, AudioToolbox / aac_at", "aac_at"),
        ("MP3, LAME / libmp3lame", "libmp3lame"),
        ("MP3, Shine / libshine", "libshine"),
        ("MP3, Media Foundation / mp3_mf", "mp3_mf"),
        ("MP2, native / mp2", "mp2"),
        ("MP2, fixed-point / mp2fixed", "mp2fixed"),
        ("MP2, TwoLAME / libtwolame", "libtwolame"),
        ("Opus, libopus / libopus", "libopus"),
        ("Opus, native / opus", "opus"),
        ("Vorbis, libvorbis / libvorbis", "libvorbis"),
        ("Vorbis, native / vorbis", "vorbis"),
        ("AC-3, native / ac3", "ac3"),
        ("AC-3, fixed-point / ac3_fixed", "ac3_fixed"),
        ("AC-3, Media Foundation / ac3_mf", "ac3_mf"),
        ("E-AC-3, native / eac3", "eac3"),
        ("DTS, native / dca", "dca"),
        ("TrueHD, native / truehd", "truehd"),
        ("MLP, native / mlp", "mlp"),
        ("WMA v1, native / wmav1", "wmav1"),
        ("WMA v2, native / wmav2", "wmav2"),
        ("FLAC, native / flac", "flac"),
        ("ALAC, native / alac", "alac"),
        ("ALAC, AudioToolbox / alac_at", "alac_at"),
        ("WavPack, native / wavpack", "wavpack"),
        ("TTA, native / tta", "tta"),
        ("Sonic, native / sonic", "sonic"),
        ("Sonic lossless, native / sonic_ls", "sonic_ls"),
        ("PCM signed 8-bit / pcm_s8", "pcm_s8"),
        ("PCM signed 8-bit planar / pcm_s8_planar", "pcm_s8_planar"),
        ("PCM signed 16-bit LE / pcm_s16le", "pcm_s16le"),
        ("PCM signed 16-bit BE / pcm_s16be", "pcm_s16be"),
        ("PCM signed 16-bit LE planar / pcm_s16le_planar", "pcm_s16le_planar"),
        ("PCM signed 16-bit BE planar / pcm_s16be_planar", "pcm_s16be_planar"),
        ("PCM signed 24-bit LE / pcm_s24le", "pcm_s24le"),
        ("PCM signed 24-bit BE / pcm_s24be", "pcm_s24be"),
        ("PCM signed 24-bit LE planar / pcm_s24le_planar", "pcm_s24le_planar"),
        ("PCM signed 24-bit D-Cinema / pcm_s24daud", "pcm_s24daud"),
        ("PCM signed 32-bit LE / pcm_s32le", "pcm_s32le"),
        ("PCM signed 32-bit BE / pcm_s32be", "pcm_s32be"),
        ("PCM signed 32-bit LE planar / pcm_s32le_planar", "pcm_s32le_planar"),
        ("PCM signed 64-bit LE / pcm_s64le", "pcm_s64le"),
        ("PCM signed 64-bit BE / pcm_s64be", "pcm_s64be"),
        ("PCM unsigned 8-bit / pcm_u8", "pcm_u8"),
        ("PCM unsigned 16-bit LE / pcm_u16le", "pcm_u16le"),
        ("PCM unsigned 16-bit BE / pcm_u16be", "pcm_u16be"),
        ("PCM unsigned 24-bit LE / pcm_u24le", "pcm_u24le"),
        ("PCM unsigned 24-bit BE / pcm_u24be", "pcm_u24be"),
        ("PCM unsigned 32-bit LE / pcm_u32le", "pcm_u32le"),
        ("PCM unsigned 32-bit BE / pcm_u32be", "pcm_u32be"),
        ("PCM float 32-bit LE / pcm_f32le", "pcm_f32le"),
        ("PCM float 32-bit BE / pcm_f32be", "pcm_f32be"),
        ("PCM float 64-bit LE / pcm_f64le", "pcm_f64le"),
        ("PCM float 64-bit BE / pcm_f64be", "pcm_f64be"),
        ("PCM A-law / pcm_alaw", "pcm_alaw"),
        ("PCM A-law, AudioToolbox / pcm_alaw_at", "pcm_alaw_at"),
        ("PCM mu-law / pcm_mulaw", "pcm_mulaw"),
        ("PCM mu-law, AudioToolbox / pcm_mulaw_at", "pcm_mulaw_at"),
        ("PCM VIDC / pcm_vidc", "pcm_vidc"),
        ("PCM Blu-ray / pcm_bluray", "pcm_bluray"),
        ("PCM DVD / pcm_dvd", "pcm_dvd"),
        ("PCM SMPTE 302M / s302m", "s302m"),
        ("ADPCM ADX / adpcm_adx", "adpcm_adx"),
        ("ADPCM Argonaut Games / adpcm_argo", "adpcm_argo"),
        ("ADPCM G.722 / adpcm_g722", "adpcm_g722"),
        ("ADPCM G.726 / adpcm_g726", "adpcm_g726"),
        ("ADPCM G.726 LE / adpcm_g726le", "adpcm_g726le"),
        ("ADPCM IMA ALP / adpcm_ima_alp", "adpcm_ima_alp"),
        ("ADPCM IMA AMV / adpcm_ima_amv", "adpcm_ima_amv"),
        ("ADPCM IMA APM / adpcm_ima_apm", "adpcm_ima_apm"),
        ("ADPCM IMA QuickTime / adpcm_ima_qt", "adpcm_ima_qt"),
        ("ADPCM IMA Simon & Schuster / adpcm_ima_ssi", "adpcm_ima_ssi"),
        ("ADPCM IMA WAV / adpcm_ima_wav", "adpcm_ima_wav"),
        ("ADPCM IMA Westwood / adpcm_ima_ws", "adpcm_ima_ws"),
        ("ADPCM Microsoft / adpcm_ms", "adpcm_ms"),
        ("ADPCM Shockwave Flash / adpcm_swf", "adpcm_swf"),
        ("ADPCM Yamaha / adpcm_yamaha", "adpcm_yamaha"),
        ("AMR-NB, OpenCORE / libopencore_amrnb", "libopencore_amrnb"),
        ("AMR-WB, VisualOn / libvo_amrwbenc", "libvo_amrwbenc"),
        ("Speex, libspeex / libspeex", "libspeex"),
        ("GSM, libgsm / libgsm", "libgsm"),
        ("GSM Microsoft, libgsm / libgsm_ms", "libgsm_ms"),
        ("iLBC, libilbc / libilbc", "libilbc"),
        ("iLBC, AudioToolbox / ilbc_at", "ilbc_at"),
        ("Codec2, libcodec2 / libcodec2", "libcodec2"),
        ("LC3, liblc3 / liblc3", "liblc3"),
        ("G.723.1, native / g723_1", "g723_1"),
        ("Comfort noise, native / comfortnoise", "comfortnoise"),
        ("Nellymoser, native / nellymoser", "nellymoser"),
        ("aptX, native / aptx", "aptx"),
        ("aptX HD, native / aptx_hd", "aptx_hd"),
        ("SBC, native / sbc", "sbc"),
        ("DFPWM, native / dfpwm", "dfpwm"),
        ("RealAudio 1.0, native / ra_144", "ra_144"),
        ("RoQ DPCM, native / roq_dpcm", "roq_dpcm")
    ];

    private static bool PSAudioContainerCheck(string pName, string pContainer) =>
        LRepertoireCatalog.LRepertoireAudioCheck(pName, pContainer);

    private static string[] PSAudioItemsRead() =>
        PSAudioCandidates
            .Where(pCandidate => LInventory.LInventoryInstalledCheck(pCandidate.PSAudioName))
            .Select(pCandidate => pCandidate.PSAudioText)
            .ToArray();

    private static string[] PSAudioItemsRead(string pContainer)
    {
        if (!LRepertoireCatalog.LRepertoireContainerNames.Contains(pContainer))
        {
            return PSAudioItemsRead();
        }

        return PSAudioCandidates
            .Where(pCandidate => LInventory.LInventoryInstalledCheck(pCandidate.PSAudioName)
                              && PSAudioContainerCheck(pCandidate.PSAudioName, pContainer))
            .Select(pCandidate => pCandidate.PSAudioText)
            .ToArray();
    }

    private static string[] PSAudioItemsRead(string pContainer, string pKeep)
    {
        string[] pItems = PSAudioItemsRead(pContainer);
        if (string.IsNullOrEmpty(pKeep) || pItems.Contains(pKeep))
        {
            return pItems;
        }

        var pCandidate = PSAudioCandidates.FirstOrDefault(pEntry => string.Equals(
            pEntry.PSAudioText,
            pKeep,
            StringComparison.Ordinal));
        bool pFits = pCandidate.PSAudioName is not null
            && (!LRepertoireCatalog.LRepertoireContainerNames.Contains(pContainer)
                || PSAudioContainerCheck(pCandidate.PSAudioName, pContainer));
        return pFits ? [pKeep, .. pItems] : pItems;
    }

    private static bool PSAudioAvailableCheck(string pText)
    {
        foreach (var pCandidate in PSAudioCandidates)
        {
            if (string.Equals(pCandidate.PSAudioText, pText, StringComparison.Ordinal))
            {
                return LInventory.LInventoryInstalledCheck(pCandidate.PSAudioName);
            }
        }

        return true;
    }
}
