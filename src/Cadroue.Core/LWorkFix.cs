using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Core;

public sealed record LWorkFixStep(
    LFlawKind LWorkFixKind, bool LWorkFixRepair, bool LWorkFixDiagnosis, bool LWorkFixPersistent);

public sealed record LWorkFix(IReadOnlyList<LWorkFixStep> LWorkFixSteps)
{
    public static LWorkFix LWorkFixCreate() => new(Array.Empty<LWorkFixStep>());

    public bool LWorkFixActive => LWorkFixSteps.Any(lStep => lStep.LWorkFixRepair || lStep.LWorkFixDiagnosis);
}
