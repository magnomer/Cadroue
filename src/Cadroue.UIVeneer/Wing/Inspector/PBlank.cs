using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PBlank
{
    private const int PBlankWheelSize = QWhitebalanceWheel.QWhitebalanceWheelSize;
    private const string PPickerIcon = "/PAsset/PPanel/PProcessingPicker.svg";

    private readonly QWhitebalanceWheel qBlankWheel;
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

    // The blank tool draws its wheel with the white balance wheel's driver.
    internal PBlank(QBlank lBlank, QWhitebalanceWheel lWheel)
    {
        QBlank = lBlank;
        qBlankWheel = lWheel;
        PBlankBodyBuild();
        PBlankWheelUpdate();
        PBlankAttach();
        PBlankUpdate();
    }

    private QBlank QBlank { get; }

    internal StackPanel PBlankBody { get; private set; } = null!;

    private void PBlankBodyBuild()
    {
        pBlankSwitch = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Detector.ApplyTooltip"));
        pBlankStack = new StackPanel();
        PBlankBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        StackPanel pStack = pBlankStack;

        (pBlankBrightnessSlider, pBlankBrightnessValue) = PSensorValueBuild(
            QBlank.QBlankBrightnessBound,
            () => QBlank.QBlankStep.LDetectorBlankBrightness,
            QBlank.QBlankBrightnessSet);
        pBlankBrightnessSlider.Width = PBlankWheelSize;
        pBlankBrightnessSlider.HorizontalAlignment = HorizontalAlignment.Center;
        pBlankBrightnessSlider.Margin = new Thickness(0, 6, 0, 0);

        pBlankColorArea = new StackPanel
        {
            Children = { PBlankPickerBuild(), PBlankWheelBuild() }
        };
        pStack.Children.Add(PBlankTypeBuild());
        pStack.Children.Add(pBlankColorArea);

        (pBlankToleranceSlider, pBlankToleranceValue) = PSensorValueBuild(
            LDetector.LDetectorToleranceRead(),
            () => QBlank.QBlankStep.LDetectorBlankTolerance,
            QBlank.QBlankToleranceSet);
        pStack.Children.Add(PInspectorField.PInspectorSliderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Tolerance"),
            pBlankToleranceSlider,
            string.Empty,
            pBlankToleranceValue));

        (pBlankCoverageSlider, pBlankCoverageValue) = PSensorValueBuild(
            LDetector.LDetectorCoverageRead(),
            () => QBlank.QBlankStep.LDetectorBlankCoverage,
            QBlank.QBlankCoverageSet);
        pStack.Children.Add(PInspectorField.PInspectorSliderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Coverage"),
            pBlankCoverageSlider,
            string.Empty,
            pBlankCoverageValue));

        (pBlankMinimumSlider, pBlankMinimumValue) = PSensorValueBuild(
            QBlank.QBlankMinimumBound,
            () => QBlank.QBlankStep.LDetectorBlankMinimum,
            QBlank.QBlankMinimumSet);
        pStack.Children.Add(PInspectorField.PInspectorSliderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Minimum"),
            pBlankMinimumSlider,
            "s",
            pBlankMinimumValue));

        PBlankBody.Children.Add(pBlankSwitch);
        PBlankBody.Children.Add(PInspectorField.PInspectorSeparatorBuild());
        PBlankBody.Children.Add(pStack);
    }

    private static (Slider, TextBox) PSensorValueBuild(LDetectorBound lBound, Func<double> pRead, Action<double> pSet)
    {
        Slider pSlider = PInspectorValue.PInspectorSliderBuild(
            lBound.LDetectorBoundLeast, lBound.LDetectorBoundMost, pRead(), () => lBound.LDetectorBoundDefault);
        TextBox pValue = PInspectorNumber.PInspectorDecimalBuild();
        PInspectorValue.PInspectorValueAttach(
            pSlider, pValue, lBound.LDetectorBoundLeast, lBound.LDetectorBoundMost, pRead, pSet);
        return (pSlider, pValue);
    }

    private void PBlankAttach()
    {
        PInspectorSwitch.PInspectorSwitchAttach(pBlankSwitch, QBlank.QBlankEnabledSet);
        pBlankPicker.Checked += (_, _) => QBlank.QBlankPickSet(true);
        pBlankPicker.Unchecked += (_, _) => QBlank.QBlankPickSet(false);
        pBlankWheelCanvas.MouseLeftButtonDown += PBlankPressHandle;
        pBlankWheelCanvas.MouseMove += PBlankWheelHandle;
        pBlankWheelCanvas.MouseLeftButtonUp += (_, _) => pBlankWheelCanvas.ReleaseMouseCapture();
        pBlankBrightnessSlider.ValueChanged += (_, _) => PBlankWheelUpdate();
        QBlank.QBlankChanged += PBlankUpdate;
        QBlank.QBlankPickChanged += PBlankPickUpdate;
    }

    private UIElement PBlankTypeBuild()
    {
        pBlankBlack = PSensor.PSensorRadioBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Black"),
            "PBlankType",
            () => QBlank.QBlankTypeSet(LDetectorType.LDetectorTypeBlack));
        pBlankColor = PSensor.PSensorRadioBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Color"),
            "PBlankType",
            () => QBlank.QBlankTypeSet(LDetectorType.LDetectorTypeColor));
        Border pBlankType = QRadio.QRadioSegmentBuild(pBlankBlack, pBlankColor);
        pBlankType.Margin = new Thickness(0, 0, 0, 10);
        return pBlankType;
    }

    private UIElement PBlankPickerBuild()
    {
        pBlankPickerIcon = new Image
        {
            Width = 18,
            Height = 18,
            Source = QIcon.QIconRead(PPickerIcon, QTool.QToolIconRead()),
            Stretch = Stretch.Uniform
        };
        pBlankPicker = new ToggleButton
        {
            Content = pBlankPickerIcon,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.PickerTooltip"),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PInspectorTool.PInspectorToolCreate(typeof(ToggleButton))
        };

        return PInspectorField.PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Picker"),
            pBlankPicker,
            true);
    }

    private UIElement PBlankWheelBuild()
    {
        pBlankWheelCanvas = new Canvas
        {
            Width = PBlankWheelSize,
            Height = PBlankWheelSize,
            Background = Brushes.Transparent,
            Cursor = Cursors.Cross
        };
        pBlankWheelImage = new Image
        {
            Width = PBlankWheelSize,
            Height = PBlankWheelSize,
            IsHitTestVisible = false
        };
        pBlankWheelCanvas.Children.Add(pBlankWheelImage);
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

        return new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10),
            Children = { pBlankWheelCanvas, pBlankBrightnessSlider }
        };
    }

    private void PBlankWheelUpdate() =>
        pBlankWheelImage.Source =
            qBlankWheel.QWhitebalanceWheelDraw(PBlankWheelSize, pBlankBrightnessSlider.Value);

    private void PBlankPressHandle(object pSender, MouseButtonEventArgs pBlankMouse)
    {
        pBlankWheelCanvas.CaptureMouse();
        PBlankWheelHandle(pSender, pBlankMouse);
    }

    private void PBlankWheelHandle(object pSender, MouseEventArgs pBlankMouse)
    {
        Point pBlankPoint = pBlankMouse.GetPosition(pBlankWheelCanvas);
        QBlank.QBlankWheelHandle(
            PLook.PLookPressed[pBlankMouse.LeftButton], pBlankPoint.X, pBlankPoint.Y, PBlankWheelSize);
    }

    private void PBlankWheelPlace()
    {
        LNeutralDot pDot = QBlank.QBlankDotRead(PBlankWheelSize, pBlankWheelDot.Width);
        Canvas.SetLeft(pBlankWheelDot, pDot.LNeutralDotLeft);
        Canvas.SetTop(pBlankWheelDot, pDot.LNeutralDotTop);
        pBlankWheelDot.Visibility = PLook.PLookVisible[pDot.LNeutralDotPresent];
    }

    private void PBlankPickUpdate(bool pPicking)
    {
        PBlankPickerSet(pPicking);
        PBlankIconApply(pPicking);
    }

    private void PBlankPickerSet(bool pPicking) => pBlankPicker.IsChecked = PLook.PLookChecked[pPicking];

    private void PBlankIconApply(bool pPicking) =>
        pBlankPickerIcon.Source = QIcon.QIconRead(PPickerIcon, QTool.QToolPickRead(pPicking));

    private void PBlankUpdate()
    {
        LDetectorBlank pBlank = QBlank.QBlankStep;
        PInspectorSwitch.PInspectorSwitchUpdate(pBlankSwitch, pBlank.LDetectorBlankEnabled);
        pBlankColor.IsChecked = PLook.PLookChecked[QBlank.QBlankWheelPresent];
        pBlankBlack.IsChecked = PLook.PLookChecked[!QBlank.QBlankWheelPresent];
        pBlankColorArea.Visibility = PLook.PLookVisible[QBlank.QBlankWheelPresent];
        PInspectorValue.PInspectorValueUpdate(
            pBlankBrightnessSlider, pBlankBrightnessValue, pBlank.LDetectorBlankBrightness, "0.00");
        PInspectorValue.PInspectorValueUpdate(
            pBlankToleranceSlider, pBlankToleranceValue, pBlank.LDetectorBlankTolerance, "0.00");
        PInspectorValue.PInspectorValueUpdate(
            pBlankCoverageSlider, pBlankCoverageValue, pBlank.LDetectorBlankCoverage, "0.00");
        PInspectorValue.PInspectorValueUpdate(
            pBlankMinimumSlider, pBlankMinimumValue, pBlank.LDetectorBlankMinimum, "0.0");
        PBlankWheelPlace();
        PInspectorState.PInspectorSectionUpdate(pBlankStack, pBlank.LDetectorBlankEnabled);
    }
}
