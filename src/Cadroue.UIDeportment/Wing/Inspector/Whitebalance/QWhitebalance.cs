using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The white balance tool's wiring: fills the method rows and the saturation field from the gate, hands every input to
// the gate and paints the section; its wheel places the dot. The members the surface still calls answer from the gate.
public sealed class QWhitebalance
{
    private const string QWhitebalancePattern = "0.#";

    private readonly CWhitebalance qWhitebalanceGate;
    private readonly CheckBox qWhitebalanceSwitch;
    private readonly CheckBox qWhitebalancePersistent;
    private readonly ComboBox qWhitebalanceMethod;
    private readonly StackPanel qWhitebalanceStack;
    private readonly StackPanel qWhitebalanceBody;
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
        qWhitebalanceGate = lInspector.QInspectorVideo.QInspectorWhitebalance;
        qWhitebalanceSwitch = lSwitch;
        qWhitebalancePersistent = lPersistent;
        qWhitebalanceMethod = lMethod;
        qWhitebalanceStack = lStack;
        qWhitebalanceBody = lBody;
        QWhitebalanceWheel = new QWhitebalanceWheel(qWhitebalanceGate, lDot, lSize);

        QWhitebalanceMethodBuild();
        qWhitebalanceField = QWhitebalanceFieldBuild();
        QWhitebalanceActiveAttach();
        QWhitebalancePersistentAttach();
        QWhitebalancePickerAttach(lPickers[0], false);
        QWhitebalancePickerAttach(lPickers[1], true);

        qWhitebalanceGate.CWhitebalanceChanged += QWhitebalanceUpdate;
        QWhitebalanceUpdate();
    }

    public QWhitebalanceWheel QWhitebalanceWheel { get; }

    public event Action? QWhitebalanceChanged
    {
        add => qWhitebalanceGate.CWhitebalanceChanged += value;
        remove => qWhitebalanceGate.CWhitebalanceChanged -= value;
    }

    public bool QWhitebalanceCapable => qWhitebalanceGate.CWhitebalanceCapable;

    public bool QWhitebalanceGreyArmed => qWhitebalanceGate.CWhitebalanceGreyArmed;

    public bool QWhitebalanceWhiteArmed => qWhitebalanceGate.CWhitebalanceWhiteArmed;

    public bool QWhitebalanceGuideShown => qWhitebalanceGate.CWhitebalanceGuideShown;

    public QInspectorTip QWhitebalanceTip => QInspectorTip.QInspectorTipResolve(
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

    public void QWhitebalanceReset() => qWhitebalanceGate.CWhitebalanceReset();

    public void QWhitebalanceToolCancel() => qWhitebalanceGate.CWhitebalanceToolCancel();

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
        QWhitebalanceWheel.QWhitebalanceWheelPlace();
    }
}

public sealed record QWhitebalanceReadout(
    bool QWhitebalanceReadoutSampled,
    byte QWhitebalanceReadoutRed,
    byte QWhitebalanceReadoutGreen,
    byte QWhitebalanceReadoutBlue,
    string QWhitebalanceReadoutText);
