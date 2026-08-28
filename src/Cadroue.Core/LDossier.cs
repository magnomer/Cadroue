namespace Cadroue.Core;

public enum LDossierPreservation
{
    LDossierPreservationLossless,
    LDossierPreservationTransparent,
    LDossierPreservationLossy,
    LDossierPreservationUnknown
}

public enum LDossierValidation
{
    LDossierValidationUntested,
    LDossierValidationPassed,
    LDossierValidationFailed,
    LDossierValidationInconclusive
}

public readonly record struct LDossier(
    string LDossierDefect,
    double LDossierConfidence,
    string LDossierEvidenceMechanism,
    string LDossierEvidenceSource,
    string LDossierEvidenceCoverage,
    string LDossierScope,
    string LDossierRepair,
    string LDossierRepairCoverage,
    LDossierPreservation LDossierPreservation,
    string LDossierEquivalence,
    string LDossierTiming,
    string LDossierLoss,
    LDossierValidation LDossierValidation);
