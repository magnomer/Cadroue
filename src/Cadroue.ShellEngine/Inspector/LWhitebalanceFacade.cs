using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The white balance math for the gate: Core's step rule, through the answer that translates it, and the Application
// neutral readings, each answered as plain values.
public sealed class LWhitebalanceFacade : LWhitebalancePort
{
    internal LWhitebalanceFacade()
    {
    }

    public LWhitebalanceAnswer LWhitebalanceCreate(bool lActive) =>
        LWhitebalanceAnswer.LWhitebalanceAnswerCreate(LWorkVideoStep.LWorkWhitebalanceCreate(lActive));

    public LWhitebalanceAnswer LWhitebalanceNormalize(LWhitebalanceAnswer lAnswer) =>
        LWhitebalanceAnswer.LWhitebalanceAnswerCreate(LWhitebalanceAnswer.LWhitebalanceStepCreate(lAnswer));

    public (double LWhitebalanceX, double LWhitebalanceY, bool LWhitebalancePresent) LWhitebalanceWheelRead(
        int lRed, int lGreen, int lBlue)
    {
        LNeutralWheel lWheel = LNeutralColor.LNeutralWheelResolve(lRed, lGreen, lBlue);
        return (lWheel.LNeutralWheelX, lWheel.LNeutralWheelY, lWheel.LNeutralWheelPresent);
    }

    public (double LWhitebalanceLeft, double LWhitebalanceTop, bool LWhitebalancePresent) LWhitebalanceDotRead(
        double lX, double lY, bool lPresent, double lSize, double lDot)
    {
        LNeutralDot lPlace = LNeutralCanvas.LNeutralDotResolve(new LNeutralWheel(lX, lY, lPresent), lSize, lDot);
        return (lPlace.LNeutralDotLeft, lPlace.LNeutralDotTop, lPlace.LNeutralDotPresent);
    }

    public (bool LWhitebalanceSampled, int LWhitebalanceRed, int LWhitebalanceGreen, int LWhitebalanceBlue)
        LWhitebalanceDisplayRead(bool lManual, int lRed, int lGreen, int lBlue)
    {
        LNeutralDisplay lDisplay = LNeutralReadout.LNeutralDisplayResolve(lManual, lRed, lGreen, lBlue);
        return (lDisplay.LNeutralDisplaySampled,
            lDisplay.LNeutralDisplayRed,
            lDisplay.LNeutralDisplayGreen,
            lDisplay.LNeutralDisplayBlue);
    }

    public (double LWhitebalanceRed, double LWhitebalanceGreen, double LWhitebalanceBlue,
        int LWhitebalanceSampleRed, int LWhitebalanceSampleGreen, int LWhitebalanceSampleBlue)
        LWhitebalanceCanvasRead(double lX, double lY, double lSize)
    {
        LNeutralWheel lWheel = LNeutralCanvas.LNeutralCanvasResolve(lX, lY, lSize);
        LNeutralSample lSample = LNeutralColor.LNeutralColorResolve(lWheel.LNeutralWheelX, lWheel.LNeutralWheelY);
        return (lSample.LNeutralRedGain, lSample.LNeutralGreenGain, lSample.LNeutralBlueGain,
            lSample.LNeutralRed, lSample.LNeutralGreen, lSample.LNeutralBlue);
    }

    public (int LWhitebalanceSize, int LWhitebalanceStride, byte[] LWhitebalancePixels) LWhitebalanceBitmapRead(
        int lSize, double lValue)
    {
        LNeutralBitmap lBitmap = LNeutralCanvas.LNeutralBitmapResolve(lSize, lValue);
        return (lBitmap.LNeutralBitmapSize, lBitmap.LNeutralBitmapStride, lBitmap.LNeutralBitmapPixels);
    }
}
