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

    private readonly Rectangle pCropBox;
    private readonly Path pCropShade;
    private readonly Rectangle[] pCropHandles;

    public PCrop(LViewer lViewer)
    {
        LViewer = lViewer;
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        Focusable = true;
        AllowDrop = true;

        pCropShade = new Path
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
        pCropHandles = Enumerable.Range(0, LCrop.LCropHandleCount).Select(PCropHandleBuild).ToArray();

        pCropBox.MouseLeftButtonDown += PCropBodyHandle;
        MouseLeftButtonDown += PCropPressHandle;
        MouseMove += PCropMoveHandle;
        MouseLeftButtonUp += PCropReleaseHandle;
        SizeChanged += PCropSizeHandle;
        KeyDown += PCropKeyHandle;
        LCrop.LCropApply += PCropApply;
        LCropDrag.LCropCaptureApply += PCropCaptureApply;
        LViewer.LViewerNeutral.LViewerFocusApply += PCropFocusApply;
    }

    public LViewer LViewer { get; }

    private LCrop LCrop => LViewer.LCrop;

    private LCropDrag LCropDrag => LViewer.LCropDrag;

    public void PCropClose()
    {
        pCropBox.MouseLeftButtonDown -= PCropBodyHandle;
        MouseLeftButtonDown -= PCropPressHandle;
        MouseMove -= PCropMoveHandle;
        MouseLeftButtonUp -= PCropReleaseHandle;
        SizeChanged -= PCropSizeHandle;
        KeyDown -= PCropKeyHandle;
        LCrop.LCropApply -= PCropApply;
        LCropDrag.LCropCaptureApply -= PCropCaptureApply;
        LViewer.LViewerNeutral.LViewerFocusApply -= PCropFocusApply;
    }

    private Rectangle PCropHandleBuild(int pIndex)
    {
        var pHandle = new Rectangle
        {
            Width = LCrop.LCropHandleSize,
            Height = LCrop.LCropHandleSize,
            Fill = Brushes.White,
            Stroke = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7)),
            StrokeThickness = 1.5,
            Cursor = pCropHandleCursors[pIndex],
            Visibility = Visibility.Collapsed
        };
        pHandle.MouseLeftButtonDown += (_, pEvent) =>
            pEvent.Handled = LCropDrag.LCropGripHandle(pIndex, pEvent.GetPosition(this).X, pEvent.GetPosition(this).Y);
        Children.Add(pHandle);
        return pHandle;
    }

    private void PCropApply()
    {
        LCropBox lBox = LCrop.LCropBoxRead();
        SetLeft(pCropBox, lBox.LCropBoxX);
        SetTop(pCropBox, lBox.LCropBoxY);
        pCropBox.Width = lBox.LCropBoxWidth;
        pCropBox.Height = lBox.LCropBoxHeight;
        pCropBox.Visibility = PLook.PLookVisible[lBox.LCropBoxShown];
        pCropBox.Cursor = PLook.PLookSizeAll[LCrop.LCropEditable];
        Cursor = PLook.PLookCross[LCrop.LCropCrossShown];
        LCrop.LCropHandlesRead().ToList().ForEach(PCropHandlePlace);
        PCropShadeApply(LCrop.LCropShadeRead());
    }

    private void PCropHandlePlace(LCropHandle lHandle)
    {
        Rectangle pHandle = pCropHandles[lHandle.LCropHandleIndex];
        pHandle.Visibility = PLook.PLookVisible[lHandle.LCropHandleShown];
        SetLeft(pHandle, lHandle.LCropHandleX);
        SetTop(pHandle, lHandle.LCropHandleY);
    }

    private void PCropShadeApply(LCropShade lShade)
    {
        var pShadeGeometry = new GeometryGroup { FillRule = FillRule.EvenOdd };
        pShadeGeometry.Children.Add(new RectangleGeometry(PCropRectBuild(lShade.LCropShadeVideo)));
        pShadeGeometry.Children.Add(new RectangleGeometry(PCropRectBuild(lShade.LCropShadeBox)));
        pCropShade.Data = pShadeGeometry;
        pCropShade.Visibility = PLook.PLookVisible[lShade.LCropShadeShown];
    }

    private static Rect PCropRectBuild(LCropbox lCropbox) =>
        new(lCropbox.LCropboxX, lCropbox.LCropboxY, lCropbox.LCropboxWidth, lCropbox.LCropboxHeight);

    private void PCropCaptureApply(bool lCapture) => pCropCaptures[lCapture](this);

    private void PCropFocusApply() => Focus();

    private void PCropBodyHandle(object pSender, MouseButtonEventArgs pEvent) =>
        pEvent.Handled = LCropDrag.LCropBodyHandle(pEvent.GetPosition(this).X, pEvent.GetPosition(this).Y);

    private void PCropPressHandle(object pSender, MouseButtonEventArgs pEvent) =>
        pEvent.Handled = LCropDrag.LCropPressHandle(pEvent.GetPosition(this).X, pEvent.GetPosition(this).Y);

    private void PCropMoveHandle(object pSender, MouseEventArgs pEvent) =>
        pEvent.Handled = LCropDrag.LCropMoveHandle(
            PLook.PLookPressed[pEvent.LeftButton], pEvent.GetPosition(this).X, pEvent.GetPosition(this).Y);

    private void PCropReleaseHandle(object pSender, MouseButtonEventArgs pEvent) =>
        pEvent.Handled = LCropDrag.LCropReleaseHandle(pEvent.GetPosition(this).X, pEvent.GetPosition(this).Y);

    private void PCropSizeHandle(object pSender, SizeChangedEventArgs pEvent) =>
        LCrop.LCropSizeHandle(pEvent.NewSize.Width, pEvent.NewSize.Height);

    private void PCropKeyHandle(object pSender, KeyEventArgs pEvent) =>
        pEvent.Handled = LViewer.LViewerNeutral.LViewerKeyHandle(pEvent.Key.ToString());
}
