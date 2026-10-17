using System.Windows;
using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The clinic's wiring: hands every input to the clinic gate, shows the gate's wording keys through the catalog and
// paints the gate's salvage answer. The members that only pass a gate answer or input on serve PClinic until its
// painting moves here.
public sealed class QClinic
{
    private readonly CClinic qClinicGate;

    public QClinic(CClinic lClinic)
    {
        ArgumentNullException.ThrowIfNull(lClinic);
        qClinicGate = lClinic;
    }

    public event Action? QClinicChanged
    {
        add => qClinicGate.CClinicChanged += value;
        remove => qClinicGate.CClinicChanged -= value;
    }

    public event Action<bool>? QClinicMinimizeChanged
    {
        add => qClinicGate.CClinicMinimizeChanged += value;
        remove => qClinicGate.CClinicMinimizeChanged -= value;
    }

    public bool QClinicSalvageShown => qClinicGate.CClinicSalvageShown;

    public bool QClinicKnown => qClinicGate.CClinicKnown;

    public bool QClinicPersistentAllowed => qClinicGate.CClinicPersistentAllowed;

    public bool QClinicRepairChecked => qClinicGate.CClinicRepairChecked;

    public bool QClinicPersistentChecked => qClinicGate.CClinicPersistentChecked;

    public bool QClinicResultShown => qClinicGate.CClinicResultShown;

    public bool QClinicScanShown => qClinicGate.CClinicScanShown;

    public double QClinicProgressValue => qClinicGate.CClinicProgressValue;

    public bool QClinicMinimized => qClinicGate.CClinicMinimized;

    public string QClinicTitleRead() => QLocalizationCatalog.QLocalizationTextRead(qClinicGate.CClinicTitleKey);

    public string QClinicSimpleRead() => QClinicTextRead(qClinicGate.CClinicSimpleKey);

    public string QClinicTechnicalRead() => QClinicTextRead(qClinicGate.CClinicTechnicalKey);

    public string QClinicResultFormat() => qClinicGate.CClinicResultFormat(
    [
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Empty"),
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Scanning"),
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Clean"),
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Failed"),
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Defect"),
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Evidence"),
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Repair")
    ]);

    public void QClinicMinimizedSet(bool lMinimized) => qClinicGate.CClinicMinimizedSet(lMinimized);

    public void QClinicStepSet(string? lStepName) => qClinicGate.CClinicStepSet(lStepName);

    public void QClinicActiveSet(bool lActive) => qClinicGate.CClinicRepairSet(lActive);

    public void QClinicPersistentSet(bool lPersistent) => qClinicGate.CClinicPersistentSet(lPersistent);

    // Each salvage switch and choice hands its raw state to the gate.
    public void QClinicSalvageAttach(
        CheckBox lActive,
        CheckBox lPersistent,
        RadioButton lRejoin,
        RadioButton lSeparate,
        RadioButton lSource,
        RadioButton lFixed)
    {
        lActive.Checked += QClinicActiveHandle;
        lActive.Unchecked += QClinicActiveHandle;
        lRejoin.Checked += QClinicRejoinHandle;
        lSeparate.Checked += QClinicSeparateHandle;
        lSource.Checked += QClinicSourceHandle;
        lFixed.Checked += QClinicFixedHandle;
        lPersistent.Checked += QClinicPersistentHandle;
        lPersistent.Unchecked += QClinicPersistentHandle;
    }

    private void QClinicActiveHandle(object lSender, RoutedEventArgs lArgs) =>
        qClinicGate.CSalvageActiveSet(((CheckBox)lSender).IsChecked == true);

    private void QClinicRejoinHandle(object lSender, RoutedEventArgs lArgs) => qClinicGate.CSalvageModeSet(false);

    private void QClinicSeparateHandle(object lSender, RoutedEventArgs lArgs) => qClinicGate.CSalvageModeSet(true);

    private void QClinicSourceHandle(object lSender, RoutedEventArgs lArgs) => qClinicGate.CSalvageBasisSet(false);

    private void QClinicFixedHandle(object lSender, RoutedEventArgs lArgs) => qClinicGate.CSalvageBasisSet(true);

    private void QClinicPersistentHandle(object lSender, RoutedEventArgs lArgs) =>
        qClinicGate.CSalvagePersistentSet(((CheckBox)lSender).IsChecked == true);

    // The salvage body paints the gate's one salvage answer.
    public void QClinicSalvageUpdate(
        UIElement lBody,
        CheckBox lActive,
        CheckBox lPersistent,
        RadioButton lRejoin,
        RadioButton lSeparate,
        RadioButton lSource,
        RadioButton lFixed,
        TextBlock lDescription)
    {
        var lSalvage = qClinicGate.CClinicSalvageRead();
        lBody.Visibility = QClinicVisibilityResolve(lSalvage.CClinicShown);
        lActive.IsChecked = lSalvage.CClinicActive;
        lPersistent.IsChecked = lSalvage.CClinicPersistent;
        lSeparate.IsChecked = lSalvage.CClinicSeparate;
        lRejoin.IsChecked = !lSalvage.CClinicSeparate;
        lFixed.IsEnabled = lSalvage.CClinicFixable;
        lFixed.IsChecked = lSalvage.CClinicFixed;
        lSource.IsChecked = !lSalvage.CClinicFixed;
        lDescription.Text = QLocalizationCatalog.QLocalizationTextRead(lSalvage.CClinicDescription);
    }

    private static string QClinicTextRead(string? lKey) =>
        lKey is null ? string.Empty : QLocalizationCatalog.QLocalizationTextRead(lKey);

    private static Visibility QClinicVisibilityResolve(bool lShown) =>
        lShown ? Visibility.Visible : Visibility.Collapsed;
}
