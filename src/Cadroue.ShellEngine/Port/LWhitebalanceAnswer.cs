using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One white balance step as plain values. The token names the method: Average, Minmax, Median or Manual; it maps to
// its Core method by name, and an unknown one throws.
public sealed record LWhitebalanceAnswer(
    bool LWhitebalanceActive,
    string LWhitebalanceToken,
    double LWhitebalanceSaturation,
    double LWhitebalanceRed,
    double LWhitebalanceGreen,
    double LWhitebalanceBlue,
    int LWhitebalanceSampleRed,
    int LWhitebalanceSampleGreen,
    int LWhitebalanceSampleBlue)
{
    // The answer a white balance plan step reads as.
    internal static LWhitebalanceAnswer LWhitebalanceAnswerCreate(LWorkVideoStep lStep)
    {
        ArgumentNullException.ThrowIfNull(lStep);
        LWorkWhitebalanceSettings lValue = lStep.LWorkWhitebalanceRead();
        return new LWhitebalanceAnswer(
            lStep.LWorkStepActive,
            LWhitebalanceTokenRead(lValue.LWorkWhitebalanceMethod),
            lValue.LWorkWhitebalanceSaturation,
            lValue.LWorkWhitebalanceRed,
            lValue.LWorkWhitebalanceGreen,
            lValue.LWorkWhitebalanceBlue,
            lValue.LWorkSampleRed,
            lValue.LWorkSampleGreen,
            lValue.LWorkSampleBlue);
    }

    // The plan step an answer stands for.
    internal static LWorkVideoStep LWhitebalanceStepCreate(LWhitebalanceAnswer lAnswer)
    {
        ArgumentNullException.ThrowIfNull(lAnswer);
        return LWorkVideoStep.LWorkWhitebalanceCreate(
            lAnswer.LWhitebalanceActive,
            LWhitebalanceMethodRead(lAnswer.LWhitebalanceToken),
            lAnswer.LWhitebalanceSaturation,
            lAnswer.LWhitebalanceRed,
            lAnswer.LWhitebalanceGreen,
            lAnswer.LWhitebalanceBlue,
            lAnswer.LWhitebalanceSampleRed,
            lAnswer.LWhitebalanceSampleGreen,
            lAnswer.LWhitebalanceSampleBlue);
    }

    // The Core method a token names; the viewer's estimate takes it.
    internal static LWhitebalanceMethod LWhitebalanceMethodRead(string lToken) => lToken switch
    {
        "Average" => LWhitebalanceMethod.LWhitebalanceMethodAverage,
        "Minmax" => LWhitebalanceMethod.LWhitebalanceMethodMinmax,
        "Median" => LWhitebalanceMethod.LWhitebalanceMethodMedian,
        "Manual" => LWhitebalanceMethod.LWhitebalanceMethodManual,
        _ => throw new ArgumentOutOfRangeException(nameof(lToken), lToken, "Unknown white balance method token.")
    };

    private static string LWhitebalanceTokenRead(LWhitebalanceMethod lMethod) => lMethod switch
    {
        LWhitebalanceMethod.LWhitebalanceMethodAverage => "Average",
        LWhitebalanceMethod.LWhitebalanceMethodMinmax => "Minmax",
        LWhitebalanceMethod.LWhitebalanceMethodMedian => "Median",
        LWhitebalanceMethod.LWhitebalanceMethodManual => "Manual",
        _ => throw new ArgumentOutOfRangeException(nameof(lMethod), lMethod, "Unknown white balance method.")
    };
}
