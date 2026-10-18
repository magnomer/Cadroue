using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private async Task PSAudioVerifyHandle(ComboBox pCombo, Button pButton, IProgress<double> pFeed)
    {
        PSAudioBusyApply(pButton);
        IReadOnlyList<string> pAvailable;
        try
        {
            pAvailable = await lsEncoder.QSEncoderAudio.QSEncoderAudioScan(pFeed);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        string pSelected = lsEncoder.QSEncoderAudio.QSEncoderAudioEncoder;
        PSEncoderComboApply(pCombo, pAvailable, PSEncoderChoiceResolve(pAvailable, pSelected));
        PSAudioEncoderUpdate();
        PSAudioIdleApply(pButton);
    }

    private static void PSAudioBusyApply(Button pButton)
    {
        pButton.IsEnabled = false;
        pButton.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.Checking");
    }

    private static void PSAudioIdleApply(Button pButton)
    {
        pButton.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify");
        pButton.IsEnabled = true;
    }
}
