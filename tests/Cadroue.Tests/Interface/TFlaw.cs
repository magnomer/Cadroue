using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal static class TFlaw
{
    internal static IReadOnlyList<string> KindsResolve(
        IReadOnlyList<string> present,
        IReadOnlyList<string> requested)
    {
        List<LDossier> dossiers = present.Select(KindParse).Select(DossierCreate).ToList();
        LFlawKind[] kinds = requested.Select(KindParse).ToArray();
        return LFlawScan.LFlawKindsResolve(dossiers, kinds)
            .Select(dossier => dossier.LDossierKind.ToString())
            .ToList();
    }

    private static LFlawKind KindParse(string token) => Enum.Parse<LFlawKind>(token);

    private static LDossier DossierCreate(LFlawKind kind) => new(
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
