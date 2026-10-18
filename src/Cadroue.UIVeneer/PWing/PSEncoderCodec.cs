using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private void PSCodecProbeDefer() =>
        LTrialCatalog.LTrialSetDefer(() => Dispatcher.BeginInvoke(() => { if (IsLoaded) PSCodecContainerHandle(); }));

    private void PSVideoEncoderUpdate()
    {
        bool pAvailable = QSEncoderVideo.QSEncoderVideoCheck(PSCombo.PSComboTextRead(psVideoEncoderCombo));
        PSVideoNoticeApply(pAvailable);
        if (!pAvailable)
        {
            PSVideoReasonSet();
        }
    }

    private void PSVideoNoticeApply(bool pAvailable) =>
        psVideoEncoderNotice.Visibility = pAvailable ? Visibility.Collapsed : Visibility.Visible;

    private void PSVideoReasonSet() =>
        psVideoEncoderNotice.Text = QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Notice.Unavailable");

    private void PSCodecContainerHandle()
    {
        string pContainer = PSCombo.PSComboTextRead(psOutputContainerCombo);
        string pCurrent = PSCodecCurrentRead();
        string[] pItems = QSEncoderVideo.QSEncoderVideoRead(pContainer, pCurrent);
        PSCodecItemsApply(psVideoEncoderCombo, pItems, PSCodecSelectResolve(pItems, pCurrent));
        PSVideoEncoderUpdate();
    }

    private string PSCodecCurrentRead() => psVideoEncoderCombo.SelectedItem as string ?? string.Empty;

    private static string? PSCodecSelectResolve(IReadOnlyList<string> pItems, string pCurrent) =>
        pItems.Contains(pCurrent) ? pCurrent : pItems.FirstOrDefault();

    private static void PSCodecItemsApply(ComboBox pCombo, IReadOnlyList<string> pItems, string? pSelected)
    {
        pCombo.ItemsSource = pItems;
        pCombo.SelectedItem = pSelected;
    }

    private async Task PSCodecVerifyHandle(ComboBox pCombo, Button pButton, IProgress<double> pFeed)
    {
        PSCodecBusyApply(pButton);
        IReadOnlyList<string>? pAvailable = await PSCodecScan(pFeed);
        if (pAvailable is null)
        {
            return;
        }

        string pSelected = lsEncoder.QSEncoderVideo.QSEncoderVideoEncoder;
        PSCodecItemsApply(pCombo, pAvailable, PSCodecSelectResolve(pAvailable, pSelected));
        PSVideoEncoderUpdate();
        PSCodecReadyApply(pButton);
    }

    private static void PSCodecBusyApply(Button pButton)
    {
        pButton.IsEnabled = false;
        pButton.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.Checking");
    }

    private async Task<IReadOnlyList<string>?> PSCodecScan(IProgress<double> pFeed)
    {
        try
        {
            return await lsEncoder.QSEncoderVideo.QSEncoderVideoScan(pFeed);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private static void PSCodecReadyApply(Button pButton)
    {
        pButton.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify");
        pButton.IsEnabled = true;
    }
}
