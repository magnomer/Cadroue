namespace Cadroue.Core;

public static class LFlawTransport
{
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

        string lFlawEvidence = LFlawCarriageRead(lFlawCopyError);
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
            LFlaw.LFlawScopeResolve(lFlawEvidence),
            "Remux -map 0 -c copy -f mpegts regenerating PAT/PMT, continuity counters and CRCs",
            "Transport tables, continuity counters and CRCs",
            LDossierPreservation.LDossierPreservationPacket,
            "Coded essence retained; transport representation regenerated, not byte-exact",
            "Preserved",
            "None",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryTransport);
    }

    private static string LFlawCarriageRead(string lFlawCopyError)
    {
        IEnumerable<string> lFlawLines = lFlawCopyError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Where(lFlawLine => !LFlaw.lFlawFramingDamage.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
            .Where(lFlawLine => LFlaw.lFlawTransportFault.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }
}
