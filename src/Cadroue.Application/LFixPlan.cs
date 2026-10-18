using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;

namespace Cadroue.Application;

public static class LFixPlan
{
    public static IReadOnlyList<LDossier> LFixRepairResolve(
        IReadOnlyList<LDossier> lFixDossiers, LWorkFix lFixPlan)
    {
        var lFixRepairKinds = lFixPlan.LWorkFixSteps
            .Where(lStep => lStep.LWorkFixRepair)
            .Select(lStep => lStep.LWorkFixKind)
            .ToHashSet();

        return lFixDossiers
            .Where(lFixDossier => lFixRepairKinds.Contains(lFixDossier.LDossierKind))
            .ToList();
    }

    public static LWorkFix LFixPlanResolve(LWorkFix? lFixSaved, LWorkFix? lFixPersistent)
    {
        var lFixSteps = new List<LWorkFixStep>();
        foreach (LFlawKind lFixKind in LFixKindsRead())
        {
            LWorkFixStep? lFixPersistentStep = lFixPersistent?.LWorkFixSteps
                .FirstOrDefault(lStep => lStep.LWorkFixKind == lFixKind);
            LWorkFixStep? lFixSavedStep = lFixSaved?.LWorkFixSteps
                .FirstOrDefault(lStep => lStep.LWorkFixKind == lFixKind);
            lFixSteps.Add(lFixPersistentStep ?? lFixSavedStep ?? new LWorkFixStep(lFixKind, false, false));
        }

        LWorkFixSalvage lFixSalvage =
            lFixPersistent?.LWorkFixSalvage is { LWorkSalvagePersistent: true } lFixPersistentSalvage
                ? lFixPersistentSalvage
                : lFixSaved?.LWorkFixSalvage ?? LWorkFixSalvage.LWorkSalvageCreate();

        return new LWorkFix(lFixSteps) { LWorkFixSalvage = lFixSalvage };
    }

    public static LWorkFix LFixPersistentResolve(LWorkFix lFixPlan) =>
        new(lFixPlan.LWorkFixSteps.Where(lStep => lStep.LWorkFixPersistent).ToArray())
        {
            LWorkFixSalvage = lFixPlan.LWorkFixSalvage.LWorkSalvagePersistent
                ? lFixPlan.LWorkFixSalvage
                : LWorkFixSalvage.LWorkSalvageCreate()
        };

    private static IReadOnlyList<LFlawKind> LFixKindsRead() => new[]
    {
        LFlawKind.LFlawKindContainer,
        LFlawKind.LFlawKindTruncation,
        LFlawKind.LFlawKindTransport,
        LFlawKind.LFlawKindMetadata,
        LFlawKind.LFlawKindIndex,
        LFlawKind.LFlawKindFraming,
        LFlawKind.LFlawKindConfig,
        LFlawKind.LFlawKindTiming,
        LFlawKind.LFlawKindSecondary,
        LFlawKind.LFlawKindCoded,
        LFlawKind.LFlawKindFfvone
    };
}
