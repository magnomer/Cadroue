using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The white balance math for the gate: Core's step rule and the Application neutral readings, each answered as plain
// values. A method token maps to its Core method by name, and an unknown one throws.
public sealed class LWhitebalanceOutlet : LWhitebalancePort
{
    public LWhitebalanceAnswer LWhitebalanceCreate(bool lActive) =>
        LWhitebalanceAnswerRead(LWorkVideoStep.LWorkWhitebalanceCreate(lActive));

    public LWhitebalanceAnswer LWhitebalanceNormalize(LWhitebalanceAnswer lAnswer) =>
        LWhitebalanceAnswerRead(LWhitebalanceStepCreate(lAnswer));

    public (double LWhitebalanceX, double LWhitebalanceY, bool LWhitebalancePresent) LWhitebalanceWheelRead(
        int lRed, int lGreen, int lBlue)
    {
        LNeutralWheel lWheel = LNeutral.LNeutralWheelResolve(lRed, lGreen, lBlue);
        return (lWheel.LNeutralWheelX, lWheel.LNeutralWheelY, lWheel.LNeutralWheelPresent);
    }

    public (double LWhitebalanceLeft, double LWhitebalanceTop, bool LWhitebalancePresent) LWhitebalanceDotRead(
        double lX, double lY, bool lPresent, double lSize, double lDot)
    {
        LNeutralDot lPlace = LNeutral.LNeutralDotResolve(new LNeutralWheel(lX, lY, lPresent), lSize, lDot);
        return (lPlace.LNeutralDotLeft, lPlace.LNeutralDotTop, lPlace.LNeutralDotPresent);
    }

    public (bool LWhitebalanceSampled, int LWhitebalanceRed, int LWhitebalanceGreen, int LWhitebalanceBlue)
        LWhitebalanceDisplayRead(bool lManual, int lRed, int lGreen, int lBlue)
    {
        LNeutralDisplay lDisplay = LNeutral.LNeutralDisplayResolve(lManual, lRed, lGreen, lBlue);
        return (lDisplay.LNeutralDisplaySampled,
            lDisplay.LNeutralDisplayRed,
            lDisplay.LNeutralDisplayGreen,
            lDisplay.LNeutralDisplayBlue);
    }

    public (double LWhitebalanceRed, double LWhitebalanceGreen, double LWhitebalanceBlue,
        int LWhitebalanceSampleRed, int LWhitebalanceSampleGreen, int LWhitebalanceSampleBlue)
        LWhitebalanceCanvasRead(double lX, double lY, double lSize)
    {
        LNeutralWheel lWheel = LNeutral.LNeutralCanvasResolve(lX, lY, lSize);
        LNeutralSample lSample = LNeutral.LNeutralColorResolve(lWheel.LNeutralWheelX, lWheel.LNeutralWheelY);
        return (lSample.LNeutralRedGain, lSample.LNeutralGreenGain, lSample.LNeutralBlueGain,
            lSample.LNeutralRed, lSample.LNeutralGreen, lSample.LNeutralBlue);
    }

    public (int LWhitebalanceSize, int LWhitebalanceStride, byte[] LWhitebalancePixels) LWhitebalanceBitmapRead(
        int lSize, double lValue)
    {
        LNeutralBitmap lBitmap = LNeutral.LNeutralBitmapResolve(lSize, lValue);
        return (lBitmap.LNeutralBitmapSize, lBitmap.LNeutralBitmapStride, lBitmap.LNeutralBitmapPixels);
    }

    // The plan step an answer stands for.
    public static LWorkVideoStep LWhitebalanceStepCreate(LWhitebalanceAnswer lAnswer)
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

    // The answer a white balance plan step reads as.
    public static LWhitebalanceAnswer LWhitebalanceAnswerRead(LWorkVideoStep lStep)
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

    // The Core method a token names; the viewer's estimate takes it.
    public static LWhitebalanceMethod LWhitebalanceMethodRead(string lToken) => lToken switch
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
