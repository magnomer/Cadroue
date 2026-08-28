using System.Globalization;
using System.Text.RegularExpressions;

namespace Cadroue.Core;

public enum LFlawKind
{
    LFlawKindContainer
}

public static class LFlaw
{
    private static readonly string[] lFlawBenign =
    {
        "cues", "cue point", "idx1", "index"
    };

    private static readonly Regex lFlawOffset = new(
        @"(?:pos|offset|at)[:=]?\s*(\d{2,})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
}
