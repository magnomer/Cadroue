namespace Cadroue.Core;

public enum LColorKind
{
    LColorKindWhitebalance,
    LColorKindExposure,
    LColorKindBrightness,
    LColorKindContrast,
    LColorKindGamma,
    LColorKindSaturation,
    LColorKindCurve
}

public static class LColorCurve
{
    public const double LColorCurveGap = 1.0 / 150;

    public static double LColorCurveClamp(IReadOnlyList<LWorkCurvePoint> lPoints, int lIndex, double lInput)
    {
        if (lIndex <= 0)
        {
            return 0;
        }

        if (lIndex >= lPoints.Count - 1)
        {
            return 1;
        }

        return Math.Clamp(
            lInput,
            lPoints[lIndex - 1].LWorkCurveInput + LColorCurveGap,
            lPoints[lIndex + 1].LWorkCurveInput - LColorCurveGap);
    }

    public static int LColorCurveInsert(List<LWorkCurvePoint> lPoints, double lInput, double lOutput)
    {
        var lPoint = new LWorkCurvePoint(Math.Clamp(lInput, LColorCurveGap, 1 - LColorCurveGap), lOutput);
        lPoints.Add(lPoint);
        lPoints.Sort((lLeft, lRight) => lLeft.LWorkCurveInput.CompareTo(lRight.LWorkCurveInput));
        return lPoints.IndexOf(lPoint);
    }

    public static double[] LColorSlopeResolve(double[] lXs, double[] lYs)
    {
        int lCount = lXs.Length;
        var lSlopes = new double[lCount];
        if (lCount < 2)
        {
            return lSlopes;
        }

        var lDeltas = new double[lCount - 1];
        for (int lIndex = 0; lIndex < lCount - 1; lIndex++)
        {
            double lRun = lXs[lIndex + 1] - lXs[lIndex];
            lDeltas[lIndex] = lRun <= 0 ? 0 : (lYs[lIndex + 1] - lYs[lIndex]) / lRun;
        }

        lSlopes[0] = lDeltas[0];
        lSlopes[lCount - 1] = lDeltas[lCount - 2];
        for (int lIndex = 1; lIndex < lCount - 1; lIndex++)
        {
            double lLeft = lDeltas[lIndex - 1];
            double lRight = lDeltas[lIndex];
            if (lLeft * lRight <= 0)
            {
                lSlopes[lIndex] = 0;
                continue;
            }

            double lSpanLeft = lXs[lIndex] - lXs[lIndex - 1];
            double lSpanRight = lXs[lIndex + 1] - lXs[lIndex];
            double lWeightLeft = (2 * lSpanRight) + lSpanLeft;
            double lWeightRight = lSpanRight + (2 * lSpanLeft);
            lSlopes[lIndex] =
                (lWeightLeft + lWeightRight) / ((lWeightLeft / lLeft) + (lWeightRight / lRight));
        }

        return lSlopes;
    }

    public static double LColorCurveResolve(double[] lXs, double[] lYs, double[] lSlopes, double lInput)
    {
        int lCount = lXs.Length;
        if (lCount == 0)
        {
            return lInput;
        }

        if (lInput <= lXs[0])
        {
            return lYs[0];
        }

        if (lInput >= lXs[lCount - 1])
        {
            return lYs[lCount - 1];
        }

        int lSegment = 0;
        while (lSegment < lCount - 2 && lInput > lXs[lSegment + 1])
        {
            lSegment++;
        }

        double lSpan = lXs[lSegment + 1] - lXs[lSegment];
        if (lSpan <= 0)
        {
            return lYs[lSegment];
        }

        double lStep = (lInput - lXs[lSegment]) / lSpan;
        double lStepSquare = lStep * lStep;
        double lStepCube = lStepSquare * lStep;
        double lHermite00 = (2 * lStepCube) - (3 * lStepSquare) + 1;
        double lHermite10 = lStepCube - (2 * lStepSquare) + lStep;
        double lHermite01 = (-2 * lStepCube) + (3 * lStepSquare);
        double lHermite11 = lStepCube - lStepSquare;
        return Math.Clamp(
            (lHermite00 * lYs[lSegment])
            + (lHermite10 * lSpan * lSlopes[lSegment])
            + (lHermite01 * lYs[lSegment + 1])
            + (lHermite11 * lSpan * lSlopes[lSegment + 1]),
            0, 1);
    }
}
