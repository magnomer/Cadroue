using System.Globalization;
using System.Text.RegularExpressions;

namespace Cadroue.Core;

public enum LFlawKind
{
    LFlawKindContainer,
    LFlawKindMetadata,
    LFlawKindIndex
}

public static class LFlaw
{
    private static readonly string[] lFlawBenign =
    {
        "cues", "cue point", "idx1", "index"
    };

    private static readonly Regex lFlawOffset = new(
        @"(?:pos|offset|at)[:=]?\s*(\d{2,})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
