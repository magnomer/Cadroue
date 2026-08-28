using Cadroue.Core;

namespace Cadroue.Application;

public enum LRemedyOutcome
{
    LRemedyOutcomeClean,
    LRemedyOutcomeCompose
}

public sealed record LRemedyAction(
    LDossierCategory LRemedyCategory,
    LDossier LRemedyDossier);

public sealed record LRemedyPlan(
    LRemedyOutcome LRemedyOutcome,
    IReadOnlyList<LRemedyAction> LRemedyActions);

public static class LRemedy
{
    public static LRemedyPlan LRemedyPlanCreate(IReadOnlyList<LDossier> lRemedyDossiers)
    {
        List<LRemedyAction> lRemedyActions = lRemedyDossiers
            .Where(lRemedyDossier => !string.IsNullOrWhiteSpace(lRemedyDossier.LDossierDefect))
            .OrderBy(lRemedyDossier => (int)lRemedyDossier.LDossierCategory)
            .ThenBy(lRemedyDossier => (int)lRemedyDossier.LDossierPreservation)
            .ThenBy(lRemedyDossier => lRemedyDossier.LDossierDefect, StringComparer.Ordinal)
            .Select(lRemedyDossier => new LRemedyAction(lRemedyDossier.LDossierCategory, lRemedyDossier))
            .ToList();

        return lRemedyActions.Count == 0
            ? new LRemedyPlan(LRemedyOutcome.LRemedyOutcomeClean, Array.Empty<LRemedyAction>())
            : new LRemedyPlan(LRemedyOutcome.LRemedyOutcomeCompose, lRemedyActions);
    }
}
