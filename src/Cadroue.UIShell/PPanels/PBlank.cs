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
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private RadioButton pBlankBlack = null!;
    private RadioButton pBlankColor = null!;
    private ToggleButton pBlankPicker = null!;
    private Canvas pBlankWheelCanvas = null!;
    private Ellipse pBlankWheelDot = null!;
    private double pBlankWheelX;
    private double pBlankWheelY;
    private bool pBlankWheelPresent;
    private TextBox pBlankBrightnessValue = null!;
    private TextBox pBlankToleranceValue = null!;
    private TextBox pBlankCoverageValue = null!;
    private TextBox pBlankMinimumValue = null!;
    private bool pBlankSuppress;

    public event Action<bool>? PBlankPickChange;

    private StackPanel PBlankBuild()
    {
        PSensorSection pSection = null!;

        CheckBox pApply = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Detector.ApplyTooltip"));
        var pStack = new StackPanel();
        var pBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };

        pStack.Children.Add(PBlankTypeBuild());
        pStack.Children.Add(PBlankPickerBuild());
        pStack.Children.Add(PBlankWheelBuild());

        LDetectorBound pBlankBrightnessBound = LDetector.LDetectorBrightnessRead();
        pBlankBrightnessValue = PSensorDecimalBuild(0, "0.00");
        Slider pBrightnessSlider = PInspectorSliderBuild(
            pBlankBrightnessValue,
            pBlankBrightnessBound.LDetectorBoundLeast,
            pBlankBrightnessBound.LDetectorBoundMost,
            0, "0.00", null, PBlankRaise);
        pStack.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Blank.Brightness"), pBrightnessSlider, string.Empty, pBlankBrightnessValue));

        LDetectorBound pBlankToleranceBound = LDetector.LDetectorToleranceRead();
        pBlankToleranceValue = PSensorDecimalBuild(pBlankToleranceBound.LDetectorBoundDefault, "0.00");
        Slider pToleranceSlider = PInspectorSliderBuild(
            pBlankToleranceValue,
            pBlankToleranceBound.LDetectorBoundLeast,
            pBlankToleranceBound.LDetectorBoundMost,
            pBlankToleranceBound.LDetectorBoundDefault, "0.00", null, PBlankRaise);
        pStack.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Blank.Tolerance"), pToleranceSlider, string.Empty, pBlankToleranceValue));

        LDetectorBound pBlankCoverageBound = LDetector.LDetectorCoverageRead();
        pBlankCoverageValue = PSensorDecimalBuild(pBlankCoverageBound.LDetectorBoundDefault, "0.00");
        Slider pCoverageSlider = PInspectorSliderBuild(
            pBlankCoverageValue,
            pBlankCoverageBound.LDetectorBoundLeast,
            pBlankCoverageBound.LDetectorBoundMost,
            pBlankCoverageBound.LDetectorBoundDefault, "0.00", null, PBlankRaise);
        pStack.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Blank.Coverage"), pCoverageSlider, string.Empty, pBlankCoverageValue));

        LDetectorBound pBlankMinimumBound = LDetector.LDetectorMinimumRead(LDetectorKind.LDetectorKindBlank);
        pBlankMinimumValue = PSensorDecimalBuild(LDetectorBlank.LDetectorBlankGap, "0.0");
        Slider pMinimumSlider = PInspectorSliderBuild(
            pBlankMinimumValue,
            pBlankMinimumBound.LDetectorBoundLeast,
            pBlankMinimumBound.LDetectorBoundMost,
            LDetectorBlank.LDetectorBlankGap, "0.0", null, PBlankRaise);
        pStack.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Detector.Minimum"), pMinimumSlider, "s", pBlankMinimumValue));

        pSection = new PSensorSection
        {
            PSensorKind = LDetectorKind.LDetectorKindBlank,
            PSensorApplyBox = pApply,
            PSensorStack = pStack,
            PSensorBody = pBody
        };

        pApply.Checked += (_, _) => PSensorApplyHandle(pSection);
        pApply.Unchecked += (_, _) => PSensorApplyHandle(pSection);

        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);
        PSensorStackUpdate(pSection);

        pSensorSections[LDetectorKind.LDetectorKindBlank] = pSection;
        return pBody;
    }

    private UIElement PBlankTypeBuild()
    {
        pBlankBlack = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Blank.Black"),
            GroupName = "PBlankType",
            IsChecked = true,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            Margin = new Thickness(0, 0, 16, 0),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        pBlankColor = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Blank.Color"),
            GroupName = "PBlankType",
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        pBlankBlack.Checked += (_, _) => PBlankRaise();
        pBlankColor.Checked += (_, _) => PBlankRaise();

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10),
            Children = { pBlankBlack, pBlankColor }
        };
    }

    private UIElement PBlankPickerBuild()
    {
        pBlankPicker = new ToggleButton
        {
            Content = new Image
            {
                Width = 16,
                Height = 16,
                Source = PIcon.PIconRead(PPickerIcon, pInspectorIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Blank.PickerTooltip"),
            Width = 32,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10)
        };
        pBlankPicker.Checked += (_, _) => PBlankPickChange?.Invoke(true);
        pBlankPicker.Unchecked += (_, _) => PBlankPickChange?.Invoke(false);
        return pBlankPicker;
    }

    private UIElement PBlankWheelBuild()
    {
        pBlankWheelCanvas = new Canvas
        {
            Width = PWhitebalanceWheelSize,
            Height = PWhitebalanceWheelSize,
            Background = Brushes.Transparent,
            Cursor = Cursors.Cross
        };
        pBlankWheelCanvas.Children.Add(new Image
        {
            Width = PWhitebalanceWheelSize,
            Height = PWhitebalanceWheelSize,
            Source = PWhitebalanceWheelDraw(),
            IsHitTestVisible = false
        });
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
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10),
            Children = { pBlankWheelCanvas }
        };
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

    private void PBlankRaise()
    {
        if (!pBlankSuppress)
        {
            PSensorRaise();
        }
    }

    public LDetectorBlank PBlankRead()
    {
        if (!pSensorSections.TryGetValue(LDetectorKind.LDetectorKindBlank, out PSensorSection? pSection))
        {
            return LDetectorBlank.LDetectorBlankCreate();
        }

        double pBlankSaturation = Math.Clamp(
            Math.Sqrt((pBlankWheelX * pBlankWheelX) + (pBlankWheelY * pBlankWheelY)), 0, 1);
        double pBlankHue = pBlankWheelPresent
            ? Math.Atan2(pBlankWheelY, pBlankWheelX) * (180.0 / Math.PI)
            : 0;
        if (pBlankHue < 0)
        {
            pBlankHue += 360;
        }

        return LDetectorBlank.LDetectorBlankClamp(new LDetectorBlank(
            pSection.PSensorApplyBox.IsChecked == true,
            pBlankColor.IsChecked == true ? LDetectorType.LDetectorTypeColor : LDetectorType.LDetectorTypeBlack,
            pBlankHue,
            pBlankSaturation,
            PInspectorDecimalRead(pBlankBrightnessValue, 0),
            PInspectorDecimalRead(pBlankToleranceValue, LDetector.LDetectorToleranceRead().LDetectorBoundDefault),
            PInspectorDecimalRead(pBlankCoverageValue, LDetector.LDetectorCoverageRead().LDetectorBoundDefault),
            PInspectorDecimalRead(pBlankMinimumValue, LDetectorBlank.LDetectorBlankGap)));
    }

    public void PBlankApply(LDetectorBlank pBlankStep)
    {
        if (!pSensorSections.TryGetValue(LDetectorKind.LDetectorKindBlank, out PSensorSection? pSection))
        {
            return;
        }

        LDetectorBlank pBlank = LDetectorBlank.LDetectorBlankClamp(pBlankStep);
        pBlankSuppress = true;
        pSection.PSensorApplyBox.IsChecked = pBlank.LDetectorBlankEnabled;
        pBlankColor.IsChecked = pBlank.LDetectorBlankType == LDetectorType.LDetectorTypeColor;
        pBlankBlack.IsChecked = pBlank.LDetectorBlankType == LDetectorType.LDetectorTypeBlack;
        pBlankWheelX = pBlank.LDetectorBlankSaturation * Math.Cos(pBlank.LDetectorBlankHue * (Math.PI / 180.0));
        pBlankWheelY = pBlank.LDetectorBlankSaturation * Math.Sin(pBlank.LDetectorBlankHue * (Math.PI / 180.0));
        pBlankWheelPresent = pBlank.LDetectorBlankType == LDetectorType.LDetectorTypeColor;
        pBlankBrightnessValue.Text = pBlank.LDetectorBlankBrightness.ToString("0.00", CultureInfo.InvariantCulture);
        pBlankToleranceValue.Text = pBlank.LDetectorBlankTolerance.ToString("0.00", CultureInfo.InvariantCulture);
        pBlankCoverageValue.Text = pBlank.LDetectorBlankCoverage.ToString("0.00", CultureInfo.InvariantCulture);
        pBlankMinimumValue.Text = pBlank.LDetectorBlankMinimum.ToString("0.0", CultureInfo.InvariantCulture);
        pBlankSuppress = false;
        PBlankWheelPlace();
        PSensorStackUpdate(pSection);
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
