using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal static class TFlaw
{
    internal static IReadOnlyList<string> TFlawKindsResolve(
        IReadOnlyList<string> present,
        IReadOnlyList<string> requested)
    {
        List<LDossier> dossiers = present.Select(TFlawKindParse).Select(TDossierCreate).ToList();
        LFlawKind[] kinds = requested.Select(TFlawKindParse).ToArray();
        return LFlawScan.LFlawKindsResolve(dossiers, kinds)
            .Select(dossier => dossier.LDossierKind.ToString())
            .ToList();
    }

    private static LFlawKind TFlawKindParse(string token) => Enum.Parse<LFlawKind>(token);

    private static LDossier TDossierCreate(LFlawKind kind) => new(
        string.Empty,
        0,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        LDossierPreservation.LDossierPreservationExact,
        string.Empty,
        string.Empty,
        string.Empty,
        LDossierValidation.LDossierValidationUntested,
        LDossierCategory.LDossierCategoryExact,
        LDossierKind: kind);
}
