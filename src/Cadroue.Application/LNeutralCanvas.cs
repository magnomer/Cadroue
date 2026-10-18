namespace Cadroue.Application;

public sealed record LNeutralDot(double LNeutralDotLeft, double LNeutralDotTop, bool LNeutralDotPresent);

public sealed record LNeutralBitmap(int LNeutralBitmapSize, int LNeutralBitmapStride, byte[] LNeutralBitmapPixels);

// The neutral wheel laid on a square canvas: a canvas point to a wheel point, a wheel point to its dot, and the
// wheel's own bitmap.
public static class LNeutralCanvas
{
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
                    LNeutralColor.LNeutralRgbResolve(lNeutralHue, lNeutralReach, lNeutralClamped);
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
}
