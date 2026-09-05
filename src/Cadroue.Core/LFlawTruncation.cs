namespace Cadroue.Core;

public static class LFlawTruncation
{
    public static LDossier? LFlawTruncationResolve(string lFlawProbeError, string lFlawCopyError)
    {
        string lFlawTail = LFlawTailRead(lFlawCopyError);
        if (lFlawTail.Length > 0)
        {
            return new LDossier(
                "Truncation and finalization",
                1.0,
                "ffprobe -show_error; ffmpeg -c copy -f null parsed to end of file",
                lFlawTail,
                "Parsed to the current end of file",
                LFlaw.LFlawScopeResolve(lFlawTail),
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

    private static string LFlawTailRead(string lFlawCopyError)
    {
        IEnumerable<string> lFlawLines = lFlawCopyError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Where(lFlawLine => LFlaw.lFlawTruncationTail.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }

    private static string LFlawFinalRead(string lFlawText)
    {
        IEnumerable<string> lFlawLines = lFlawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Where(lFlawLine => LFlaw.lFlawTruncationFinal.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }
}
