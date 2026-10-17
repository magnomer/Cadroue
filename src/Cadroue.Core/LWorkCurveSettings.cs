namespace Cadroue.Core;

public sealed record LWorkCurveSettings(
    IReadOnlyList<LWorkCurvePoint> LWorkCurveMaster,
    IReadOnlyList<LWorkCurvePoint> LWorkCurveRed,
    IReadOnlyList<LWorkCurvePoint> LWorkCurveGreen,
    IReadOnlyList<LWorkCurvePoint> LWorkCurveBlue)
{
    public static bool LWorkIdentityCheck(IReadOnlyList<LWorkCurvePoint> lPoints) =>
        lPoints.Count == 2
        && lPoints[0].LWorkCurveInput == 0 && lPoints[0].LWorkCurveOutput == 0
        && lPoints[1].LWorkCurveInput == 1 && lPoints[1].LWorkCurveOutput == 1;

    [System.Text.Json.Serialization.JsonIgnore]
    public bool LWorkCurveActive =>
        !LWorkIdentityCheck(LWorkCurveMaster)
        || !LWorkIdentityCheck(LWorkCurveRed)
        || !LWorkIdentityCheck(LWorkCurveGreen)
        || !LWorkIdentityCheck(LWorkCurveBlue);
}
