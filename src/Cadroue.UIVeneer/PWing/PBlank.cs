using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
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
    private CheckBox pBlankSwitch = null!;
    private StackPanel pBlankStack = null!;
    private StackPanel pBlankBody = null!;

    private StackPanel PBlankBuild()
    {
        pBlankSwitch = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Detector.ApplyTooltip"));
        PInspectorSwitchAttach(pBlankSwitch, QBlank.QBlankEnabledSet);
        pBlankStack = new StackPanel();
        pBlankBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        StackPanel pStack = pBlankStack;

        (pBlankBrightnessSlider, pBlankBrightnessValue) = PSensorValueBuild(
            QBlank.QBlankBrightnessBound,
            () => QBlank.QBlankStep.LDetectorBlankBrightness,
            QBlank.QBlankBrightnessSet);
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
            () => QBlank.QBlankStep.LDetectorBlankTolerance,
            QBlank.QBlankToleranceSet);
        pStack.Children.Add(PInspectorSliderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Tolerance"),
            pBlankToleranceSlider,
            string.Empty,
            pBlankToleranceValue));

        (pBlankCoverageSlider, pBlankCoverageValue) = PSensorValueBuild(
            LDetector.LDetectorCoverageRead(),
            () => QBlank.QBlankStep.LDetectorBlankCoverage,
            QBlank.QBlankCoverageSet);
        pStack.Children.Add(PInspectorSliderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Coverage"),
            pBlankCoverageSlider,
            string.Empty,
            pBlankCoverageValue));

        (pBlankMinimumSlider, pBlankMinimumValue) = PSensorValueBuild(
            QBlank.QBlankMinimumBound,
            () => QBlank.QBlankStep.LDetectorBlankMinimum,
            QBlank.QBlankMinimumSet);
        pStack.Children.Add(PInspectorSliderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Minimum"),
            pBlankMinimumSlider,
            "s",
            pBlankMinimumValue));

        pBlankBody.Children.Add(pBlankSwitch);
        pBlankBody.Children.Add(PInspectorSeparatorBuild());
        pBlankBody.Children.Add(pStack);

        QBlank.QBlankChanged += PBlankUpdate;
        QBlank.QBlankPickChanged += PBlankPickUpdate;
        PBlankUpdate();
        return pBlankBody;
    }

    private UIElement PBlankTypeBuild()
    {
        pBlankBlack = PSensorRadioBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Black"),
            "PBlankType",
            () => QBlank.QBlankTypeSet(LDetectorType.LDetectorTypeBlack));
        pBlankColor = PSensorRadioBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Color"),
            "PBlankType",
            () => QBlank.QBlankTypeSet(LDetectorType.LDetectorTypeColor));
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
            Source = QIcon.QIconRead(PPickerIcon, pInspectorIconBrush),
            Stretch = Stretch.Uniform
        };
        pBlankPicker = new ToggleButton
        {
            Content = pBlankPickerIcon,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.PickerTooltip"),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PInspectorToolCreate(typeof(ToggleButton))
        };
        pBlankPicker.Checked += (_, _) => QBlank.QBlankPickSet(true);
        pBlankPicker.Unchecked += (_, _) => QBlank.QBlankPickSet(false);

        return PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Picker"),
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
        QBlank.QBlankWheelHandle(
            PLook.PLookPressed[pBlankMouse.LeftButton], pBlankPoint.X, pBlankPoint.Y, PWhitebalanceWheelSize);
    }

    private void PBlankWheelPlace()
    {
        LNeutralDot pDot = QBlank.QBlankDotRead(PWhitebalanceWheelSize, pBlankWheelDot.Width);
        Canvas.SetLeft(pBlankWheelDot, pDot.LNeutralDotLeft);
        Canvas.SetTop(pBlankWheelDot, pDot.LNeutralDotTop);
        pBlankWheelDot.Visibility = PLook.PLookVisible[pDot.LNeutralDotPresent];
    }

    private void PBlankPickUpdate(bool pPicking)
    {
        pBlankPicker.IsChecked = PLook.PLookChecked[pPicking];
        pBlankPickerIcon.Source = QIcon.QIconRead(PPickerIcon, PInspectorPickBrush[pPicking]);
    }

    private void PBlankUpdate()
    {
        LDetectorBlank pBlank = QBlank.QBlankStep;
        PInspectorSwitchUpdate(pBlankSwitch, pBlank.LDetectorBlankEnabled);
        pBlankColor.IsChecked = PLook.PLookChecked[QBlank.QBlankWheelPresent];
        pBlankBlack.IsChecked = PLook.PLookChecked[!QBlank.QBlankWheelPresent];
        pBlankColorArea.Visibility = PLook.PLookVisible[QBlank.QBlankWheelPresent];
        PInspectorValueUpdate(pBlankBrightnessSlider, pBlankBrightnessValue, pBlank.LDetectorBlankBrightness, "0.00");
        PInspectorValueUpdate(pBlankToleranceSlider, pBlankToleranceValue, pBlank.LDetectorBlankTolerance, "0.00");
        PInspectorValueUpdate(pBlankCoverageSlider, pBlankCoverageValue, pBlank.LDetectorBlankCoverage, "0.00");
        PInspectorValueUpdate(pBlankMinimumSlider, pBlankMinimumValue, pBlank.LDetectorBlankMinimum, "0.0");
        PBlankWheelPlace();
        PInspectorSectionUpdate(pBlankStack, pBlank.LDetectorBlankEnabled);
    }
}
