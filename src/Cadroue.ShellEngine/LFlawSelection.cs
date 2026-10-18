using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LFlawSelection
{
    internal static IReadOnlyList<LDossier> LFlawKindsResolve(
        IReadOnlyList<LDossier> lFlawDossiers,
        IReadOnlyCollection<LFlawKind> lFlawKinds)
    {
        if (lFlawKinds.Count == 0)
        {
            return lFlawDossiers;
        }

        var lFlawFiltered = new List<LDossier>(lFlawDossiers.Count);
        foreach (LDossier lFlawDossier in lFlawDossiers)
        {
            if (lFlawKinds.Contains(lFlawDossier.LDossierKind))
            {
                lFlawFiltered.Add(lFlawDossier);
            }
        }

        return lFlawFiltered;
    }
}
