using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QCurveCanvas
{
    private const double QCurveCanvasSize = 150;
    private const int QCurveSampleCount = 96;
    private const int QCurveGridSteps = 4;
    private const double QCurveHistogramHeadroom = 0.94;
    private const double QCurveDotPlain = 8;
    private const double QCurveDotSelected = 11;
    private const double QCurveStrokePlain = 1.4;
    private const double QCurveStrokeSelected = 2;

    private static readonly Brush QCurveGuideBrush =
        new SolidColorBrush(Color.FromRgb(0xC5, 0xCC, 0xD3));

    private static readonly IReadOnlyDictionary<string, Brush> QCurveLineBrush = new Dictionary<string, Brush>
    {
        ["Grid"] = new SolidColorBrush(Color.FromRgb(0xDD, 0xE2, 0xE7)),
        ["Guide"] = QCurveGuideBrush,
        ["Identity"] = new SolidColorBrush(Color.FromArgb(0x60, 0x8A, 0x94, 0x9E))
    };

    private static readonly Brush[] QCurveChannelBrush =
    {
        new SolidColorBrush(Color.FromRgb(0x33, 0x3A, 0x41)),
        new SolidColorBrush(Color.FromRgb(0xD1, 0x3A, 0x3A)),
        new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x44)),
        new SolidColorBrush(Color.FromRgb(0x2B, 0x6C, 0xB0))
    };

    private static readonly IReadOnlyDictionary<string, Brush>[] QCurveDotFill =
        QCurveChannelBrush.Select(QCurveFillCreate).ToArray();

    private static readonly Brush[] QCurveHistogramBrush =
    {
        new SolidColorBrush(Color.FromArgb(0x30, 0x8A, 0x94, 0x9E)),
        new SolidColorBrush(Color.FromArgb(0x30, 0xD1, 0x3A, 0x3A)),
        new SolidColorBrush(Color.FromArgb(0x30, 0x2F, 0x9E, 0x44)),
        new SolidColorBrush(Color.FromArgb(0x30, 0x2B, 0x6C, 0xB0))
    };

    private readonly QCurve qCurve;

    public QCurveCanvas(QCurve lOwner)
    {
        qCurve = lOwner;
    }

    private static IReadOnlyDictionary<string, Brush> QCurveFillCreate(Brush lChannel) =>
        new Dictionary<string, Brush>
        {
            ["Selected"] = lChannel,
            ["Endpoint"] = QCurveGuideBrush,
            ["Plain"] = Brushes.White
        };

    public void QCurveAttach(Canvas lCanvas) => QCurveAttach(lCanvas, true);

    public void QCurveAttach(Canvas lCanvas, bool lAttached)
    {
        lCanvas.MouseLeftButtonDown -= QCurvePressHandle;
        lCanvas.MouseMove -= QCurveMoveHandle;
        lCanvas.MouseLeftButtonUp -= QCurveReleaseHandle;
        if (!lAttached) return;
        lCanvas.MouseLeftButtonDown += QCurvePressHandle;
        lCanvas.MouseMove += QCurveMoveHandle;
        lCanvas.MouseLeftButtonUp += QCurveReleaseHandle;
    }

    private void QCurvePressHandle(object lSender, MouseButtonEventArgs lArgs)
    {
        var lCanvas = (Canvas)lSender;
        Point lPixel = lArgs.GetPosition(lCanvas);
        qCurve.QCurveDrag.QCurvePressHandle(lPixel.X, lPixel.Y, QCurveCanvasSize);
        lCanvas.CaptureMouse();
        lArgs.Handled = true;
    }

    private void QCurveMoveHandle(object lSender, MouseEventArgs lArgs)
    {
        var lCanvas = (Canvas)lSender;
        Point lPixel = lArgs.GetPosition(lCanvas);
        lArgs.Handled = qCurve.QCurveDrag.QCurveMoveHandle(lPixel.X, lPixel.Y, QCurveCanvasSize);
    }

    private void QCurveReleaseHandle(object lSender, MouseButtonEventArgs lArgs)
    {
        lArgs.Handled = qCurve.QCurveDrag.QCurveReleaseHandle();
        ((Canvas)lSender).ReleaseMouseCapture();
    }

    public void QCurveRebuild(Canvas lCanvas)
    {
        lCanvas.Children.Clear();
        lCanvas.Children.Add(QCurveHistogramBuild(QCurveHistogramRead(QCurveCanvasSize)));
        QCurveGridRead(QCurveCanvasSize).ToList().ForEach(lLine => QCurveLineDraw(lCanvas, lLine));
        lCanvas.Children.Add(QCurveTrackBuild(QCurveTrackRead(QCurveCanvasSize)));
        QCurveDotsRead(QCurveCanvasSize).ToList().ForEach(lDot => QCurveDotDraw(lCanvas, lDot));
    }

    private Polygon QCurveHistogramBuild(IReadOnlyList<QCurvePoint> lArea) => new()
    {
        Fill = QCurveHistogramBrush[qCurve.QCurveChannel],
        IsHitTestVisible = false,
        Points = new PointCollection(lArea.Select(QCurvePointBuild))
    };

    private static void QCurveLineDraw(Canvas lCanvas, QCurveLine lLine) => lCanvas.Children.Add(new Line
    {
        X1 = lLine.QCurveLineFrom.QCurvePointX,
        Y1 = lLine.QCurveLineFrom.QCurvePointY,
        X2 = lLine.QCurveLineTo.QCurvePointX,
        Y2 = lLine.QCurveLineTo.QCurvePointY,
        Stroke = QCurveLineBrush[lLine.QCurveLineKey],
        StrokeThickness = 1,
        SnapsToDevicePixels = true
    });

    private Polyline QCurveTrackBuild(IReadOnlyList<QCurvePoint> lTrack) => new()
    {
        Stroke = QCurveChannelBrush[qCurve.QCurveChannel],
        StrokeThickness = 1.6,
        StrokeLineJoin = PenLineJoin.Round,
        Points = new PointCollection(lTrack.Select(QCurvePointBuild))
    };

    private void QCurveDotDraw(Canvas lCanvas, QCurveDot lDot)
    {
        var lEllipse = new Ellipse
        {
            Width = lDot.QCurveDotSize,
            Height = lDot.QCurveDotSize,
            Stroke = QCurveChannelBrush[qCurve.QCurveChannel],
            StrokeThickness = lDot.QCurveDotThickness,
            Fill = QCurveDotFill[qCurve.QCurveChannel][lDot.QCurveDotFill]
        };
        Canvas.SetLeft(lEllipse, lDot.QCurveDotLeft);
        Canvas.SetTop(lEllipse, lDot.QCurveDotTop);
        lCanvas.Children.Add(lEllipse);
    }

    private static Point QCurvePointBuild(QCurvePoint lPoint) =>
        new(lPoint.QCurvePointX, lPoint.QCurvePointY);

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
