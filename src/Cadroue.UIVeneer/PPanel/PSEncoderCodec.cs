using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Application;
using Cadroue.UIDeportment;

using static Cadroue.UIVeneer.PSCasement.PSField;
using static Cadroue.UIVeneer.PSCasement.PSCombo;

namespace Cadroue.UIVeneer.PPanel;

internal sealed partial class PSEncoder
{
    private void PSCodecProbeDefer() =>
        LTrialSet.LTrialSetDefer(() => Dispatcher.BeginInvoke(() => { if (IsLoaded) PSCodecContainerHandle(); }));

    private void PSVideoEncoderUpdate()
    {
        bool pAvailable = LSEncoder.LSEncoderVideoCheck(PSComboTextRead(psVideoEncoderCombo));
        psVideoEncoderNotice.Visibility = pAvailable ? Visibility.Collapsed : Visibility.Visible;
        if (!pAvailable)
        {
            psVideoEncoderNotice.Text = LLocalization.LLocalizationTextRead("Encoder.Video.Notice.Unavailable");
        }
    }

    private void PSCodecContainerHandle()
    {
        string pContainer = PSComboTextRead(psOutputContainerCombo);
        string pCurrent = psVideoEncoderCombo.SelectedItem as string ?? string.Empty;
        string[] pItems = LSEncoder.LSEncoderVideoRead(pContainer, pCurrent);
        psVideoEncoderCombo.ItemsSource = pItems;
        psVideoEncoderCombo.SelectedItem = pItems.Contains(pCurrent) ? pCurrent : pItems.FirstOrDefault();
        PSVideoEncoderUpdate();
    }

    private async Task PSCodecVerifyHandle(ComboBox pCombo, Button pButton, IProgress<double> pFeed)
    {
        pButton.IsEnabled = false;
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Verification.Checking");
        IReadOnlyList<string> pAvailable;
        try
        {
            pAvailable = await lsEncoder.LSEncoderVideoScan(pFeed);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        string pSelected = lsEncoder.LSEncoderVideoEncoder;
        pCombo.ItemsSource = pAvailable;
        pCombo.SelectedItem = pAvailable.Contains(pSelected) ? pSelected : pAvailable.FirstOrDefault();
        PSVideoEncoderUpdate();
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Button.Verify");
        pButton.IsEnabled = true;
    }
}
