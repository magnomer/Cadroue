using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PInspectorCurveCanvas
{
    private const double PCurveCanvasSize = 150;

    private static readonly Brush PCurveGuideBrush =
        new SolidColorBrush(Color.FromRgb(0xC5, 0xCC, 0xD3));

    private static readonly IReadOnlyDictionary<string, Brush> PCurveLineBrush = new Dictionary<string, Brush>
    {
        ["Grid"] = new SolidColorBrush(Color.FromRgb(0xDD, 0xE2, 0xE7)),
        ["Guide"] = PCurveGuideBrush,
        ["Identity"] = new SolidColorBrush(Color.FromArgb(0x60, 0x8A, 0x94, 0x9E))
    };

    private static readonly Brush[] PCurveChannelBrush =
    {
        new SolidColorBrush(Color.FromRgb(0x33, 0x3A, 0x41)),
        new SolidColorBrush(Color.FromRgb(0xD1, 0x3A, 0x3A)),
        new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x44)),
        new SolidColorBrush(Color.FromRgb(0x2B, 0x6C, 0xB0))
    };

    private static readonly IReadOnlyDictionary<string, Brush>[] PCurveDotFill =
        PCurveChannelBrush.Select(PCurveFillCreate).ToArray();

    private static readonly Brush[] PCurveHistogramBrush =
    {
        new SolidColorBrush(Color.FromArgb(0x30, 0x8A, 0x94, 0x9E)),
        new SolidColorBrush(Color.FromArgb(0x30, 0xD1, 0x3A, 0x3A)),
        new SolidColorBrush(Color.FromArgb(0x30, 0x2F, 0x9E, 0x44)),
        new SolidColorBrush(Color.FromArgb(0x30, 0x2B, 0x6C, 0xB0))
    };

    internal PInspectorCurveCanvas(QCurve lCurve)
    {
        QCurve = lCurve;
        PCurveCanvas = new Canvas
        {
            Width = PCurveCanvasSize,
            Height = PCurveCanvasSize,
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        PCurveCanvasAttach();
    }

    private QCurve QCurve { get; }

    internal Canvas PCurveCanvas { get; }

    private static IReadOnlyDictionary<string, Brush> PCurveFillCreate(Brush pChannel) => new Dictionary<string, Brush>
    {
        ["Selected"] = pChannel,
        ["Endpoint"] = PCurveGuideBrush,
        ["Plain"] = Brushes.White
    };

    private void PCurveCanvasAttach()
    {
        PCurveCanvas.MouseLeftButtonDown += PCurvePressHandle;
        PCurveCanvas.MouseMove += PCurveMoveHandle;
        PCurveCanvas.MouseLeftButtonUp += PCurveReleaseHandle;
    }

    private void PCurvePressHandle(object pSender, MouseButtonEventArgs pArgs)
    {
        Point pPixel = pArgs.GetPosition(PCurveCanvas);
        QCurve.QCurveDrag.QCurvePressHandle(pPixel.X, pPixel.Y, PCurveCanvasSize);
        PCurveCanvas.CaptureMouse();
        pArgs.Handled = true;
    }

    private void PCurveMoveHandle(object pSender, MouseEventArgs pArgs)
    {
        Point pPixel = pArgs.GetPosition(PCurveCanvas);
        pArgs.Handled = QCurve.QCurveDrag.QCurveMoveHandle(pPixel.X, pPixel.Y, PCurveCanvasSize);
    }

    private void PCurveReleaseHandle(object pSender, MouseButtonEventArgs pArgs)
    {
        pArgs.Handled = QCurve.QCurveDrag.QCurveReleaseHandle();
        PCurveCanvas.ReleaseMouseCapture();
    }

    internal void PCurveRebuild()
    {
        PCurveCanvas.Children.Clear();
        QCurveCanvas pPlan = QCurve.QCurveCanvas;
        PCurveCanvas.Children.Add(PCurveHistogramBuild(pPlan.QCurveHistogramRead(PCurveCanvasSize)));
        pPlan.QCurveGridRead(PCurveCanvasSize).ToList().ForEach(PCurveLineDraw);
        PCurveCanvas.Children.Add(PCurveTrackBuild(pPlan.QCurveTrackRead(PCurveCanvasSize)));
        pPlan.QCurveDotsRead(PCurveCanvasSize).ToList().ForEach(PCurveDotDraw);
    }

    private Polygon PCurveHistogramBuild(IReadOnlyList<QCurvePoint> pArea) => new()
    {
        Fill = PCurveHistogramBrush[QCurve.QCurveChannel],
        IsHitTestVisible = false,
        Points = new PointCollection(pArea.Select(PCurvePointBuild))
    };

    private void PCurveLineDraw(QCurveLine pLine) => PCurveCanvas.Children.Add(new Line
    {
        X1 = pLine.QCurveLineFrom.QCurvePointX,
        Y1 = pLine.QCurveLineFrom.QCurvePointY,
        X2 = pLine.QCurveLineTo.QCurvePointX,
        Y2 = pLine.QCurveLineTo.QCurvePointY,
        Stroke = PCurveLineBrush[pLine.QCurveLineKey],
        StrokeThickness = 1,
        SnapsToDevicePixels = true
    });

    private Polyline PCurveTrackBuild(IReadOnlyList<QCurvePoint> pTrack) => new()
    {
        Stroke = PCurveChannelBrush[QCurve.QCurveChannel],
        StrokeThickness = 1.6,
        StrokeLineJoin = PenLineJoin.Round,
        Points = new PointCollection(pTrack.Select(PCurvePointBuild))
    };

    private void PCurveDotDraw(QCurveDot pDot)
    {
        var pEllipse = new Ellipse
        {
            Width = pDot.QCurveDotSize,
            Height = pDot.QCurveDotSize,
            Stroke = PCurveChannelBrush[QCurve.QCurveChannel],
            StrokeThickness = pDot.QCurveDotThickness,
            Fill = PCurveDotFill[QCurve.QCurveChannel][pDot.QCurveDotFill]
        };
        Canvas.SetLeft(pEllipse, pDot.QCurveDotLeft);
        Canvas.SetTop(pEllipse, pDot.QCurveDotTop);
        PCurveCanvas.Children.Add(pEllipse);
    }

    private static Point PCurvePointBuild(QCurvePoint pPoint) => new(pPoint.QCurvePointX, pPoint.QCurvePointY);
}
