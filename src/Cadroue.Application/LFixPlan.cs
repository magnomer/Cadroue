using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;

namespace Cadroue.Application;

public static partial class LFix
{
    public static LSidecarFixRecord LFixPersistentCreate(LWorkFix lFixPlan) => new()
    {
        LSidecarSteps = lFixPlan.LWorkFixSteps.Select(LFixRecordCreate).ToList(),
        LSidecarSalvageActive = lFixPlan.LWorkFixSalvage.LWorkSalvageActive,
        LSidecarSalvageMode = LFixSalvageFormat(lFixPlan.LWorkFixSalvage.LWorkSalvageMode),
        LSidecarSalvageBasis = LFixBasisFormat(lFixPlan.LWorkFixSalvage.LWorkSalvageBasis),
        LSidecarSalvagePersistent = lFixPlan.LWorkFixSalvage.LWorkSalvagePersistent
    };

    public static LWorkFix LFixPersistentRead(LSidecarFixRecord lFixRecord) =>
        new(lFixRecord.LSidecarSteps.Select(LFixStepCreate).ToArray())
        {
            LWorkFixSalvage = new LWorkFixSalvage(
                lFixRecord.LSidecarSalvageActive,
                LFixSalvageCreate(lFixRecord.LSidecarSalvageMode),
                LFixBasisCreate(lFixRecord.LSidecarSalvageBasis),
                lFixRecord.LSidecarSalvagePersistent)
        };

    public static LWorkFix? LFixPlanRead(string lFixSourcePath, Func<string, LSidecarFixRecord?> lSidecarRead) =>
        lSidecarRead(lFixSourcePath) is { } lFixRecord ? LFixPersistentRead(lFixRecord) : null;

    public static void LFixPlanSave(
        string lFixSourcePath, LWorkFix lFixPlan, Func<string, LSidecarFixRecord?, bool> lSidecarSave) =>
        lSidecarSave(lFixSourcePath, LFixPersistentCreate(lFixPlan));

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
            lFixSteps.Add(lFixPersistentStep ?? lFixSavedStep ?? new LWorkFixStep(lFixKind, false, false, false));
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

    private static LWorkFixStep LFixStepCreate(LSidecarFixStep lFixRecord) =>
        new(
            LFixKindCreate(lFixRecord.LSidecarKind),
            lFixRecord.LSidecarRepair,
            lFixRecord.LSidecarDiagnosis,
            lFixRecord.LSidecarPersistent);

    private static LSidecarFixStep LFixRecordCreate(LWorkFixStep lFixStep) => new()
    {
        LSidecarKind = LFixKindFormat(lFixStep.LWorkFixKind),
        LSidecarRepair = lFixStep.LWorkFixRepair,
        LSidecarDiagnosis = lFixStep.LWorkFixDiagnosis,
        LSidecarPersistent = lFixStep.LWorkFixPersistent
    };

    private static string LFixKindFormat(LFlawKind lFixKind) => lFixKind switch
    {
        LFlawKind.LFlawKindContainer => "Container",
        LFlawKind.LFlawKindTruncation => "Truncation",
        LFlawKind.LFlawKindTransport => "Transport",
        LFlawKind.LFlawKindMetadata => "Metadata",
        LFlawKind.LFlawKindIndex => "Index",
        LFlawKind.LFlawKindFraming => "Framing",
        LFlawKind.LFlawKindConfig => "Config",
        LFlawKind.LFlawKindTiming => "Timing",
        LFlawKind.LFlawKindSecondary => "Secondary",
        LFlawKind.LFlawKindCoded => "Coded",
        _ => "Ffvone"
    };

    private static string LFixSalvageFormat(LSalvageMode lFixMode) =>
        lFixMode == LSalvageMode.LSalvageModeSeparate ? "Separate" : "Rejoin";

    private static LSalvageMode LFixSalvageCreate(string lFixMode) =>
        lFixMode == "Separate" ? LSalvageMode.LSalvageModeSeparate : LSalvageMode.LSalvageModeRejoin;

    private static string LFixBasisFormat(LSalvageBasis lFixBasis) =>
        lFixBasis == LSalvageBasis.LSalvageBasisFixed ? "Fixed" : "Source";

    private static LSalvageBasis LFixBasisCreate(string lFixBasis) =>
        lFixBasis == "Fixed" ? LSalvageBasis.LSalvageBasisFixed : LSalvageBasis.LSalvageBasisSource;

    private static LFlawKind LFixKindCreate(string lFixKind) => lFixKind switch
    {
        "Container" => LFlawKind.LFlawKindContainer,
        "Truncation" => LFlawKind.LFlawKindTruncation,
        "Transport" => LFlawKind.LFlawKindTransport,
        "Metadata" => LFlawKind.LFlawKindMetadata,
        "Index" => LFlawKind.LFlawKindIndex,
        "Framing" => LFlawKind.LFlawKindFraming,
        "Config" => LFlawKind.LFlawKindConfig,
        "Timing" => LFlawKind.LFlawKindTiming,
        "Secondary" => LFlawKind.LFlawKindSecondary,
        "Coded" => LFlawKind.LFlawKindCoded,
        _ => LFlawKind.LFlawKindFfvone
    };
}
