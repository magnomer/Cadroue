using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The white balance tool's wiring: fills the method rows and the saturation field from the gate, hands every input to
// the gate and paints the section; its wheel places the dot. The members the surface still calls answer from the gate.
public sealed class QWhitebalance
{
    private const string QWhitebalancePattern = "0.#";
    private const string QWhitebalanceSheet = "PWhitebalance";
    private const string QWhitebalancePersistentSheet = "PWhitebalancePersistent";
    private const string QWhitebalanceActive = "PWhitebalanceActive";
    private const string QWhitebalanceStack = "PWhitebalanceStack";
    private const string QWhitebalanceMethod = "PWhitebalanceMethod";
    private const string QWhitebalanceMethodLabel = "PWhitebalanceMethodLabel";
    private const string QWhitebalanceWarning = "PWhitebalanceWarning";
    private const string QWhitebalanceResetButton = "PWhitebalanceResetButton";

    private readonly CWhitebalance qWhitebalanceGate;
    private readonly QWhitebalanceNeutral qWhitebalanceNeutral;
    private readonly CheckBox qWhitebalanceSwitch;
    private readonly ComboBox qWhitebalanceMethod;
    private readonly StackPanel qWhitebalanceStack;
    private readonly Grid qWhitebalanceField;

    public QWhitebalance(QInspector lInspector)
    {
        qWhitebalanceGate = lInspector.QInspectorVideo.QInspectorWhitebalance;
        QWhitebalanceBody = QContract.QContractSheetFind<StackPanel>(QWhitebalanceSheet);
        QWhitebalancePersistent = QContract.QContractSheetFind<CheckBox>(QWhitebalancePersistentSheet);
        qWhitebalanceSwitch = QContract.QContractFind<CheckBox>(QWhitebalanceBody, QWhitebalanceActive);
        qWhitebalanceStack = QContract.QContractFind<StackPanel>(QWhitebalanceBody, QWhitebalanceStack);
        qWhitebalanceMethod = QContract.QContractFind<ComboBox>(QWhitebalanceBody, QWhitebalanceMethod);
        qWhitebalanceNeutral = new QWhitebalanceNeutral();
        QWhitebalanceWheel = new QWhitebalanceWheel(lInspector);
        qWhitebalanceStack.Children.Insert(0, qWhitebalanceNeutral.QWhitebalanceNeutralBody);
        qWhitebalanceStack.Children.Insert(1, QWhitebalanceWheel.QWhitebalanceWheelBody);

        qWhitebalanceSwitch.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        qWhitebalanceSwitch.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyWhitebalance");
        QWhitebalancePersistent.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        QWhitebalancePersistent.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistWhitebalance");
        QContract.QContractFind<TextBlock>(QWhitebalanceBody, QWhitebalanceMethodLabel).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceMethod");
        QContract.QContractFind<TextBlock>(QWhitebalanceBody, QWhitebalanceWarning).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceWarning");
        QDropdown.QDropdownAttach(qWhitebalanceMethod);
        Button lReset = QContract.QContractFind<Button>(QWhitebalanceBody, QWhitebalanceResetButton);
        lReset.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceReset");
        lReset.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceResetTooltip");
        QButton.QButtonAttach(lReset, QButtonLook.QButtonLookPanel);

        var lSetup = qWhitebalanceGate.CWhitebalanceSetupRead();
        (Action, Action) lMethod = QWhitebalanceMethodBuild(
            lSetup.CWhitebalanceMethodKeys,
            lSetup.CWhitebalanceMethodIndex,
            qWhitebalanceGate.CWhitebalanceMethodSelect);
        qWhitebalanceField = QWhitebalanceFieldBuild(
            lSetup.CWhitebalanceSaturationLeast,
            lSetup.CWhitebalanceSaturationMost,
            lSetup.CWhitebalanceSaturationOrigin);
        QCheckbox.QCheckboxAttach(qWhitebalanceSwitch);
        QCheckbox.QCheckboxAttach(QWhitebalancePersistent);
        QWhitebalanceBody.Resources[typeof(QWhitebalance)] = new List<(Action, Action)>
        {
            QWhitebalanceAttach(lReset, qWhitebalanceGate.CWhitebalanceReset),
            lMethod,
            (ValueTuple<Action, Action>)qWhitebalanceField.Resources[typeof(QWhitebalance)],
            QWhitebalanceActiveAttach(qWhitebalanceGate.CWhitebalanceActiveSet),
            QWhitebalancePersistentAttach(qWhitebalanceGate.CWhitebalancePersistentSet),
        };
        qWhitebalanceNeutral.QWhitebalanceNeutralAttach(this);
        QWhitebalanceWheel.QWhitebalanceWheelAttach();
        QWhitebalanceChangeAttach();
        QWhitebalanceStateUpdate();
    }

    public StackPanel QWhitebalanceBody { get; }

    public CheckBox QWhitebalancePersistent { get; }

    public QWhitebalanceWheel QWhitebalanceWheel { get; }

    public void QWhitebalanceChangeAttach()
    {
        qWhitebalanceNeutral.QWhitebalanceNeutralAttach(true);
        QWhitebalanceWheel.QWhitebalanceWheelAttach(true);
        foreach ((Action lAttach, _) in
                 (List<(Action, Action)>)QWhitebalanceBody.Resources[typeof(QWhitebalance)])
        {
            lAttach();
        }

        qWhitebalanceGate.CWhitebalanceChanged -= QWhitebalanceStateUpdate;
        qWhitebalanceGate.CWhitebalanceChanged -= QWhitebalanceUpdate;
        qWhitebalanceGate.CWhitebalanceChanged += QWhitebalanceStateUpdate;
        qWhitebalanceGate.CWhitebalanceChanged += QWhitebalanceUpdate;
    }

    public void QWhitebalanceChangeDetach()
    {
        qWhitebalanceNeutral.QWhitebalanceNeutralAttach(false);
        QWhitebalanceWheel.QWhitebalanceWheelAttach(false);
        qWhitebalanceGate.CWhitebalanceChanged -= QWhitebalanceStateUpdate;
        qWhitebalanceGate.CWhitebalanceChanged -= QWhitebalanceUpdate;
        foreach ((_, Action lDetach) in
                 (List<(Action, Action)>)QWhitebalanceBody.Resources[typeof(QWhitebalance)])
        {
            lDetach();
        }
    }

    public void QWhitebalanceUpdate() => qWhitebalanceNeutral.QWhitebalanceNeutralUpdate(this);

    public bool QWhitebalanceCapable => qWhitebalanceGate.CWhitebalanceCapable;

    public bool QWhitebalanceGreyArmed => qWhitebalanceGate.CWhitebalanceGreyArmed;

    public bool QWhitebalanceWhiteArmed => qWhitebalanceGate.CWhitebalanceWhiteArmed;

    public bool QWhitebalanceGuideShown => qWhitebalanceGate.CWhitebalanceGuideShown;

    public QInspectorTip QWhitebalanceTip => QInspectorTip.QInspectorTipResolve(
        CInspectorTip.CInspectorTipResolve(
            qWhitebalanceGate.CWhitebalanceActive,
            qWhitebalanceGate.CWhitebalanceCapable,
            qWhitebalanceGate.CWhitebalancePreview,
            "Inspector.Video.WhitebalanceRequiresEq",
            "Inspector.Video.WhitebalancePreviewMpv"),
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

    public void QWhitebalanceToolToggle(bool lWhite, bool lArmed) =>
        qWhitebalanceGate.CWhitebalanceToolToggle(lWhite, lArmed);

    // The combo shows each method row's wording in the gate's order and selects through the supplied gate operation.
    private (Action, Action) QWhitebalanceMethodBuild(
        IReadOnlyList<string> lMethodKeys,
        int lMethodIndex,
        Action<int> lSelect)
    {
        foreach (string lKey in lMethodKeys)
        {
            qWhitebalanceMethod.Items.Add(QLocalizationCatalog.QLocalizationTextRead(lKey));
        }

        qWhitebalanceMethod.SelectedIndex = lMethodIndex;
        SelectionChangedEventHandler lHandler = (_, _) => lSelect(qWhitebalanceMethod.SelectedIndex);
        Action lAttach = () =>
        {
            qWhitebalanceMethod.SelectionChanged -= lHandler;
            qWhitebalanceMethod.SelectionChanged += lHandler;
        };
        Action lDetach = () => qWhitebalanceMethod.SelectionChanged -= lHandler;
        lAttach();
        return (lAttach, lDetach);
    }

    // The saturation field row, placed right after the method combo's field row.
    private Grid QWhitebalanceFieldBuild(double lLeast, double lMost, double lOrigin)
    {
        Grid lField = QField.QFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceSaturation"),
            "%",
            lLeast,
            lMost,
            lOrigin);
        int lRow = qWhitebalanceStack.Children.IndexOf((UIElement)qWhitebalanceMethod.Parent);
        qWhitebalanceStack.Children.Insert(lRow + 1, lField);
        lField.Resources[typeof(QWhitebalance)] = QField.QFieldAttach(
            lField,
            lLeast,
            lMost,
            () => qWhitebalanceGate.CWhitebalanceSaturation,
            qWhitebalanceGate.CWhitebalanceSaturationSet);
        return lField;
    }

    private (Action, Action) QWhitebalanceActiveAttach(Action<bool> lSet) =>
        QWhitebalanceSwitchAttach(qWhitebalanceSwitch, lSet);

    private (Action, Action) QWhitebalancePersistentAttach(Action<bool> lSet) =>
        QWhitebalanceSwitchAttach(QWhitebalancePersistent, lSet);

    private static (Action, Action) QWhitebalanceAttach(Button lButton, Action lSet)
    {
        RoutedEventHandler lHandler = (_, _) => lSet();
        Action lAttach = () =>
        {
            lButton.Click -= lHandler;
            lButton.Click += lHandler;
        };
        Action lDetach = () => lButton.Click -= lHandler;
        lAttach();
        return (lAttach, lDetach);
    }

    // A switch hands its check and uncheck to the gate.
    private static (Action, Action) QWhitebalanceSwitchAttach(ToggleButton lSwitch, Action<bool> lSet)
    {
        RoutedEventHandler lChecked = (_, _) => lSet(true);
        RoutedEventHandler lUnchecked = (_, _) => lSet(false);
        Action lAttach = () =>
        {
            lSwitch.Checked -= lChecked;
            lSwitch.Unchecked -= lUnchecked;
            lSwitch.Checked += lChecked;
            lSwitch.Unchecked += lUnchecked;
        };
        Action lDetach = () =>
        {
            lSwitch.Checked -= lChecked;
            lSwitch.Unchecked -= lUnchecked;
        };
        lAttach();
        return (lAttach, lDetach);
    }

    private void QWhitebalanceStateUpdate()
    {
        QTool.QToolSwitchUpdate(qWhitebalanceSwitch, qWhitebalanceGate.CWhitebalanceActive);
        QTool.QToolSwitchUpdate(QWhitebalancePersistent, qWhitebalanceGate.CWhitebalancePersistent);
        qWhitebalanceMethod.SelectedIndex = qWhitebalanceGate.CWhitebalanceMethodIndex;
        QField.QFieldUpdate(qWhitebalanceField, qWhitebalanceGate.CWhitebalanceSaturation, QWhitebalancePattern);
        QTool.QToolTipApply(
            qWhitebalanceSwitch,
            QWhitebalancePersistent,
            qWhitebalanceStack,
            QWhitebalanceBody,
            QWhitebalanceTip);
        QWhitebalanceWheel.QWhitebalanceWheelPlace();
    }
}

public sealed record QWhitebalanceReadout(
    bool QWhitebalanceReadoutSampled,
    byte QWhitebalanceReadoutRed,
    byte QWhitebalanceReadoutGreen,
    byte QWhitebalanceReadoutBlue,
    string QWhitebalanceReadoutText);
