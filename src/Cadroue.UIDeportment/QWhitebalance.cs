using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The white balance tool's wiring: fills the method rows and the saturation field from the gate, hands every input to
// the gate and paints the section and the wheel dot. The members the surface still calls answer from the gate.
public sealed class QWhitebalance
{
    private const string QWhitebalancePattern = "0.#";

    private readonly QInspector qWhitebalanceInspector;
    private readonly CWhitebalance qWhitebalanceGate;
    private readonly CheckBox qWhitebalanceSwitch;
    private readonly CheckBox qWhitebalancePersistent;
    private readonly ComboBox qWhitebalanceMethod;
    private readonly StackPanel qWhitebalanceStack;
    private readonly StackPanel qWhitebalanceBody;
    private readonly Ellipse qWhitebalanceDot;
    private readonly int qWhitebalanceSize;
    private readonly Grid qWhitebalanceField;

    // The section's parts: its switches, the method combo whose field row the saturation field follows, the stack
    // and body the tip lands on, the grey and white picker buttons in that order, and the dot on a wheel that size.
    public QWhitebalance(
        QInspector lInspector,
        CheckBox lSwitch,
        CheckBox lPersistent,
        ComboBox lMethod,
        StackPanel lStack,
        StackPanel lBody,
        IReadOnlyList<ToggleButton> lPickers,
        Ellipse lDot,
        int lSize)
    {
        qWhitebalanceInspector = lInspector;
        qWhitebalanceGate = lInspector.QInspectorVideo.QInspectorWhitebalance;
        qWhitebalanceSwitch = lSwitch;
        qWhitebalancePersistent = lPersistent;
        qWhitebalanceMethod = lMethod;
        qWhitebalanceStack = lStack;
        qWhitebalanceBody = lBody;
        qWhitebalanceDot = lDot;
        qWhitebalanceSize = lSize;

        QWhitebalanceMethodBuild();
        qWhitebalanceField = QWhitebalanceFieldBuild();
        QWhitebalanceActiveAttach();
        QWhitebalancePersistentAttach();
        QWhitebalancePickerAttach(lPickers[0], false);
        QWhitebalancePickerAttach(lPickers[1], true);

        qWhitebalanceGate.CWhitebalanceChanged += QWhitebalanceUpdate;
        QWhitebalanceUpdate();
    }

    public event Action? QWhitebalanceChanged
    {
        add => qWhitebalanceGate.CWhitebalanceChanged += value;
        remove => qWhitebalanceGate.CWhitebalanceChanged -= value;
    }

    public bool QWhitebalanceCapable => qWhitebalanceGate.CWhitebalanceCapable;

    public bool QWhitebalanceGreyArmed => qWhitebalanceGate.CWhitebalanceGreyArmed;

    public bool QWhitebalanceWhiteArmed => qWhitebalanceGate.CWhitebalanceWhiteArmed;

    public bool QWhitebalanceGuideShown => qWhitebalanceGate.CWhitebalanceGuideShown;

    public QInspectorTip QWhitebalanceTip => qWhitebalanceInspector.QInspectorTipResolve(
        qWhitebalanceGate.CWhitebalanceActive,
        qWhitebalanceGate.CWhitebalanceCapable,
        qWhitebalanceGate.CWhitebalancePreview,
        "Inspector.Video.WhitebalanceRequiresEq",
        "Inspector.Video.WhitebalancePreviewMpv",
        "Inspector.Video.ApplyWhitebalance",
        "Inspector.Video.PersistWhitebalance");

    public string QWhitebalanceGuideRead() => qWhitebalanceGate.CWhitebalanceGuideShown
        ? QLocalizationCatalog.QLocalizationTextRead(qWhitebalanceGate.CWhitebalanceGuideKey)
        : string.Empty;

    public QWhitebalanceReadout QWhitebalanceReadoutRead()
    {
        (bool lSampled, int lRed, int lGreen, int lBlue) = qWhitebalanceGate.CWhitebalanceReadoutRead();
        return new QWhitebalanceReadout(
            lSampled,
            (byte)lRed,
            (byte)lGreen,
            (byte)lBlue,
            lSampled
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceSample"),
                    lRed,
                    lGreen,
                    lBlue)
                : string.Empty);
    }

    // The colour wheel at the given size and brightness.
    public ImageSource QWhitebalanceWheelDraw(int lSize, double lValue)
    {
        (int lSide, int lStride, byte[] lPixels) = qWhitebalanceGate.CWhitebalanceBitmapRead(lSize, lValue);
        var lBitmap = new WriteableBitmap(lSide, lSide, 96, 96, PixelFormats.Bgra32, null);
        lBitmap.WritePixels(new Int32Rect(0, 0, lSide, lSide), lPixels, lStride, 0);
        lBitmap.Freeze();
        return lBitmap;
    }

    public void QWhitebalanceReset() => qWhitebalanceGate.CWhitebalanceReset();

    public void QWhitebalanceToolCancel() => qWhitebalanceGate.CWhitebalanceToolCancel();

    public void QWhitebalanceWheelHandle(bool lPressed, double lX, double lY, double lSize) =>
        qWhitebalanceGate.CWhitebalanceWheelHandle(lPressed, lX, lY, lSize);

    // The combo shows each method row's wording in the gate's order and selects the gate's method.
    private void QWhitebalanceMethodBuild()
    {
        foreach (string lKey in qWhitebalanceGate.CWhitebalanceMethodKeys)
        {
            qWhitebalanceMethod.Items.Add(QLocalizationCatalog.QLocalizationTextRead(lKey));
        }

        qWhitebalanceMethod.SelectedIndex = qWhitebalanceGate.CWhitebalanceMethodIndex;
        qWhitebalanceMethod.SelectionChanged += (_, _) =>
            qWhitebalanceGate.CWhitebalanceMethodSelect(qWhitebalanceMethod.SelectedIndex);
    }

    // The saturation field row, placed right after the method combo's field row.
    private Grid QWhitebalanceFieldBuild()
    {
        Grid lField = QField.QFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceSaturation"),
            "%",
            qWhitebalanceGate.CWhitebalanceSaturationLeast,
            qWhitebalanceGate.CWhitebalanceSaturationMost,
            qWhitebalanceGate.CWhitebalanceSaturationOrigin);
        int lRow = qWhitebalanceStack.Children.IndexOf((UIElement)qWhitebalanceMethod.Parent);
        qWhitebalanceStack.Children.Insert(lRow + 1, lField);
        QField.QFieldAttach(
            lField,
            qWhitebalanceGate.CWhitebalanceSaturationLeast,
            qWhitebalanceGate.CWhitebalanceSaturationMost,
            () => qWhitebalanceGate.CWhitebalanceSaturation,
            qWhitebalanceGate.CWhitebalanceSaturationSet);
        return lField;
    }

    private void QWhitebalanceActiveAttach() =>
        QWhitebalanceSwitchAttach(qWhitebalanceSwitch, lActive => qWhitebalanceGate.CWhitebalanceActiveSet(lActive));

    private void QWhitebalancePersistentAttach() =>
        QWhitebalanceSwitchAttach(
            qWhitebalancePersistent, lFlag => qWhitebalanceGate.CWhitebalancePersistentSet(lFlag));

    private void QWhitebalancePickerAttach(ToggleButton lPicker, bool lWhite) =>
        QWhitebalanceSwitchAttach(lPicker, lArmed => qWhitebalanceGate.CWhitebalanceToolToggle(lWhite, lArmed));

    // A switch or picker button hands its check and uncheck to the gate.
    private static void QWhitebalanceSwitchAttach(ToggleButton lSwitch, Action<bool> lSet)
    {
        lSwitch.Checked += (_, _) => lSet(true);
        lSwitch.Unchecked += (_, _) => lSet(false);
    }

    private void QWhitebalanceUpdate()
    {
        QTool.QToolSwitchUpdate(qWhitebalanceSwitch, qWhitebalanceGate.CWhitebalanceActive);
        QTool.QToolSwitchUpdate(qWhitebalancePersistent, qWhitebalanceGate.CWhitebalancePersistent);
        qWhitebalanceMethod.SelectedIndex = qWhitebalanceGate.CWhitebalanceMethodIndex;
        QField.QFieldUpdate(qWhitebalanceField, qWhitebalanceGate.CWhitebalanceSaturation, QWhitebalancePattern);
        QTool.QToolTipApply(
            qWhitebalanceSwitch, qWhitebalancePersistent, qWhitebalanceStack, qWhitebalanceBody, QWhitebalanceTip);
        QWhitebalanceWheelPlace();
    }

    private void QWhitebalanceWheelPlace()
    {
        (double lLeft, double lTop, bool lPresent) =
            qWhitebalanceGate.CWhitebalanceDotRead(qWhitebalanceSize, qWhitebalanceDot.Width);
        Canvas.SetLeft(qWhitebalanceDot, lLeft);
        Canvas.SetTop(qWhitebalanceDot, lTop);
        qWhitebalanceDot.Visibility = lPresent ? Visibility.Visible : Visibility.Collapsed;
    }
}

public sealed record QWhitebalanceReadout(
    bool QWhitebalanceReadoutSampled,
    byte QWhitebalanceReadoutRed,
    byte QWhitebalanceReadoutGreen,
    byte QWhitebalanceReadoutBlue,
    string QWhitebalanceReadoutText);
