using Cadroue.Core;

namespace Cadroue.Application;

public enum LNeutralOutcome
{
    LNeutralOutcomeResolved,
    LNeutralOutcomeOutside,
    LNeutralOutcomeEmpty,
    LNeutralOutcomeDark,
    LNeutralOutcomeDecode
}

public enum LNeutralStatus
{
    LNeutralStatusValid,
    LNeutralStatusInvalid,
    LNeutralStatusDecode
}

public enum LNeutralTarget
{
    LNeutralTargetGrey,
    LNeutralTargetWhite
}

public sealed record LNeutralPoint(bool LNeutralPointInside, int LNeutralPointX, int LNeutralPointY);

public sealed record LNeutralDisplay(
    bool LNeutralDisplayVisible,
    bool LNeutralDisplaySampled,
    int LNeutralDisplayRed,
    int LNeutralDisplayGreen,
    int LNeutralDisplayBlue);

public sealed record LNeutralWheel(double LNeutralWheelX, double LNeutralWheelY, bool LNeutralWheelPresent);

public sealed record LNeutralSample(
    LNeutralOutcome LNeutralOutcome,
    int LNeutralRed,
    int LNeutralGreen,
    int LNeutralBlue,
    double LNeutralRedGain,
    double LNeutralGreenGain,
    double LNeutralBlueGain)
{
    public bool LNeutralResolved => LNeutralOutcome == LNeutralOutcome.LNeutralOutcomeResolved;
}

public static class LNeutral
{
    private const int LNeutralRadius = 5;
    private const int LNeutralDarkFloor = 16;
    private const double LNeutralGainLeast = 0;
    private const double LNeutralGainMost = 2;
    private const double LNeutralEpsilon = 1e-6;
    private const double LNeutralWheelValue = 0.75;

    public static LNeutralStatus LNeutralStatusResolve(LNeutralOutcome lNeutralOutcome) => lNeutralOutcome switch
    {
        LNeutralOutcome.LNeutralOutcomeResolved => LNeutralStatus.LNeutralStatusValid,
        LNeutralOutcome.LNeutralOutcomeDecode => LNeutralStatus.LNeutralStatusDecode,
        _ => LNeutralStatus.LNeutralStatusInvalid
    };

    public static LNeutralSample LNeutralResolve(
        IReadOnlyList<byte> lNeutralPixels,
        int lNeutralWidth,
        int lNeutralHeight,
        int lNeutralCenterX,
        int lNeutralCenterY,
        LNeutralTarget lNeutralTarget = LNeutralTarget.LNeutralTargetGrey)
    {
        if (lNeutralPixels is null
            || lNeutralWidth <= 0
            || lNeutralHeight <= 0
            || lNeutralPixels.Count < lNeutralWidth * lNeutralHeight * 4
            || lNeutralCenterX < 0
            || lNeutralCenterY < 0
            || lNeutralCenterX >= lNeutralWidth
            || lNeutralCenterY >= lNeutralHeight)
        {
            return LNeutralFailCreate(LNeutralOutcome.LNeutralOutcomeOutside);
        }

        int lNeutralLeft = Math.Max(0, lNeutralCenterX - LNeutralRadius);
        int lNeutralTop = Math.Max(0, lNeutralCenterY - LNeutralRadius);
        int lNeutralRight = Math.Min(lNeutralWidth - 1, lNeutralCenterX + LNeutralRadius);
        int lNeutralBottom = Math.Min(lNeutralHeight - 1, lNeutralCenterY + LNeutralRadius);

        List<int> lNeutralRedList = new();
        List<int> lNeutralGreenList = new();
        List<int> lNeutralBlueList = new();
        for (int lNeutralY = lNeutralTop; lNeutralY <= lNeutralBottom; lNeutralY++)
        {
            for (int lNeutralX = lNeutralLeft; lNeutralX <= lNeutralRight; lNeutralX++)
            {
                int lNeutralIndex = ((lNeutralY * lNeutralWidth) + lNeutralX) * 4;
                if (lNeutralPixels[lNeutralIndex + 3] == 0)
                {
                    continue;
                }

                lNeutralRedList.Add(lNeutralPixels[lNeutralIndex]);
                lNeutralGreenList.Add(lNeutralPixels[lNeutralIndex + 1]);
                lNeutralBlueList.Add(lNeutralPixels[lNeutralIndex + 2]);
            }
        }

        if (lNeutralRedList.Count == 0)
        {
            return LNeutralFailCreate(LNeutralOutcome.LNeutralOutcomeEmpty);
        }

        int lNeutralRed = LNeutralMedianResolve(lNeutralRedList);
        int lNeutralGreen = LNeutralMedianResolve(lNeutralGreenList);
        int lNeutralBlue = LNeutralMedianResolve(lNeutralBlueList);

        if (Math.Max(lNeutralRed, Math.Max(lNeutralGreen, lNeutralBlue)) < LNeutralDarkFloor)
        {
            return LNeutralFailCreate(LNeutralOutcome.LNeutralOutcomeDark);
        }

        return LNeutralSampleCreate(lNeutralRed, lNeutralGreen, lNeutralBlue, lNeutralTarget);
    }

    // Resolve the correction sample for one gray triple, whether it came from a
    // sampled region, a colour-wheel pick, or a whole-frame analysis.
    //
    // Work in linear light so the correction matches how the sensor mixes colour,
    // not the gamma-compressed sRGB byte values. Two targets drive the diagonal gain
    // of target / channel:
    //   Grey  — target = the sample's own Rec.709 luminance. Every channel is driven
    //           to that single luminance, so a truly neutral sample yields gains of 1,
    //           the corrected channels come out equal, and brightness is preserved.
    //           Strict: assumes the pick is genuinely neutral grey.
    //   White — target = the sample's brightest linear channel. Only the deficient
    //           channels are lifted to that max (gains >= 1); nothing is pushed down.
    //           Lenient: the pick need only sit on the black-to-white axis (its own
    //           channel ratio names the cast); brightness rises slightly.
    // Clamping each gain to 0..2 is the only cap on amplification — a near-black
    // channel would otherwise demand an unbounded multiplier and blow out highlights;
    // the clamp bounds that to a controlled 2x while leaving ordinary casts untouched.
    private static LNeutralSample LNeutralSampleCreate(
        int lNeutralRed, int lNeutralGreen, int lNeutralBlue, LNeutralTarget lNeutralTargetKind)
    {
        double lNeutralRedLinear = LNeutralLinearResolve(lNeutralRed);
        double lNeutralGreenLinear = LNeutralLinearResolve(lNeutralGreen);
        double lNeutralBlueLinear = LNeutralLinearResolve(lNeutralBlue);

        double lNeutralTarget = lNeutralTargetKind == LNeutralTarget.LNeutralTargetWhite
            ? Math.Max(lNeutralRedLinear, Math.Max(lNeutralGreenLinear, lNeutralBlueLinear))
            : (0.2126 * lNeutralRedLinear)
                + (0.7152 * lNeutralGreenLinear)
                + (0.0722 * lNeutralBlueLinear);

        double lNeutralRedGain = LNeutralGainResolve(lNeutralTarget, lNeutralRedLinear);
        double lNeutralGreenGain = LNeutralGainResolve(lNeutralTarget, lNeutralGreenLinear);
        double lNeutralBlueGain = LNeutralGainResolve(lNeutralTarget, lNeutralBlueLinear);

        return new LNeutralSample(
            LNeutralOutcome.LNeutralOutcomeResolved,
            lNeutralRed,
            lNeutralGreen,
            lNeutralBlue,
            lNeutralRedGain,
            lNeutralGreenGain,
            lNeutralBlueGain);
    }

    // A colour-wheel pick: disc coordinates (each -1..1, centre neutral) name the
    // source cast as a hue/saturation offset. Reconstruct the gray that carries that
    // cast at a fixed reference value, then resolve it exactly as a picked sample —
    // so wheel and picker feed one correction pipeline.
    public static LNeutralSample LNeutralColorResolve(double lNeutralX, double lNeutralY)
    {
        double lNeutralSaturation = Math.Clamp(Math.Sqrt((lNeutralX * lNeutralX) + (lNeutralY * lNeutralY)), 0, 1);
        double lNeutralHue = Math.Atan2(lNeutralY, lNeutralX) * (180.0 / Math.PI);
        if (lNeutralHue < 0)
        {
            lNeutralHue += 360;
        }

        (int lNeutralRed, int lNeutralGreen, int lNeutralBlue) =
            LNeutralRgbResolve(lNeutralHue, lNeutralSaturation, LNeutralWheelValue);
        return LNeutralSampleCreate(lNeutralRed, lNeutralGreen, lNeutralBlue, LNeutralTarget.LNeutralTargetGrey);
    }

    // Convert an HSV colour (hue degrees, saturation and value each 0..1) to an sRGB
    // byte triple. Shared by the wheel pick and the inspector's disc rendering so the
    // dot the user clicks matches the hue drawn under it.
    public static (int Red, int Green, int Blue) LNeutralRgbResolve(
        double lNeutralHue,
        double lNeutralSaturation,
        double lNeutralValue)
    {
        double lNeutralChroma = lNeutralValue * lNeutralSaturation;
        double lNeutralSection = lNeutralHue / 60.0;
        double lNeutralSecond = lNeutralChroma * (1 - Math.Abs((lNeutralSection % 2) - 1));
        double lNeutralMatch = lNeutralValue - lNeutralChroma;

        (double lNeutralRedUnit, double lNeutralGreenUnit, double lNeutralBlueUnit) = lNeutralSection switch
        {
            < 1 => (lNeutralChroma, lNeutralSecond, 0.0),
            < 2 => (lNeutralSecond, lNeutralChroma, 0.0),
            < 3 => (0.0, lNeutralChroma, lNeutralSecond),
            < 4 => (0.0, lNeutralSecond, lNeutralChroma),
            < 5 => (lNeutralSecond, 0.0, lNeutralChroma),
            _ => (lNeutralChroma, 0.0, lNeutralSecond)
        };

        return (
            LNeutralByteResolve(lNeutralRedUnit + lNeutralMatch),
            LNeutralByteResolve(lNeutralGreenUnit + lNeutralMatch),
            LNeutralByteResolve(lNeutralBlueUnit + lNeutralMatch));
    }

    // Place the wheel dot for a gray sample: hue as angle, saturation as radius, value
    // discarded (only the cast direction matters on a neutral disc).
    public static LNeutralWheel LNeutralWheelResolve(int lNeutralRed, int lNeutralGreen, int lNeutralBlue)
    {
        bool lNeutralSet = (lNeutralRed | lNeutralGreen | lNeutralBlue) != 0;
        double lNeutralRedUnit = Math.Clamp(lNeutralRed, 0, 255) / 255.0;
        double lNeutralGreenUnit = Math.Clamp(lNeutralGreen, 0, 255) / 255.0;
        double lNeutralBlueUnit = Math.Clamp(lNeutralBlue, 0, 255) / 255.0;
        double lNeutralMax = Math.Max(lNeutralRedUnit, Math.Max(lNeutralGreenUnit, lNeutralBlueUnit));
        double lNeutralMin = Math.Min(lNeutralRedUnit, Math.Min(lNeutralGreenUnit, lNeutralBlueUnit));
        double lNeutralDelta = lNeutralMax - lNeutralMin;
        double lNeutralSaturation = lNeutralMax <= LNeutralEpsilon ? 0 : lNeutralDelta / lNeutralMax;

        double lNeutralHue;
        if (lNeutralDelta < LNeutralEpsilon)
        {
            lNeutralHue = 0;
        }
        else if (lNeutralMax == lNeutralRedUnit)
        {
            lNeutralHue = ((lNeutralGreenUnit - lNeutralBlueUnit) / lNeutralDelta) % 6;
        }
        else if (lNeutralMax == lNeutralGreenUnit)
        {
            lNeutralHue = ((lNeutralBlueUnit - lNeutralRedUnit) / lNeutralDelta) + 2;
        }
        else
        {
            lNeutralHue = ((lNeutralRedUnit - lNeutralGreenUnit) / lNeutralDelta) + 4;
        }

        double lNeutralHueRadians = lNeutralHue * (Math.PI / 3.0);
        return new LNeutralWheel(
            lNeutralSaturation * Math.Cos(lNeutralHueRadians),
            lNeutralSaturation * Math.Sin(lNeutralHueRadians),
            lNeutralSet);
    }

    // Estimate where an automatic method's neutral point falls in the current frame,
    // for the display-only wheel dot. Average = per-channel mean, Median = per-channel
    // median, Minmax = per-channel midpoint. The actual export correction is computed
    // by ffmpeg's colorcorrect; this only previews the resulting cast direction.
    public static LNeutralWheel LNeutralAnalyzeResolve(
        IReadOnlyList<byte> lNeutralPixels,
        int lNeutralWidth,
        int lNeutralHeight,
        LWhitebalanceMethod lNeutralMethod)
    {
        if (lNeutralPixels is null
            || lNeutralWidth <= 0
            || lNeutralHeight <= 0
            || lNeutralPixels.Count < lNeutralWidth * lNeutralHeight * 4)
        {
            return new LNeutralWheel(0, 0, false);
        }

        List<int> lNeutralRedList = new();
        List<int> lNeutralGreenList = new();
        List<int> lNeutralBlueList = new();
        for (int lNeutralIndex = 0; lNeutralIndex + 3 < lNeutralPixels.Count; lNeutralIndex += 4)
        {
            if (lNeutralPixels[lNeutralIndex + 3] == 0)
            {
                continue;
            }

            lNeutralRedList.Add(lNeutralPixels[lNeutralIndex]);
            lNeutralGreenList.Add(lNeutralPixels[lNeutralIndex + 1]);
            lNeutralBlueList.Add(lNeutralPixels[lNeutralIndex + 2]);
        }

        if (lNeutralRedList.Count == 0)
        {
            return new LNeutralWheel(0, 0, false);
        }

        (int lNeutralRed, int lNeutralGreen, int lNeutralBlue) = lNeutralMethod switch
        {
            LWhitebalanceMethod.LWhitebalanceMethodAverage => (
                LNeutralMeanResolve(lNeutralRedList),
                LNeutralMeanResolve(lNeutralGreenList),
                LNeutralMeanResolve(lNeutralBlueList)),
            LWhitebalanceMethod.LWhitebalanceMethodMinmax => (
                LNeutralMidResolve(lNeutralRedList),
                LNeutralMidResolve(lNeutralGreenList),
                LNeutralMidResolve(lNeutralBlueList)),
            _ => (
                LNeutralMedianResolve(lNeutralRedList),
                LNeutralMedianResolve(lNeutralGreenList),
                LNeutralMedianResolve(lNeutralBlueList))
        };

        return LNeutralWheelResolve(lNeutralRed, lNeutralGreen, lNeutralBlue);
    }

    private static int LNeutralByteResolve(double lNeutralUnit) =>
        Math.Clamp((int)Math.Round(lNeutralUnit * 255.0), 0, 255);

    private static int LNeutralMeanResolve(List<int> lNeutralValues)
    {
        long lNeutralSum = 0;
        foreach (int lNeutralValue in lNeutralValues)
        {
            lNeutralSum += lNeutralValue;
        }

        return (int)Math.Round(lNeutralSum / (double)lNeutralValues.Count);
    }

    private static int LNeutralMidResolve(List<int> lNeutralValues)
    {
        int lNeutralLow = 255;
        int lNeutralHigh = 0;
        foreach (int lNeutralValue in lNeutralValues)
        {
            if (lNeutralValue < lNeutralLow)
            {
                lNeutralLow = lNeutralValue;
            }

            if (lNeutralValue > lNeutralHigh)
            {
                lNeutralHigh = lNeutralValue;
            }
        }

        return (int)Math.Round((lNeutralLow + lNeutralHigh) / 2.0);
    }

    // The Manual white-balance group shows only for the Manual method, and its swatch
    // and readout render only when a valid (non-black) sample is present. Automatic
    // methods collapse the group; kept-in-memory manual values stay off-screen.
    public static LNeutralDisplay LNeutralDisplayResolve(
        bool lNeutralManual,
        int lNeutralRed,
        int lNeutralGreen,
        int lNeutralBlue)
    {
        int lNeutralClampedRed = Math.Clamp(lNeutralRed, 0, 255);
        int lNeutralClampedGreen = Math.Clamp(lNeutralGreen, 0, 255);
        int lNeutralClampedBlue = Math.Clamp(lNeutralBlue, 0, 255);
        bool lNeutralSampled = lNeutralManual
            && (lNeutralClampedRed | lNeutralClampedGreen | lNeutralClampedBlue) != 0;
        return lNeutralSampled
            ? new LNeutralDisplay(
                true, true, lNeutralClampedRed, lNeutralClampedGreen, lNeutralClampedBlue)
            : new LNeutralDisplay(lNeutralManual, false, 0, 0, 0);
    }

    private static double LNeutralGainResolve(double lNeutralTarget, double lNeutralChannel)
    {
        if (lNeutralChannel < LNeutralEpsilon)
        {
            return LNeutralGainMost;
        }

        double lNeutralGain = lNeutralTarget / lNeutralChannel;
        if (!double.IsFinite(lNeutralGain))
        {
            return 1;
        }

        return Math.Clamp(lNeutralGain, LNeutralGainLeast, LNeutralGainMost);
    }

    private static double LNeutralLinearResolve(int lNeutralChannel)
    {
        double lNeutralValue = lNeutralChannel / 255.0;
        return lNeutralValue <= 0.04045
            ? lNeutralValue / 12.92
            : Math.Pow((lNeutralValue + 0.055) / 1.055, 2.4);
    }

    private static int LNeutralMedianResolve(List<int> lNeutralValues)
    {
        lNeutralValues.Sort();
        int lNeutralCount = lNeutralValues.Count;
        int lNeutralMiddle = lNeutralCount / 2;
        if ((lNeutralCount & 1) == 1)
        {
            return lNeutralValues[lNeutralMiddle];
        }

        return (int)Math.Round(
            (lNeutralValues[lNeutralMiddle - 1] + lNeutralValues[lNeutralMiddle]) / 2.0,
            MidpointRounding.AwayFromZero);
    }

    private static LNeutralSample LNeutralFailCreate(LNeutralOutcome lNeutralOutcome) =>
        new(lNeutralOutcome, 0, 0, 0, 1, 1, 1);

    // Map a viewer-overlay click back to the raw (untransformed, stored-orientation)
    // source pixel. The overlay shows the frame after the mpv display pipeline
    // hflip -> vflip -> transpose(rotate) -> crop -> scale-to-fit(letterbox); this
    // resolver walks that chain in reverse. The shown region is the crop rect when a
    // crop is applied, otherwise the whole rotated frame, both in the final
    // (post-transpose) pixel space. Source dimensions are the raw stored dimensions.
    public static LNeutralPoint LNeutralPointResolve(
        double lNeutralClickX,
        double lNeutralClickY,
        double lNeutralDisplayX,
        double lNeutralDisplayY,
        double lNeutralDisplayWidth,
        double lNeutralDisplayHeight,
        double lNeutralShownX,
        double lNeutralShownY,
        double lNeutralShownWidth,
        double lNeutralShownHeight,
        LRotateKind lNeutralRotateKind,
        bool lNeutralFlipHorizontal,
        bool lNeutralFlipVertical,
        int lNeutralSourceWidth,
        int lNeutralSourceHeight)
    {
        if (lNeutralSourceWidth <= 0
            || lNeutralSourceHeight <= 0
            || lNeutralDisplayWidth <= 0
            || lNeutralDisplayHeight <= 0
            || lNeutralShownWidth <= 0
            || lNeutralShownHeight <= 0
            || lNeutralClickX < lNeutralDisplayX
            || lNeutralClickY < lNeutralDisplayY
            || lNeutralClickX >= lNeutralDisplayX + lNeutralDisplayWidth
            || lNeutralClickY >= lNeutralDisplayY + lNeutralDisplayHeight)
        {
            return new LNeutralPoint(false, 0, 0);
        }

        bool lNeutralQuarter =
            lNeutralRotateKind is LRotateKind.LRotate90 or LRotateKind.LRotate270;
        int lNeutralRotatedWidth = lNeutralQuarter ? lNeutralSourceHeight : lNeutralSourceWidth;
        int lNeutralRotatedHeight = lNeutralQuarter ? lNeutralSourceWidth : lNeutralSourceHeight;

        double lNeutralFractionX =
            (lNeutralClickX - lNeutralDisplayX) / lNeutralDisplayWidth;
        double lNeutralFractionY =
            (lNeutralClickY - lNeutralDisplayY) / lNeutralDisplayHeight;

        double lNeutralFinalX = lNeutralShownX + (lNeutralFractionX * lNeutralShownWidth);
        double lNeutralFinalY = lNeutralShownY + (lNeutralFractionY * lNeutralShownHeight);

        int lNeutralFinalPixelX = Math.Clamp(
            (int)Math.Floor(lNeutralFinalX), 0, lNeutralRotatedWidth - 1);
        int lNeutralFinalPixelY = Math.Clamp(
            (int)Math.Floor(lNeutralFinalY), 0, lNeutralRotatedHeight - 1);

        (int lNeutralSourceX, int lNeutralSourceY) = lNeutralRotateKind switch
        {
            LRotateKind.LRotate90 => (
                lNeutralFinalPixelY,
                lNeutralSourceHeight - 1 - lNeutralFinalPixelX),
            LRotateKind.LRotate270 => (
                lNeutralSourceWidth - 1 - lNeutralFinalPixelY,
                lNeutralFinalPixelX),
            LRotateKind.LRotate180 => (
                lNeutralSourceWidth - 1 - lNeutralFinalPixelX,
                lNeutralSourceHeight - 1 - lNeutralFinalPixelY),
            _ => (lNeutralFinalPixelX, lNeutralFinalPixelY)
        };

        if (lNeutralFlipVertical)
        {
            lNeutralSourceY = lNeutralSourceHeight - 1 - lNeutralSourceY;
        }

        if (lNeutralFlipHorizontal)
        {
            lNeutralSourceX = lNeutralSourceWidth - 1 - lNeutralSourceX;
        }

        return new LNeutralPoint(true, lNeutralSourceX, lNeutralSourceY);
    }
}
