using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Core;

public sealed record LWorkFixStep(
    LFlawKind LWorkFixKind, bool LWorkFixRepair, bool LWorkFixPersistent);

public sealed record LWorkFixSalvage(
    bool LWorkSalvageActive,
    LSalvageMode LWorkSalvageMode,
    LSalvageBasis LWorkSalvageBasis,
    bool LWorkSalvagePersistent)
{
    public static LWorkFixSalvage LWorkSalvageCreate() =>
        new(false, LSalvageMode.LSalvageModeRejoin, LSalvageBasis.LSalvageBasisSource, false);
}

public sealed record LWorkFix(IReadOnlyList<LWorkFixStep> LWorkFixSteps)
{
    public LWorkFixSalvage LWorkFixSalvage { get; init; } = LWorkFixSalvage.LWorkSalvageCreate();

    public static LWorkFix LWorkFixCreate() => new(Array.Empty<LWorkFixStep>());

    public bool LWorkFixActive =>
        LWorkFixSalvage.LWorkSalvageActive
        || LWorkFixSteps.Any(lStep => lStep.LWorkFixRepair);
}
