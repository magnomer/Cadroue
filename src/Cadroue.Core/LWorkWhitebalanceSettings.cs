namespace Cadroue.Core;

public sealed record LWorkWhitebalanceSettings(
    LWhitebalanceMethod LWorkWhitebalanceMethod,
    double LWorkWhitebalanceSaturation)
{
    public double LWorkWhitebalanceRed { get; init; } = 1;
    public double LWorkWhitebalanceGreen { get; init; } = 1;
    public double LWorkWhitebalanceBlue { get; init; } = 1;
    public int LWorkSampleRed { get; init; }
    public int LWorkSampleGreen { get; init; }
    public int LWorkSampleBlue { get; init; }

    public IReadOnlyList<string> LWorkWhitebalanceFormat()
    {
        static string LWorkNumberFormat(double lValue) =>
            lValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        var lFilters = new List<string>();
        if (LWorkWhitebalanceMethod == LWhitebalanceMethod.LWhitebalanceMethodManual)
        {
            lFilters.Add(
                "colorchannelmixer=rr=" + LWorkNumberFormat(LWorkWhitebalanceRed)
                + ":gg=" + LWorkNumberFormat(LWorkWhitebalanceGreen)
                + ":bb=" + LWorkNumberFormat(LWorkWhitebalanceBlue));
            double lSaturation = LWorkWhitebalanceSaturation / 100d;
            if (lSaturation != 1)
            {
                lFilters.Add("eq=saturation=" + LWorkNumberFormat(lSaturation));
            }

            return lFilters;
        }

        string lAnalyze = LWorkWhitebalanceMethod switch
        {
            LWhitebalanceMethod.LWhitebalanceMethodAverage => "average",
            LWhitebalanceMethod.LWhitebalanceMethodMinmax => "minmax",
            _ => "median"
        };
        lFilters.Add(
            "colorcorrect=analyze=" + lAnalyze
            + ":saturation=" + LWorkNumberFormat(LWorkWhitebalanceSaturation / 100d));
        return lFilters;
    }
}
