using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private async Task PSAudioVerifyHandle(ComboBox pCombo, Button pButton, IProgress<double> pFeed)
    {
        pButton.IsEnabled = false;
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Verification.Checking");
        IReadOnlyList<string> pAvailable;
        try
        {
            pAvailable = await lsEncoder.LSEncoderAudioScan(pFeed);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        string pSelected = lsEncoder.LSEncoderAudioEncoder;
        pCombo.ItemsSource = pAvailable;
        pCombo.SelectedItem = pAvailable.Contains(pSelected) ? pSelected : pAvailable.FirstOrDefault();
        PSAudioEncoderUpdate();
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Button.Verify");
        pButton.IsEnabled = true;
    }
}
