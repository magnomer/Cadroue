using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private const double PCurveCanvasSize = 150;

    private Canvas pCurveCanvas = null!;

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

    private static IReadOnlyDictionary<string, Brush> PCurveFillCreate(Brush pChannel) => new Dictionary<string, Brush>
    {
        ["Selected"] = pChannel,
        ["Endpoint"] = PCurveGuideBrush,
        ["Plain"] = Brushes.White
    };

    private Canvas PCurveCanvasBuild()
    {
        pCurveCanvas = new Canvas
        {
            Width = PCurveCanvasSize,
            Height = PCurveCanvasSize,
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        pCurveCanvas.MouseLeftButtonDown += PCurvePressHandle;
        pCurveCanvas.MouseMove += PCurveMoveHandle;
        pCurveCanvas.MouseLeftButtonUp += PCurveReleaseHandle;
        return pCurveCanvas;
    }

    private void PCurvePressHandle(object pSender, MouseButtonEventArgs pArgs)
    {
        Point pPixel = pArgs.GetPosition(pCurveCanvas);
        LCurve.LCurvePressHandle(pPixel.X, pPixel.Y, PCurveCanvasSize);
        pCurveCanvas.CaptureMouse();
        pArgs.Handled = true;
    }

    private void PCurveMoveHandle(object pSender, MouseEventArgs pArgs)
    {
        Point pPixel = pArgs.GetPosition(pCurveCanvas);
        pArgs.Handled = LCurve.LCurveMoveHandle(pPixel.X, pPixel.Y, PCurveCanvasSize);
    }

    private void PCurveReleaseHandle(object pSender, MouseButtonEventArgs pArgs)
    {
        pArgs.Handled = LCurve.LCurveReleaseHandle();
        pCurveCanvas.ReleaseMouseCapture();
    }

    private void PCurveRebuild()
    {
        pCurveCanvas.Children.Clear();
        LCurveCanvas pPlan = LCurve.LCurveCanvas;
        pCurveCanvas.Children.Add(PCurveHistogramBuild(pPlan.LCurveHistogramRead(PCurveCanvasSize)));
        pPlan.LCurveGridRead(PCurveCanvasSize).ToList().ForEach(PCurveLineDraw);
        pCurveCanvas.Children.Add(PCurveTrackBuild(pPlan.LCurveTrackRead(PCurveCanvasSize)));
        pPlan.LCurveDotsRead(PCurveCanvasSize).ToList().ForEach(PCurveDotDraw);
    }

    private Polygon PCurveHistogramBuild(IReadOnlyList<LCurvePoint> pArea) => new()
    {
        Fill = PCurveHistogramBrush[LCurve.LCurveChannel],
        IsHitTestVisible = false,
        Points = new PointCollection(pArea.Select(PCurvePointBuild))
    };

    private void PCurveLineDraw(LCurveLine pLine) => pCurveCanvas.Children.Add(new Line
    {
        X1 = pLine.LCurveLineFrom.LCurvePointX,
        Y1 = pLine.LCurveLineFrom.LCurvePointY,
        X2 = pLine.LCurveLineTo.LCurvePointX,
        Y2 = pLine.LCurveLineTo.LCurvePointY,
        Stroke = PCurveLineBrush[pLine.LCurveLineKey],
        StrokeThickness = 1,
        SnapsToDevicePixels = true
    });

    private Polyline PCurveTrackBuild(IReadOnlyList<LCurvePoint> pTrack) => new()
    {
        Stroke = PCurveChannelBrush[LCurve.LCurveChannel],
        StrokeThickness = 1.6,
        StrokeLineJoin = PenLineJoin.Round,
        Points = new PointCollection(pTrack.Select(PCurvePointBuild))
    };

    private void PCurveDotDraw(LCurveDot pDot)
    {
        var pEllipse = new Ellipse
        {
            Width = pDot.LCurveDotSize,
            Height = pDot.LCurveDotSize,
            Stroke = PCurveChannelBrush[LCurve.LCurveChannel],
            StrokeThickness = pDot.LCurveDotThickness,
            Fill = PCurveDotFill[LCurve.LCurveChannel][pDot.LCurveDotFill]
        };
        Canvas.SetLeft(pEllipse, pDot.LCurveDotLeft);
        Canvas.SetTop(pEllipse, pDot.LCurveDotTop);
        pCurveCanvas.Children.Add(pEllipse);
    }

    private static Point PCurvePointBuild(LCurvePoint pPoint) => new(pPoint.LCurvePointX, pPoint.LCurvePointY);
}
