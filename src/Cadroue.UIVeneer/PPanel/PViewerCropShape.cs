using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.Application;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PViewer
{
    private const double PCropHandleSize = 10;

    private static readonly int[] PCropEdgeX = [-1, 0, 1, 1, 1, 0, -1, -1];
    private static readonly int[] PCropEdgeY = [-1, -1, -1, 0, 1, 1, 1, 0];

    private static readonly Cursor[] PCropHandleCursors =
    [
        Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE,
        Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE
    ];

    private void PCropHandlesBuild()
    {
        for (int pHandleIndex = 0; pHandleIndex < pViewerCropHandles.Length; pHandleIndex++)
        {
            var pHandle = new Rectangle
            {
                Width = PCropHandleSize,
                Height = PCropHandleSize,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7)),
                StrokeThickness = 1.5,
                Cursor = PCropHandleCursors[pHandleIndex],
                Visibility = Visibility.Collapsed,
                Tag = pHandleIndex
            };
            pHandle.MouseLeftButtonDown += PCropGripHandle;
            pViewerCropHandles[pHandleIndex] = pHandle;
            pViewerOverlay.Children.Add(pHandle);
        }
    }

    private void PCropOverlayUpdate()
    {
        PCropHandlesPlace();
        PCropShadeUpdate();
    }

    private void PCropShadeUpdate()
    {
        if (pViewerCropBox.Visibility != Visibility.Visible
            || pViewerCropBox.Width <= 0
            || pViewerCropBox.Height <= 0)
        {
            pViewerCropShade.Visibility = Visibility.Collapsed;
            return;
        }

        var pShadeGeometry = new GeometryGroup { FillRule = FillRule.EvenOdd };
        pShadeGeometry.Children.Add(new RectangleGeometry(PCropRectRead()));
        pShadeGeometry.Children.Add(new RectangleGeometry(new Rect(
            Canvas.GetLeft(pViewerCropBox),
            Canvas.GetTop(pViewerCropBox),
            pViewerCropBox.Width,
            pViewerCropBox.Height)));

        pViewerCropShade.Data = pShadeGeometry;
        pViewerCropShade.Visibility = Visibility.Visible;
    }

    private void PCropHandlesPlace()
    {
        bool pHandlesVisible = PCropEditableCheck()
            && pViewerCropBox.Visibility == Visibility.Visible
            && pViewerCropBox.Width > 0
            && pViewerCropBox.Height > 0;

        double pBoxLeft = Canvas.GetLeft(pViewerCropBox);
        double pBoxTop = Canvas.GetTop(pViewerCropBox);

        for (int pHandleIndex = 0; pHandleIndex < pViewerCropHandles.Length; pHandleIndex++)
        {
            Rectangle pHandle = pViewerCropHandles[pHandleIndex];
            pHandle.Visibility = pHandlesVisible ? Visibility.Visible : Visibility.Collapsed;
            if (!pHandlesVisible)
            {
                continue;
            }

            int pEdgeX = PCropEdgeX[pHandleIndex];
            int pEdgeY = PCropEdgeY[pHandleIndex];
            double pPointX = pEdgeX == 0
                ? pBoxLeft + (pViewerCropBox.Width / 2)
                : pEdgeX < 0 ? pBoxLeft : pBoxLeft + pViewerCropBox.Width;
            double pPointY = pEdgeY == 0
                ? pBoxTop + (pViewerCropBox.Height / 2)
                : pEdgeY < 0 ? pBoxTop : pBoxTop + pViewerCropBox.Height;

            Canvas.SetLeft(pHandle, pPointX - (PCropHandleSize / 2));
            Canvas.SetTop(pHandle, pPointY - (PCropHandleSize / 2));
        }
    }

    private void PCropBoxPlace(Point startPoint, Point endPoint)
    {
        Point clampedStart = PCropPointClamp(startPoint);
        Point clampedEnd = PCropPointClamp(endPoint);
        LCropbox pCropDrawn = LCropbox.LCropboxDrawResolve(
            clampedStart.X, clampedStart.Y, clampedEnd.X, clampedEnd.Y,
            LCrop.LCropRatioWidth, LCrop.LCropRatioHeight);
        Canvas.SetLeft(pViewerCropBox, pCropDrawn.LCropboxX);
        Canvas.SetTop(pViewerCropBox, pCropDrawn.LCropboxY);
        pViewerCropBox.Width = pCropDrawn.LCropboxWidth;
        pViewerCropBox.Height = pCropDrawn.LCropboxHeight;
        PCropOverlayUpdate();
    }

    private void PCropBoxRestore()
    {
        if (PCropVideo is not { } pCropVideo || !LViewer.LViewerVideoPresent)
        {
            return;
        }

        Rect videoRect = PCropRectRead();
        Size displaySize = PCropDisplayRead();
        if (displaySize.Width <= 0 || displaySize.Height <= 0)
        {
            return;
        }

        LCropbox pCropOverlay = LCropbox.LCropboxOverlayResolve(
            PCropboxResolve(pCropVideo), PCropboxResolve(videoRect), displaySize.Width, displaySize.Height);
        Canvas.SetLeft(pViewerCropBox, pCropOverlay.LCropboxX);
        Canvas.SetTop(pViewerCropBox, pCropOverlay.LCropboxY);
        pViewerCropBox.Width = pCropOverlay.LCropboxWidth;
        pViewerCropBox.Height = pCropOverlay.LCropboxHeight;
        PCropOverlayUpdate();
    }
}
