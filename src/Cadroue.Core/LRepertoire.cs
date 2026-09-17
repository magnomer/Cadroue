using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cadroue.Core;

public sealed record LRepertoireEncoder(
    string LRepertoireText,
    IReadOnlyList<string> LRepertoireTokens);

public static partial class LRepertoireCatalog
{
    private sealed record LRepertoireFamily(
        string LRepertoireName,
        IReadOnlyList<string> LRepertoireProbeNames,
        string LRepertoirePreferred,
        IReadOnlyList<string> LRepertoireContainers,
        IReadOnlyList<LRepertoireEncoder> LRepertoireEncoders);

    private static LRepertoireEncoder LRepertoireEncoderCreate(string lText, params string[] lTokens) =>
        new(lText, lTokens);

    public static readonly IReadOnlyList<string> LRepertoireContainerNames =
        ["MP4", "Matroska", "MOV", "WebM", "AVI", "MPEG-TS", "FLV", "Ogg"];

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> LRepertoireExtensionTable =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["MP4"] = ["mp4", "m4v"],
            ["Matroska"] = ["mkv"],
            ["MOV"] = ["mov"],
            ["WebM"] = ["webm"],
            ["AVI"] = ["avi"],
            ["MPEG-TS"] = ["ts", "m2ts", "mts"],
            ["FLV"] = ["flv", "f4v"],
            ["Ogg"] = ["ogv"]
        };

    private static readonly IReadOnlyDictionary<string, string> LRepertoireMuxerTable =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MP4"] = "mp4",
            ["Matroska"] = "matroska",
            ["MOV"] = "mov",
            ["WebM"] = "webm",
            ["AVI"] = "avi",
            ["MPEG-TS"] = "mpegts",
            ["FLV"] = "flv",
            ["Ogg"] = "ogg"
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> LRepertoireAudioTable =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["AAC"] = ["MP4", "Matroska", "MOV", "MPEG-TS", "FLV", "AVI"],
            ["MP3"] = ["MP4", "Matroska", "MOV", "AVI", "MPEG-TS", "FLV"],
            ["MP2"] = ["Matroska", "MPEG-TS", "AVI"],
            ["AC-3"] = ["MP4", "Matroska", "MOV", "MPEG-TS", "AVI"],
            ["E-AC-3"] = ["MP4", "Matroska", "MOV", "MPEG-TS"],
            ["Opus"] = ["MP4", "Matroska", "WebM", "Ogg"],
            ["Vorbis"] = ["Matroska", "WebM", "Ogg"],
            ["FLAC"] = ["MP4", "Matroska", "Ogg"],
            ["ALAC"] = ["MP4", "Matroska", "MOV"],
            ["WavPack"] = ["Matroska"],
            ["TTA"] = ["Matroska"],
            ["TrueHD"] = ["Matroska", "MPEG-TS"],
            ["MLP"] = ["Matroska", "MPEG-TS"],
            ["DTS"] = ["Matroska", "MOV", "MPEG-TS", "AVI"],
            ["WMA"] = ["Matroska", "AVI"],
            ["PCM"] = ["Matroska", "MOV", "AVI"]
        };

    private static readonly IReadOnlyList<LRepertoireFamily> LRepertoireFamilies =
    [
        new("H.264", ["h264", "avc"], "libx264",
            ["MP4", "Matroska", "MOV", "AVI", "MPEG-TS", "FLV"],
            [
                LRepertoireEncoderCreate("H.264, x264 / libx264", "libx264"),
                LRepertoireEncoderCreate("H.264, Media Foundation / h264_mf", "h264_mf"),
                LRepertoireEncoderCreate("H.264, OpenH264 / libopenh264", "libopenh264"),
                LRepertoireEncoderCreate("H.264, Intel QSV / h264_qsv", "h264_qsv"),
                LRepertoireEncoderCreate("H.264, AMD AMF / h264_amf", "h264_amf"),
                LRepertoireEncoderCreate("H.264, NVIDIA NVENC / h264_nvenc", "h264_nvenc"),
            ]),
        new("H.265", ["hevc", "h265"], "libx265",
            ["MP4", "Matroska", "MOV", "MPEG-TS"],
            [
                LRepertoireEncoderCreate("H.265, x265 / libx265", "libx265"),
                LRepertoireEncoderCreate("H.265, Intel QSV / hevc_qsv", "hevc_qsv"),
                LRepertoireEncoderCreate("H.265, AMD AMF / hevc_amf", "hevc_amf"),
                LRepertoireEncoderCreate("H.265, Media Foundation / hevc_mf", "hevc_mf"),
                LRepertoireEncoderCreate("H.265, NVIDIA NVENC / hevc_nvenc", "hevc_nvenc"),
            ]),
        new("H.266/VVC", ["vvc", "h266"], "libvvenc",
            ["Matroska", "MPEG-TS"],
            [
                LRepertoireEncoderCreate("H.266/VVC, vvenc / libvvenc", "libvvenc"),
            ]),
        new("AV1", ["av1"], "libaom-av1",
            ["MP4", "Matroska", "MOV", "WebM"],
            [
                LRepertoireEncoderCreate("AV1, AOM / libaom-av1", "libaom-av1"),
                LRepertoireEncoderCreate("AV1, SVT-AV1 / libsvtav1", "libsvtav1"),
                LRepertoireEncoderCreate("AV1, rav1e / librav1e", "librav1e"),
                LRepertoireEncoderCreate("AV1, Intel QSV / av1_qsv", "av1_qsv"),
                LRepertoireEncoderCreate("AV1, AMD AMF / av1_amf", "av1_amf"),
                LRepertoireEncoderCreate("AV1, NVIDIA NVENC / av1_nvenc", "av1_nvenc"),
            ]),
        new("VP8", ["vp8"], "libvpx",
            ["Matroska", "WebM"],
            [
                LRepertoireEncoderCreate("VP8, libvpx / libvpx", "libvpx"),
            ]),
        new("VP9", ["vp9"], "libvpx-vp9",
            ["MP4", "Matroska", "WebM"],
            [
                LRepertoireEncoderCreate("VP9, libvpx / libvpx-vp9", "libvpx-vp9"),
                LRepertoireEncoderCreate("VP9, Intel QSV / vp9_qsv", "vp9_qsv"),
            ]),
        new("MPEG-4 Part 2", ["mpeg4"], "mpeg4",
            ["MP4", "Matroska", "MOV", "AVI"],
            [
                LRepertoireEncoderCreate("MPEG-4 Part 2, Xvid / libxvid", "libxvid"),
                LRepertoireEncoderCreate("MPEG-4 Part 2, native / mpeg4", "mpeg4"),
            ]),
        new("Theora", ["theora"], "libtheora",
            ["Matroska", "Ogg"],
            [
                LRepertoireEncoderCreate("Theora, libtheora / libtheora", "libtheora"),
            ]),
        new("ProRes", ["prores"], "prores_ks",
            ["Matroska", "MOV"],
            [
                LRepertoireEncoderCreate("ProRes, native / prores", "prores"),
                LRepertoireEncoderCreate("ProRes, Anatoliy / prores_aw", "prores_aw"),
                LRepertoireEncoderCreate("ProRes, Kostya / prores_ks", "prores_ks"),
            ]),
        new("FFV1", ["ffv1"], "ffv1",
            ["Matroska", "AVI"],
            [
                LRepertoireEncoderCreate("FFV1, native / ffv1", "ffv1"),
            ]),
        new("MJPEG", ["mjpeg"], "mjpeg",
            ["MP4", "Matroska", "MOV", "AVI"],
            [
                LRepertoireEncoderCreate("MJPEG, native / mjpeg", "mjpeg"),
            ]),
        new("JPEG 2000", ["jpeg2000"], "jpeg2000",
            ["Matroska", "MOV", "AVI"],
            [
                LRepertoireEncoderCreate("JPEG 2000, native / jpeg2000", "jpeg2000"),
                LRepertoireEncoderCreate("JPEG 2000, OpenJPEG / libopenjpeg", "libopenjpeg"),
            ]),
        new("WebP", [], string.Empty,
            [],
            [
                LRepertoireEncoderCreate("WebP, libwebp / libwebp", "libwebp"),
                LRepertoireEncoderCreate("WebP, animated libwebp / libwebp_anim", "libwebp_anim"),
            ]),
        new("EVC", ["evc"], "libxeve",
            ["MP4", "Matroska"],
            [
                LRepertoireEncoderCreate("EVC, XEVE / libxeve", "libxeve"),
            ]),
        new("AVS2", ["avs2"], "libxavs2",
            ["Matroska"],
            [
                LRepertoireEncoderCreate("AVS2, xavs2 / libxavs2", "libxavs2"),
            ]),
        new("APV", ["apv"], "liboapv",
            ["MP4", "Matroska"],
            [
                LRepertoireEncoderCreate("APV, OpenAPV / liboapv", "liboapv"),
            ]),
    ];

    public static IReadOnlyList<LRepertoireEncoder> LRepertoireEncodersRead()
    {
        var lEncoders = new List<LRepertoireEncoder>();
        foreach (LRepertoireFamily lFamily in LRepertoireFamilies)
        {
            lEncoders.AddRange(lFamily.LRepertoireEncoders);
        }

        return lEncoders;
    }

    private static readonly IReadOnlyDictionary<string, string> LRepertoireLegacyTexts =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["VP8, libvpx / libvpx / libvpx-vp8"] = "VP8, libvpx / libvpx"
        };

    public static string LRepertoireTextNormalize(string lText) =>
        LRepertoireLegacyTexts.TryGetValue(lText.Trim(), out string? lCurrent) ? lCurrent : lText;

    public static string? LRepertoireTokenResolve(string lText)
    {
        string lTrimmed = LRepertoireTextNormalize(lText).Trim();
        foreach (LRepertoireFamily lFamily in LRepertoireFamilies)
        {
            foreach (LRepertoireEncoder lEncoder in lFamily.LRepertoireEncoders)
            {
                if (string.Equals(lEncoder.LRepertoireText, lTrimmed, StringComparison.Ordinal))
                {
                    return lEncoder.LRepertoireTokens[0];
                }
            }
        }

        return null;
    }

    public static bool LRepertoireContainerCheck(string lText, string lContainer)
    {
        foreach (LRepertoireFamily lFamily in LRepertoireFamilies)
        {
            foreach (LRepertoireEncoder lEncoder in lFamily.LRepertoireEncoders)
            {
                if (string.Equals(lEncoder.LRepertoireText, lText, StringComparison.Ordinal))
                {
                    return lFamily.LRepertoireContainers.Contains(lContainer);
                }
            }
        }

        return false;
    }

    public static string LRepertoireMuxerResolve(string lContainer, string lOutputPath)
    {
        if (LRepertoireMuxerTable.TryGetValue(lContainer, out string? lNamedMuxer))
        {
            return lNamedMuxer;
        }

        string lSuffix = Path.GetExtension(lOutputPath).TrimStart('.');
        if (lSuffix.Length == 0)
        {
            return string.Empty;
        }

        foreach (KeyValuePair<string, IReadOnlyList<string>> lEntry in LRepertoireExtensionTable)
        {
            foreach (string lExtension in lEntry.Value)
            {
                if (string.Equals(lExtension, lSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    return LRepertoireMuxerTable[lEntry.Key];
                }
            }
        }

        return string.Empty;
    }

    public static bool LRepertoireVideoCheck(string lCodecName, string lContainer)
    {
        if (!LRepertoireMuxerTable.ContainsKey(lContainer))
        {
            return true;
        }

        LRepertoireFamily? lFamily = LRepertoireVideoFind(lCodecName);
        return lFamily is null || lFamily.LRepertoireContainers.Contains(lContainer);
    }

    public static bool LRepertoireAudioCheck(string lCodecName, string lContainer)
    {
        if (!LRepertoireMuxerTable.ContainsKey(lContainer))
        {
            return true;
        }

        string lFamily = LRepertoireAudioFind(lCodecName);
        return lFamily.Length == 0
            || (LRepertoireAudioTable.TryGetValue(lFamily, out IReadOnlyList<string>? lContainers)
                && lContainers.Contains(lContainer));
    }

    private static LRepertoireFamily? LRepertoireVideoFind(string lCodecName)
    {
        if (string.IsNullOrWhiteSpace(lCodecName))
        {
            return null;
        }

        string lNormalized = lCodecName.Trim().ToLowerInvariant();
        foreach (LRepertoireFamily lFamily in LRepertoireFamilies)
        {
            if (lFamily.LRepertoireContainers.Count == 0)
            {
                continue;
            }

            foreach (string lProbeName in lFamily.LRepertoireProbeNames)
            {
                if (string.Equals(lProbeName, lNormalized, StringComparison.Ordinal))
                {
                    return lFamily;
                }
            }
        }

        return null;
    }

    public static string LRepertoireAudioFind(string lCodecName)
    {
        if (string.IsNullOrWhiteSpace(lCodecName))
        {
            return string.Empty;
        }

        string lNormalized = lCodecName.Trim().ToLowerInvariant();
        if (lNormalized.StartsWith("pcm_", StringComparison.Ordinal) || lNormalized == "s302m")
        {
            return "PCM";
        }

        return lNormalized switch
        {
            "aac" or "libfdk_aac" or "aac_mf" or "aac_at" or "aac_latm" => "AAC",
            "mp3" or "mp3float" or "libmp3lame" or "libshine" or "mp3_mf" => "MP3",
            "mp2" or "mp2float" or "mp2fixed" or "libtwolame" => "MP2",
            "ac3" or "ac3_fixed" or "ac3_mf" => "AC-3",
            "eac3" => "E-AC-3",
            "opus" or "libopus" => "Opus",
            "vorbis" or "libvorbis" => "Vorbis",
            "flac" => "FLAC",
            "alac" or "alac_at" => "ALAC",
            "wavpack" => "WavPack",
            "tta" => "TTA",
            "truehd" => "TrueHD",
            "mlp" => "MLP",
            "dts" or "dca" => "DTS",
            "wmav1" or "wmav2" => "WMA",
            _ => string.Empty
        };
    }

    public static string? LRepertoireEncoderResolve(string? lCodecName)
    {
        if (string.IsNullOrWhiteSpace(lCodecName))
        {
            return null;
        }

        string lNormalized = lCodecName.Trim().ToLowerInvariant();
        foreach (LRepertoireFamily lFamily in LRepertoireFamilies)
        {
            if (lFamily.LRepertoirePreferred.Length == 0)
            {
                continue;
            }

            foreach (string lProbeName in lFamily.LRepertoireProbeNames)
            {
                if (string.Equals(lProbeName, lNormalized, StringComparison.Ordinal))
                {
                    return lFamily.LRepertoirePreferred;
                }
            }
        }

        return null;
    }
}
