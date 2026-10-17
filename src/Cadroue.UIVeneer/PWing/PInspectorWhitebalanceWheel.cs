using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.UIDeportment;
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
        UIElement pWheel = PWhitebalancePaletteBuild();
        PWhitebalanceWheelAttach();
        return pWheel;
    }

    private UIElement PWhitebalancePaletteBuild()
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
        QSlider.QSliderAttach(pWhitebalanceWheelBrightness);

        return new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
            Children = { pWhitebalanceWheelCanvas, pWhitebalanceWheelBrightness }
        };
    }

    private void PWhitebalanceWheelAttach()
    {
        pWhitebalanceWheelCanvas.MouseLeftButtonDown += PWhitebalancePressHandle;
        pWhitebalanceWheelCanvas.MouseMove += PWhitebalanceWheelHandle;
        pWhitebalanceWheelCanvas.MouseLeftButtonUp += (_, _) => pWhitebalanceWheelCanvas.ReleaseMouseCapture();
        pWhitebalanceWheelBrightness.ValueChanged += (_, _) => PWhitebalanceBrightnessHandle();
    }

    private void PWhitebalanceBrightnessHandle() =>
        pWhitebalanceWheelImage.Source = PWhitebalanceWheelDraw(pWhitebalanceWheelBrightness.Value);

    private void PWhitebalancePressHandle(object pSender, MouseButtonEventArgs pWheelMouse)
    {
        pWhitebalanceWheelCanvas.CaptureMouse();
        PWhitebalanceWheelHandle(pSender, pWheelMouse);
    }

    private void PWhitebalanceWheelHandle(object pSender, MouseEventArgs pWheelMouse)
    {
        Point pWheelPoint = pWheelMouse.GetPosition(pWhitebalanceWheelCanvas);
        QWhitebalance.QWhitebalanceWheelHandle(
            PLook.PLookPressed[pWheelMouse.LeftButton], pWheelPoint.X, pWheelPoint.Y, PWhitebalanceWheelSize);
    }

    // The driver draws the wheel; the blank tool's wheel shares it.
    private ImageSource PWhitebalanceWheelDraw(double pWheelValue) =>
        QWhitebalance.QWhitebalanceWheelDraw(PWhitebalanceWheelSize, pWheelValue);
}
