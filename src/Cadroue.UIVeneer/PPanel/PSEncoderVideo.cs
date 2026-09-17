using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIDeportment;

using static Cadroue.UIVeneer.PSCasement.PSField;
using static Cadroue.UIVeneer.PSCasement.PSCombo;
using static Cadroue.UIVeneer.PSCasement.PSNotice;
using static Cadroue.UIVeneer.PSCasement.PSEntry;
using static Cadroue.UIVeneer.PSCasement.PSFader;
using static Cadroue.UIVeneer.PSCasement.PSInline;
using static Cadroue.UIVeneer.PSCasement.PSPlate;

namespace Cadroue.UIVeneer.PPanel;

internal sealed partial class PSEncoder
{
    private UIElement PSVideoPlateBuild()
    {
        var pPanel = new StackPanel();
        var pVerify = PSInlineButtonBuild(
            LLocalization.LLocalizationTextRead("Encoder.Button.Verify"),
            84,
            new Thickness(8, 0, 0, 0));
        var pLog = PSInlineButtonBuild(
            LLocalization.LLocalizationTextRead("Encoder.Button.Result"),
            64,
            new Thickness(6, 0, 0, 0));
        pLog.IsEnabled = lsEncoder.LSEncoderVideoResults.Count > 0;
        ProgressBar pProgress = PSFieldProgressBuild();
        var pFeed = new Progress<double>(pValue => pProgress.Value = pValue);
        pVerify.Click += async (_, _) =>
        {
            pProgress.Value = 0;
            pProgress.Visibility = Visibility.Visible;
            try
            {
                await PSCodecVerifyHandle(psVideoEncoderCombo, pVerify, pFeed);
            }
            finally
            {
                pProgress.Visibility = Visibility.Collapsed;
            }

            pLog.IsEnabled = lsEncoder.LSEncoderVideoResults.Count > 0;
        };
        pLog.Click += (_, _) => PSVerdict.PSVerdictShow(
            this,
            new LSVerdict(
                LLocalization.LLocalizationTextRead("Encoder.Verification.VideoTitle"),
                lsEncoder.LSEncoderVideoResults));
        psVideoEncoderCombo.SelectionChanged += (_, _) => PSVideoSet();
        psVideoRateCombo.SelectionChanged += (_, _) => PSVideoSet();

        psVideoEncodePanel.Children.Add(
            PSFieldButtonBuild(
                LLocalization.LLocalizationTextRead("Encoder.Video.Field.Encoder"),
                psVideoEncoderCombo,
                pVerify,
                pLog,
                pProgress));
        psVideoEncodePanel.Children.Add(psVideoEncoderNotice);
        psVideoEncodePanel.Children.Add(
            PSFieldBuild(
                LLocalization.LLocalizationTextRead("Encoder.Video.Field.RateControl"),
                psVideoRateCombo));
        psVideoEncodePanel.Children.Add(psVideoRowsPanel);
        PSVideoResolutionBuild(psVideoEncodePanel);
        psVideoReactiveBox.Checked += (_, _) => PSVideoReactiveApply();
        psVideoReactiveBox.Unchecked += (_, _) => PSVideoReactiveApply();
        psVideoEncodePanel.Children.Add(
            PSFieldBuild(
                LLocalization.LLocalizationTextRead("Encoder.Video.Field.Reactive"),
                psVideoReactiveBox));
        PSVideoFpsBuild(psVideoEncodePanel);
        psVideoEncodePanel.Children.Add(
            PSFieldBuild(
                LLocalization.LLocalizationTextRead("Encoder.Video.Field.PixelFormat"),
                psVideoPixelCombo));

        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.Mode"), psVideoMode));
        pPanel.Children.Add(psVideoEncodePanel);
        pPanel.Children.Add(psVideoNotice);

        PSVideoRowsRebuild();
        PSVideoScopeUpdate();
        PSVideoEncoderUpdate();
        return PSPlateBuild(pPanel);
    }

    private static CheckBox PSVideoReactiveBuild(bool pReactive)
    {
        var pReactiveBox = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Encoder.Video.Orientation"),
            IsChecked = pReactive,
            VerticalAlignment = VerticalAlignment.Center
        };
        PCheckbox.PCheckboxApply(pReactiveBox);
        return pReactiveBox;
    }

    private void PSVideoScopeUpdate()
    {
        string pMode = PSModeTextRead(psVideoMode);
        bool pEncoded = pMode == "Encode";

        psVideoEncodePanel.Visibility = pEncoded ? Visibility.Visible : Visibility.Collapsed;
        psVideoNotice.Visibility = pEncoded ? Visibility.Collapsed : Visibility.Visible;
        psVideoNotice.Text = LLocalization.LLocalizationTextRead(PSVideoNoticeRead(pMode));
    }

    private string PSVideoNoticeRead(string pMode) => lsEncoder.LSEncoderNoticeResolve(pMode);

    private void PSVideoSet()
    {
        string pEncoder = PSComboTextRead(psVideoEncoderCombo);
        string pRate = PSComboTextRead(psVideoRateCombo);
        if (pEncoder.Length > 0 && pRate.Length > 0)
        {
            lsEncoder.LSEncoderVideoSet(pEncoder, pRate);
        }
    }

    private void PSVideoRowsApply()
    {
        string[] pModeLabels = lsEncoder.LSEncoderVideoCodec.LCapabilityModeLabels;
        if (psVideoRateCombo.ItemsSource is not string[] pShown || !pShown.SequenceEqual(pModeLabels))
        {
            psVideoRateCombo.ItemsSource = pModeLabels;
        }

        psVideoRateCombo.SelectedItem = lsEncoder.LSEncoderVideoRate;
        PSVideoRowsRebuild();
        PSVideoEncoderUpdate();
    }

    private void PSVideoRowsRebuild()
    {
        psVideoRowsPanel.Children.Clear();
        psVideoQualityBox = null;
        psVideoSpeedSlider = null;
        psVideoSpeedChoices = null;
        psVideoExtraCombos.Clear();

        LCapabilityCodec pCodec = lsEncoder.LSEncoderVideoCodec;
        LCapabilityMode pMode = pCodec.LCapabilityModeFind(lsEncoder.LSEncoderVideoRate);
        bool pModeStored = string.Equals(
            pMode.LCapabilityModeLabel,
            lsExportSpecificEdit.LPresetVideo.LPresetRateControl,
            StringComparison.Ordinal);

        PSVideoQualityBuild(pMode, pModeStored);

        if (!string.IsNullOrWhiteSpace(pCodec.LCapabilityNotice))
        {
            psVideoRowsPanel.Children.Add(PSNoticeBuild(pCodec.LCapabilityNotice));
        }

        PSVideoSpeedBuild(pCodec, pModeStored);
        PSVideoExtraBuild(pCodec, pMode);
    }

    private void PSVideoQualityBuild(LCapabilityMode pMode, bool pModeStored)
    {
        if (pMode.LCapabilityModeQuality is not LCapabilityQuality pQuality)
        {
            return;
        }

        string pStored = lsExportSpecificEdit.LPresetVideo.LPresetQuality;
        string pText = pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pQuality.LCapabilityQualityDefault;

        psVideoQualityBox = PSEntryBuild(pText, 120);
        if (pQuality.LCapabilityQualityMinimum is double pMinimum
            && pQuality.LCapabilityQualityMaximum is double pMaximum)
        {
            UIElement pSliderRow = pQuality.LCapabilityQualityBitrate
                ? PSFaderBitrateBuild(pMinimum, pMaximum, pText, psVideoQualityBox)
                : PSFaderQualityBuild(
                    pMinimum,
                    pMaximum,
                    pQuality.LCapabilityQualityStep,
                    pText,
                    psVideoQualityBox,
                    pQuality.LCapabilityQualityAscending);
            psVideoRowsPanel.Children.Add(PSFieldBuild(pQuality.LCapabilityQualityLabel, pSliderRow));
        }
        else
        {
            psVideoRowsPanel.Children.Add(PSFieldBuild(pQuality.LCapabilityQualityLabel, psVideoQualityBox));
        }

        string pRange = pQuality.LCapabilityQualityRange;
        psVideoRowsPanel.Children.Add(PSNoticeBuild(string.IsNullOrEmpty(pRange)
            ? LLocalization.LLocalizationFormat("Encoder.Video.FFmpegOption", pQuality.LCapabilityQualityOption)
            : LLocalization.LLocalizationFormat(
                "Encoder.Video.FFmpegOptionRange",
                pQuality.LCapabilityQualityOption,
                pRange)));
    }

    private void PSVideoSpeedBuild(LCapabilityCodec pCodec, bool pModeStored)
    {
        if (pCodec.LCapabilitySpeed is not LCapabilitySpeed pSpeed)
        {
            return;
        }

        string pStored = lsExportSpecificEdit.LPresetVideo.LPresetSpeedPreset;
        string pSelected = pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pSpeed.LCapabilitySpeedDefault;

        IReadOnlyList<LCapabilityChoice> pChoices = pSpeed.LCapabilitySpeedValues;
        psVideoSpeedChoices = pChoices;
        int pLast = pChoices.Count - 1;

        int pIndex = 0;
        for (int pAt = 0; pAt < pChoices.Count; pAt++)
        {
            if (string.Equals(pChoices[pAt].LCapabilityChoiceValue, pSelected, StringComparison.Ordinal))
            {
                pIndex = pAt;
                break;
            }
        }

        Slider pSlider = PSFaderCreate(0, pLast, pLast - pIndex);
        psVideoSpeedSlider = pSlider;

        var pText = new TextBlock
        {
            Text = pChoices[pIndex].LCapabilityChoiceLabel,
            Foreground = PSFieldText,
            VerticalAlignment = VerticalAlignment.Center
        };
        pSlider.ValueChanged += (_, _) =>
        {
            int pAt = pLast - Math.Clamp((int)Math.Round(pSlider.Value), 0, pLast);
            pText.Text = pChoices[pAt].LCapabilityChoiceLabel;
        };

        psVideoRowsPanel.Children.Add(PSFieldBuild(pSpeed.LCapabilitySpeedLabel, PSFaderRowBuild(pSlider, pText)));
    }

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

            string pSelected = lsExportSpecificEdit.LPresetVideo.LPresetExtras.TryGetValue(
                    pExtra.LCapabilityExtraOption,
                    out string? pStored)
                && pExtra.LCapabilityExtraValues.Any(pChoice => string.Equals(
                    pChoice.LCapabilityChoiceValue,
                    pStored,
                    StringComparison.Ordinal))
                ? pStored
                : pExtra.LCapabilityExtraDefault;

            ComboBox pCombo = PSComboBuild(pSelected, PSEncoderChoicesRead(pExtra.LCapabilityExtraValues));
            psVideoExtraCombos[pExtra.LCapabilityExtraOption] = pCombo;
            psVideoRowsPanel.Children.Add(PSFieldBuild(pExtra.LCapabilityExtraLabel, pCombo));
        }
    }
}
