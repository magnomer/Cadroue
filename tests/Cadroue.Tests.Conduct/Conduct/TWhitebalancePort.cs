using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// A white balance engine with plain rules: saturation clamps to 0..300, and off Manual the gains read 1 and the
// sample 0. Every read is recorded by name. It also stands for one viewer pick, answering the pick it holds.
internal sealed class TWhitebalancePort : LWhitebalancePort, LWhitebalanceSamplePort
{
    public (double, double, double, int, int, int) TWhitebalanceCanvas { get; set; } = (1.5, 1, 0.5, 200, 100, 50);

    public (string, double, double, double, int, int, int) TWhitebalancePick { get; set; } =
        ("Valid", 1.25, 1, 0.75, 180, 120, 60);

    public (string LWhitebalanceStatus, double LWhitebalanceRed, double LWhitebalanceGreen, double LWhitebalanceBlue,
        int LWhitebalanceSampleRed, int LWhitebalanceSampleGreen, int LWhitebalanceSampleBlue)
        LWhitebalanceSampleRead()
    {
        TWhitebalanceCalls.Add("sample");
        return TWhitebalancePick;
    }

    public List<string> TWhitebalanceCalls { get; } = [];

    public LWhitebalanceAnswer LWhitebalanceCreate(bool lActive)
    {
        TWhitebalanceCalls.Add("create");
        return Conduct.TInterface.TWhitebalanceAnswerCreate(lActive, "Median", 100, 1, 1, 1, 0, 0, 0);
    }

    public LWhitebalanceAnswer LWhitebalanceNormalize(LWhitebalanceAnswer lAnswer)
    {
        TWhitebalanceCalls.Add("normalize");
        LWhitebalanceAnswer lClamped =
            lAnswer with { LWhitebalanceSaturation = Math.Clamp(lAnswer.LWhitebalanceSaturation, 0, 300) };
        return lAnswer.LWhitebalanceToken == "Manual"
            ? lClamped
            : lClamped with
            {
                LWhitebalanceRed = 1,
                LWhitebalanceGreen = 1,
                LWhitebalanceBlue = 1,
                LWhitebalanceSampleRed = 0,
                LWhitebalanceSampleGreen = 0,
                LWhitebalanceSampleBlue = 0
            };
    }

    public (double LWhitebalanceX, double LWhitebalanceY, bool LWhitebalancePresent) LWhitebalanceWheelRead(
        int lRed, int lGreen, int lBlue)
    {
        TWhitebalanceCalls.Add("wheel");
        return (lRed / 255d, lBlue / 255d, (lRed | lGreen | lBlue) != 0);
    }

    public (double LWhitebalanceLeft, double LWhitebalanceTop, bool LWhitebalancePresent) LWhitebalanceDotRead(
        double lX, double lY, bool lPresent, double lSize, double lDot)
    {
        TWhitebalanceCalls.Add("dot");
        return (lX * lSize - lDot, lY * lSize - lDot, lPresent);
    }

    public (bool LWhitebalanceSampled, int LWhitebalanceRed, int LWhitebalanceGreen, int LWhitebalanceBlue)
        LWhitebalanceDisplayRead(bool lManual, int lRed, int lGreen, int lBlue)
    {
        TWhitebalanceCalls.Add("display");
        bool lSampled = lManual && (lRed | lGreen | lBlue) != 0;
        return lSampled ? (true, lRed, lGreen, lBlue) : (false, 0, 0, 0);
    }

    public (double LWhitebalanceRed, double LWhitebalanceGreen, double LWhitebalanceBlue,
        int LWhitebalanceSampleRed, int LWhitebalanceSampleGreen, int LWhitebalanceSampleBlue)
        LWhitebalanceCanvasRead(double lX, double lY, double lSize)
    {
        TWhitebalanceCalls.Add("canvas");
        return TWhitebalanceCanvas;
    }

    public (int LWhitebalanceSize, int LWhitebalanceStride, byte[] LWhitebalancePixels) LWhitebalanceBitmapRead(
        int lSize, double lValue)
    {
        TWhitebalanceCalls.Add("bitmap");
        return (lSize, lSize * 4, new byte[lSize * lSize * 4]);
    }
}
