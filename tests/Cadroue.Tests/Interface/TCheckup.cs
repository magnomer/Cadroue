using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Tests;

internal static class TCheckup
{
    internal const string Untested = "Not run";
    internal const string Scanning = "Checking";
    internal const string Clean = "No defect";
    internal const string Failed = "Failed";
    internal const string DefectLabel = "Defect";
    internal const string EvidenceLabel = "Evidence";
    internal const string RepairLabel = "Repair";

    private static readonly LCheckupStrings Strings = new(
        Untested,
        Scanning,
        Clean,
        Failed,
        DefectLabel,
        EvidenceLabel,
        RepairLabel);

    internal static string CleanFormat() =>
        BodyFormat(new LCheckupResult("a.mp4", LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeClean));

    internal static string FailedFormat() =>
        BodyFormat(new LCheckupResult("a.mp4", LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeFailed));

    internal static string DefectMissingDossierFormat() =>
        BodyFormat(new LCheckupResult("a.mp4", LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeDefect));

    internal static string DefectFormat(string defect, string evidence, string repair)
    {
        var dossier = new LDossier(
            defect,
            0.9,
            evidence,
            string.Empty,
            string.Empty,
            string.Empty,
            repair,
            string.Empty,
            LDossierPreservation.LDossierPreservationExact,
            string.Empty,
            string.Empty,
            string.Empty,
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryContainer,
            LDossierKind: LFlawKind.LFlawKindContainer);
        return BodyFormat(new LCheckupResult("a.mp4", LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeDefect, dossier));
    }

    private static string BodyFormat(LCheckupResult result) =>
        LCheckupFormat.LCheckupBodyFormat(result, Strings);
}
