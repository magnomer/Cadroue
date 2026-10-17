using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PCrop : Canvas
{
    private static readonly Cursor[] pCropHandleCursors =
    [
        Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE,
        Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE
    ];

    private static readonly IReadOnlyDictionary<bool, Action<PCrop>> pCropCaptures =
        new Dictionary<bool, Action<PCrop>>
        {
            [true] = pCrop => pCrop.CaptureMouse(),
            [false] = pCrop => pCrop.ReleaseMouseCapture(),
        };

    private Rectangle pCropBox = null!;
    private System.Windows.Shapes.Path pCropShade = null!;
    private Rectangle[] pCropHandles = null!;

    public PCrop(QViewer lViewer)
    {
        QViewer = lViewer;
        PCropBuild();
        PCropAttach();
    }

    private void PCropBuild()
    {
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        Focusable = true;
        AllowDrop = true;

        pCropShade = new System.Windows.Shapes.Path
        {
            Fill = new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        pCropBox = new Rectangle
        {
            Stroke = Brushes.White,
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 3 },
            Fill = Brushes.Transparent,
            Visibility = Visibility.Collapsed
        };
        Children.Add(pCropShade);
        Children.Add(pCropBox);
        pCropHandles = Enumerable.Range(0, QCrop.QCropHandleCount).Select(PCropHandleBuild).ToArray();
    }

    private void PCropAttach()
    {
        Enumerable.Range(0, pCropHandles.Length).ToList().ForEach(PCropGripAttach);
        pCropBox.MouseLeftButtonDown += PCropBodyHandle;
        MouseLeftButtonDown += PCropPressHandle;
        MouseMove += PCropMoveHandle;
        MouseLeftButtonUp += PCropReleaseHandle;
        SizeChanged += PCropSizeHandle;
        KeyDown += PCropKeyHandle;
        QCrop.QCropApplied += PCropApply;
        QCropDrag.QCropCaptureApplied += PCropCaptureApply;
        QViewer.QViewerNeutral.QViewerFocusApplied += PCropFocusApply;
    }

    public QViewer QViewer { get; }

    private QCrop QCrop => QViewer.QCrop;

    private QCropDrag QCropDrag => QViewer.QCropDrag;

    public void PCropClose()
    {
        pCropBox.MouseLeftButtonDown -= PCropBodyHandle;
        MouseLeftButtonDown -= PCropPressHandle;
        MouseMove -= PCropMoveHandle;
        MouseLeftButtonUp -= PCropReleaseHandle;
        SizeChanged -= PCropSizeHandle;
        KeyDown -= PCropKeyHandle;
        QCrop.QCropApplied -= PCropApply;
        QCropDrag.QCropCaptureApplied -= PCropCaptureApply;
        QViewer.QViewerNeutral.QViewerFocusApplied -= PCropFocusApply;
    }

    private Rectangle PCropHandleBuild(int pIndex)
    {
        var pHandle = new Rectangle
        {
            Width = QCrop.QCropHandleSize,
            Height = QCrop.QCropHandleSize,
            Fill = Brushes.White,
            Stroke = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7)),
            StrokeThickness = 1.5,
            Cursor = pCropHandleCursors[pIndex],
            Visibility = Visibility.Collapsed
        };
        Children.Add(pHandle);
        return pHandle;
    }

    private void PCropGripAttach(int pIndex) =>
        pCropHandles[pIndex].MouseLeftButtonDown += (_, pEvent) =>
            pEvent.Handled = QCropDrag.QCropGripHandle(pIndex, pEvent.GetPosition(this).X, pEvent.GetPosition(this).Y);

    private void PCropApply()
    {
        PCropBoxPlace(QCrop.QCropBoxRead());
        PCropCursorApply();
        QCrop.QCropHandlesRead().ToList().ForEach(PCropHandlePlace);
        PCropShadeApply(QCrop.QCropShadeRead());
    }

    private void PCropBoxPlace(QCropBox lBox)
    {
        SetLeft(pCropBox, lBox.QCropBoxX);
        SetTop(pCropBox, lBox.QCropBoxY);
        pCropBox.Width = lBox.QCropBoxWidth;
        pCropBox.Height = lBox.QCropBoxHeight;
        pCropBox.Visibility = PLook.PLookVisible[lBox.QCropBoxShown];
    }

    private void PCropCursorApply()
    {
        pCropBox.Cursor = PLook.PLookSizeAll[QCrop.QCropEditable];
        Cursor = PLook.PLookCross[QCrop.QCropCrossShown];
    }

    private void PCropHandlePlace(QCropGrip lHandle)
    {
        Rectangle pHandle = pCropHandles[lHandle.QCropHandleIndex];
        pHandle.Visibility = PLook.PLookVisible[lHandle.QCropHandleShown];
        SetLeft(pHandle, lHandle.QCropHandleX);
        SetTop(pHandle, lHandle.QCropHandleY);
    }

    private void PCropShadeApply(QCropShade lShade)
    {
        GeometryGroup pShadeGeometry = PCropGeometryBuild(lShade);
        PCropShadeSet(pShadeGeometry, lShade.QCropShadeShown);
    }

    private static GeometryGroup PCropGeometryBuild(QCropShade lShade)
    {
        var pShadeGeometry = new GeometryGroup { FillRule = FillRule.EvenOdd };
        pShadeGeometry.Children.Add(new RectangleGeometry(PCropRectBuild(lShade.QCropShadeVideo)));
        pShadeGeometry.Children.Add(new RectangleGeometry(PCropRectBuild(lShade.QCropShadeBox)));
        return pShadeGeometry;
    }

    private void PCropShadeSet(GeometryGroup pShadeGeometry, bool pShown)
    {
        pCropShade.Data = pShadeGeometry;
        pCropShade.Visibility = PLook.PLookVisible[pShown];
    }

    private static Rect PCropRectBuild(LCropbox lCropbox) =>
        new(lCropbox.LCropboxX, lCropbox.LCropboxY, lCropbox.LCropboxWidth, lCropbox.LCropboxHeight);

    private void PCropCaptureApply(bool lCapture) => pCropCaptures[lCapture](this);

    private void PCropFocusApply() => Focus();

    private void PCropBodyHandle(object pSender, MouseButtonEventArgs pEvent) =>
        pEvent.Handled = QCropDrag.QCropBodyHandle(pEvent.GetPosition(this).X, pEvent.GetPosition(this).Y);

    private void PCropPressHandle(object pSender, MouseButtonEventArgs pEvent) =>
        pEvent.Handled = QCropDrag.QCropPressHandle(pEvent.GetPosition(this).X, pEvent.GetPosition(this).Y);

    private void PCropMoveHandle(object pSender, MouseEventArgs pEvent) =>
        pEvent.Handled = QCropDrag.QCropMoveHandle(
            PLook.PLookPressed[pEvent.LeftButton], pEvent.GetPosition(this).X, pEvent.GetPosition(this).Y);

    private void PCropReleaseHandle(object pSender, MouseButtonEventArgs pEvent) =>
        pEvent.Handled = QCropDrag.QCropReleaseHandle(pEvent.GetPosition(this).X, pEvent.GetPosition(this).Y);

    private void PCropSizeHandle(object pSender, SizeChangedEventArgs pEvent) =>
        QCrop.QCropSizeHandle(pEvent.NewSize.Width, pEvent.NewSize.Height);

    private void PCropKeyHandle(object pSender, KeyEventArgs pEvent) =>
        pEvent.Handled = QViewer.QViewerNeutral.QViewerKeyHandle(pEvent.Key.ToString());
}
