using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal static class PSEncoderVerification
{
    internal static string? PSEncoderChoiceResolve(IReadOnlyList<string> pItems, string pCurrent) =>
        pItems.Contains(pCurrent) ? pCurrent : pItems.FirstOrDefault();

    internal static void PSEncoderComboApply(ComboBox pCombo, IReadOnlyList<string> pItems, string? pSelected)
    {
        pCombo.ItemsSource = pItems;
        pCombo.SelectedItem = pSelected;
    }

    internal static void PSEncoderBusyApply(Button pButton)
    {
        pButton.IsEnabled = false;
        pButton.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.Checking");
    }

    internal static void PSEncoderIdleApply(Button pButton)
    {
        pButton.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify");
        pButton.IsEnabled = true;
    }

    internal static void PSEncoderProgressStart(ProgressBar pProgress)
    {
        pProgress.Value = 0;
        pProgress.Visibility = Visibility.Visible;
    }

    internal static void PSEncoderProgressStop(ProgressBar pProgress) => pProgress.Visibility = Visibility.Collapsed;
}
