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

    public string LWorkCurveFormat()
    {
        static string LWorkNumberFormat(double lValue) =>
            lValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        static string LWorkChannelFormat(IReadOnlyList<LWorkCurvePoint> lPoints) =>
            string.Join(" ", lPoints.Select(lPoint =>
                LWorkNumberFormat(lPoint.LWorkCurveInput)
                + "/" + LWorkNumberFormat(lPoint.LWorkCurveOutput)));

        var lParts = new List<string>();
        if (!LWorkIdentityCheck(LWorkCurveMaster))
        {
            lParts.Add("master='" + LWorkChannelFormat(LWorkCurveMaster) + "'");
        }

        if (!LWorkIdentityCheck(LWorkCurveRed))
        {
            lParts.Add("red='" + LWorkChannelFormat(LWorkCurveRed) + "'");
        }

        if (!LWorkIdentityCheck(LWorkCurveGreen))
        {
            lParts.Add("green='" + LWorkChannelFormat(LWorkCurveGreen) + "'");
        }

        if (!LWorkIdentityCheck(LWorkCurveBlue))
        {
            lParts.Add("blue='" + LWorkChannelFormat(LWorkCurveBlue) + "'");
        }

        if (lParts.Count == 0)
        {
            return "";
        }

        lParts.Add("interp=pchip");
        return "curves=" + string.Join(":", lParts);
    }
}
