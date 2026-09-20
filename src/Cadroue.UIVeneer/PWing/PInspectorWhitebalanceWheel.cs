using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private const int PWhitebalanceWheelSize = 120;
    private const double PWhitebalanceWheelValue = 0.9;

    private Canvas pWhitebalanceWheelCanvas = null!;
    private Image pWhitebalanceWheelImage = null!;
    private Slider pWhitebalanceWheelBrightness = null!;
    private Ellipse pWhitebalanceWheelDot = null!;

    private UIElement PWhitebalanceWheelBuild()
    {
        pWhitebalanceWheelCanvas = new Canvas
        {
            Width = PWhitebalanceWheelSize,
            Height = PWhitebalanceWheelSize,
            Background = Brushes.Transparent,
            Cursor = Cursors.Cross
        };

        pWhitebalanceWheelImage = new Image
        {
            Width = PWhitebalanceWheelSize,
            Height = PWhitebalanceWheelSize,
            Source = PWhitebalanceWheelDraw(),
            IsHitTestVisible = false
        };
        pWhitebalanceWheelCanvas.Children.Add(pWhitebalanceWheelImage);

        pWhitebalanceWheelDot = new Ellipse
        {
            Width = 11,
            Height = 11,
            Stroke = Brushes.White,
            StrokeThickness = 2,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 2,
                ShadowDepth = 0,
                Opacity = 0.8
            }
        };
        pWhitebalanceWheelCanvas.Children.Add(pWhitebalanceWheelDot);

        pWhitebalanceWheelCanvas.MouseLeftButtonDown += PWhitebalancePressHandle;
        pWhitebalanceWheelCanvas.MouseMove += PWhitebalanceWheelHandle;
        pWhitebalanceWheelCanvas.MouseLeftButtonUp += (_, _) => pWhitebalanceWheelCanvas.ReleaseMouseCapture();

        pWhitebalanceWheelBrightness = new Slider
        {
            Minimum = 0,
            Maximum = 1,
            Value = PWhitebalanceWheelValue,
            Width = PWhitebalanceWheelSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        };
        PSlider.PSliderApply(pWhitebalanceWheelBrightness);
        pWhitebalanceWheelBrightness.ValueChanged += (_, _) =>
            pWhitebalanceWheelImage.Source = PWhitebalanceWheelDraw(pWhitebalanceWheelBrightness.Value);

        return new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
            Children = { pWhitebalanceWheelCanvas, pWhitebalanceWheelBrightness }
        };
    }

    private void PWhitebalancePressHandle(object pSender, MouseButtonEventArgs pWheelMouse)
    {
        pWhitebalanceWheelCanvas.CaptureMouse();
        PWhitebalanceWheelHandle(pSender, pWheelMouse);
    }

    private void PWhitebalanceWheelHandle(object pSender, MouseEventArgs pWheelMouse)
    {
        Point pWheelPoint = pWheelMouse.GetPosition(pWhitebalanceWheelCanvas);
        LWhitebalance.LWhitebalanceWheelHandle(
            PLook.PLookPressed[pWheelMouse.LeftButton], pWheelPoint.X, pWheelPoint.Y, PWhitebalanceWheelSize);
    }

    private void PWhitebalanceWheelPlace()
    {
        LNeutralDot pDot = LWhitebalance.LWhitebalanceDotRead(PWhitebalanceWheelSize, pWhitebalanceWheelDot.Width);
        Canvas.SetLeft(pWhitebalanceWheelDot, pDot.LNeutralDotLeft);
        Canvas.SetTop(pWhitebalanceWheelDot, pDot.LNeutralDotTop);
        pWhitebalanceWheelDot.Visibility = PLook.PLookVisible[pDot.LNeutralDotPresent];
    }

    private static ImageSource PWhitebalanceWheelDraw() =>
        PWhitebalanceWheelDraw(PWhitebalanceWheelValue);

    private static ImageSource PWhitebalanceWheelDraw(double pWheelValue)
    {
        LNeutralBitmap pWheel = LNeutral.LNeutralBitmapResolve(PWhitebalanceWheelSize, pWheelValue);
        var pWheelBitmap = new WriteableBitmap(
            pWheel.LNeutralBitmapSize, pWheel.LNeutralBitmapSize, 96, 96, PixelFormats.Bgra32, null);
        pWheelBitmap.WritePixels(
            new Int32Rect(0, 0, pWheel.LNeutralBitmapSize, pWheel.LNeutralBitmapSize),
            pWheel.LNeutralBitmapPixels,
            pWheel.LNeutralBitmapStride,
            0);
        pWheelBitmap.Freeze();
        return pWheelBitmap;
    }
}
