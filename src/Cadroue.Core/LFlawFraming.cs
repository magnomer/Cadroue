using System.Linq;

namespace Cadroue.Core;

public static class LFlawFraming
{
    public static LDossier? LFlawFramingResolve(string lFlawCopyError, string lFlawProbeReport)
    {
        string lFlawEvidence = LFlawFramingRead(lFlawCopyError);
        if (lFlawEvidence.Length == 0)
        {
            return null;
        }

        string? lFlawFilter = LFlawFilterResolve(lFlawProbeReport);
        if (lFlawFilter is null)
        {
            return null;
        }

        return new LDossier(
            "Packetization and framing",
            1.0,
            "ffmpeg -c copy -f null; codec bitstream parse",
            lFlawEvidence,
            "Full stream copy over the coded units",
            "Coded unit framing",
            $"Reframe with the {lFlawFilter} bitstream filter, no decode",
            "Coded unit boundaries and carriage form",
            LDossierPreservation.LDossierPreservationCoded,
            "Coded units unchanged; framing representation converted",
            "Preserved",
            "None",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryPacket,
            $"-bsf:v {lFlawFilter}");
    }

    private static string LFlawFramingRead(string lFlawCopyError)
    {
        IEnumerable<string> lFlawLines = lFlawCopyError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Where(lFlawLine => !LFlaw.lFlawFramingDamage.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => LFlaw.lFlawFramingFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }

    private static string? LFlawFilterResolve(string lFlawProbeReport)
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

        IReadOnlyDictionary<string, string>? lFlawFormat =
            LFlawSection.LFlawSectionRead(lFlawProbeReport, "FORMAT").FirstOrDefault();
        string lFlawContainer = lFlawFormat is not null
            && lFlawFormat.TryGetValue("format_name", out string? lFlawName)
                ? lFlawName.ToLowerInvariant()
                : string.Empty;

        bool lFlawAnnexb = lFlawContainer.Contains("mpegts", StringComparison.Ordinal)
            || lFlawContainer.Contains("mpeg", StringComparison.Ordinal)
            || lFlawContainer.Contains("h264", StringComparison.Ordinal)
            || lFlawContainer.Contains("hevc", StringComparison.Ordinal)
            || lFlawContainer.Contains("rawvideo", StringComparison.Ordinal);

        return lFlawAnnexb ? $"{lFlawPrefix}_mp4toannexb" : "extract_extradata";
    }
}
