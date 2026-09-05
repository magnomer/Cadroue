namespace Cadroue.Core;

public sealed record LWorkVideo(IReadOnlyList<LWorkVideoStep> LWorkVideoSteps)
{
    public static LWorkVideo LWorkVideoCreate() => new(Array.Empty<LWorkVideoStep>());

    public bool LWorkVideoActive => LWorkVideoSteps.Any(lStep =>
        lStep.LWorkStepActive
        && (lStep.LWorkStepKind != LColorKind.LColorKindCurve
            || lStep.LWorkCurveFormat().Length > 0));
}
