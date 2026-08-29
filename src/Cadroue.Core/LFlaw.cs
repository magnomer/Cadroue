using System.Globalization;
using System.Text.RegularExpressions;

namespace Cadroue.Core;

public enum LFlawKind
{
    LFlawKindContainer,
    LFlawKindTransport,
    LFlawKindMetadata,
    LFlawKindIndex,
    LFlawKindFraming,
    LFlawKindConfig,
    LFlawKindTiming
}

public static class LFlaw
{
    private static readonly string[] lFlawBenign =
    {
        "cues", "cue point", "idx1", "index"
    };

    private static readonly Regex lFlawOffset = new(
        @"(?:pos|offset|at)[:=]?\s*(\d{2,})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] lFlawTransportFault =
    {
        "continuity", "pes packet", "pes header", "sync byte", "pcr",
        "invalid packet size", "program map", "program association", "invalid ts packet"
    };

    private static readonly string[] lFlawIndexFault =
    {
        "seek", "offset", "sample", "timestamp", "index", "keyframe"
    };

    private static readonly string[] lFlawIndexAbsence =
    {
        "cues", "cue point", "idx1", "could not find", "will be slow", "creation"
    };

    private static readonly string[] lFlawFramingFault =
    {
        "invalid nal unit size", "nal unit size", "annexb", "annex b", "mp4toannexb",
        "error splitting the input into nal units", "missing picture in access unit"
    };

    private static readonly string[] lFlawFramingDamage =
    {
        "error while decoding", "concealing", "corrupt", "damaged", "decode_slice",
        "invalid data found", "out of range"
    };

    private static readonly string[] lFlawConfigFault =
    {
        "non-existing pps", "non-existing sps", "non-existing vps",
        "sps unavailable", "pps unavailable", "vps unavailable",
        "missing sps", "missing pps", "no frame!", "could not find codec parameters"
    };

    public static LDossier? LFlawContainerResolve(string lFlawProbeError, string lFlawCopyError)
    {
        string lFlawEvidence = LFlawStructuralRead(lFlawProbeError, lFlawCopyError);
        if (lFlawEvidence.Length == 0)
        {
            return null;
        }

        return new LDossier(
            "Container structure",
            1.0,
            "ffprobe -show_format -show_error; ffmpeg -c copy -f null",
            lFlawEvidence,
            "Full container parse",
            LFlawScopeResolve(lFlawEvidence),
            "Remux -map 0 -c copy into a fresh container of the same format",
            "Container framing and finalization",
            LDossierPreservation.LDossierPreservationPacket,
            "Coded packets copied unchanged; container reframed",
            "Preserved",
            "None",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryContainer);
    }

    private static string LFlawStructuralRead(string lFlawProbeError, string lFlawCopyError)
    {
        IEnumerable<string> lFlawLines = $"{lFlawProbeError}\n{lFlawCopyError}"
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Where(lFlawLine => !lFlawBenign.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => !lFlawFramingFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => !lFlawTransportFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }

    private static string LFlawScopeResolve(string lFlawEvidence)
    {
        Match lFlawMatch = lFlawOffset.Match(lFlawEvidence);
        return lFlawMatch.Success
            && long.TryParse(lFlawMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long lFlawByte)
                ? FormattableString.Invariant($"Byte offset {lFlawByte}")
                : "Container structure";
    }

    public static LDossier? LFlawTransportResolve(string lFlawProbeReport, string lFlawCopyError)
    {
        // Transport-layer repair is meaningful only for MPEG-TS/M2TS carriage; any other
        // container is NotApplicable and produces no dossier.
        IReadOnlyDictionary<string, string>? lFlawFormat = LFlawSectionRead(lFlawProbeReport, "FORMAT").FirstOrDefault();
        string lFlawContainer = lFlawFormat is not null
            && lFlawFormat.TryGetValue("format_name", out string? lFlawName)
                ? lFlawName.ToLowerInvariant()
                : string.Empty;
        if (!lFlawContainer.Contains("mpegts", StringComparison.Ordinal))
        {
            return null;
        }

        string lFlawEvidence = LFlawTransportRead(lFlawCopyError);
        if (lFlawEvidence.Length == 0)
        {
            return null;
        }

        return new LDossier(
            "MPEG-TS transport",
            1.0,
            "ffmpeg -c copy -f null; transport sync and per-PID continuity analysis",
            lFlawEvidence,
            "Full transport-stream traversal",
            LFlawScopeResolve(lFlawEvidence),
            "Remux -map 0 -c copy -f mpegts regenerating PAT/PMT, continuity counters and CRCs",
            "Transport tables, continuity counters and CRCs",
            LDossierPreservation.LDossierPreservationPacket,
            "Coded essence retained; transport representation regenerated, not byte-exact",
            "Preserved",
            "None",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryTransport);
    }

    private static string LFlawTransportRead(string lFlawCopyError)
    {
        IEnumerable<string> lFlawLines = lFlawCopyError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Where(lFlawLine => !lFlawFramingDamage.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => lFlawTransportFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }

    public static LDossier? LFlawMetadataResolve(string lFlawProbeReport)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams = LFlawSectionRead(lFlawProbeReport, "STREAM");
        IReadOnlyDictionary<string, string>? lFlawFormat = LFlawSectionRead(lFlawProbeReport, "FORMAT").FirstOrDefault();
        if (lFlawFormat is null)
        {
            return null;
        }

        var lFlawFindings = new List<string>();

        if (lFlawFormat.TryGetValue("nb_streams", out string? lFlawDeclaredCount)
            && int.TryParse(lFlawDeclaredCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lFlawCount)
            && lFlawCount != lFlawStreams.Count)
        {
            lFlawFindings.Add(FormattableString.Invariant(
                $"Declared stream count {lFlawCount} contradicts {lFlawStreams.Count} present"));
        }

        double lFlawObserved = LFlawDurationResolve(lFlawStreams);
        double lFlawDeclared = LFlawDurationRead(lFlawFormat);
        if (lFlawObserved > 0)
        {
            if (lFlawDeclared <= 0)
            {
                lFlawFindings.Add("Declared duration missing; establishable from stream timing");
            }
            else if (Math.Abs(lFlawDeclared - lFlawObserved) > 1.0
                && Math.Abs(lFlawDeclared - lFlawObserved) / lFlawObserved > 0.05)
            {
                lFlawFindings.Add(FormattableString.Invariant(
                    $"Declared duration {lFlawDeclared:0.###}s contradicts stream timing {lFlawObserved:0.###}s"));
            }
        }

        if (lFlawFindings.Count == 0)
        {
            return null;
        }

        return new LDossier(
            "Technical metadata",
            1.0,
            "ffprobe -show_streams -show_format -count_packets",
            string.Join(" | ", lFlawFindings),
            "All streams probed",
            "Container technical metadata",
            "Remux -map 0 -c copy regenerating the corrected technical field",
            "Technical interpretation metadata",
            LDossierPreservation.LDossierPreservationPacket,
            "Coded packets copied unchanged; technical metadata rewritten",
            "Preserved",
            "None",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryMetadata);
    }

    public static LDossier? LFlawIndexResolve(
        string lFlawIndexedError, string lFlawIgnidxError, string lFlawSeekError)
    {
        string lFlawFaults = LFlawAddressingRead($"{lFlawIndexedError}\n{lFlawSeekError}");
        if (lFlawFaults.Length == 0)
        {
            return null;
        }

        if (!LFlawBoundaryCheck(lFlawIgnidxError))
        {
            return null;
        }

        return new LDossier(
            "Index and addressing",
            1.0,
            "ffmpeg -c copy -f null with and without -ignidx; boundary seek tests",
            lFlawFaults,
            "Full traversal; index compared against sequential read",
            LFlawScopeResolve(lFlawFaults),
            "Remux -map 0 -c copy rebuilding addressing from sequential traversal; +faststart rebuilds MP4 sample tables",
            "Sample and chunk indexes and random-access tables",
            LDossierPreservation.LDossierPreservationPacket,
            "Coded packets copied unchanged; addressing rebuilt",
            "Preserved",
            "None",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryIndex);
    }

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
            .Where(lFlawLine => !lFlawFramingDamage.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => lFlawFramingFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }

    private static string? LFlawFilterResolve(string lFlawProbeReport)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams = LFlawSectionRead(lFlawProbeReport, "STREAM");
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

        IReadOnlyDictionary<string, string>? lFlawFormat = LFlawSectionRead(lFlawProbeReport, "FORMAT").FirstOrDefault();
        string lFlawContainer = lFlawFormat is not null
            && lFlawFormat.TryGetValue("format_name", out string? lFlawName)
                ? lFlawName.ToLowerInvariant()
                : string.Empty;

        // An Annex-B-native container (MPEG-TS, raw elementary stream) must carry
        // start-code framing, so length-prefixed units there are converted with
        // mp4toannexb. Any other container (ISO-BMFF, Matroska) stores length-prefixed
        // units with out-of-band parameter sets, so the framing fault is normalized by
        // lifting the in-band parameter sets to extradata for the muxer to reframe.
        bool lFlawAnnexb = lFlawContainer.Contains("mpegts", StringComparison.Ordinal)
            || lFlawContainer.Contains("mpeg", StringComparison.Ordinal)
            || lFlawContainer.Contains("h264", StringComparison.Ordinal)
            || lFlawContainer.Contains("hevc", StringComparison.Ordinal)
            || lFlawContainer.Contains("rawvideo", StringComparison.Ordinal);

        return lFlawAnnexb ? $"{lFlawPrefix}_mp4toannexb" : "extract_extradata";
    }

    public static LDossier? LFlawConfigResolve(string lFlawProbeReport, string lFlawDecodeError)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams = LFlawSectionRead(lFlawProbeReport, "STREAM");
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

        // No out-of-band extradata: derive it from the valid in-band long-term headers,
        // changing only container-side configuration and leaving the packets exact.
        // Extradata present but inconsistent with the samples: reinsert the stored
        // configuration into the packets, an in-place coded-carriage change. Never
        // synthesize an unknown parameter set — that would cause silent misdecode.
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
            .Where(lFlawLine => !lFlawFramingDamage.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => lFlawConfigFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }

    // MPEG-TS 33-bit 90 kHz timestamps wrap at 2^33. A decode-timestamp step that
    // falls back from above this guard is a legal wraparound, not a defect.
    private const long LFlawWrapGuard = 8_000_000_000L;

    public static LDossier? LFlawTimingResolve(string lFlawPacketReport)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawPackets =
            LFlawSectionRead(lFlawPacketReport, "PACKET");
        if (lFlawPackets.Count == 0)
        {
            return null;
        }

        var lFlawLastDts = new Dictionary<string, long>(StringComparer.Ordinal);
        bool lFlawMissingPts = false;
        bool lFlawMissingDts = false;
        bool lFlawDisorderDts = false;

        foreach (IReadOnlyDictionary<string, string> lFlawPacket in lFlawPackets)
        {
            string lFlawStream = lFlawPacket.TryGetValue("stream_index", out string? lFlawIndex)
                ? lFlawIndex
                : "0";
            long? lFlawPts = LFlawTicksRead(lFlawPacket, "pts");
            long? lFlawDts = LFlawTicksRead(lFlawPacket, "dts");

            // A packet with neither timestamp is not reconstructable from timing alone;
            // it is a decode-recovery case, so it does not raise a timeline defect here.
            if (lFlawPts is null && lFlawDts is not null)
            {
                lFlawMissingPts = true;
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

        // genpts regenerates presentation timestamps from decode order; igndts drops the
        // unreliable decode timestamps so the muxer re-derives them from the authoritative
        // presentation timing. Both are demuxer flags placed before -i; packets stay exact
        // and no start offset is normalized to zero.
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

    private static string LFlawAddressingRead(string lFlawText)
    {
        IEnumerable<string> lFlawLines = lFlawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Where(lFlawLine => !lFlawIndexAbsence.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => lFlawIndexFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }

    private static bool LFlawBoundaryCheck(string lFlawIgnidxError) =>
        !lFlawIgnidxError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Any(lFlawLine => !lFlawIndexAbsence.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

    private static double LFlawDurationRead(IReadOnlyDictionary<string, string> lFlawSection) =>
        lFlawSection.TryGetValue("duration", out string? lFlawText)
        && double.TryParse(lFlawText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lFlawValue)
            ? lFlawValue
            : 0;

    private static double LFlawDurationResolve(IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams)
    {
        double lFlawMax = 0;
        foreach (IReadOnlyDictionary<string, string> lFlawStream in lFlawStreams)
        {
            double lFlawValue = LFlawDurationRead(lFlawStream);
            if (lFlawValue > lFlawMax)
            {
                lFlawMax = lFlawValue;
            }
        }

        return lFlawMax;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> LFlawSectionRead(
        string lFlawReport, string lFlawSection)
    {
        var lFlawSections = new List<IReadOnlyDictionary<string, string>>();
        Dictionary<string, string>? lFlawCurrent = null;
        foreach (string lFlawRaw in lFlawReport.Split('\n'))
        {
            string lFlawLine = lFlawRaw.Trim();
            if (lFlawLine.Equals($"[{lFlawSection}]", StringComparison.Ordinal))
            {
                lFlawCurrent = new Dictionary<string, string>(StringComparer.Ordinal);
            }
            else if (lFlawLine.Equals($"[/{lFlawSection}]", StringComparison.Ordinal))
            {
                if (lFlawCurrent is not null)
                {
                    lFlawSections.Add(lFlawCurrent);
                    lFlawCurrent = null;
                }
            }
            else if (lFlawCurrent is not null)
            {
                int lFlawEquals = lFlawLine.IndexOf('=', StringComparison.Ordinal);
                if (lFlawEquals > 0)
                {
                    lFlawCurrent[lFlawLine[..lFlawEquals]] = lFlawLine[(lFlawEquals + 1)..];
                }
            }
        }

        return lFlawSections;
    }
}
