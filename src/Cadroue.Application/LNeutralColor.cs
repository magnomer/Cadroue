namespace Cadroue.Application;

public readonly record struct LNeutralRgb(int LNeutralRed, int LNeutralGreen, int LNeutralBlue);

public sealed record LNeutralDot(double LNeutralDotLeft, double LNeutralDotTop, bool LNeutralDotPresent);

public sealed record LNeutralBitmap(int LNeutralBitmapSize, int LNeutralBitmapStride, byte[] LNeutralBitmapPixels);

public static partial class LNeutral
{
    private const double LNeutralWheelValue = 0.75;
    private const double LNeutralWheelRatio = 0.45;
    private const int LNeutralBitmapChannels = 4;

    public static LNeutralWheel LNeutralCanvasResolve(double lNeutralX, double lNeutralY, double lNeutralSize)
    {
        double lNeutralCenter = lNeutralSize / 2.0;
        double lNeutralRadius = lNeutralSize * LNeutralWheelRatio;
        double lNeutralUnitX = (lNeutralX - lNeutralCenter) / lNeutralRadius;
        double lNeutralUnitY = (lNeutralCenter - lNeutralY) / lNeutralRadius;
        double lNeutralReach = Math.Sqrt((lNeutralUnitX * lNeutralUnitX) + (lNeutralUnitY * lNeutralUnitY));
        if (lNeutralReach > 1)
        {
            lNeutralUnitX /= lNeutralReach;
            lNeutralUnitY /= lNeutralReach;
        }

        return new LNeutralWheel(lNeutralUnitX, lNeutralUnitY, true);
    }

    public static LNeutralDot LNeutralDotResolve(LNeutralWheel lNeutralWheel, double lNeutralSize, double lNeutralDot)
    {
        double lNeutralCenter = lNeutralSize / 2.0;
        double lNeutralRadius = lNeutralSize * LNeutralWheelRatio;
        double lNeutralCenterX = lNeutralCenter + (lNeutralWheel.LNeutralWheelX * lNeutralRadius);
        double lNeutralCenterY = lNeutralCenter - (lNeutralWheel.LNeutralWheelY * lNeutralRadius);
        return new LNeutralDot(
            lNeutralCenterX - (lNeutralDot / 2),
            lNeutralCenterY - (lNeutralDot / 2),
            lNeutralWheel.LNeutralWheelPresent);
    }

    public static LNeutralBitmap LNeutralBitmapResolve(int lNeutralSize, double lNeutralValue)
    {
        double lNeutralCenter = lNeutralSize / 2.0;
        double lNeutralRadius = lNeutralSize * LNeutralWheelRatio;
        double lNeutralClamped = Math.Clamp(lNeutralValue, 0, 1);
        int lNeutralStride = lNeutralSize * LNeutralBitmapChannels;
        var lNeutralPixels = new byte[lNeutralSize * lNeutralStride];
        for (int lNeutralRow = 0; lNeutralRow < lNeutralSize; lNeutralRow++)
        {
            for (int lNeutralColumn = 0; lNeutralColumn < lNeutralSize; lNeutralColumn++)
            {
                double lNeutralX = (lNeutralColumn + 0.5 - lNeutralCenter) / lNeutralRadius;
                double lNeutralY = (lNeutralCenter - (lNeutralRow + 0.5)) / lNeutralRadius;
                double lNeutralReach = Math.Sqrt((lNeutralX * lNeutralX) + (lNeutralY * lNeutralY));
                if (lNeutralReach > 1)
                {
                    continue;
                }

                double lNeutralHue = Math.Atan2(lNeutralY, lNeutralX) * (180.0 / Math.PI);
                if (lNeutralHue < 0)
                {
                    lNeutralHue += 360;
                }

                (int lNeutralRed, int lNeutralGreen, int lNeutralBlue) =
                    LNeutralRgbResolve(lNeutralHue, lNeutralReach, lNeutralClamped);
                double lNeutralEdge = Math.Clamp((1 - lNeutralReach) * lNeutralRadius, 0, 1);
                int lNeutralOffset = (lNeutralRow * lNeutralStride) + (lNeutralColumn * LNeutralBitmapChannels);
                lNeutralPixels[lNeutralOffset] = (byte)lNeutralBlue;
                lNeutralPixels[lNeutralOffset + 1] = (byte)lNeutralGreen;
                lNeutralPixels[lNeutralOffset + 2] = (byte)lNeutralRed;
                lNeutralPixels[lNeutralOffset + 3] = (byte)Math.Round(lNeutralEdge * 255);
            }
        }

        return new LNeutralBitmap(lNeutralSize, lNeutralStride, lNeutralPixels);
    }

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

    public static LNeutralRgb LNeutralRgbResolve(
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

        return new LNeutralRgb(
            LNeutralByteResolve(lNeutralRedUnit + lNeutralMatch),
            LNeutralByteResolve(lNeutralGreenUnit + lNeutralMatch),
            LNeutralByteResolve(lNeutralBlueUnit + lNeutralMatch));
    }

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
