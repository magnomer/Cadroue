using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
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
    private Canvas pBlankWheelCanvas = null!;
    private Image pBlankWheelImage = null!;
    private Ellipse pBlankWheelDot = null!;

    private PSensorSection PBlankBuild()
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
            LBlank.LBlankBrightnessBound,
            () => LBlank.LBlankStep.LDetectorBlankBrightness,
            LBlank.LBlankBrightnessSet);
        pBlankBrightnessSlider.Width = PWhitebalanceWheelSize;
        pBlankBrightnessSlider.HorizontalAlignment = HorizontalAlignment.Center;
        pBlankBrightnessSlider.Margin = new Thickness(0, 6, 0, 0);

        pBlankColorArea = new StackPanel
        {
            Children = { PBlankPickerBuild(), PBlankWheelBuild() }
        };
        pBlankBrightnessSlider.ValueChanged += (_, _) => PBlankWheelUpdate();
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
            LBlank.LBlankMinimumBound,
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

        var pSection = new PSensorSection
        {
            PSensorKind = LDetectorKind.LDetectorKindBlank,
            PSensorApplyBox = pApply,
            PSensorStack = pStack,
            PSensorBody = pBody
        };
        LBlank.LBlankChange += () => PBlankUpdate(pSection);
        LBlank.LBlankPickChange += PBlankPickUpdate;
        PBlankUpdate(pSection);
        return pSection;
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

        pBlankWheelCanvas.MouseLeftButtonDown += PBlankPressHandle;
        pBlankWheelCanvas.MouseMove += PBlankWheelHandle;
        pBlankWheelCanvas.MouseLeftButtonUp += (_, _) => pBlankWheelCanvas.ReleaseMouseCapture();

        return new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10),
            Children = { pBlankWheelCanvas, pBlankBrightnessSlider }
        };
    }

    private void PBlankWheelUpdate() =>
        pBlankWheelImage.Source = PWhitebalanceWheelDraw(pBlankBrightnessSlider.Value);

    private void PBlankPressHandle(object pSender, MouseButtonEventArgs pBlankMouse)
    {
        pBlankWheelCanvas.CaptureMouse();
        PBlankWheelHandle(pSender, pBlankMouse);
    }

    private void PBlankWheelHandle(object pSender, MouseEventArgs pBlankMouse)
    {
        Point pBlankPoint = pBlankMouse.GetPosition(pBlankWheelCanvas);
        LBlank.LBlankWheelHandle(
            PLook.PLookPressed[pBlankMouse.LeftButton], pBlankPoint.X, pBlankPoint.Y, PWhitebalanceWheelSize);
    }

    private void PBlankWheelPlace()
    {
        LNeutralDot pDot = LBlank.LBlankDotRead(PWhitebalanceWheelSize, pBlankWheelDot.Width);
        Canvas.SetLeft(pBlankWheelDot, pDot.LNeutralDotLeft);
        Canvas.SetTop(pBlankWheelDot, pDot.LNeutralDotTop);
        pBlankWheelDot.Visibility = PLook.PLookVisible[pDot.LNeutralDotPresent];
    }

    private void PBlankPickUpdate(bool pPicking)
    {
        pBlankPicker.IsChecked = PLook.PLookChecked[pPicking];
        pBlankPickerIcon.Source = PIcon.PIconRead(PPickerIcon, PInspectorPickBrush[pPicking]);
    }

    private void PBlankUpdate(PSensorSection pSection)
    {
        LDetectorBlank pBlank = LBlank.LBlankStep;
        PInspectorSwitchUpdate(pSection.PSensorApplyBox, pBlank.LDetectorBlankEnabled);
        pBlankColor.IsChecked = PLook.PLookChecked[LBlank.LBlankWheelPresent];
        pBlankBlack.IsChecked = PLook.PLookChecked[!LBlank.LBlankWheelPresent];
        pBlankColorArea.Visibility = PLook.PLookVisible[LBlank.LBlankWheelPresent];
        PInspectorValueUpdate(pBlankBrightnessSlider, pBlankBrightnessValue, pBlank.LDetectorBlankBrightness, "0.00");
        PInspectorValueUpdate(pBlankToleranceSlider, pBlankToleranceValue, pBlank.LDetectorBlankTolerance, "0.00");
        PInspectorValueUpdate(pBlankCoverageSlider, pBlankCoverageValue, pBlank.LDetectorBlankCoverage, "0.00");
        PInspectorValueUpdate(pBlankMinimumSlider, pBlankMinimumValue, pBlank.LDetectorBlankMinimum, "0.0");
        PBlankWheelPlace();
        PInspectorSectionUpdate(pSection.PSensorStack, pBlank.LDetectorBlankEnabled);
        PSensorChange?.Invoke();
    }
}
