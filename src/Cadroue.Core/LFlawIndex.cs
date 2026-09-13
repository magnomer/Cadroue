namespace Cadroue.Core;

public static class LFlawIndex
{
    private static readonly string[] lFlawIndexFault =
    {
        "seek", "offset", "sample", "timestamp", "index", "keyframe"
    };

    private static readonly string[] lFlawIndexAbsence =
    {
        "cues", "cue point", "idx1", "could not find", "will be slow", "creation"
    };

    public static LDossier? LFlawIndexResolve(
        string lFlawIndexedError, string lFlawIgnidxError, string lFlawSeekError)
    {
        string lFlawFaults = LFlawAddressingRead($"{lFlawIndexedError}\n{lFlawSeekError}");
        if (lFlawFaults.Length == 0)
        {
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
            LFlaw.LFlawScopeResolve(lFlawFaults),
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
}
