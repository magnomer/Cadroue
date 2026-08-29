namespace Cadroue.Core;

public static class LFlawCoded
{
    public static LDossier? LFlawCodedResolve(string lFlawDecodeError)
    {
        // The diagnostic decode is a software decode; a single hardware-decoder
        // failure over a clean software decode is not proof of corruption and
        // never reaches this evidence. Only genuine decode damage that survives
        // container, framing, timing and codec-config diagnosis is a coded defect.
        string lFlawEvidence = LFlawDamageRead(lFlawDecodeError);
        if (lFlawEvidence.Length == 0)
        {
            return null;
        }

        return new LDossier(
            "Coded media",
            1.0,
            "ffmpeg -err_detect +explode -i src -f null; full software decode",
            lFlawEvidence,
            "Full software decode over the coded frames",
            "Damaged coded video frames",
            "Decode and re-encode the principal video in its source codec, dropping isolated corrupt packets; healthy streams copied",
            "The damaged coded video essence",
            LDossierPreservation.LDossierPreservationLossy,
            "Recovered frames decoded and re-encoded; irrecoverable corrupt packets dropped",
            "Reconstructed",
            "Corrupt packets that could not be decoded are dropped or concealed",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryReencode,
            LDossierRepairInput: "-err_detect ignore_err -fflags +discardcorrupt+genpts");
    }

    private static string LFlawDamageRead(string lFlawDecodeError) =>
        string.Join(
            " | ",
            lFlawDecodeError
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(lFlawLine => lFlawLine.Length > 0)
                .Where(lFlawLine => LFlaw.lFlawFramingDamage.Any(
                    lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.Ordinal)
                .Take(3));
}
