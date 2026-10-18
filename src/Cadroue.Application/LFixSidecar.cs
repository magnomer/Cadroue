using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;

namespace Cadroue.Application;

public static class LFixSidecar
{
    public static LSidecarFixEntry LFixPersistentCreate(LWorkFix lFixPlan) => new()
    {
        LSidecarSteps = lFixPlan.LWorkFixSteps.Select(LFixRecordCreate).ToList(),
        LSidecarSalvageActive = lFixPlan.LWorkFixSalvage.LWorkSalvageActive,
        LSidecarSalvageMode = LFixSalvageFormat(lFixPlan.LWorkFixSalvage.LWorkSalvageMode),
        LSidecarSalvageBasis = LFixBasisFormat(lFixPlan.LWorkFixSalvage.LWorkSalvageBasis)
    };

    public static LWorkFix LFixPersistentRead(LSidecarFixEntry lFixRecord, bool lFixSalvagePersistent) =>
        LFixWorkCreate(lFixRecord, lFixStepPersistent: true, lFixSalvagePersistent);

    public static LWorkFix? LFixSidecarRead(string lFixSourcePath, Func<string, LSidecarFixEntry?> lSidecarRead) =>
        lSidecarRead(lFixSourcePath) is { } lFixRecord
            ? LFixWorkCreate(lFixRecord, lFixStepPersistent: false, lFixSalvagePersistent: false)
            : null;

    private static LWorkFix LFixWorkCreate(
        LSidecarFixEntry lFixRecord, bool lFixStepPersistent, bool lFixSalvagePersistent) =>
        new(lFixRecord.LSidecarSteps
            .Select(lStep => new LWorkFixStep(
                LFixKindCreate(lStep.LSidecarKind),
                lStep.LSidecarRepair,
                lFixStepPersistent))
            .ToArray())
        {
            LWorkFixSalvage = new LWorkFixSalvage(
                lFixRecord.LSidecarSalvageActive,
                LFixSalvageCreate(lFixRecord.LSidecarSalvageMode),
                LFixBasisCreate(lFixRecord.LSidecarSalvageBasis),
                lFixSalvagePersistent)
        };

    public static void LFixSidecarSave(
        string lFixSourcePath, LWorkFix lFixPlan, Func<string, LSidecarFixEntry?, bool> lSidecarSave) =>
        lSidecarSave(lFixSourcePath, LFixPersistentCreate(lFixPlan));

    private static LSidecarFixStep LFixRecordCreate(LWorkFixStep lFixStep) => new()
    {
        LSidecarKind = LFixKindFormat(lFixStep.LWorkFixKind),
        LSidecarRepair = lFixStep.LWorkFixRepair
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
