using System.Windows;
using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The salvage block's wiring: each switch and choice hands its raw state to the clinic gate, and the block paints the
// gate's one salvage answer.
public sealed class QClinicSalvage
{
    private readonly CClinic qClinicGate;

    public QClinicSalvage(CClinic lClinic)
    {
        qClinicGate = lClinic;
    }

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

    private static Visibility QClinicVisibilityResolve(bool lShown) =>
        lShown ? Visibility.Visible : Visibility.Collapsed;
}
