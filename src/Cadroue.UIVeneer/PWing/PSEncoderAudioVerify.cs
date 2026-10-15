using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private async Task PSAudioVerifyHandle(ComboBox pCombo, Button pButton, IProgress<double> pFeed)
    {
        pButton.IsEnabled = false;
        pButton.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.Checking");
        IReadOnlyList<string> pAvailable;
        try
        {
            pAvailable = await lsEncoder.QSEncoderAudioScan(pFeed);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        string pSelected = lsEncoder.QSEncoderAudioEncoder;
        pCombo.ItemsSource = pAvailable;
        pCombo.SelectedItem = pAvailable.Contains(pSelected) ? pSelected : pAvailable.FirstOrDefault();
        PSAudioEncoderUpdate();
        pButton.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify");
        pButton.IsEnabled = true;
    }
}
