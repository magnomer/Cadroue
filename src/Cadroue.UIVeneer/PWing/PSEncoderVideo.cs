using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private UIElement PSVideoPlateBuild()
    {
        var pVerify = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify"),
            84,
            new Thickness(8, 0, 0, 0));
        var pLog = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Result"),
            64,
            new Thickness(6, 0, 0, 0));
        PSVideoLogApply(pLog);
        ProgressBar pProgress = PSField.PSFieldProgressBuild();
        PSVideoPlateAttach(pVerify, pLog, pProgress);
        StackPanel pPanel = PSVideoPanelBuild(pVerify, pLog, pProgress);
        PSVideoRowsRebuild();
        PSVideoScopeUpdate();
        PSVideoEncoderUpdate();
        return PSPlate.PSPlateBuild(pPanel);
    }

    private void PSVideoLogApply(Button pLog) =>
        pLog.IsEnabled = lsEncoder.QSEncoderVideo.QSEncoderVideoResults.Count > 0;

    private void PSVideoPlateAttach(Button pVerify, Button pLog, ProgressBar pProgress)
    {
        var pFeed = new Progress<double>(pValue => pProgress.Value = pValue);
        pVerify.Click += async (_, _) => await PSVideoVerifyHandle(pVerify, pLog, pProgress, pFeed);
        pLog.Click += (_, _) => PSVerdict.PSVerdictShow(this, lsEncoder.QSEncoderVideo.QSEncoderVerdictCreate());
        psVideoEncoderCombo.SelectionChanged += (_, _) => PSVideoSet();
        psVideoRateCombo.SelectionChanged += (_, _) => PSVideoSet();
        psVideoReactiveBox.Checked += (_, _) => PSVideoReactiveApply();
        psVideoReactiveBox.Unchecked += (_, _) => PSVideoReactiveApply();
    }

    private async Task PSVideoVerifyHandle(Button pVerify, Button pLog, ProgressBar pProgress, IProgress<double> pFeed)
    {
        PSVideoProgressReset(pProgress);
        try
        {
            await PSCodecVerifyHandle(psVideoEncoderCombo, pVerify, pFeed);
        }
        finally
        {
            PSVideoProgressHide(pProgress);
        }

        PSVideoLogApply(pLog);
    }

    private static void PSVideoProgressReset(ProgressBar pProgress)
    {
        pProgress.Value = 0;
        pProgress.Visibility = Visibility.Visible;
    }

    private static void PSVideoProgressHide(ProgressBar pProgress) => pProgress.Visibility = Visibility.Collapsed;

    private StackPanel PSVideoPanelBuild(Button pVerify, Button pLog, ProgressBar pProgress)
    {
        var pPanel = new StackPanel();
        psVideoEncodePanel.Children.Add(
            PSField.PSFieldButtonBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.Encoder"),
                psVideoEncoderCombo,
                pVerify,
                pLog,
                pProgress));
        psVideoEncodePanel.Children.Add(psVideoEncoderNotice);
        psVideoEncodePanel.Children.Add(
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.RateControl"),
                psVideoRateCombo));
        psVideoEncodePanel.Children.Add(psVideoRowsPanel);
        PSVideoResolutionBuild(psVideoEncodePanel);
        psVideoEncodePanel.Children.Add(
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.Reactive"),
                psVideoReactiveBox));
        PSVideoFpsBuild(psVideoEncodePanel);
        psVideoEncodePanel.Children.Add(
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.PixelFormat"),
                psVideoPixelCombo));

        pPanel.Children.Add(
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.Mode"), psVideoMode));
        pPanel.Children.Add(psVideoEncodePanel);
        pPanel.Children.Add(psVideoNotice);
        return pPanel;
    }

    private static CheckBox PSVideoReactiveBuild(bool pReactive)
    {
        var pReactiveBox = new CheckBox
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Orientation"),
            IsChecked = pReactive,
            VerticalAlignment = VerticalAlignment.Center
        };
        QInteractionClick.QInteractionClickAttach(pReactiveBox);
        QCheckbox.QCheckboxAttach(pReactiveBox);
        return pReactiveBox;
    }

    private void PSVideoScopeUpdate()
    {
        string pMode = PSField.PSModeTextRead(psVideoMode);
        bool pEncoded = PSVideoEncodeCheck(pMode);
        PSVideoScopeApply(pEncoded, pMode);
    }

    private static bool PSVideoEncodeCheck(string pMode) => pMode == "Encode";

    private void PSVideoScopeApply(bool pEncoded, string pMode)
    {
        psVideoEncodePanel.Visibility = pEncoded ? Visibility.Visible : Visibility.Collapsed;
        psVideoNotice.Visibility = pEncoded ? Visibility.Collapsed : Visibility.Visible;
        psVideoNotice.Text = QLocalizationCatalog.QLocalizationTextRead(PSVideoNoticeRead(pMode));
    }

    private string PSVideoNoticeRead(string pMode) => lsEncoder.QSEncoderVideo.QSEncoderNoticeResolve(pMode);

    private void PSVideoSet()
    {
        string pEncoder = PSCombo.PSComboTextRead(psVideoEncoderCombo);
        string pRate = PSCombo.PSComboTextRead(psVideoRateCombo);
        if (PSVideoChoiceCheck(pEncoder, pRate))
        {
            lsEncoder.QSEncoderVideo.QSEncoderVideoSet(pEncoder, pRate);
        }
    }

    private static bool PSVideoChoiceCheck(string pEncoder, string pRate) => pEncoder.Length > 0 && pRate.Length > 0;

    private void PSVideoRowsApply()
    {
        string[] pModeLabels = lsEncoder.QSEncoderVideo.QSEncoderVideoCodec.LCapabilityModeLabels;
        if (PSVideoLabelsCheck(pModeLabels))
        {
            PSVideoLabelsSet(pModeLabels);
        }

        PSVideoRateSelect();
        PSVideoRowsRebuild();
        PSVideoEncoderUpdate();
    }

    private bool PSVideoLabelsCheck(string[] pModeLabels) =>
        psVideoRateCombo.ItemsSource is not string[] pShown || !pShown.SequenceEqual(pModeLabels);

    private void PSVideoLabelsSet(string[] pModeLabels) => psVideoRateCombo.ItemsSource = pModeLabels;

    private void PSVideoRateSelect() => psVideoRateCombo.SelectedItem = lsEncoder.QSEncoderVideo.QSEncoderVideoRate;

    private void PSVideoRowsRebuild()
    {
        PSVideoRowsClear();
        PSVideoRowsReset();
        LCapabilityCodec pCodec = lsEncoder.QSEncoderVideo.QSEncoderVideoCodec;
        LCapabilityMode pMode = pCodec.LCapabilityModeFind(lsEncoder.QSEncoderVideo.QSEncoderVideoRate);
        bool pModeStored = PSVideoStoredCheck(pMode);
        PSVideoQualityBuild(pMode, pModeStored);
        PSVideoCautionBuild(pCodec);
        PSVideoSpeedBuild(pCodec, pModeStored);
        PSVideoExtraBuild(pCodec, pMode);
    }

    private void PSVideoRowsClear() => psVideoRowsPanel.Children.Clear();

    private void PSVideoRowsReset()
    {
        psVideoQualityBox = null;
        psVideoSpeedSlider = null;
        psVideoSpeedChoices = null;
        psVideoExtraCombos.Clear();
    }

    private bool PSVideoStoredCheck(LCapabilityMode pMode) =>
        string.Equals(
            pMode.LCapabilityModeLabel,
            lsExportSpecificEdit.LPresetVideo.LPresetRateControl,
            StringComparison.Ordinal);

    private void PSVideoCautionBuild(LCapabilityCodec pCodec)
    {
        if (!string.IsNullOrWhiteSpace(pCodec.LCapabilityNotice))
        {
            psVideoRowsPanel.Children.Add(PSNotice.PSNoticeBuild(pCodec.LCapabilityNotice));
        }
    }

    private void PSVideoQualityBuild(LCapabilityMode pMode, bool pModeStored)
    {
        if (pMode.LCapabilityModeQuality is not LCapabilityQuality pQuality)
        {
            return;
        }

        string pText = PSVideoQualityResolve(pQuality, pModeStored);
        PSVideoEntryBuild(pQuality, pText);
    }

    private string PSVideoQualityResolve(LCapabilityQuality pQuality, bool pModeStored)
    {
        string pStored = lsExportSpecificEdit.LPresetVideo.LPresetQuality;
        return pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pQuality.LCapabilityQualityDefault;
    }

    private void PSVideoEntryBuild(LCapabilityQuality pQuality, string pText)
    {
        psVideoQualityBox = PSEntry.PSEntryBuild(pText, 120);
        if (pQuality.LCapabilityQualityMinimum is double pMinimum
            && pQuality.LCapabilityQualityMaximum is double pMaximum)
        {
            UIElement pSliderRow = pQuality.LCapabilityQualityBitrate
                ? PSFader.PSFaderBitrateBuild(pMinimum, pMaximum, pText, psVideoQualityBox)
                : PSFader.PSFaderQualityBuild(
                    pMinimum,
                    pMaximum,
                    pQuality.LCapabilityQualityStep,
                    pText,
                    psVideoQualityBox,
                    pQuality.LCapabilityQualityAscending);
            psVideoRowsPanel.Children.Add(PSField.PSFieldBuild(pQuality.LCapabilityQualityLabel, pSliderRow));
        }
        else
        {
            psVideoRowsPanel.Children.Add(PSField.PSFieldBuild(pQuality.LCapabilityQualityLabel, psVideoQualityBox));
        }

        psVideoRowsPanel.Children.Add(PSNotice.PSNoticeBuild(PSVideoRangeFormat(pQuality)));
    }

    private static string PSVideoRangeFormat(LCapabilityQuality pQuality)
    {
        string pRange = pQuality.LCapabilityQualityRange;
        return string.IsNullOrEmpty(pRange)
            ? QLocalizationCatalog.QLocalizationFormat("Encoder.Video.FFmpegOption", pQuality.LCapabilityQualityOption)
            : QLocalizationCatalog.QLocalizationFormat(
                "Encoder.Video.FFmpegOptionRange",
                pQuality.LCapabilityQualityOption,
                pRange);
    }

    private void PSVideoSpeedBuild(LCapabilityCodec pCodec, bool pModeStored)
    {
        if (pCodec.LCapabilitySpeed is not LCapabilitySpeed pSpeed)
        {
            return;
        }

        string pSelected = PSVideoSpeedResolve(pSpeed, pModeStored);
        IReadOnlyList<LCapabilityChoice> pChoices = pSpeed.LCapabilitySpeedValues;
        PSVideoChoicesSet(pChoices);
        int pIndex = PSVideoIndexFind(pChoices, pSelected);
        Slider pSlider = PSVideoSliderBuild(pChoices, pIndex);
        PSVideoSliderSet(pSlider);
        TextBlock pText = PSVideoLabelBuild(pChoices[pIndex].LCapabilityChoiceLabel);
        PSVideoSliderAttach(pSlider, pText, pChoices);
        psVideoRowsPanel.Children.Add(
            PSField.PSFieldBuild(pSpeed.LCapabilitySpeedLabel, PSFader.PSFaderRowBuild(pSlider, pText)));
    }

    private string PSVideoSpeedResolve(LCapabilitySpeed pSpeed, bool pModeStored)
    {
        string pStored = lsExportSpecificEdit.LPresetVideo.LPresetSpeedPreset;
        return pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pSpeed.LCapabilitySpeedDefault;
    }

    private void PSVideoChoicesSet(IReadOnlyList<LCapabilityChoice> pChoices) => psVideoSpeedChoices = pChoices;

    private static int PSVideoIndexFind(IReadOnlyList<LCapabilityChoice> pChoices, string pSelected)
    {
        for (int pAt = 0; pAt < pChoices.Count; pAt++)
        {
            if (string.Equals(pChoices[pAt].LCapabilityChoiceValue, pSelected, StringComparison.Ordinal))
            {
                return pAt;
            }
        }

        return 0;
    }

    private static Slider PSVideoSliderBuild(IReadOnlyList<LCapabilityChoice> pChoices, int pIndex)
    {
        int pLast = pChoices.Count - 1;
        return PSFader.PSFaderCreate(0, pLast, pLast - pIndex);
    }

    private void PSVideoSliderSet(Slider pSlider) => psVideoSpeedSlider = pSlider;

    private static TextBlock PSVideoLabelBuild(string pLabel) => new()
    {
        Text = pLabel,
        Foreground = PSField.PSFieldText,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static void PSVideoSliderAttach(Slider pSlider, TextBlock pText, IReadOnlyList<LCapabilityChoice> pChoices)
    {
        int pLast = pChoices.Count - 1;
        pSlider.ValueChanged += (_, _) =>
            pText.Text = pChoices[PSVideoIndexResolve(pSlider.Value, pLast)].LCapabilityChoiceLabel;
    }

    private static int PSVideoIndexResolve(double pValue, int pLast) =>
        pLast - Math.Clamp((int)Math.Round(pValue), 0, pLast);

    private string PSVideoSpeedRead()
    {
        if (psVideoSpeedSlider is null || psVideoSpeedChoices is null || psVideoSpeedChoices.Count == 0)
        {
            return string.Empty;
        }

        int pLast = psVideoSpeedChoices.Count - 1;
        int pAt = pLast - Math.Clamp((int)Math.Round(psVideoSpeedSlider.Value), 0, pLast);
        return psVideoSpeedChoices[pAt].LCapabilityChoiceValue;
    }

    private void PSVideoExtraBuild(LCapabilityCodec pCodec, LCapabilityMode pMode)
    {
        foreach (LCapabilityExtra pExtra in pCodec.LCapabilityExtraList)
        {
            if (pMode.LCapabilityConflictCheck(pExtra.LCapabilityExtraOption))
            {
                continue;
            }

            string pSelected = PSVideoExtraResolve(pExtra);
            ComboBox pCombo = PSCombo.PSComboBuild(pSelected, PSEncoderChoicesRead(pExtra.LCapabilityExtraValues));
            PSVideoExtraSet(pExtra, pCombo);
            psVideoRowsPanel.Children.Add(PSField.PSFieldBuild(pExtra.LCapabilityExtraLabel, pCombo));
        }
    }

    private string PSVideoExtraResolve(LCapabilityExtra pExtra) =>
        lsExportSpecificEdit.LPresetVideo.LPresetExtras.TryGetValue(
                pExtra.LCapabilityExtraOption,
                out string? pStored)
            && pExtra.LCapabilityExtraValues.Any(pChoice => string.Equals(
                pChoice.LCapabilityChoiceValue,
                pStored,
                StringComparison.Ordinal))
            ? pStored
            : pExtra.LCapabilityExtraDefault;

    private void PSVideoExtraSet(LCapabilityExtra pExtra, ComboBox pCombo) =>
        psVideoExtraCombos[pExtra.LCapabilityExtraOption] = pCombo;
}
