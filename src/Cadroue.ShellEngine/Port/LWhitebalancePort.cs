using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The white balance math the gate asks for: the step as Core keeps it, and each neutral reading as plain values.
public interface LWhitebalancePort
{
    // The Core default step past its switch.
    LWhitebalanceAnswer LWhitebalanceCreate(bool lActive);

    // The step as Core holds it: saturation, gains and sample inside their ranges, gains and sample cleared off manual.
    LWhitebalanceAnswer LWhitebalanceNormalize(LWhitebalanceAnswer lAnswer);

    // The wheel point a manual sample colour sits at.
    (double LWhitebalanceX, double LWhitebalanceY, bool LWhitebalancePresent) LWhitebalanceWheelRead(
        int lRed, int lGreen, int lBlue);

    (double LWhitebalanceLeft, double LWhitebalanceTop, bool LWhitebalancePresent) LWhitebalanceDotRead(
        double lX, double lY, bool lPresent, double lSize, double lDot);

    (bool LWhitebalanceSampled, int LWhitebalanceRed, int LWhitebalanceGreen, int LWhitebalanceBlue)
        LWhitebalanceDisplayRead(bool lManual, int lRed, int lGreen, int lBlue);

    // The gains and sample colour of a wheel point given in canvas pixels.
    (double LWhitebalanceRed, double LWhitebalanceGreen, double LWhitebalanceBlue,
        int LWhitebalanceSampleRed, int LWhitebalanceSampleGreen, int LWhitebalanceSampleBlue)
        LWhitebalanceCanvasRead(double lX, double lY, double lSize);

    (int LWhitebalanceSize, int LWhitebalanceStride, byte[] LWhitebalancePixels) LWhitebalanceBitmapRead(
        int lSize, double lValue);

    // The plan step an answer stands for; static, since it reads only the answer given.
    static LWorkVideoStep LWhitebalanceStepCreate(LWhitebalanceAnswer lAnswer) =>
        LWhitebalanceAnswer.LWhitebalanceStepCreate(lAnswer);

    // The Core method a token names; the viewer's estimate takes it. An unknown token throws.
    static LWhitebalanceMethod LWhitebalanceMethodRead(string lToken) =>
        LWhitebalanceAnswer.LWhitebalanceMethodRead(lToken);
}
