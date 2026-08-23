using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const double PCurveCanvasSize = 150;
    private const int PCurveSampleCount = 96;
    private const double PCurveHitRadius = 9;
    private const double PCurveMinGap = 1.0 / PCurveCanvasSize;

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
        List<LWorkCurvePoint> pPoints = PCurveActiveRead();
        int pHit = PCurveHitFind(pPixel, pPoints);
        if (pHit < 0)
        {
            pHit = PCurvePointAdd(pPixel, pPoints);
            PInspectorVideoChange?.Invoke();
        }

        pCurveSelected = pHit;
        pCurveDragActive = true;
        pCurveCanvas.CaptureMouse();
        PCurveBoxesUpdate();
        pArgs.Handled = true;
    }

    private void PCurveMoveHandle(object pSender, MouseEventArgs pArgs)
    {
        if (!pCurveDragActive)
        {
            return;
        }

        List<LWorkCurvePoint> pPoints = PCurveActiveRead();
        if (pCurveSelected < 0 || pCurveSelected >= pPoints.Count)
        {
            return;
        }

        LWorkCurvePoint pValue = PCurveValueResolve(pArgs.GetPosition(pCurveCanvas));
        double pInput;
        if (pCurveSelected == 0)
        {
            pInput = 0;
        }
        else if (pCurveSelected == pPoints.Count - 1)
        {
            pInput = 1;
        }
        else
        {
            pInput = Math.Clamp(
                pValue.LWorkCurveInput,
                pPoints[pCurveSelected - 1].LWorkCurveInput + PCurveMinGap,
                pPoints[pCurveSelected + 1].LWorkCurveInput - PCurveMinGap);
        }

        pPoints[pCurveSelected] = new LWorkCurvePoint(pInput, pValue.LWorkCurveOutput);
        PCurveBoxesUpdate();
        PInspectorVideoChange?.Invoke();
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

    // Nearest control point within the hit radius, else -1.
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

    // Insert a new interior control point at the clicked value, returning its
    // sorted index.
    private static int PCurvePointAdd(Point pPixel, List<LWorkCurvePoint> pPoints)
    {
        LWorkCurvePoint pClicked = PCurveValueResolve(pPixel);
        var pPoint = new LWorkCurvePoint(
            Math.Clamp(pClicked.LWorkCurveInput, PCurveMinGap, 1 - PCurveMinGap),
            pClicked.LWorkCurveOutput);
        pPoints.Add(pPoint);
        pPoints.Sort((pLeft, pRight) => pLeft.LWorkCurveInput.CompareTo(pRight.LWorkCurveInput));
        return pPoints.IndexOf(pPoint);
    }

    // Value → canvas pixel: input on X (0 left → 1 right), output on Y (0 bottom
    // → 1 top). Reused next job for hit-testing.
    private static Point PCurvePointResolve(double pInput, double pOutput) =>
        new(
            Math.Clamp(pInput, 0, 1) * PCurveCanvasSize,
            (1 - Math.Clamp(pOutput, 0, 1)) * PCurveCanvasSize);

    // Canvas pixel → value, the inverse of PCurvePointResolve.
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

        List<LWorkCurvePoint> pPoints = PCurveActiveRead();
        int pChannel = Math.Clamp(pCurveChannel.SelectedIndex, 0, PCurveChannelBrush.Length - 1);
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
        double[] pSlopes = PCurveTangentResolve(pXs, pYs);

        for (int pSample = 0; pSample <= PCurveSampleCount; pSample++)
        {
            double pInput = (double)pSample / PCurveSampleCount;
            double pOutput = PCurveSampleResolve(pXs, pYs, pSlopes, pInput);
            pTrack.Points.Add(PCurvePointResolve(pInput, pOutput));
        }

        pCurveCanvas.Children.Add(pTrack);
    }

    private void PCurvePointsDraw(IReadOnlyList<LWorkCurvePoint> pPoints, Brush pBrush)
    {
        for (int pIndex = 0; pIndex < pPoints.Count; pIndex++)
        {
            bool pSelected = pIndex == pCurveSelected;
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

    // Monotone pchip tangents (Fritsch–Carlson), matching FFmpeg's interp=pchip so
    // the on-screen track equals the rendered result.
    private static double[] PCurveTangentResolve(double[] pXs, double[] pYs)
    {
        int pCount = pXs.Length;
        var pSlopes = new double[pCount];
        if (pCount < 2)
        {
            return pSlopes;
        }

        var pDeltas = new double[pCount - 1];
        for (int pIndex = 0; pIndex < pCount - 1; pIndex++)
        {
            double pRun = pXs[pIndex + 1] - pXs[pIndex];
            pDeltas[pIndex] = pRun <= 0 ? 0 : (pYs[pIndex + 1] - pYs[pIndex]) / pRun;
        }

        pSlopes[0] = pDeltas[0];
        pSlopes[pCount - 1] = pDeltas[pCount - 2];
        for (int pIndex = 1; pIndex < pCount - 1; pIndex++)
        {
            double pLeft = pDeltas[pIndex - 1];
            double pRight = pDeltas[pIndex];
            if (pLeft * pRight <= 0)
            {
                pSlopes[pIndex] = 0;
                continue;
            }

            double pSpanLeft = pXs[pIndex] - pXs[pIndex - 1];
            double pSpanRight = pXs[pIndex + 1] - pXs[pIndex];
            double pWeightLeft = (2 * pSpanRight) + pSpanLeft;
            double pWeightRight = pSpanRight + (2 * pSpanLeft);
            pSlopes[pIndex] =
                (pWeightLeft + pWeightRight) / ((pWeightLeft / pLeft) + (pWeightRight / pRight));
        }

        return pSlopes;
    }

    private static double PCurveSampleResolve(
        double[] pXs, double[] pYs, double[] pSlopes, double pInput)
    {
        int pCount = pXs.Length;
        if (pCount == 0)
        {
            return pInput;
        }

        if (pInput <= pXs[0])
        {
            return pYs[0];
        }

        if (pInput >= pXs[pCount - 1])
        {
            return pYs[pCount - 1];
        }

        int pSegment = 0;
        while (pSegment < pCount - 2 && pInput > pXs[pSegment + 1])
        {
            pSegment++;
        }

        double pSpan = pXs[pSegment + 1] - pXs[pSegment];
        if (pSpan <= 0)
        {
            return pYs[pSegment];
        }

        double pStep = (pInput - pXs[pSegment]) / pSpan;
        double pStepSquare = pStep * pStep;
        double pStepCube = pStepSquare * pStep;
        double pHermite00 = (2 * pStepCube) - (3 * pStepSquare) + 1;
        double pHermite10 = pStepCube - (2 * pStepSquare) + pStep;
        double pHermite01 = (-2 * pStepCube) + (3 * pStepSquare);
        double pHermite11 = pStepCube - pStepSquare;
        return Math.Clamp(
            (pHermite00 * pYs[pSegment])
            + (pHermite10 * pSpan * pSlopes[pSegment])
            + (pHermite01 * pYs[pSegment + 1])
            + (pHermite11 * pSpan * pSlopes[pSegment + 1]),
            0, 1);
    }
}
