using System.Globalization;
using System.Linq;

namespace Cadroue.Core;

public static class LFlawConfig
{
    public static LDossier? LFlawConfigResolve(string lFlawProbeReport, string lFlawDecodeError)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams =
            LFlawSection.LFlawSectionRead(lFlawProbeReport, "STREAM");
        IReadOnlyDictionary<string, string>? lFlawVideo = lFlawStreams.FirstOrDefault(
            lFlawStream => lFlawStream.TryGetValue("codec_type", out string? lFlawType)
                && string.Equals(lFlawType, "video", StringComparison.OrdinalIgnoreCase));
        if (lFlawVideo is null || !lFlawVideo.TryGetValue("codec_name", out string? lFlawCodec))
        {
            return null;
        }

        string? lFlawPrefix = lFlawCodec.ToLowerInvariant() switch
        {
            "h264" or "avc" => "h264",
            "hevc" or "h265" => "hevc",
            _ => null
        };
        if (lFlawPrefix is null)
        {
            return null;
        }

        string lFlawEvidence = LFlawConfigRead(lFlawDecodeError);
        if (lFlawEvidence.Length == 0)
        {
            return null;
        }

        bool lFlawExtradata = lFlawVideo.TryGetValue("extradata_size", out string? lFlawSize)
            && int.TryParse(lFlawSize, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lFlawBytes)
            && lFlawBytes > 0;

        string lFlawFilter = lFlawExtradata ? "dump_extra" : "extract_extradata";
        LDossierPreservation lFlawPreservation = lFlawExtradata
            ? LDossierPreservation.LDossierPreservationCoded
            : LDossierPreservation.LDossierPreservationPacket;
        string lFlawEquivalence = lFlawExtradata
            ? "Coded units unchanged; stored configuration reinserted into packets"
            : "Coded packets copied unchanged; configuration derived to extradata";

        return new LDossier(
            "Codec configuration",
            1.0,
            "ffprobe -show_streams; diagnostic decode pass",
            lFlawEvidence,
            "Full configuration context and diagnostic decode",
            "Decoder parameter sets and extradata",
            $"Reconstruct configuration with the {lFlawFilter} bitstream filter, no payload re-encode",
            "Parameter sets and codec extradata",
            lFlawPreservation,
            lFlawEquivalence,
            "Preserved",
            "None",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryConfig,
            $"-bsf:v {lFlawFilter}");
    }

    private static string LFlawConfigRead(string lFlawDecodeError)
    {
        IEnumerable<string> lFlawLines = lFlawDecodeError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Where(lFlawLine => !LFlaw.lFlawFramingDamage.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => LFlaw.lFlawConfigFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }
}
