namespace Cadroue.Core;

public enum LDetectorType
{
    LDetectorTypeBlack,
    LDetectorTypeColor
}

public readonly record struct LDetectorBlank(
    bool LDetectorBlankEnabled,
    LDetectorType LDetectorBlankType,
    double LDetectorBlankHue,
    double LDetectorBlankSaturation,
    double LDetectorBlankBrightness,
    double LDetectorBlankTolerance,
    double LDetectorBlankCoverage,
    double LDetectorBlankMinimum)
{
    public const double LDetectorBlankGap = 0.5;

    public const double LDetectorBlankValue = 1.0;

    public static LDetectorBlank LDetectorBlankCreate() => new(
        false,
        LDetectorType.LDetectorTypeBlack,
        0,
        0,
        LDetectorBlankValue,
        LDetector.LDetectorToleranceRead().LDetectorBoundDefault,
        LDetector.LDetectorCoverageRead().LDetectorBoundDefault,
        LDetectorBlankGap);

    public static LDetectorBlank LDetectorBlankClamp(LDetectorBlank lDetectorBlank)
    {
        double lDetectorBlankHue = double.IsFinite(lDetectorBlank.LDetectorBlankHue)
            ? lDetectorBlank.LDetectorBlankHue % 360
            : 0;
        if (lDetectorBlankHue < 0)
        {
            lDetectorBlankHue += 360;
        }

        LDetectorBound lDetectorBlankTolerance = LDetector.LDetectorToleranceRead();
        LDetectorBound lDetectorBlankCoverage = LDetector.LDetectorCoverageRead();
        LDetectorBound lDetectorBlankBrightness = LDetector.LDetectorBrightnessRead();
        LDetectorBound lDetectorBlankMinimum = LDetector.LDetectorMinimumRead(LDetectorKind.LDetectorKindBlank);

        return lDetectorBlank with
        {
            LDetectorBlankHue = lDetectorBlankHue,
            LDetectorBlankSaturation = Math.Clamp(lDetectorBlank.LDetectorBlankSaturation, 0, 1),
            LDetectorBlankBrightness = Math.Clamp(
                lDetectorBlank.LDetectorBlankBrightness,
                lDetectorBlankBrightness.LDetectorBoundLeast,
                lDetectorBlankBrightness.LDetectorBoundMost),
            LDetectorBlankTolerance = Math.Clamp(
                lDetectorBlank.LDetectorBlankTolerance,
                lDetectorBlankTolerance.LDetectorBoundLeast,
                lDetectorBlankTolerance.LDetectorBoundMost),
            LDetectorBlankCoverage = Math.Clamp(
                lDetectorBlank.LDetectorBlankCoverage,
                lDetectorBlankCoverage.LDetectorBoundLeast,
                lDetectorBlankCoverage.LDetectorBoundMost),
            LDetectorBlankMinimum = Math.Clamp(
                lDetectorBlank.LDetectorBlankMinimum,
                lDetectorBlankMinimum.LDetectorBoundLeast,
                lDetectorBlankMinimum.LDetectorBoundMost)
        };
    }
}
