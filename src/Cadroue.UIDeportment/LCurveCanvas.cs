using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LCurveCanvas
{
    private const double LCurveHitRadius = 9;
    private const int LCurveSampleCount = 96;
    private const int LCurveGridSteps = 4;
    private const double LCurveHistogramHeadroom = 0.94;
    private const double LCurveDotPlain = 8;
    private const double LCurveDotSelected = 11;
    private const double LCurveStrokePlain = 1.4;
    private const double LCurveStrokeSelected = 2;

    private readonly LCurve lCurve;

    public LCurveCanvas(LCurve lOwner)
    {
        lCurve = lOwner;
    }

    public IReadOnlyList<LCurveLine> LCurveGridRead(double lSize)
    {
        var lLines = new List<LCurveLine>();
        for (int lStep = 1; lStep < LCurveGridSteps; lStep++)
        {
            double lOffset = lSize * lStep / LCurveGridSteps;
            string lKey = lStep == LCurveGridSteps / 2 ? "Guide" : "Grid";
            lLines.Add(new LCurveLine(new LCurvePoint(lOffset, 0), new LCurvePoint(lOffset, lSize), lKey));
            lLines.Add(new LCurveLine(new LCurvePoint(0, lOffset), new LCurvePoint(lSize, lOffset), lKey));
        }

        if (LWorkCurveSettings.LWorkIdentityCheck(lCurve.LCurvePoints))
        {
            lLines.Add(new LCurveLine(new LCurvePoint(0, lSize), new LCurvePoint(lSize, 0), "Identity"));
        }

        return lLines;
    }

    public IReadOnlyList<LCurvePoint> LCurveTrackRead(double lSize)
    {
        IReadOnlyList<LWorkCurvePoint> lPoints = lCurve.LCurvePoints;
        double[] lXs = lPoints.Select(lPoint => lPoint.LWorkCurveInput).ToArray();
        double[] lYs = lPoints.Select(lPoint => lPoint.LWorkCurveOutput).ToArray();
        double[] lSlopes = LColorCurve.LColorSlopeResolve(lXs, lYs);
        var lTrack = new List<LCurvePoint>();
        for (int lSample = 0; lSample <= LCurveSampleCount; lSample++)
        {
            double lInput = (double)lSample / LCurveSampleCount;
            double lOutput = LColorCurve.LColorCurveResolve(lXs, lYs, lSlopes, lInput);
            lTrack.Add(LCurvePixelResolve(lInput, lOutput, lSize));
        }

        return lTrack;
    }

    public IReadOnlyList<LCurveDot> LCurveDotsRead(double lSize)
    {
        IReadOnlyList<LWorkCurvePoint> lPoints = lCurve.LCurvePoints;
        var lDots = new List<LCurveDot>();
        for (int lIndex = 0; lIndex < lPoints.Count; lIndex++)
        {
            bool lSelected = lIndex == lCurve.LCurveSelected;
            bool lEndpoint = lIndex == 0 || lIndex == lPoints.Count - 1;
            double lDot = lSelected ? LCurveDotSelected : LCurveDotPlain;
            LCurvePoint lCenter = LCurvePixelResolve(
                lPoints[lIndex].LWorkCurveInput, lPoints[lIndex].LWorkCurveOutput, lSize);
            lDots.Add(new LCurveDot(
                lCenter.LCurvePointX - (lDot / 2),
                lCenter.LCurvePointY - (lDot / 2),
                lDot,
                lSelected ? LCurveStrokeSelected : LCurveStrokePlain,
                lSelected ? "Selected" : lEndpoint ? "Endpoint" : "Plain"));
        }

        return lDots;
    }

    public IReadOnlyList<LCurvePoint> LCurveHistogramRead(double lSize)
    {
        if (lCurve.LCurveHistogram is not { } lHistogram)
        {
            return [];
        }

        int[] lBins = lCurve.LCurveChannel switch
        {
            1 => lHistogram.LHistogramRed,
            2 => lHistogram.LHistogramGreen,
            3 => lHistogram.LHistogramBlue,
            _ => lHistogram.LHistogramLuminance
        };
        int lPeak = lBins.Length == 0 ? 0 : lBins.Max();
        if (lPeak <= 0)
        {
            return [];
        }

        var lArea = new List<LCurvePoint> { new(0, lSize) };
        for (int lBin = 0; lBin < lBins.Length; lBin++)
        {
            double lX = (double)lBin / (lBins.Length - 1) * lSize;
            double lY = lSize - ((double)lBins[lBin] / lPeak * lSize * LCurveHistogramHeadroom);
            lArea.Add(new LCurvePoint(lX, lY));
        }

        lArea.Add(new LCurvePoint(lSize, lSize));
        return lArea;
    }

    public int LCurveHitFind(double lX, double lY, double lSize)
    {
        IReadOnlyList<LWorkCurvePoint> lPoints = lCurve.LCurvePoints;
        int lBest = -1;
        double lBestDistance = LCurveHitRadius;
        for (int lIndex = 0; lIndex < lPoints.Count; lIndex++)
        {
            LCurvePoint lCenter = LCurvePixelResolve(
                lPoints[lIndex].LWorkCurveInput, lPoints[lIndex].LWorkCurveOutput, lSize);
            double lDistance = Math.Sqrt(
                Math.Pow(lCenter.LCurvePointX - lX, 2) + Math.Pow(lCenter.LCurvePointY - lY, 2));
            if (lDistance <= lBestDistance)
            {
                lBest = lIndex;
                lBestDistance = lDistance;
            }
        }

        return lBest;
    }

    public static (double, double) LCurveValueResolve(double lX, double lY, double lSize) =>
        (Math.Clamp(lX / lSize, 0, 1), Math.Clamp(1 - (lY / lSize), 0, 1));

    private static LCurvePoint LCurvePixelResolve(double lInput, double lOutput, double lSize) =>
        new(Math.Clamp(lInput, 0, 1) * lSize, (1 - Math.Clamp(lOutput, 0, 1)) * lSize);
}

public sealed record LCurvePoint(double LCurvePointX, double LCurvePointY);

public sealed record LCurveLine(LCurvePoint LCurveLineFrom, LCurvePoint LCurveLineTo, string LCurveLineKey);

public sealed record LCurveDot(
    double LCurveDotLeft,
    double LCurveDotTop,
    double LCurveDotSize,
    double LCurveDotThickness,
    string LCurveDotFill);
