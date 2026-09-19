using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private const double PCurveCanvasSize = 150;
    private const int PCurveSampleCount = 96;
    private const double PCurveHitRadius = 9;

    private Canvas pCurveCanvas = null!;
    private bool pCurveDragActive;

    private static readonly Brush PCurveGridBrush =
        new SolidColorBrush(Color.FromRgb(0xDD, 0xE2, 0xE7));
    private static readonly Brush PCurveGuideBrush =
        new SolidColorBrush(Color.FromRgb(0xC5, 0xCC, 0xD3));
    private static readonly Brush PCurveIdentityBrush =
        new SolidColorBrush(Color.FromArgb(0x60, 0x8A, 0x94, 0x9E));

    private static readonly Brush[] PCurveChannelBrush =
    {
        new SolidColorBrush(Color.FromRgb(0x33, 0x3A, 0x41)),
        new SolidColorBrush(Color.FromRgb(0xD1, 0x3A, 0x3A)),
        new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x44)),
        new SolidColorBrush(Color.FromRgb(0x2B, 0x6C, 0xB0))
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
        int pHit = PCurveHitFind(pPixel, LCurve.LCurvePoints);
        if (pHit < 0)
        {
            LWorkCurvePoint pClicked = PCurveValueResolve(pPixel);
            LCurve.LCurvePointAdd(pClicked.LWorkCurveInput, pClicked.LWorkCurveOutput);
        }
        else
        {
            LCurve.LCurvePointSelect(pHit);
        }

        pCurveDragActive = true;
        pCurveCanvas.CaptureMouse();
        pArgs.Handled = true;
    }

    private void PCurveMoveHandle(object pSender, MouseEventArgs pArgs)
    {
        if (!pCurveDragActive)
        {
            return;
        }

        LWorkCurvePoint pValue = PCurveValueResolve(pArgs.GetPosition(pCurveCanvas));
        LCurve.LCurvePointSet(pValue.LWorkCurveInput, pValue.LWorkCurveOutput);
        pArgs.Handled = true;
    }

    private void PCurveReleaseHandle(object pSender, MouseButtonEventArgs pArgs)
    {
        if (!pCurveDragActive)
        {
            return;
        }

        pCurveDragActive = false;
        pCurveCanvas.ReleaseMouseCapture();
        pArgs.Handled = true;
    }

    private static int PCurveHitFind(Point pPixel, IReadOnlyList<LWorkCurvePoint> pPoints)
    {
        int pBest = -1;
        double pBestDistance = PCurveHitRadius;
        for (int pIndex = 0; pIndex < pPoints.Count; pIndex++)
        {
            Point pCenter = PCurvePointResolve(
                pPoints[pIndex].LWorkCurveInput, pPoints[pIndex].LWorkCurveOutput);
            double pDistance = Math.Sqrt(
                Math.Pow(pCenter.X - pPixel.X, 2) + Math.Pow(pCenter.Y - pPixel.Y, 2));
            if (pDistance <= pBestDistance)
            {
                pBest = pIndex;
                pBestDistance = pDistance;
            }
        }

        return pBest;
    }

    private static Point PCurvePointResolve(double pInput, double pOutput) =>
        new(
            Math.Clamp(pInput, 0, 1) * PCurveCanvasSize,
            (1 - Math.Clamp(pOutput, 0, 1)) * PCurveCanvasSize);

    private static LWorkCurvePoint PCurveValueResolve(Point pPixel) =>
        new(
            Math.Clamp(pPixel.X / PCurveCanvasSize, 0, 1),
            Math.Clamp(1 - (pPixel.Y / PCurveCanvasSize), 0, 1));

    private void PCurveRebuild()
    {
        if (pCurveCanvas is null)
        {
            return;
        }

        pCurveCanvas.Children.Clear();
        PCurveHistogramDraw();
        PCurveGridDraw();

        IReadOnlyList<LWorkCurvePoint> pPoints = LCurve.LCurvePoints;
        int pChannel = Math.Clamp(LCurve.LCurveChannel, 0, PCurveChannelBrush.Length - 1);
        bool pIdentity = LWorkCurveSettings.LWorkIdentityCheck(pPoints);

        if (pIdentity)
        {
            PCurveLineDraw(
                PCurvePointResolve(0, 0), PCurvePointResolve(1, 1),
                PCurveIdentityBrush, 1, false);
        }

        PCurveTrackDraw(pPoints, PCurveChannelBrush[pChannel]);
        PCurvePointsDraw(pPoints, PCurveChannelBrush[pChannel]);
    }

    private void PCurveGridDraw()
    {
        for (int pStep = 1; pStep < 4; pStep++)
        {
            double pOffset = PCurveCanvasSize * pStep / 4;
            Brush pBrush = pStep == 2 ? PCurveGuideBrush : PCurveGridBrush;
            PCurveLineDraw(
                new Point(pOffset, 0), new Point(pOffset, PCurveCanvasSize), pBrush, 1, false);
            PCurveLineDraw(
                new Point(0, pOffset), new Point(PCurveCanvasSize, pOffset), pBrush, 1, false);
        }
    }

    private void PCurveLineDraw(Point pFrom, Point pTo, Brush pBrush, double pWidth, bool pRound)
    {
        var pLine = new Line
        {
            X1 = pFrom.X,
            Y1 = pFrom.Y,
            X2 = pTo.X,
            Y2 = pTo.Y,
            Stroke = pBrush,
            StrokeThickness = pWidth,
            SnapsToDevicePixels = true
        };
        if (pRound)
        {
            pLine.StrokeStartLineCap = PenLineCap.Round;
            pLine.StrokeEndLineCap = PenLineCap.Round;
        }

        pCurveCanvas.Children.Add(pLine);
    }

    private void PCurveTrackDraw(IReadOnlyList<LWorkCurvePoint> pPoints, Brush pBrush)
    {
        var pTrack = new Polyline
        {
            Stroke = pBrush,
            StrokeThickness = 1.6,
            StrokeLineJoin = PenLineJoin.Round,
            Points = new PointCollection()
        };

        double[] pXs = pPoints.Select(pPoint => pPoint.LWorkCurveInput).ToArray();
        double[] pYs = pPoints.Select(pPoint => pPoint.LWorkCurveOutput).ToArray();
        double[] pSlopes = LColorCurve.LColorSlopeResolve(pXs, pYs);

        for (int pSample = 0; pSample <= PCurveSampleCount; pSample++)
        {
            double pInput = (double)pSample / PCurveSampleCount;
            double pOutput = LColorCurve.LColorCurveResolve(pXs, pYs, pSlopes, pInput);
            pTrack.Points.Add(PCurvePointResolve(pInput, pOutput));
        }

        pCurveCanvas.Children.Add(pTrack);
    }

    private void PCurvePointsDraw(IReadOnlyList<LWorkCurvePoint> pPoints, Brush pBrush)
    {
        for (int pIndex = 0; pIndex < pPoints.Count; pIndex++)
        {
            bool pSelected = pIndex == LCurve.LCurveSelected;
            bool pEndpoint = pIndex == 0 || pIndex == pPoints.Count - 1;
            double pSize = pSelected ? 11 : 8;
            Point pCenter = PCurvePointResolve(
                pPoints[pIndex].LWorkCurveInput, pPoints[pIndex].LWorkCurveOutput);

            var pDot = new Ellipse
            {
                Width = pSize,
                Height = pSize,
                Stroke = pBrush,
                StrokeThickness = pSelected ? 2 : 1.4,
                Fill = pSelected ? pBrush : (pEndpoint ? PCurveGuideBrush : Brushes.White)
            };
            Canvas.SetLeft(pDot, pCenter.X - (pSize / 2));
            Canvas.SetTop(pDot, pCenter.Y - (pSize / 2));
            pCurveCanvas.Children.Add(pDot);
        }
    }
}
