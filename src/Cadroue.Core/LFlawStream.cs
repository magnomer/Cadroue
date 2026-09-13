using System.Globalization;
using System.Linq;

namespace Cadroue.Core;

public static class LFlawStream
{
    private const long LFlawWrapGuard = 8_000_000_000L;

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
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams = LFlaw.LFlawSectionRead(lFlawProbeReport, "STREAM");
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

        IReadOnlyDictionary<string, string>? lFlawFormat = LFlaw.LFlawSectionRead(lFlawProbeReport, "FORMAT").FirstOrDefault();
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

    public static LDossier? LFlawConfigResolve(string lFlawProbeReport, string lFlawDecodeError)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams = LFlaw.LFlawSectionRead(lFlawProbeReport, "STREAM");
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

    public static LDossier? LFlawTimingResolve(string lFlawPacketReport)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawPackets =
            LFlaw.LFlawSectionRead(lFlawPacketReport, "PACKET");
        if (lFlawPackets.Count == 0)
        {
            return null;
        }

        var lFlawLastDts = new Dictionary<string, long>(StringComparer.Ordinal);
        var lFlawPresentPts = new Dictionary<string, int>(StringComparer.Ordinal);
        var lFlawMissingPtsCount = new Dictionary<string, int>(StringComparer.Ordinal);
        bool lFlawMissingDts = false;
        bool lFlawDisorderDts = false;

        foreach (IReadOnlyDictionary<string, string> lFlawPacket in lFlawPackets)
        {
            string lFlawStream = lFlawPacket.TryGetValue("stream_index", out string? lFlawIndex)
                ? lFlawIndex
                : "0";
            long? lFlawPts = LFlawTicksRead(lFlawPacket, "pts");
            long? lFlawDts = LFlawTicksRead(lFlawPacket, "dts");

            if (lFlawPts is not null)
            {
                lFlawPresentPts[lFlawStream] = lFlawPresentPts.GetValueOrDefault(lFlawStream) + 1;
            }

            if (lFlawPts is null && lFlawDts is not null)
            {
                lFlawMissingPtsCount[lFlawStream] = lFlawMissingPtsCount.GetValueOrDefault(lFlawStream) + 1;
            }
            else if (lFlawDts is null && lFlawPts is not null)
            {
                lFlawMissingDts = true;
            }

            if (lFlawDts is { } lFlawCurrent)
            {
                if (lFlawLastDts.TryGetValue(lFlawStream, out long lFlawPrevious)
                    && lFlawCurrent < lFlawPrevious
                    && lFlawPrevious < LFlawWrapGuard)
                {
                    lFlawDisorderDts = true;
                }

                lFlawLastDts[lFlawStream] = lFlawCurrent;
            }
        }

        bool lFlawMissingPts = lFlawMissingPtsCount.Any(lFlawEntry =>
            lFlawEntry.Value > 0
            && lFlawPresentPts.GetValueOrDefault(lFlawEntry.Key) > lFlawEntry.Value);

        var lFlawFindings = new List<string>();
        if (lFlawMissingPts)
        {
            lFlawFindings.Add("Missing presentation timestamps recoverable from decode timestamps");
        }

        if (lFlawMissingDts)
        {
            lFlawFindings.Add("Missing decode timestamps with authoritative presentation timestamps");
        }

        if (lFlawDisorderDts)
        {
            lFlawFindings.Add("Non-monotonic decode timestamps");
        }

        if (lFlawFindings.Count == 0)
        {
            return null;
        }

        var lFlawFlags = new List<string>();
        if (lFlawMissingPts)
        {
            lFlawFlags.Add("+genpts");
        }

        if (lFlawMissingDts || lFlawDisorderDts)
        {
            lFlawFlags.Add("+igndts");
        }

        string lFlawInput = $"-fflags {string.Concat(lFlawFlags)}";

        return new LDossier(
            "Timeline and timestamps",
            1.0,
            "ffprobe -show_packets reading pts/dts/duration",
            string.Join(" | ", lFlawFindings),
            "Full packet traversal across all streams",
            "Packet presentation and decode timing",
            "Regenerate container timing with demuxer flags, packets copied unchanged, no decode",
            "Presentation and decode timestamps",
            LDossierPreservation.LDossierPreservationPacket,
            "Coded packets copied unchanged; container timestamps regenerated",
            "Reconstructed",
            "None",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryTimeline,
            LDossierRepairInput: lFlawInput);
    }

    private static long? LFlawTicksRead(IReadOnlyDictionary<string, string> lFlawPacket, string lFlawKey) =>
        lFlawPacket.TryGetValue(lFlawKey, out string? lFlawText)
        && long.TryParse(lFlawText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long lFlawValue)
            ? lFlawValue
            : null;
}
