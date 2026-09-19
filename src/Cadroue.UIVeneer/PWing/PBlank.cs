using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private RadioButton pBlankBlack = null!;
    private RadioButton pBlankColor = null!;
    private StackPanel pBlankColorArea = null!;
    private ToggleButton pBlankPicker = null!;
    private Image pBlankPickerIcon = null!;
    private Slider pBlankBrightnessSlider = null!;
    private TextBox pBlankBrightnessValue = null!;
    private Slider pBlankToleranceSlider = null!;
    private TextBox pBlankToleranceValue = null!;
    private Slider pBlankCoverageSlider = null!;
    private TextBox pBlankCoverageValue = null!;
    private Slider pBlankMinimumSlider = null!;
    private TextBox pBlankMinimumValue = null!;

    public event Action<bool>? PBlankPickChange;

    private StackPanel PBlankBuild()
    {
        CheckBox pApply = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Detector.ApplyTooltip"));
        PInspectorSwitchAttach(pApply, LBlank.LBlankEnabledSet);
        var pStack = new StackPanel();
        var pBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };

        (pBlankBrightnessSlider, pBlankBrightnessValue) = PSensorValueBuild(
            LDetector.LDetectorBrightnessRead() with { LDetectorBoundDefault = LDetectorBlank.LDetectorBlankValue },
            () => LBlank.LBlankStep.LDetectorBlankBrightness,
            LBlank.LBlankBrightnessSet);
        pBlankBrightnessSlider.Width = PWhitebalanceWheelSize;
        pBlankBrightnessSlider.HorizontalAlignment = HorizontalAlignment.Center;
        pBlankBrightnessSlider.Margin = new Thickness(0, 6, 0, 0);
        pBlankBrightnessSlider.ValueChanged += (_, _) => PBlankWheelUpdate();

        pBlankColorArea = new StackPanel
        {
            Children = { PBlankPickerBuild(), PBlankWheelBuild() }
        };
        pStack.Children.Add(PBlankTypeBuild());
        pStack.Children.Add(pBlankColorArea);

        (pBlankToleranceSlider, pBlankToleranceValue) = PSensorValueBuild(
            LDetector.LDetectorToleranceRead(),
            () => LBlank.LBlankStep.LDetectorBlankTolerance,
            LBlank.LBlankToleranceSet);
        pStack.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Blank.Tolerance"),
            pBlankToleranceSlider,
            string.Empty,
            pBlankToleranceValue));

        (pBlankCoverageSlider, pBlankCoverageValue) = PSensorValueBuild(
            LDetector.LDetectorCoverageRead(),
            () => LBlank.LBlankStep.LDetectorBlankCoverage,
            LBlank.LBlankCoverageSet);
        pStack.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Blank.Coverage"),
            pBlankCoverageSlider,
            string.Empty,
            pBlankCoverageValue));

        (pBlankMinimumSlider, pBlankMinimumValue) = PSensorValueBuild(
            LDetector.LDetectorMinimumRead(LDetectorKind.LDetectorKindBlank)
                with { LDetectorBoundDefault = LDetectorBlank.LDetectorBlankGap },
            () => LBlank.LBlankStep.LDetectorBlankMinimum,
            LBlank.LBlankMinimumSet);
        pStack.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Blank.Minimum"),
            pBlankMinimumSlider,
            "s",
            pBlankMinimumValue));

        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);

        pSensorSections[LDetectorKind.LDetectorKindBlank] = new PSensorSection
        {
            PSensorKind = LDetectorKind.LDetectorKindBlank,
            PSensorApplyBox = pApply,
            PSensorStack = pStack,
            PSensorBody = pBody
        };
        LBlank.LBlankChange += PBlankUpdate;
        LBlank.LBlankPickChange += pPicking =>
        {
            if ((pBlankPicker.IsChecked == true) != pPicking)
            {
                pBlankPicker.IsChecked = pPicking;
            }

            pBlankPickerIcon.Source = PIcon.PIconRead(
                PPickerIcon, pPicking ? pInspectorAccentBrush : pInspectorIconBrush);
            PBlankPickChange?.Invoke(pPicking);
        };
        PBlankUpdate();
        return pBody;
    }

    private UIElement PBlankTypeBuild()
    {
        pBlankBlack = PSensorRadioBuild(
            "Inspector.Blank.Black", "PBlankType", () => LBlank.LBlankTypeSet(LDetectorType.LDetectorTypeBlack));
        pBlankColor = PSensorRadioBuild(
            "Inspector.Blank.Color", "PBlankType", () => LBlank.LBlankTypeSet(LDetectorType.LDetectorTypeColor));
        Border pBlankType = PRadio.PRadioSegmentBuild(pBlankBlack, pBlankColor);
        pBlankType.Margin = new Thickness(0, 0, 0, 10);
        return pBlankType;
    }

    private UIElement PBlankPickerBuild()
    {
        pBlankPickerIcon = new Image
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
        pBlankPicker.Checked += (_, _) => LBlank.LBlankPickSet(true);
        pBlankPicker.Unchecked += (_, _) => LBlank.LBlankPickSet(false);

        return PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Blank.Picker"),
            pBlankPicker,
            true);
    }
}
