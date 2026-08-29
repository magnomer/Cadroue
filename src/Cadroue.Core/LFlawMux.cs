using System.Globalization;
using System.Text.RegularExpressions;

namespace Cadroue.Core;

public static class LFlawMux
{
    private static readonly string[] lFlawBenign =
    {
        "cues", "cue point", "idx1", "index"
    };

    // Complaints the throwaway null muxer of the -c copy -f null probe raises about its
    // own output stage. They describe our pipeline, not the input container, and follow
    // from a codec-configuration defect the config detector already owns.
    private static readonly string[] lFlawPipeline =
    {
        "dimensions not set", "could not write header", "incorrect codec parameters",
        "does not contain any stream", "last message repeated"
    };

    private static readonly Regex lFlawOffset = new(
        @"(?:pos|offset|at)[:=]?\s*(\d{2,})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] lFlawTruncationTail =
    {
        "truncated", "invalid data found", "premature end", "unexpected end",
        "truncating", "partial file"
    };

    private static readonly string[] lFlawTruncationFinal =
    {
        "moov atom not found"
    };

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
            .Where(lFlawLine => !lFlawPipeline.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => !LFlaw.lFlawFramingFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => !LFlaw.lFlawFramingDamage.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => !LFlaw.lFlawConfigFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => !lFlawTransportFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => !lFlawTruncationTail.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => !lFlawTruncationFinal.Any(
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
        IReadOnlyDictionary<string, string>? lFlawFormat = LFlaw.LFlawSectionRead(lFlawProbeReport, "FORMAT").FirstOrDefault();
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
            .Where(lFlawLine => !LFlaw.lFlawFramingDamage.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => lFlawTransportFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }

    public static LDossier? LFlawTruncationResolve(string lFlawProbeError, string lFlawCopyError)
    {
        string lFlawTail = LFlawTruncationRead(lFlawCopyError);
        if (lFlawTail.Length > 0)
        {
            return new LDossier(
                "Truncation and finalization",
                1.0,
                "ffprobe -show_error; ffmpeg -c copy -f null parsed to end of file",
                lFlawTail,
                "Parsed to the current end of file",
                LFlawScopeResolve(lFlawTail),
                "Copy the valid prefix -map 0 -c copy to the last complete unit, discarding the incomplete terminal unit and rebuilding the container",
                "The file tail beyond the last trustworthy boundary",
                LDossierPreservation.LDossierPreservationLossy,
                "Complete units copied unchanged; the incomplete terminal unit discarded",
                "Preserved",
                "Confirmed: prefix only; content past the last complete unit is unrecoverable",
                LDossierValidation.LDossierValidationUntested,
                LDossierCategory.LDossierCategoryTruncation);
        }

        string lFlawFinal = LFlawFinalRead($"{lFlawProbeError}\n{lFlawCopyError}");
        if (lFlawFinal.Length > 0)
        {
            return new LDossier(
                "Truncation and finalization",
                1.0,
                "ffprobe -show_error; ffmpeg -c copy -f null parsed to end of file",
                lFlawFinal,
                "Parsed to the end of file; recorded essence complete",
                "Final container metadata",
                "Remux -map 0 -c copy rebuilding the finalization metadata and index; +faststart rebuilds MP4 sample tables",
                "Container finalization metadata and index",
                LDossierPreservation.LDossierPreservationPacket,
                "Coded packets copied unchanged; finalization metadata rebuilt",
                "Preserved",
                "None",
                LDossierValidation.LDossierValidationUntested,
                LDossierCategory.LDossierCategoryTruncation);
        }

        return null;
    }

    private static string LFlawTruncationRead(string lFlawCopyError)
    {
        IEnumerable<string> lFlawLines = lFlawCopyError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Where(lFlawLine => lFlawTruncationTail.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }

    private static string LFlawFinalRead(string lFlawText)
    {
        IEnumerable<string> lFlawLines = lFlawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Where(lFlawLine => lFlawTruncationFinal.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }

    public static LDossier? LFlawMetadataResolve(string lFlawProbeReport)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams = LFlaw.LFlawSectionRead(lFlawProbeReport, "STREAM");
        IReadOnlyDictionary<string, string>? lFlawFormat = LFlaw.LFlawSectionRead(lFlawProbeReport, "FORMAT").FirstOrDefault();
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

        // Two tracks whose own declared timelines disagree by more than a rounding
        // margin is a container-metadata defect even when the format duration matches
        // the longest track: the shorter essence has been stretched by an inflated
        // per-track timescale or sample delta, not by real content.
        (double lFlawLow, double lFlawHigh) = LFlawSpanResolve(lFlawStreams);
        if (lFlawLow > 0
            && lFlawHigh - lFlawLow > 1.0
            && (lFlawHigh - lFlawLow) / lFlawLow > 0.05)
        {
            lFlawFindings.Add(FormattableString.Invariant(
                $"Track timelines disagree: {lFlawHigh:0.###}s against {lFlawLow:0.###}s"));
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
            // No demuxer line names the index, yet a boundary seek can still fail on a
            // file that reads cleanly front to back. Random access failing over a clean
            // sequential read is itself the addressing defect, whatever words the
            // demuxer chose; a file whose sequential read already errors belongs to
            // whichever container or coded detector owns that error, not here.
            if (LFlawSequentialCheck(lFlawIndexedError))
            {
                lFlawFaults = LFlawSeekRead(lFlawSeekError);
            }

            if (lFlawFaults.Length == 0)
            {
                return null;
            }
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

    private static bool LFlawSequentialCheck(string lFlawCopyError) =>
        !lFlawCopyError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Any(lFlawLine => !lFlawIndexAbsence.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

    private static string LFlawSeekRead(string lFlawSeekError)
    {
        IEnumerable<string> lFlawLines = lFlawSeekError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Where(lFlawLine => !lFlawIndexAbsence.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => !LFlaw.lFlawFramingDamage.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }

    private static double LFlawDurationRead(IReadOnlyDictionary<string, string> lFlawSection) =>
        lFlawSection.TryGetValue("duration", out string? lFlawText)
        && double.TryParse(lFlawText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lFlawValue)
            ? lFlawValue
            : 0;

    private static (double Low, double High) LFlawSpanResolve(
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams)
    {
        double lFlawLow = double.MaxValue;
        double lFlawHigh = 0;
        int lFlawTimed = 0;
        foreach (IReadOnlyDictionary<string, string> lFlawStream in lFlawStreams)
        {
            double lFlawValue = LFlawDurationRead(lFlawStream);
            if (lFlawValue <= 0)
            {
                continue;
            }

            lFlawTimed++;
            lFlawLow = Math.Min(lFlawLow, lFlawValue);
            lFlawHigh = Math.Max(lFlawHigh, lFlawValue);
        }

        return lFlawTimed >= 2 ? (lFlawLow, lFlawHigh) : (0, 0);
    }

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
}
