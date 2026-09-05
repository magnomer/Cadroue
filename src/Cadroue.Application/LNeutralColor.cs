namespace Cadroue.Application;

public static partial class LNeutral
{
    private const double LNeutralWheelValue = 0.75;

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

    private static int LNeutralByteResolve(double lNeutralUnit) =>
        Math.Clamp((int)Math.Round(lNeutralUnit * 255.0), 0, 255);
}
