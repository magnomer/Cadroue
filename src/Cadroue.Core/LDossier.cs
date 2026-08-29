namespace Cadroue.Core;

public enum LDossierPreservation
{
    LDossierPreservationExact,
    LDossierPreservationPacket,
    LDossierPreservationCoded,
    LDossierPreservationDecoded,
    LDossierPreservationLossy,
    LDossierPreservationApproximate,
    LDossierPreservationUnknown
}

public enum LDossierValidation
{
    LDossierValidationUntested,
    LDossierValidationPassed,
    LDossierValidationFailed,
    LDossierValidationInconclusive
}

public enum LDossierCategory
{
    LDossierCategoryExact,
    LDossierCategoryContainer,
    LDossierCategoryTruncation,
    LDossierCategoryTransport,
    LDossierCategoryMetadata,
    LDossierCategoryIndex,
    LDossierCategoryPacket,
    LDossierCategoryConfig,
    LDossierCategoryTimeline,
    LDossierCategorySecondary,
    LDossierCategorySelective,
    LDossierCategoryReencode
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
    LDossierValidation LDossierValidation,
    LDossierCategory LDossierCategory,
    string LDossierRepairArgument = "",
    string LDossierRepairInput = "",
    LFlawKind LDossierKind = default);
