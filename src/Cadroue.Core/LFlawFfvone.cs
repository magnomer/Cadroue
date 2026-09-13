namespace Cadroue.Core;

public static class LFlawFfvone
{
    public const string LFlawReport = "report";

    private static readonly string[] lFlawFfvoneMismatch =
    {
        "crc mismatch", "slice crc", "crc error", "crc failed"
    };

    public static LDossier? LFlawFfvoneResolve(string lFlawProbeReport, string lFlawCrcError)
    {
        if (!LFlawFfvoneCheck(lFlawProbeReport))
        {
            return null;
        }

        string lFlawEvidence = LFlawMismatchRead(lFlawCrcError);
        if (lFlawEvidence.Length == 0)
        {
            return null;
        }

        return new LDossier(
            "FFV1 integrity",
            1.0,
            "ffmpeg -err_detect +crccheck -i src -f null; FFV1 slice CRC verification",
            lFlawEvidence,
            "Full slice-CRC verification over the FFV1 stream",
            "FFV1 coded slices reporting a CRC mismatch",
            LFlawReport,
            "None; detection and reporting only",
            LDossierPreservation.LDossierPreservationExact,
            "No repair performed; the source is copied unchanged",
            "Unchanged",
            "A CRC mismatch proves the covered slice is inconsistent, not which byte changed; no bytes are altered",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryReencode);
    }

    public static bool LFlawFfvoneCheck(string lFlawProbeReport)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams =
            LFlaw.LFlawSectionRead(lFlawProbeReport, "STREAM");
        return lFlawStreams.Any(lFlawStream =>
            lFlawStream.TryGetValue("codec_name", out string? lFlawCodec)
            && string.Equals(lFlawCodec, "ffv1", StringComparison.OrdinalIgnoreCase));
    }

    private static string LFlawMismatchRead(string lFlawCrcError) =>
        string.Join(
            " | ",
            lFlawCrcError
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(lFlawLine => lFlawLine.Length > 0)
                .Where(lFlawLine => lFlawFfvoneMismatch.Any(
                    lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.Ordinal)
                .Take(3));
}
