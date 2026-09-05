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
    private RadioButton pBlankBlack = null!;
    private RadioButton pBlankColor = null!;
    private StackPanel pBlankColorArea = null!;
    private ToggleButton pBlankPicker = null!;
    private Canvas pBlankWheelCanvas = null!;
    private Image pBlankWheelImage = null!;
    private Ellipse pBlankWheelDot = null!;
    private Slider pBlankBrightnessSlider = null!;
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

        LDetectorBound pBlankBrightnessBound = LDetector.LDetectorBrightnessRead();
        pBlankBrightnessValue = PSensorDecimalBuild(1, "0.00");
        pBlankBrightnessSlider = PInspectorSliderBuild(
            pBlankBrightnessValue,
            pBlankBrightnessBound.LDetectorBoundLeast,
            pBlankBrightnessBound.LDetectorBoundMost,
            1, "0.00", null, PBlankBrightnessChange);
        pBlankBrightnessSlider.Width = PWhitebalanceWheelSize;
        pBlankBrightnessSlider.HorizontalAlignment = HorizontalAlignment.Center;
        pBlankBrightnessSlider.Margin = new Thickness(0, 6, 0, 0);

        pBlankColorArea = new StackPanel
        {
            Children = { PBlankPickerBuild(), PBlankWheelBuild() }
        };
        pStack.Children.Add(PBlankTypeBuild());
        pStack.Children.Add(pBlankColorArea);
        PBlankTypeApply();

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
            LLocalization.LLocalizationTextRead("Inspector.Blank.Minimum"), pMinimumSlider, "s", pBlankMinimumValue));

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
            VerticalContentAlignment = VerticalAlignment.Center
        };
        pBlankColor = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Blank.Color"),
            GroupName = "PBlankType",
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        pBlankBlack.Checked += (_, _) =>
        {
            PBlankTypeApply();
            PBlankRaise();
        };
        pBlankColor.Checked += (_, _) =>
        {
            PBlankTypeApply();
            PBlankRaise();
        };

        Border pBlankType = PRadio.PRadioSegmentBuild(pBlankBlack, pBlankColor);
        pBlankType.Margin = new Thickness(0, 0, 0, 10);
        return pBlankType;
    }

    private UIElement PBlankPickerBuild()
    {
        var pBlankPickerIcon = new Image
        {
            Width = 18,
            Height = 18,
            Source = PIcon.PIconRead(PPickerIcon, pInspectorIconBrush),
            Stretch = Stretch.Uniform
        };
        pBlankPicker = new ToggleButton
        {
            Content = pBlankPickerIcon,
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Blank.PickerTooltip"),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PInspectorToolCreate(typeof(ToggleButton))
        };
        pBlankPicker.Checked += (_, _) =>
        {
            pBlankPickerIcon.Source = PIcon.PIconRead(PPickerIcon, pInspectorAccentBrush);
            PBlankPickChange?.Invoke(true);
        };
        pBlankPicker.Unchecked += (_, _) =>
        {
            pBlankPickerIcon.Source = PIcon.PIconRead(PPickerIcon, pInspectorIconBrush);
            PBlankPickChange?.Invoke(false);
        };

        return PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Blank.Picker"),
            pBlankPicker,
            true);
    }

    private void PBlankTypeApply()
    {
        if (pBlankColorArea is null)
        {
            return;
        }

        pBlankColorArea.Visibility = pBlankColor.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
