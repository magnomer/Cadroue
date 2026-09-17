using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private Canvas pBlankWheelCanvas = null!;
    private Image pBlankWheelImage = null!;
    private Ellipse pBlankWheelDot = null!;

    private UIElement PBlankWheelBuild()
    {
        pBlankWheelCanvas = new Canvas
        {
            Width = PWhitebalanceWheelSize,
            Height = PWhitebalanceWheelSize,
            Background = Brushes.Transparent,
            Cursor = Cursors.Cross
        };
        pBlankWheelImage = new Image
        {
            Width = PWhitebalanceWheelSize,
            Height = PWhitebalanceWheelSize,
            IsHitTestVisible = false
        };
        pBlankWheelCanvas.Children.Add(pBlankWheelImage);
        PBlankWheelUpdate();
        pBlankWheelDot = new Ellipse
        {
            Width = 11,
            Height = 11,
            Stroke = Brushes.White,
            StrokeThickness = 2,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        pBlankWheelCanvas.Children.Add(pBlankWheelDot);

        pBlankWheelCanvas.MouseLeftButtonDown += PBlankWheelHandle;
        pBlankWheelCanvas.MouseMove += PBlankWheelHandle;
        pBlankWheelCanvas.MouseLeftButtonUp += (_, _) =>
        {
            if (pBlankWheelCanvas.IsMouseCaptured)
            {
                pBlankWheelCanvas.ReleaseMouseCapture();
            }
        };

        return new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10),
            Children = { pBlankWheelCanvas, pBlankBrightnessSlider }
        };
    }

    private void PBlankWheelUpdate()
    {
        if (pBlankWheelImage is not null)
        {
            pBlankWheelImage.Source = PWhitebalanceWheelDraw(Math.Clamp(pBlankBrightnessSlider.Value, 0, 1));
        }
    }

    private void PBlankWheelHandle(object sender, MouseEventArgs pBlankMouse)
    {
        if (pBlankMouse.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (!pBlankWheelCanvas.IsMouseCaptured)
        {
            pBlankWheelCanvas.CaptureMouse();
        }

        Point pBlankPoint = pBlankMouse.GetPosition(pBlankWheelCanvas);
        LBlank.LBlankWheelSet(
            (pBlankPoint.X - (PWhitebalanceWheelSize / 2.0)) / PWhitebalanceWheelRadius,
            ((PWhitebalanceWheelSize / 2.0) - pBlankPoint.Y) / PWhitebalanceWheelRadius);
    }

    private void PBlankWheelPlace()
    {
        if (pBlankWheelDot is null)
        {
            return;
        }

        if (!LBlank.LBlankWheelPresent)
        {
            pBlankWheelDot.Visibility = Visibility.Collapsed;
            return;
        }

        double pBlankCenterX = (PWhitebalanceWheelSize / 2.0) + (LBlank.LBlankWheelX * PWhitebalanceWheelRadius);
        double pBlankCenterY = (PWhitebalanceWheelSize / 2.0) - (LBlank.LBlankWheelY * PWhitebalanceWheelRadius);
        Canvas.SetLeft(pBlankWheelDot, pBlankCenterX - (pBlankWheelDot.Width / 2));
        Canvas.SetTop(pBlankWheelDot, pBlankCenterY - (pBlankWheelDot.Height / 2));
        pBlankWheelDot.Visibility = Visibility.Visible;
    }
}
