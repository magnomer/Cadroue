namespace Cadroue.Core;

public static class LWorkVideoFilter
{
    public static double LWorkFfmpegResolve(LWorkVideoStep lStep) => lStep.LWorkStepKind switch
    {
        LColorKind.LColorKindBrightness => Math.Clamp(lStep.LWorkStepValue * 0.005d, -1, 1),
        LColorKind.LColorKindGamma => LWorkGammaResolve(lStep.LWorkStepValue),
        LColorKind.LColorKindExposure => lStep.LWorkStepValue,
        _ => lStep.LWorkStepValue / 100d
    };

    public static double LWorkGammaResolve(double lStepValue) =>
        Math.Pow(10d, Math.Clamp(lStepValue, -100, 100) / 100d);

    public static string LWorkCurveFormat(LWorkCurveSettings lCurve)
    {
        var lParts = new List<string>();
        if (!LWorkCurveSettings.LWorkIdentityCheck(lCurve.LWorkCurveMaster))
        {
            lParts.Add("master='" + LWorkChannelFormat(lCurve.LWorkCurveMaster) + "'");
        }

        if (!LWorkCurveSettings.LWorkIdentityCheck(lCurve.LWorkCurveRed))
        {
            lParts.Add("red='" + LWorkChannelFormat(lCurve.LWorkCurveRed) + "'");
        }

        if (!LWorkCurveSettings.LWorkIdentityCheck(lCurve.LWorkCurveGreen))
        {
            lParts.Add("green='" + LWorkChannelFormat(lCurve.LWorkCurveGreen) + "'");
        }

        if (!LWorkCurveSettings.LWorkIdentityCheck(lCurve.LWorkCurveBlue))
        {
            lParts.Add("blue='" + LWorkChannelFormat(lCurve.LWorkCurveBlue) + "'");
        }

        if (lParts.Count == 0)
        {
            return "";
        }

        lParts.Add("interp=pchip");
        return "curves=" + string.Join(":", lParts);
    }

    public static IReadOnlyList<string> LWorkWhitebalanceFormat(LWorkWhitebalanceSettings lWhitebalance)
    {
        var lFilters = new List<string>();
        if (lWhitebalance.LWorkWhitebalanceMethod == LWhitebalanceMethod.LWhitebalanceMethodManual)
        {
            lFilters.Add(
                "colorchannelmixer=rr=" + LWorkNumberFormat(lWhitebalance.LWorkWhitebalanceRed)
                + ":gg=" + LWorkNumberFormat(lWhitebalance.LWorkWhitebalanceGreen)
                + ":bb=" + LWorkNumberFormat(lWhitebalance.LWorkWhitebalanceBlue));
            double lSaturation = lWhitebalance.LWorkWhitebalanceSaturation / 100d;
            if (lSaturation != 1)
            {
                lFilters.Add("eq=saturation=" + LWorkNumberFormat(lSaturation));
            }

            return lFilters;
        }

        string lAnalyze = lWhitebalance.LWorkWhitebalanceMethod switch
        {
            LWhitebalanceMethod.LWhitebalanceMethodAverage => "average",
            LWhitebalanceMethod.LWhitebalanceMethodMinmax => "minmax",
            _ => "median"
        };
        lFilters.Add(
            "colorcorrect=analyze=" + lAnalyze
            + ":saturation=" + LWorkNumberFormat(lWhitebalance.LWorkWhitebalanceSaturation / 100d));
        return lFilters;
    }

    private static string LWorkNumberFormat(double lValue) =>
        lValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    private static string LWorkChannelFormat(IReadOnlyList<LWorkCurvePoint> lPoints) =>
        string.Join(" ", lPoints.Select(lPoint =>
            LWorkNumberFormat(lPoint.LWorkCurveInput)
            + "/" + LWorkNumberFormat(lPoint.LWorkCurveOutput)));
}
