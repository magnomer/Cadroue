namespace Cadroue.Core;

public sealed record LWorkVideo(IReadOnlyList<LWorkVideoStep> LWorkVideoSteps)
{
    public IReadOnlyList<LWorkVideoStep> LWorkVideoSteps { get; init; } =
        LWorkStepsNormalize(LWorkVideoSteps);

    public static LWorkVideo LWorkVideoCreate() => new(Array.Empty<LWorkVideoStep>());

    public bool LWorkVideoActive => LWorkVideoSteps.Any(lStep =>
        lStep.LWorkStepActive
        && (lStep.LWorkStepKind != LColorKind.LColorKindCurve
            || lStep.LWorkCurveFormat().Length > 0));

    private static IReadOnlyList<LWorkVideoStep> LWorkStepsNormalize(
        IReadOnlyList<LWorkVideoStep> lWorkSteps) =>
        lWorkSteps
            .GroupBy(lStep => lStep.LWorkStepKind)
            .OrderBy(lGroup => lGroup.Key)
            .Select(lGroup => lGroup.First())
            .ToArray();
}
