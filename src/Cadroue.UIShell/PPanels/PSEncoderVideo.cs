using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private UIElement PSVideoPlateBuild()
    {
        var pPanel = new StackPanel();
        var pVerify = PSInlineButtonBuild("Verify", 84, new Thickness(8, 0, 0, 0));
        var pLog = PSInlineButtonBuild("Log", 64, new Thickness(6, 0, 0, 0));
        pVerify.Click += async (_, _) => await PSCodecVerifyHandle(psVideoEncoderCombo, pVerify);
        pLog.Click += (_, _) => MessageBox.Show(this, psCodecLog, "Encoder verification log", MessageBoxButton.OK, MessageBoxImage.Information);
        psVideoEncoderCombo.SelectionChanged += (_, _) => PSVideoEncoderChangeHandle();
        psVideoRateCombo.SelectionChanged += (_, _) => PSVideoRowsRebuild();

        psVideoEncodePanel.Children.Add(PSFieldButtonBuild("Encoder", psVideoEncoderCombo, pVerify, pLog));
        psVideoEncodePanel.Children.Add(PSFieldBuild("Rate control", psVideoRateCombo));
        psVideoEncodePanel.Children.Add(psVideoRowsPanel);
        psVideoEncodePanel.Children.Add(PSFieldBuild("Size", psVideoSizeCombo));
        psVideoEncodePanel.Children.Add(PSFieldBuild("FPS", psVideoFpsCombo));
        psVideoEncodePanel.Children.Add(PSFieldBuild("Pixel format", psPixelCombo));

        pPanel.Children.Add(PSFieldBuild("Stream", psVideoStreamCombo));
        pPanel.Children.Add(PSFieldBuild("Mode", psVideoModeCombo));
        pPanel.Children.Add(psVideoEncodePanel);
        pPanel.Children.Add(psVideoNotice);

        psVideoStreamCombo.SelectionChanged += (_, _) => PSVideoScopeUpdate();
        psVideoModeCombo.SelectionChanged += (_, _) => PSVideoScopeUpdate();

        PSVideoRowsRebuild();
        PSVideoScopeUpdate();
        return PSPlateBuild(pPanel);
    }

    private void PSVideoScopeUpdate()
    {
        string pStream = PSComboTextRead(psVideoStreamCombo);
        string pMode = PSComboTextRead(psVideoModeCombo);

        bool pExcluded = pStream == "Exclude" || pMode == "Exclude";
        bool pCopied = pMode == "Copy";
        bool pEncoded = !pExcluded && !pCopied;

        psVideoEncodePanel.Visibility = pEncoded ? Visibility.Visible : Visibility.Collapsed;
        psVideoNotice.Visibility = pEncoded ? Visibility.Collapsed : Visibility.Visible;
        psVideoNotice.Text = pExcluded
            ? "No video stream is written, so no video settings apply."
            : "The video stream is copied as-is, so encoder settings do not apply.";
    }

    private LCapabilityCodec PSVideoCapabilityRead() =>
        LCapability.LCapabilityRead(PSCodecValueRead(PSComboTextRead(psVideoEncoderCombo)));

    private void PSVideoEncoderChangeHandle()
    {
        LCapabilityCodec pCodec = PSVideoCapabilityRead();
        string[] pModeLabels = pCodec.CapabilityModeLabels;

        string pPreviousMode = PSComboTextRead(psVideoRateCombo);

        psVideoRowsBusy = true;
        psVideoRateCombo.ItemsSource = pModeLabels;
        psVideoRateCombo.SelectedItem = pModeLabels.Contains(pPreviousMode) ? pPreviousMode : pModeLabels[0];
        psVideoRowsBusy = false;

        PSVideoRowsRebuild();
    }

    private void PSVideoRowsRebuild()
    {
        if (psVideoRowsBusy)
        {
            return;
        }

        psVideoRowsPanel.Children.Clear();
        psVideoQualityBox = null;
        psVideoSpeedCombo = null;
        psVideoExtraCombos.Clear();

        LCapabilityCodec pCodec = PSVideoCapabilityRead();
        LCapabilityMode pMode = pCodec.CapabilityModeFind(PSComboTextRead(psVideoRateCombo));
        bool pModeStored = string.Equals(pMode.CapabilityModeLabel, lsExportSpecificEdit.VideoRateControl, StringComparison.Ordinal);

        PSVideoQualityBuild(pMode, pModeStored);
        PSVideoSpeedBuild(pCodec, pModeStored);
        PSVideoExtraBuild(pCodec);

        if (!string.IsNullOrWhiteSpace(pCodec.CapabilityNotice))
        {
            psVideoRowsPanel.Children.Add(PSNoticeBuild(pCodec.CapabilityNotice));
        }
    }

    private void PSVideoQualityBuild(LCapabilityMode pMode, bool pModeStored)
    {
        if (pMode.CapabilityModeQuality is not LCapabilityQuality pQuality)
        {
            return;
        }

        string pStored = lsExportSpecificEdit.VideoQuality;
        string pText = pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pQuality.CapabilityQualityDefault;

        psVideoQualityBox = PSEntryBuild(pText, 120);
        psVideoRowsPanel.Children.Add(PSFieldBuild(pQuality.CapabilityQualityLabel, psVideoQualityBox));

        string pRange = pQuality.CapabilityQualityRange;
        psVideoRowsPanel.Children.Add(PSNoticeBuild(string.IsNullOrEmpty(pRange)
            ? $"FFmpeg option: {pQuality.CapabilityQualityOption}"
            : $"FFmpeg option: {pQuality.CapabilityQualityOption}  ({pRange})"));
    }

    private void PSVideoSpeedBuild(LCapabilityCodec pCodec, bool pModeStored)
    {
        if (pCodec.CapabilitySpeed is not LCapabilitySpeed pSpeed)
        {
            return;
        }

        string pStored = lsExportSpecificEdit.VideoSpeedPreset;
        string pSelected = pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pSpeed.CapabilitySpeedDefault;

        psVideoSpeedCombo = PSComboBuild(pSelected, [.. pSpeed.CapabilitySpeedValues]);
        psVideoRowsPanel.Children.Add(PSFieldBuild(pSpeed.CapabilitySpeedLabel, psVideoSpeedCombo));
    }

    private void PSVideoExtraBuild(LCapabilityCodec pCodec)
    {
        foreach (LCapabilityExtra pExtra in pCodec.CapabilityExtraList)
        {
            string pSelected = lsExportSpecificEdit.VideoExtras.TryGetValue(pExtra.CapabilityExtraOption, out string? pStored)
                               && pExtra.CapabilityExtraValues.Contains(pStored)
                ? pStored
                : pExtra.CapabilityExtraDefault;

            ComboBox pCombo = PSComboBuild(pSelected, [.. pExtra.CapabilityExtraValues]);
            psVideoExtraCombos[pExtra.CapabilityExtraOption] = pCombo;
            psVideoRowsPanel.Children.Add(PSFieldBuild(pExtra.CapabilityExtraLabel, pCombo));
        }
    }
}
