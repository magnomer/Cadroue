using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PInspectorWhitebalanceWheel
{
    internal const int PWhitebalanceWheelSize = 120;
    private const double PWhitebalanceWheelValue = 0.9;

    private readonly Canvas pWhitebalanceWheelCanvas;
    private readonly Image pWhitebalanceWheelImage;
    private readonly Slider pWhitebalanceWheelBrightness;

    internal PInspectorWhitebalanceWheel()
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

        PWhitebalanceWheelDot = new Ellipse
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
        pWhitebalanceWheelCanvas.Children.Add(PWhitebalanceWheelDot);

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

        PWhitebalancePalette = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
            Children = { pWhitebalanceWheelCanvas, pWhitebalanceWheelBrightness }
        };
    }

    internal Ellipse PWhitebalanceWheelDot { get; }

    internal StackPanel PWhitebalancePalette { get; }

    // The wheel driver exists only once the section's driver is made from this wheel's dot; the wheel is then drawn
    // at its slider's brightness.
    internal void PWhitebalanceWheelAttach(QWhitebalanceWheel lWheel)
    {
        pWhitebalanceWheelCanvas.MouseLeftButtonDown += (_, pWheelMouse) =>
            PWhitebalancePressHandle(lWheel, pWheelMouse);
        pWhitebalanceWheelCanvas.MouseMove += (_, pWheelMouse) => PWhitebalanceWheelHandle(lWheel, pWheelMouse);
        pWhitebalanceWheelCanvas.MouseLeftButtonUp += (_, _) => pWhitebalanceWheelCanvas.ReleaseMouseCapture();
        pWhitebalanceWheelBrightness.ValueChanged += (_, _) => PWhitebalanceBrightnessHandle(lWheel);
        PWhitebalanceBrightnessHandle(lWheel);
    }

    private void PWhitebalanceBrightnessHandle(QWhitebalanceWheel lWheel) =>
        pWhitebalanceWheelImage.Source = PWhitebalanceWheelDraw(lWheel, pWhitebalanceWheelBrightness.Value);

    private void PWhitebalancePressHandle(QWhitebalanceWheel lWheel, MouseButtonEventArgs pWheelMouse)
    {
        pWhitebalanceWheelCanvas.CaptureMouse();
        PWhitebalanceWheelHandle(lWheel, pWheelMouse);
    }

    private void PWhitebalanceWheelHandle(QWhitebalanceWheel lWheel, MouseEventArgs pWheelMouse)
    {
        Point pWheelPoint = pWheelMouse.GetPosition(pWhitebalanceWheelCanvas);
        lWheel.QWhitebalanceWheelHandle(
            PLook.PLookPressed[pWheelMouse.LeftButton], pWheelPoint.X, pWheelPoint.Y, PWhitebalanceWheelSize);
    }

    // The driver draws the wheel; the blank tool's wheel shares it.
    internal static ImageSource PWhitebalanceWheelDraw(QWhitebalanceWheel lWheel, double pWheelValue) =>
        lWheel.QWhitebalanceWheelDraw(PWhitebalanceWheelSize, pWheelValue);
}
