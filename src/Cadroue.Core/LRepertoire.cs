using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cadroue.Core;

public static class LRepertoireCatalog
{
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

        LRepertoireFamily? lFamily = LRepertoireVideo.LRepertoireVideoFind(lCodecName);
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
}
