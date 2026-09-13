using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PAsset;
using Cadroue.UIShell.PHouse;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
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

    private void PBlankBrightnessChange()
    {
        PBlankWheelUpdate();
        PBlankRaise();
    }

    private void PBlankWheelUpdate()
    {
        if (pBlankWheelImage is null)
        {
            return;
        }

        double pBlankValue = Math.Clamp(PInspectorDecimalRead(pBlankBrightnessValue, 1), 0, 1);
        pBlankWheelImage.Source = PWhitebalanceWheelDraw(pBlankValue);
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
        double pBlankX = (pBlankPoint.X - (PWhitebalanceWheelSize / 2.0)) / PWhitebalanceWheelRadius;
        double pBlankY = ((PWhitebalanceWheelSize / 2.0) - pBlankPoint.Y) / PWhitebalanceWheelRadius;
        double pBlankReach = Math.Sqrt((pBlankX * pBlankX) + (pBlankY * pBlankY));
        if (pBlankReach > 1)
        {
            pBlankX /= pBlankReach;
            pBlankY /= pBlankReach;
        }

        pBlankWheelX = pBlankX;
        pBlankWheelY = pBlankY;
        pBlankWheelPresent = true;
        pBlankColor.IsChecked = true;
        if (Math.Clamp(PInspectorDecimalRead(pBlankBrightnessValue, LDetectorBlank.LDetectorBlankValue), 0, 1)
            <= 0.0001)
        {
            pBlankBrightnessValue.Text = LDetectorBlank.LDetectorBlankValue.ToString(
                "0.00",
                CultureInfo.InvariantCulture);
        }

        PBlankWheelPlace();
        PBlankRaise();
    }

    private void PBlankWheelPlace()
    {
        if (pBlankWheelDot is null)
        {
            return;
        }

        if (!pBlankWheelPresent)
        {
            pBlankWheelDot.Visibility = Visibility.Collapsed;
            return;
        }

        double pBlankCenterX = (PWhitebalanceWheelSize / 2.0) + (pBlankWheelX * PWhitebalanceWheelRadius);
        double pBlankCenterY = (PWhitebalanceWheelSize / 2.0) - (pBlankWheelY * PWhitebalanceWheelRadius);
        Canvas.SetLeft(pBlankWheelDot, pBlankCenterX - (pBlankWheelDot.Width / 2));
        Canvas.SetTop(pBlankWheelDot, pBlankCenterY - (pBlankWheelDot.Height / 2));
        pBlankWheelDot.Visibility = Visibility.Visible;
    }

    public void PBlankSampleApply(int pBlankRed, int pBlankGreen, int pBlankBlue)
    {
        if (pBlankPicker is { })
        {
            pBlankPicker.IsChecked = false;
        }

        LNeutralWheel pBlankWheel = LNeutral.LNeutralWheelResolve(pBlankRed, pBlankGreen, pBlankBlue);
        pBlankWheelX = pBlankWheel.LNeutralWheelX;
        pBlankWheelY = pBlankWheel.LNeutralWheelY;
        pBlankWheelPresent = true;
        pBlankColor.IsChecked = true;
        double pBlankBrightness = Math.Max(pBlankRed, Math.Max(pBlankGreen, pBlankBlue)) / 255.0;
        pBlankSuppress = true;
        pBlankBrightnessValue.Text = pBlankBrightness.ToString("0.00", CultureInfo.InvariantCulture);
        pBlankSuppress = false;
        PBlankWheelPlace();
        PBlankRaise();
    }
}
