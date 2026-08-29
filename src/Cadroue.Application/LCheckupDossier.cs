using Cadroue.Core;

namespace Cadroue.Application;

public static class LCheckupDossier
{
    public static LSidecarDossier LCheckupDossierResolve(LDossier lCheckupDossier)
    {
        return new LSidecarDossier
        {
            LSidecarDefect = lCheckupDossier.LDossierDefect,
            LSidecarConfidence = lCheckupDossier.LDossierConfidence,
            LSidecarEvidenceMechanism = lCheckupDossier.LDossierEvidenceMechanism,
            LSidecarEvidenceSource = lCheckupDossier.LDossierEvidenceSource,
            LSidecarEvidenceCoverage = lCheckupDossier.LDossierEvidenceCoverage,
            LSidecarScope = lCheckupDossier.LDossierScope,
            LSidecarRepair = lCheckupDossier.LDossierRepair,
            LSidecarRepairCoverage = lCheckupDossier.LDossierRepairCoverage,
            LSidecarPreservation = lCheckupDossier.LDossierPreservation,
            LSidecarEquivalence = lCheckupDossier.LDossierEquivalence,
            LSidecarTiming = lCheckupDossier.LDossierTiming,
            LSidecarLoss = lCheckupDossier.LDossierLoss,
            LSidecarValidation = lCheckupDossier.LDossierValidation,
            LSidecarCategory = lCheckupDossier.LDossierCategory,
            LSidecarRepairArgument = lCheckupDossier.LDossierRepairArgument,
            LSidecarRepairInput = lCheckupDossier.LDossierRepairInput,
            LSidecarKind = lCheckupDossier.LDossierKind
        };
    }

    public static LDossier LCheckupDossierResolve(LSidecarDossier lCheckupDossier)
    {
        return new LDossier(
            lCheckupDossier.LSidecarDefect,
            lCheckupDossier.LSidecarConfidence,
            lCheckupDossier.LSidecarEvidenceMechanism,
            lCheckupDossier.LSidecarEvidenceSource,
            lCheckupDossier.LSidecarEvidenceCoverage,
            lCheckupDossier.LSidecarScope,
            lCheckupDossier.LSidecarRepair,
            lCheckupDossier.LSidecarRepairCoverage,
            lCheckupDossier.LSidecarPreservation,
            lCheckupDossier.LSidecarEquivalence,
            lCheckupDossier.LSidecarTiming,
            lCheckupDossier.LSidecarLoss,
            lCheckupDossier.LSidecarValidation,
            lCheckupDossier.LSidecarCategory,
            lCheckupDossier.LSidecarRepairArgument,
            lCheckupDossier.LSidecarRepairInput,
            lCheckupDossier.LSidecarKind);
    }
}
