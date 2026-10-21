using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QCurveCanvas
{
    private const int QCurveSampleCount = 96;
    private const int QCurveGridSteps = 4;
    private const double QCurveHistogramHeadroom = 0.94;
    private const double QCurveDotPlain = 8;
    private const double QCurveDotSelected = 11;
    private const double QCurveStrokePlain = 1.4;
    private const double QCurveStrokeSelected = 2;

    private readonly QCurve qCurve;

    public QCurveCanvas(QCurve lOwner)
    {
        qCurve = lOwner;
    }

    public IReadOnlyList<QCurveLine> QCurveGridRead(double lSize)
    {
        var lLines = new List<QCurveLine>();
        for (int lStep = 1; lStep < QCurveGridSteps; lStep++)
        {
            double lOffset = lSize * lStep / QCurveGridSteps;
            string lKey = lStep == QCurveGridSteps / 2 ? "Guide" : "Grid";
            lLines.Add(new QCurveLine(new QCurvePoint(lOffset, 0), new QCurvePoint(lOffset, lSize), lKey));
            lLines.Add(new QCurveLine(new QCurvePoint(0, lOffset), new QCurvePoint(lSize, lOffset), lKey));
        }

        if (LWorkCurveSettings.LWorkIdentityCheck(qCurve.QCurvePoints))
        {
            lLines.Add(new QCurveLine(new QCurvePoint(0, lSize), new QCurvePoint(lSize, 0), "Identity"));
        }

        return lLines;
    }

    public IReadOnlyList<QCurvePoint> QCurveTrackRead(double lSize)
    {
        IReadOnlyList<(double, double)> lSamples = qCurve.QCurveTrackRead(QCurveSampleCount);
        var lTrack = new List<QCurvePoint>();
        foreach ((double lInput, double lOutput) in lSamples)
        {
            lTrack.Add(QCurvePixelResolve(lInput, lOutput, lSize));
        }

        return lTrack;
    }

    public IReadOnlyList<QCurveDot> QCurveDotsRead(double lSize)
    {
        IReadOnlyList<LWorkCurvePoint> lPoints = qCurve.QCurvePoints;
        var lDots = new List<QCurveDot>();
        for (int lIndex = 0; lIndex < lPoints.Count; lIndex++)
        {
            bool lSelected = lIndex == qCurve.QCurveSelected;
            bool lEndpoint = lIndex == 0 || lIndex == lPoints.Count - 1;
            double lDot = lSelected ? QCurveDotSelected : QCurveDotPlain;
            QCurvePoint lCenter = QCurvePixelResolve(
                lPoints[lIndex].LWorkCurveInput, lPoints[lIndex].LWorkCurveOutput, lSize);
            lDots.Add(new QCurveDot(
                lCenter.QCurvePointX - (lDot / 2),
                lCenter.QCurvePointY - (lDot / 2),
                lDot,
                lSelected ? QCurveStrokeSelected : QCurveStrokePlain,
                lSelected ? "Selected" : lEndpoint ? "Endpoint" : "Plain"));
        }

        return lDots;
    }

    public IReadOnlyList<QCurvePoint> QCurveHistogramRead(double lSize)
    {
        if (qCurve.QCurveHistogram is not { } lHistogram)
        {
            return [];
        }

        int[] lBins = qCurve.QCurveChannel switch
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

        var lArea = new List<QCurvePoint> { new(0, lSize) };
        for (int lBin = 0; lBin < lBins.Length; lBin++)
        {
            double lX = (double)lBin / (lBins.Length - 1) * lSize;
            double lY = lSize - ((double)lBins[lBin] / lPeak * lSize * QCurveHistogramHeadroom);
            lArea.Add(new QCurvePoint(lX, lY));
        }

        lArea.Add(new QCurvePoint(lSize, lSize));
        return lArea;
    }

    public static (double, double) QCurveValueResolve(double lX, double lY, double lSize) =>
        (Math.Clamp(lX / lSize, 0, 1), Math.Clamp(1 - (lY / lSize), 0, 1));

    private static QCurvePoint QCurvePixelResolve(double lInput, double lOutput, double lSize) =>
        new(Math.Clamp(lInput, 0, 1) * lSize, (1 - Math.Clamp(lOutput, 0, 1)) * lSize);
}

public sealed record QCurvePoint(double QCurvePointX, double QCurvePointY);

public sealed record QCurveLine(QCurvePoint QCurveLineFrom, QCurvePoint QCurveLineTo, string QCurveLineKey);

public sealed record QCurveDot(
    double QCurveDotLeft,
    double QCurveDotTop,
    double QCurveDotSize,
    double QCurveDotThickness,
    string QCurveDotFill);
