using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QBlank
{
    private const int QBlankWheelSize = QWhitebalanceWheel.QWhitebalanceWheelSize;
    private const string QBlankPickerIcon = "/PAsset/PPanel/PProcessingPicker.svg";

    private readonly CBlank qBlankGate;
    private QWhitebalanceWheel qBlankWheel = null!;
    private RadioButton qBlankBlack = null!;
    private RadioButton qBlankColor = null!;
    private StackPanel qBlankColorArea = null!;
    private ToggleButton qBlankPicker = null!;
    private Image qBlankPickerIcon = null!;
    private Slider qBlankBrightnessSlider = null!;
    private TextBox qBlankBrightnessValue = null!;
    private Slider qBlankToleranceSlider = null!;
    private TextBox qBlankToleranceValue = null!;
    private Slider qBlankCoverageSlider = null!;
    private TextBox qBlankCoverageValue = null!;
    private Slider qBlankMinimumSlider = null!;
    private TextBox qBlankMinimumValue = null!;
    private Canvas qBlankWheelCanvas = null!;
    private Image qBlankWheelImage = null!;
    private Ellipse qBlankWheelDot = null!;
    private CheckBox qBlankSwitch = null!;
    private StackPanel qBlankStack = null!;
    private QBlankLifecycle qBlankLifecycle = null!;
    private bool qBlankPicking;
    public QBlank()
        : this(new CBlank())
    {
    }

    public QBlank(CBlank qBlankGate)
    {
        this.qBlankGate = qBlankGate;
        qBlankGate.CBlankChanged += () => QBlankChanged?.Invoke();
    }

    public event Action? QBlankChanged;
    public event Action<bool>? QBlankPickChanged;
    public StackPanel QBlankBody { get; private set; } = null!;

    public LDetectorBlank QBlankStep => qBlankGate.CBlankStep;

    public LDetectorKind QBlankKind => LDetectorKind.LDetectorKindBlank;

    public bool QBlankWheelPresent => qBlankGate.CBlankWheelPresent;

    public bool QBlankPicking => qBlankPicking;

    public static LDetectorBound QBlankBrightnessBound => CBlank.CBlankBrightnessBound;

    public static LDetectorBound QBlankMinimumBound => CBlank.CBlankMinimumBound;

    public void QBlankBuild(QWhitebalanceWheel lWheel)
    {
        qBlankWheel = lWheel;
        QBlankBody = QContract.QContractSheetFind<StackPanel>("PBlankBody");
        qBlankSwitch = QContract.QContractFind<CheckBox>(QBlankBody, "PBlankSwitch");
        qBlankStack = QContract.QContractFind<StackPanel>(QBlankBody, "PBlankStack");
        qBlankBlack = QContract.QContractFind<RadioButton>(QBlankBody, "PBlankBlack");
        qBlankColor = QContract.QContractFind<RadioButton>(QBlankBody, "PBlankColor");
        qBlankColorArea = QContract.QContractFind<StackPanel>(QBlankBody, "PBlankColorArea");
        qBlankPicker = QContract.QContractFind<ToggleButton>(QBlankBody, "PBlankPicker");
        qBlankPickerIcon = QContract.QContractFind<Image>(QBlankBody, "PBlankPickerIcon");
        qBlankBrightnessSlider = QContract.QContractFind<Slider>(QBlankBody, "PBlankBrightnessSlider");
        qBlankBrightnessValue = QContract.QContractFind<TextBox>(QBlankBody, "PBlankBrightnessValue");
        qBlankToleranceSlider = QContract.QContractFind<Slider>(QBlankBody, "PBlankToleranceSlider");
        qBlankToleranceValue = QContract.QContractFind<TextBox>(QBlankBody, "PBlankToleranceValue");
        qBlankCoverageSlider = QContract.QContractFind<Slider>(QBlankBody, "PBlankCoverageSlider");
        qBlankCoverageValue = QContract.QContractFind<TextBox>(QBlankBody, "PBlankCoverageValue");
        qBlankMinimumSlider = QContract.QContractFind<Slider>(QBlankBody, "PBlankMinimumSlider");
        qBlankMinimumValue = QContract.QContractFind<TextBox>(QBlankBody, "PBlankMinimumValue");
        qBlankWheelCanvas = QContract.QContractFind<Canvas>(QBlankBody, "PBlankWheelCanvas");
        qBlankWheelImage = QContract.QContractFind<Image>(QBlankBody, "PBlankWheelImage");
        qBlankWheelDot = QContract.QContractFind<Ellipse>(QBlankBody, "PBlankWheelDot");

        qBlankSwitch.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        qBlankSwitch.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Detector.ApplyTooltip");
        qBlankBlack.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Black");
        qBlankColor.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Color");
        ((TextBlock)((Grid)qBlankPicker.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Picker");
        qBlankPicker.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.PickerTooltip");
        QContract.QContractFind<TextBlock>(QBlankBody, "PBlankToleranceLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Tolerance");
        QContract.QContractFind<TextBlock>(QBlankBody, "PBlankCoverageLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Coverage");
        QContract.QContractFind<TextBlock>(QBlankBody, "PBlankMinimumLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Blank.Minimum");
        qBlankPickerIcon.Source = QIcon.QIconRead(QBlankPickerIcon, QTool.QToolIconRead());

        LDetectorBound lBrightness = QBlankBrightnessBound;
        qBlankBrightnessSlider.Minimum = lBrightness.LDetectorBoundLeast;
        qBlankBrightnessSlider.Maximum = lBrightness.LDetectorBoundMost;
        LDetectorBound lTolerance = LDetector.LDetectorToleranceRead();
        qBlankToleranceSlider.Minimum = lTolerance.LDetectorBoundLeast;
        qBlankToleranceSlider.Maximum = lTolerance.LDetectorBoundMost;
        LDetectorBound lCoverage = LDetector.LDetectorCoverageRead();
        qBlankCoverageSlider.Minimum = lCoverage.LDetectorBoundLeast;
        qBlankCoverageSlider.Maximum = lCoverage.LDetectorBoundMost;
        LDetectorBound lMinimum = QBlankMinimumBound;
        qBlankMinimumSlider.Minimum = lMinimum.LDetectorBoundLeast;
        qBlankMinimumSlider.Maximum = lMinimum.LDetectorBoundMost;

        qBlankLifecycle = new QBlankLifecycle(
            QBlankBody,
            qBlankSwitch,
            qBlankGate.CBlankEnabledSet,
            qBlankBlack,
            qBlankColor,
            qBlankPicker,
            [
                (qBlankBrightnessSlider, () => QBlankBrightnessBound.LDetectorBoundDefault,
                    qBlankGate.CBlankBrightnessSet),
                (qBlankToleranceSlider, () => LDetector.LDetectorToleranceRead().LDetectorBoundDefault,
                    qBlankGate.CBlankToleranceSet),
                (qBlankCoverageSlider, () => LDetector.LDetectorCoverageRead().LDetectorBoundDefault,
                    qBlankGate.CBlankCoverageSet),
                (qBlankMinimumSlider, () => QBlankMinimumBound.LDetectorBoundDefault,
                    qBlankGate.CBlankMinimumSet),
            ],
            [
                (qBlankBrightnessValue, QBlankBrightnessCommit),
                (qBlankToleranceValue, QBlankToleranceCommit),
                (qBlankCoverageValue, QBlankCoverageCommit),
                (qBlankMinimumValue, QBlankMinimumCommit),
            ],
            qBlankWheelCanvas,
            () => qBlankGate.CBlankTypeSet(LDetectorType.LDetectorTypeBlack),
            () => qBlankGate.CBlankTypeSet(LDetectorType.LDetectorTypeColor),
            () => QBlankPickSet(qBlankPicker.IsChecked == true),
            lMouse =>
            {
                qBlankWheelCanvas.CaptureMouse();
                QBlankWheelHandle(qBlankWheelCanvas, lMouse);
            },
            lMouse => QBlankWheelHandle(qBlankWheelCanvas, lMouse));

        QBlankAttach();
        QBlankPickUpdate(qBlankPicking);
        QBlankUpdate();
    }

    public void QBlankStepSet(LDetectorBlank lStep) => qBlankGate.CBlankStepSet(lStep);

    public LNeutralDot QBlankDotRead(double lSize, double lDot) => qBlankGate.CBlankDotRead(lSize, lDot);

    public void QBlankWheelHandle(bool lPressed, double lX, double lY, double lSize)
    {
        if (lPressed)
        {
            qBlankGate.CBlankWheelSet(lX, lY, lSize);
        }
    }

    public void QBlankSampleSet(int lRed, int lGreen, int lBlue)
    {
        QBlankPickSet(false);
        qBlankGate.CBlankSampleSet(lRed, lGreen, lBlue);
    }

    public void QBlankPickSet(bool lPicking)
    {
        if (qBlankPicking == lPicking)
        {
            return;
        }

        qBlankPicking = lPicking;
        QBlankPickChanged?.Invoke(lPicking);
    }

    public void QBlankAttach()
    {
        qBlankLifecycle.QBlankLifecycleAttach();
        qBlankGate.CBlankChanged -= QBlankUpdate;
        QBlankPickChanged -= QBlankPickUpdate;
        qBlankGate.CBlankChanged += QBlankUpdate;
        QBlankPickChanged += QBlankPickUpdate;
    }

    public void QBlankDetach()
    {
        if (QBlankBody is null || qBlankLifecycle is null)
        {
            return;
        }

        qBlankLifecycle.QBlankLifecycleDetach();
        qBlankGate.CBlankChanged -= QBlankUpdate;
        QBlankPickChanged -= QBlankPickUpdate;
    }

    private void QBlankBrightnessCommit()
    {
        if (QBlankBody.Resources[typeof(QInspectorFrame)] is not true) return;
        qBlankGate.CBlankBrightnessSet(QInspectorField.QInspectorValueCommit(qBlankBrightnessValue.Text,
            QBlankStep.LDetectorBlankBrightness, qBlankBrightnessSlider.Minimum, qBlankBrightnessSlider.Maximum));
        qBlankBrightnessValue.CaretIndex = qBlankBrightnessValue.Text.Length;
    }
    private void QBlankToleranceCommit()
    {
        if (QBlankBody.Resources[typeof(QInspectorFrame)] is not true) return;
        qBlankGate.CBlankToleranceSet(QInspectorField.QInspectorValueCommit(qBlankToleranceValue.Text,
            QBlankStep.LDetectorBlankTolerance, qBlankToleranceSlider.Minimum, qBlankToleranceSlider.Maximum));
        qBlankToleranceValue.CaretIndex = qBlankToleranceValue.Text.Length;
    }

    private void QBlankCoverageCommit()
    {
        if (QBlankBody.Resources[typeof(QInspectorFrame)] is not true) return;
        qBlankGate.CBlankCoverageSet(QInspectorField.QInspectorValueCommit(qBlankCoverageValue.Text,
            QBlankStep.LDetectorBlankCoverage, qBlankCoverageSlider.Minimum, qBlankCoverageSlider.Maximum));
        qBlankCoverageValue.CaretIndex = qBlankCoverageValue.Text.Length;
    }

    private void QBlankMinimumCommit()
    {
        if (QBlankBody.Resources[typeof(QInspectorFrame)] is not true) return;
        qBlankGate.CBlankMinimumSet(QInspectorField.QInspectorValueCommit(qBlankMinimumValue.Text,
            QBlankStep.LDetectorBlankMinimum, qBlankMinimumSlider.Minimum, qBlankMinimumSlider.Maximum));
        qBlankMinimumValue.CaretIndex = qBlankMinimumValue.Text.Length;
    }

    private void QBlankWheelUpdate() => qBlankWheelImage.Source =
        qBlankWheel.QWhitebalanceWheelDraw(QBlankWheelSize, qBlankBrightnessSlider.Value);

    private void QBlankWheelHandle(object lSender, MouseEventArgs lMouse)
    {
        if (QBlankBody.Resources[typeof(QInspectorFrame)] is not true) return;
        Point lPoint = lMouse.GetPosition(qBlankWheelCanvas);
        QBlankWheelHandle(
            lMouse.LeftButton == MouseButtonState.Pressed, lPoint.X, lPoint.Y, QBlankWheelSize);
    }

    private void QBlankWheelPlace()
    {
        LNeutralDot lDot = QBlankDotRead(QBlankWheelSize, qBlankWheelDot.Width);
        Canvas.SetLeft(qBlankWheelDot, lDot.LNeutralDotLeft);
        Canvas.SetTop(qBlankWheelDot, lDot.LNeutralDotTop);
        qBlankWheelDot.Visibility = lDot.LNeutralDotPresent ? Visibility.Visible : Visibility.Collapsed;
    }

    private void QBlankPickUpdate(bool lPicking)
    {
        QBlankPickerSet(lPicking);
        QBlankIconApply(lPicking);
    }

    private void QBlankPickerSet(bool lPicking)
    {
        bool lAttached = QBlankBody.Resources[typeof(QInspectorFrame)] is true;
        QBlankBody.Resources[typeof(QInspectorFrame)] = false;
        try
        {
            qBlankPicker.IsChecked = lPicking;
        }
        finally
        {
            QBlankBody.Resources[typeof(QInspectorFrame)] = lAttached;
        }
    }

    private void QBlankIconApply(bool lPicking) =>
        qBlankPickerIcon.Source = QIcon.QIconRead(QBlankPickerIcon, QTool.QToolPickRead(lPicking));

    private void QBlankUpdate()
    {
        bool lAttached = QBlankBody.Resources[typeof(QInspectorFrame)] is true;
        QBlankBody.Resources[typeof(QInspectorFrame)] = false;
        try
        {
            LDetectorBlank lBlank = QBlankStep;
            qBlankSwitch.IsChecked = lBlank.LDetectorBlankEnabled;
            qBlankColor.IsChecked = QBlankWheelPresent;
            qBlankBlack.IsChecked = !QBlankWheelPresent;
            qBlankColorArea.Visibility = QBlankWheelPresent ? Visibility.Visible : Visibility.Collapsed;
            qBlankBrightnessSlider.Value = Math.Clamp(
                lBlank.LDetectorBlankBrightness, qBlankBrightnessSlider.Minimum, qBlankBrightnessSlider.Maximum);
            qBlankToleranceSlider.Value = Math.Clamp(
                lBlank.LDetectorBlankTolerance, qBlankToleranceSlider.Minimum, qBlankToleranceSlider.Maximum);
            qBlankCoverageSlider.Value = Math.Clamp(
                lBlank.LDetectorBlankCoverage, qBlankCoverageSlider.Minimum, qBlankCoverageSlider.Maximum);
            qBlankMinimumSlider.Value = Math.Clamp(
                lBlank.LDetectorBlankMinimum, qBlankMinimumSlider.Minimum, qBlankMinimumSlider.Maximum);
            qBlankBrightnessValue.Text = QInspectorField.QInspectorValueFormat(
                qBlankBrightnessValue.Text, lBlank.LDetectorBlankBrightness, "0.00");
            qBlankToleranceValue.Text = QInspectorField.QInspectorValueFormat(
                qBlankToleranceValue.Text, lBlank.LDetectorBlankTolerance, "0.00");
            qBlankCoverageValue.Text = QInspectorField.QInspectorValueFormat(
                qBlankCoverageValue.Text, lBlank.LDetectorBlankCoverage, "0.00");
            qBlankMinimumValue.Text = QInspectorField.QInspectorValueFormat(
                qBlankMinimumValue.Text, lBlank.LDetectorBlankMinimum, "0.0");
            QBlankWheelUpdate();
            QBlankWheelPlace();
            QTool.QToolStackUpdate(qBlankStack, lBlank.LDetectorBlankEnabled);
        }
        finally
        {
            QBlankBody.Resources[typeof(QInspectorFrame)] = lAttached;
        }
    }
}
