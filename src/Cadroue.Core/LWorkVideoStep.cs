namespace Cadroue.Core;

public sealed record LWorkVideoStep(
    LColorKind LWorkStepKind,
    bool LWorkStepActive,
    double LWorkStepValue)
{
    private static readonly IReadOnlyList<LWorkCurvePoint> LWorkCurveIdentity =
        new[] { new LWorkCurvePoint(0, 0), new LWorkCurvePoint(1, 1) };

    public LWorkGammaSettings? LWorkStepGamma { get; init; }

    public LWorkWhitebalanceSettings? LWorkStepWhitebalance { get; init; }

    public LWorkCurveSettings? LWorkStepCurve { get; init; }

    public static LWorkVideoStep LWorkBrightnessCreate(bool lStepActive, double lStepValue) =>
        new(LColorKind.LColorKindBrightness, lStepActive, lStepValue);

    public static LWorkVideoStep LWorkContrastCreate(bool lStepActive, double lStepValue) =>
        new(LColorKind.LColorKindContrast, lStepActive, Math.Clamp(lStepValue, 0, 200));

    public static LWorkVideoStep LWorkSaturationCreate(bool lStepActive, double lStepValue) =>
        new(LColorKind.LColorKindSaturation, lStepActive, Math.Clamp(lStepValue, 0, 200));

    public static LWorkVideoStep LWorkExposureCreate(bool lStepActive, double lStepValue) =>
        new(LColorKind.LColorKindExposure, lStepActive, Math.Clamp(lStepValue, -3, 3));

    public static LWorkVideoStep LWorkGammaCreate(
        bool lStepActive,
        double lStepGlobal,
        double lStepRed = 0,
        double lStepGreen = 0,
        double lStepBlue = 0,
        double lStepHighlightProtection = 0)
    {
        double lGlobal = Math.Clamp(lStepGlobal, -100, 100);
        return new(LColorKind.LColorKindGamma, lStepActive, lGlobal)
        {
            LWorkStepGamma = new LWorkGammaSettings(
                lGlobal,
                Math.Clamp(lStepRed, -100, 100),
                Math.Clamp(lStepGreen, -100, 100),
                Math.Clamp(lStepBlue, -100, 100),
                Math.Clamp(lStepHighlightProtection, 0, 100))
        };
    }

    public static LWorkVideoStep LWorkWhitebalanceCreate(
        bool lStepActive,
        LWhitebalanceMethod lStepMethod = LWhitebalanceMethod.LWhitebalanceMethodMedian,
        double lStepSaturation = 100,
        double lStepRed = 1,
        double lStepGreen = 1,
        double lStepBlue = 1,
        int lStepSampleRed = 0,
        int lStepSampleGreen = 0,
        int lStepSampleBlue = 0)
    {
        LWhitebalanceMethod lMethod = Enum.IsDefined(lStepMethod)
            ? lStepMethod
            : LWhitebalanceMethod.LWhitebalanceMethodMedian;
        double lSaturation = Math.Clamp(lStepSaturation, 0, 300);
        bool lManual = lMethod == LWhitebalanceMethod.LWhitebalanceMethodManual;
        return new(LColorKind.LColorKindWhitebalance, lStepActive, lSaturation)
        {
            LWorkStepWhitebalance = new LWorkWhitebalanceSettings(lMethod, lSaturation)
            {
                LWorkWhitebalanceRed = lManual ? LWorkCoefficientNormalize(lStepRed) : 1,
                LWorkWhitebalanceGreen = lManual ? LWorkCoefficientNormalize(lStepGreen) : 1,
                LWorkWhitebalanceBlue = lManual ? LWorkCoefficientNormalize(lStepBlue) : 1,
                LWorkSampleRed = lManual ? Math.Clamp(lStepSampleRed, 0, 255) : 0,
                LWorkSampleGreen = lManual ? Math.Clamp(lStepSampleGreen, 0, 255) : 0,
                LWorkSampleBlue = lManual ? Math.Clamp(lStepSampleBlue, 0, 255) : 0
            }
        };
    }

    private static double LWorkCoefficientNormalize(double lValue) =>
        double.IsFinite(lValue) ? Math.Clamp(lValue, 0, 2) : 1;

    public static LWorkVideoStep LWorkCurveCreate(
        bool lStepActive,
        IReadOnlyList<LWorkCurvePoint>? lStepMaster = null,
        IReadOnlyList<LWorkCurvePoint>? lStepRed = null,
        IReadOnlyList<LWorkCurvePoint>? lStepGreen = null,
        IReadOnlyList<LWorkCurvePoint>? lStepBlue = null) =>
        new(LColorKind.LColorKindCurve, lStepActive, 0)
        {
            LWorkStepCurve = new LWorkCurveSettings(
                LWorkCurveNormalize(lStepMaster),
                LWorkCurveNormalize(lStepRed),
                LWorkCurveNormalize(lStepGreen),
                LWorkCurveNormalize(lStepBlue))
        };

    private static IReadOnlyList<LWorkCurvePoint> LWorkCurveNormalize(
        IReadOnlyList<LWorkCurvePoint>? lPoints)
    {
        if (lPoints is null || lPoints.Count == 0)
        {
            return LWorkCurveIdentity;
        }

        var lResult = new List<LWorkCurvePoint>();
        foreach (LWorkCurvePoint lPoint in lPoints
            .Where(lEntry => double.IsFinite(lEntry.LWorkCurveInput)
                && double.IsFinite(lEntry.LWorkCurveOutput))
            .Select(lEntry => new LWorkCurvePoint(
                Math.Clamp(lEntry.LWorkCurveInput, 0, 1),
                Math.Clamp(lEntry.LWorkCurveOutput, 0, 1)))
            .OrderBy(lEntry => lEntry.LWorkCurveInput))
        {
            if (lResult.Count > 0 && lResult[^1].LWorkCurveInput == lPoint.LWorkCurveInput)
            {
                continue;
            }

            lResult.Add(lPoint);
        }

        return lResult.Count == 0 ? LWorkCurveIdentity : lResult;
    }

    public LWorkCurveSettings LWorkCurveRead() =>
        LWorkStepKind == LColorKind.LColorKindCurve && LWorkStepCurve is { } lCurve
            ? lCurve
            : new LWorkCurveSettings(
                LWorkCurveIdentity, LWorkCurveIdentity, LWorkCurveIdentity, LWorkCurveIdentity);

    public LWorkGammaSettings LWorkGammaRead() =>
        LWorkStepKind == LColorKind.LColorKindGamma && LWorkStepGamma is { } lGamma
            ? lGamma
            : new LWorkGammaSettings(
                Math.Clamp(LWorkStepValue, -100, 100), 0, 0, 0, 0);

    public LWorkWhitebalanceSettings LWorkWhitebalanceRead()
    {
        LWorkWhitebalanceSettings lWhitebalance =
            LWorkStepKind == LColorKind.LColorKindWhitebalance && LWorkStepWhitebalance is { } lSettings
                ? lSettings
                : new LWorkWhitebalanceSettings(
                    LWhitebalanceMethod.LWhitebalanceMethodMedian,
                    LWorkStepKind == LColorKind.LColorKindWhitebalance ? LWorkStepValue : 100);
        LWhitebalanceMethod lMethod = Enum.IsDefined(lWhitebalance.LWorkWhitebalanceMethod)
            ? lWhitebalance.LWorkWhitebalanceMethod
            : LWhitebalanceMethod.LWhitebalanceMethodMedian;
        bool lManual = lMethod == LWhitebalanceMethod.LWhitebalanceMethodManual;
        return new LWorkWhitebalanceSettings(
            lMethod,
            Math.Clamp(lWhitebalance.LWorkWhitebalanceSaturation, 0, 300))
        {
            LWorkWhitebalanceRed = lManual
                ? LWorkCoefficientNormalize(lWhitebalance.LWorkWhitebalanceRed) : 1,
            LWorkWhitebalanceGreen = lManual
                ? LWorkCoefficientNormalize(lWhitebalance.LWorkWhitebalanceGreen) : 1,
            LWorkWhitebalanceBlue = lManual
                ? LWorkCoefficientNormalize(lWhitebalance.LWorkWhitebalanceBlue) : 1,
            LWorkSampleRed = lManual
                ? Math.Clamp(lWhitebalance.LWorkSampleRed, 0, 255) : 0,
            LWorkSampleGreen = lManual
                ? Math.Clamp(lWhitebalance.LWorkSampleGreen, 0, 255) : 0,
            LWorkSampleBlue = lManual
                ? Math.Clamp(lWhitebalance.LWorkSampleBlue, 0, 255) : 0
        };
    }
}
