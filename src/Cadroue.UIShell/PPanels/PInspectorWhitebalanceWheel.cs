using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const int PWhitebalanceWheelSize = 120;
    private const double PWhitebalanceWheelRadius = 54;
    private const double PWhitebalanceWheelValue = 0.9;

    private Canvas pWhitebalanceWheelCanvas = null!;
    private Ellipse pWhitebalanceWheelDot = null!;
    private double pWhitebalanceWheelX;
    private double pWhitebalanceWheelY;
    private bool pWhitebalanceWheelPresent;

    public event Action<LWhitebalanceMethod>? PWhitebalanceEstimateChange;

    private UIElement PWhitebalanceWheelBuild()
    {
        pWhitebalanceWheelCanvas = new Canvas
        {
            Width = PWhitebalanceWheelSize,
            Height = PWhitebalanceWheelSize,
            Background = Brushes.Transparent,
            Cursor = Cursors.Cross
        };

        var pWhitebalanceWheelFace = new Image
        {
            Width = PWhitebalanceWheelSize,
            Height = PWhitebalanceWheelSize,
            Source = PWhitebalanceWheelDraw(),
            IsHitTestVisible = false
        };
        pWhitebalanceWheelCanvas.Children.Add(pWhitebalanceWheelFace);

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

        pWhitebalanceWheelCanvas.MouseLeftButtonDown += PWhitebalanceWheelHandle;
        pWhitebalanceWheelCanvas.MouseMove += PWhitebalanceWheelHandle;
        pWhitebalanceWheelCanvas.MouseLeftButtonUp += (_, _) =>
        {
            if (pWhitebalanceWheelCanvas.IsMouseCaptured)
            {
                pWhitebalanceWheelCanvas.ReleaseMouseCapture();
            }
        };

        PWhitebalanceWheelPlace();
        return new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 8),
            Children = { pWhitebalanceWheelCanvas }
        };
    }

    private void PWhitebalanceWheelHandle(object sender, MouseEventArgs pWheelMouse)
    {
        if (pWheelMouse.LeftButton != MouseButtonState.Pressed || !pWhitebalanceCapable)
        {
            return;
        }

        if (!pWhitebalanceWheelCanvas.IsMouseCaptured)
        {
            pWhitebalanceWheelCanvas.CaptureMouse();
        }

        Point pWheelPoint = pWheelMouse.GetPosition(pWhitebalanceWheelCanvas);
        double pWheelX = (pWheelPoint.X - (PWhitebalanceWheelSize / 2.0)) / PWhitebalanceWheelRadius;
        double pWheelY = ((PWhitebalanceWheelSize / 2.0) - pWheelPoint.Y) / PWhitebalanceWheelRadius;
        double pWheelReach = Math.Sqrt((pWheelX * pWheelX) + (pWheelY * pWheelY));
        if (pWheelReach > 1)
        {
            pWheelX /= pWheelReach;
            pWheelY /= pWheelReach;
        }

        PToneNeutralApply(LNeutral.LNeutralColorResolve(pWheelX, pWheelY));
    }

    // Manual mode owns the dot from its sample; automatic modes leave it to the
    // frame-analysis estimate. Either way, redraw at the stored coordinates.
    private void PWhitebalanceWheelUpdate()
    {
        if (pWhitebalanceManual)
        {
            LNeutralWheel pWheel = LNeutral.LNeutralWheelResolve(
                pWhitebalanceSampleRed,
                pWhitebalanceSampleGreen,
                pWhitebalanceSampleBlue);
            pWhitebalanceWheelX = pWheel.LNeutralWheelX;
            pWhitebalanceWheelY = pWheel.LNeutralWheelY;
            pWhitebalanceWheelPresent = pWheel.LNeutralWheelPresent;
        }

        PWhitebalanceWheelPlace();
    }

    private void PWhitebalanceWheelPlace()
    {
        if (pWhitebalanceWheelDot is null)
        {
            return;
        }

        if (!pWhitebalanceWheelPresent)
        {
            pWhitebalanceWheelDot.Visibility = Visibility.Collapsed;
            return;
        }

        double pWheelCenterX = (PWhitebalanceWheelSize / 2.0) + (pWhitebalanceWheelX * PWhitebalanceWheelRadius);
        double pWheelCenterY = (PWhitebalanceWheelSize / 2.0) - (pWhitebalanceWheelY * PWhitebalanceWheelRadius);
        Canvas.SetLeft(pWhitebalanceWheelDot, pWheelCenterX - (pWhitebalanceWheelDot.Width / 2));
        Canvas.SetTop(pWhitebalanceWheelDot, pWheelCenterY - (pWhitebalanceWheelDot.Height / 2));
        pWhitebalanceWheelDot.Visibility = Visibility.Visible;
    }

    private void PWhitebalanceEstimateRaise()
    {
        LWhitebalanceMethod pWheelMethod = PWhitebalanceMethodRead();
        if (pWheelMethod != LWhitebalanceMethod.LWhitebalanceMethodManual)
        {
            PWhitebalanceEstimateChange?.Invoke(pWheelMethod);
        }
    }

    public void PWhitebalanceEstimateApply(LNeutralWheel pWheelEstimate)
    {
        if (pWhitebalanceManual)
        {
            return;
        }

        pWhitebalanceWheelX = pWheelEstimate.LNeutralWheelX;
        pWhitebalanceWheelY = pWheelEstimate.LNeutralWheelY;
        pWhitebalanceWheelPresent = pWheelEstimate.LNeutralWheelPresent;
        PWhitebalanceWheelPlace();
    }

    private static ImageSource PWhitebalanceWheelDraw()
    {
        int pWheelSize = PWhitebalanceWheelSize;
        double pWheelCenter = pWheelSize / 2.0;
        var pWheelPixels = new byte[pWheelSize * pWheelSize * 4];
        for (int pWheelRow = 0; pWheelRow < pWheelSize; pWheelRow++)
        {
            for (int pWheelColumn = 0; pWheelColumn < pWheelSize; pWheelColumn++)
            {
                double pWheelX = (pWheelColumn + 0.5 - pWheelCenter) / PWhitebalanceWheelRadius;
                double pWheelY = (pWheelCenter - (pWheelRow + 0.5)) / PWhitebalanceWheelRadius;
                double pWheelReach = Math.Sqrt((pWheelX * pWheelX) + (pWheelY * pWheelY));
                if (pWheelReach > 1)
                {
                    continue;
                }

                double pWheelHue = Math.Atan2(pWheelY, pWheelX) * (180.0 / Math.PI);
                if (pWheelHue < 0)
                {
                    pWheelHue += 360;
                }

                (int pWheelRed, int pWheelGreen, int pWheelBlue) =
                    LNeutral.LNeutralRgbResolve(pWheelHue, pWheelReach, PWhitebalanceWheelValue);
                double pWheelEdge = Math.Clamp((1 - pWheelReach) * PWhitebalanceWheelRadius, 0, 1);
                int pWheelOffset = ((pWheelRow * pWheelSize) + pWheelColumn) * 4;
                pWheelPixels[pWheelOffset] = (byte)pWheelBlue;
                pWheelPixels[pWheelOffset + 1] = (byte)pWheelGreen;
                pWheelPixels[pWheelOffset + 2] = (byte)pWheelRed;
                pWheelPixels[pWheelOffset + 3] = (byte)Math.Round(pWheelEdge * 255);
            }
        }

        var pWheelBitmap = new WriteableBitmap(pWheelSize, pWheelSize, 96, 96, PixelFormats.Bgra32, null);
        pWheelBitmap.WritePixels(
            new Int32Rect(0, 0, pWheelSize, pWheelSize),
            pWheelPixels,
            pWheelSize * 4,
            0);
        pWheelBitmap.Freeze();
        return pWheelBitmap;
    }
}
