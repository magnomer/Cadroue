using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private UIElement PSVideoPlateBuild()
    {
        var pPanel = new StackPanel();
        var pVerify = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Button.Verify"), 84, new Thickness(8, 0, 0, 0));
        var pLog = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Button.Log"), 64, new Thickness(6, 0, 0, 0));
        pVerify.Click += async (_, _) => await PSCodecVerifyHandle(psVideoEncoderCombo, pVerify);
        pLog.Click += (_, _) => PSVerdict.PSVerdictShow(this, LLocalization.LLocalizationTextRead("Encoder.Verification.VideoTitle"), psCodecResults);
        psVideoEncoderCombo.SelectionChanged += (_, _) => PSVideoChangeHandle();
        psVideoRateCombo.SelectionChanged += (_, _) => PSVideoRowsRebuild();

        psVideoEncodePanel.Children.Add(PSFieldButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.Encoder"), psVideoEncoderCombo, pVerify, pLog));
        psVideoEncodePanel.Children.Add(psVideoEncoderNotice);
        psVideoEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.RateControl"), psVideoRateCombo));
        psVideoEncodePanel.Children.Add(psVideoRowsPanel);
        PSVideoResolutionBuild(psVideoEncodePanel);
        psVideoReactiveBox.Checked += (_, _) => PSVideoReactiveApply();
        psVideoReactiveBox.Unchecked += (_, _) => PSVideoReactiveApply();
        psVideoEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.Reactive"), psVideoReactiveBox));
        psVideoEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.FPS"), psVideoFpsSource));
        psVideoFpsRow = PSVideoFpsBuild();
        psVideoEncodePanel.Children.Add(psVideoFpsRow);
        psVideoFpsSource.Checked += (_, _) => PSVideoFpsUpdate();
        psVideoFpsSource.Unchecked += (_, _) => PSVideoFpsUpdate();
        PSVideoFpsUpdate();
        psVideoEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.PixelFormat"), psVideoPixelCombo));

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

    private string PSVideoNoticeRead(string pMode)
    {
        if (pMode != "Smart")
        {
            return "Encoder.Video.Notice.Copied";
        }

        return psSmartAllowed ? "Encoder.Video.Notice.Smart" : "Encoder.Video.Notice.SmartFull";
    }

    private LCapabilityCodec PSVideoCapabilityRead() =>
        LCapability.LCapabilityRead(PSCodecValueRead(PSComboTextRead(psVideoEncoderCombo)));

    private void PSVideoChangeHandle()
    {
        LCapabilityCodec pCodec = PSVideoCapabilityRead();
        string[] pModeLabels = pCodec.LCapabilityModeLabels;

        string pPreviousMode = PSComboTextRead(psVideoRateCombo);

        psVideoRowsBusy = true;
        psVideoRateCombo.ItemsSource = pModeLabels;
        psVideoRateCombo.SelectedItem = pModeLabels.Contains(pPreviousMode) ? pPreviousMode : pModeLabels[0];
        psVideoRowsBusy = false;

        PSVideoRowsRebuild();
        PSVideoEncoderUpdate();
    }

    private void PSVideoRowsRebuild()
    {
        if (psVideoRowsBusy)
        {
            return;
        }

        psVideoRowsPanel.Children.Clear();
        psVideoQualityBox = null;
        psVideoSpeedSlider = null;
        psVideoSpeedChoices = null;
        psVideoExtraCombos.Clear();

        LCapabilityCodec pCodec = PSVideoCapabilityRead();
        LCapabilityMode pMode = pCodec.LCapabilityModeFind(PSComboTextRead(psVideoRateCombo));
        bool pModeStored = string.Equals(pMode.CapabilityModeLabel, lsExportSpecificEdit.LPresetVideo.LPresetRateControl, StringComparison.Ordinal);

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

        string pStored = lsExportSpecificEdit.LPresetVideo.LPresetQuality;
        string pText = pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pQuality.CapabilityQualityDefault;

        psVideoQualityBox = PSEntryBuild(pText, 120);
        if (pQuality.CapabilityQualityMinimum is double pMinimum && pQuality.CapabilityQualityMaximum is double pMaximum)
        {
            UIElement pSliderRow = pQuality.LCapabilityQualityBitrate
                ? PSFieldBitrateBuild(pMinimum, pMaximum, pText, psVideoQualityBox)
                : PSFieldSliderBuild(pMinimum, pMaximum, pQuality.LCapabilityQualityStep, pText, psVideoQualityBox, pQuality.CapabilityQualityHigherBetter);
            psVideoRowsPanel.Children.Add(PSFieldBuild(pQuality.CapabilityQualityLabel, pSliderRow));
        }
        else
        {
            psVideoRowsPanel.Children.Add(PSFieldBuild(pQuality.CapabilityQualityLabel, psVideoQualityBox));
        }

        string pRange = pQuality.LCapabilityQualityRange;
        psVideoRowsPanel.Children.Add(PSNoticeBuild(string.IsNullOrEmpty(pRange)
            ? LLocalization.LLocalizationFormat("Encoder.Video.FFmpegOption", pQuality.CapabilityQualityOption)
            : LLocalization.LLocalizationFormat("Encoder.Video.FFmpegOptionRange", pQuality.CapabilityQualityOption, pRange)));
    }

    // Speed values are registered faster -> slower, but the slider reads slowest on the
    // left and fastest on the right, so slider position mirrors the choice index.
    private void PSVideoSpeedBuild(LCapabilityCodec pCodec, bool pModeStored)
    {
        if (pCodec.CapabilitySpeed is not LCapabilitySpeed pSpeed)
        {
            return;
        }

        string pStored = lsExportSpecificEdit.LPresetVideo.LPresetSpeedPreset;
        string pSelected = pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pSpeed.CapabilitySpeedDefault;

        IReadOnlyList<LCapabilityChoice> pChoices = pSpeed.CapabilitySpeedValues;
        psVideoSpeedChoices = pChoices;
        int pLast = pChoices.Count - 1;

        int pIndex = 0;
        for (int pAt = 0; pAt < pChoices.Count; pAt++)
        {
            if (string.Equals(pChoices[pAt].CapabilityChoiceValue, pSelected, StringComparison.Ordinal))
            {
                pIndex = pAt;
                break;
            }
        }

        Slider pSlider = PSFieldSliderCreate(0, pLast, pLast - pIndex);
        psVideoSpeedSlider = pSlider;

        var pText = new TextBlock
        {
            Text = pChoices[pIndex].CapabilityChoiceLabel,
            Foreground = PSFieldText,
            VerticalAlignment = VerticalAlignment.Center
        };
        pSlider.ValueChanged += (_, _) =>
        {
            int pAt = pLast - Math.Clamp((int)Math.Round(pSlider.Value), 0, pLast);
            pText.Text = pChoices[pAt].CapabilityChoiceLabel;
        };

        psVideoRowsPanel.Children.Add(PSFieldBuild(pSpeed.CapabilitySpeedLabel, PSFieldRowBuild(pSlider, pText)));
    }

    private string PSVideoSpeedRead()
    {
        if (psVideoSpeedSlider is null || psVideoSpeedChoices is null || psVideoSpeedChoices.Count == 0)
        {
            return string.Empty;
        }

        int pLast = psVideoSpeedChoices.Count - 1;
        int pAt = pLast - Math.Clamp((int)Math.Round(psVideoSpeedSlider.Value), 0, pLast);
        return psVideoSpeedChoices[pAt].CapabilityChoiceValue;
    }

    private static readonly (double Rate, string Value)[] psVideoFpsScale = PSVideoScaleCreate();
    private static readonly int psVideoFpsDefault = PSVideoScaleFind(30);

    private static (double Rate, string Value)[] PSVideoScaleCreate()
    {
        var pList = new List<(double Rate, string Value)>();
        for (int pAt = 1; pAt <= 240; pAt++)
        {
            pList.Add((pAt, pAt.ToString(CultureInfo.InvariantCulture)));
        }

        foreach (double pRate in new[] { 23.976, 29.97, 59.94, 119.88 })
        {
            pList.Add((pRate, pRate.ToString(CultureInfo.InvariantCulture)));
        }

        return pList.OrderBy(pEntry => pEntry.Rate).ToArray();
    }

    private static int PSVideoScaleFind(double pRate)
    {
        for (int pAt = 0; pAt < psVideoFpsScale.Length; pAt++)
        {
            if (psVideoFpsScale[pAt].Rate == pRate)
            {
                return pAt;
            }
        }

        return 0;
    }

    private static bool PSVideoSourceCheck(string pFps) =>
        string.IsNullOrWhiteSpace(pFps) || string.Equals(pFps.Trim(), "Same as source", StringComparison.Ordinal);

    private static CheckBox PSVideoSourceBuild(bool pSource)
    {
        var pBox = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Encoder.Location.Source"),
            IsChecked = pSource,
            VerticalAlignment = VerticalAlignment.Center
        };
        PCheckbox.PCheckboxApply(pBox);
        return pBox;
    }

    private static int PSVideoFpsResolve(string pText)
    {
        if (!double.TryParse(pText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double pRate)
            || !double.IsFinite(pRate))
        {
            return psVideoFpsDefault;
        }

        int pBest = 0;
        double pBestDiff = double.MaxValue;
        for (int pAt = 0; pAt < psVideoFpsScale.Length; pAt++)
        {
            double pDiff = Math.Abs(psVideoFpsScale[pAt].Rate - pRate);
            if (pDiff < pBestDiff)
            {
                pBestDiff = pDiff;
                pBest = pAt;
            }
        }

        return pBest;
    }

    // The editable field is authoritative and accepts any FFmpeg rate expression; the
    // slider only offers the ordered scale and writes its pick into the field.
    private UIElement PSVideoFpsBuild()
    {
        Slider pSlider = PSFieldSliderCreate(0, psVideoFpsScale.Length - 1, PSVideoFpsResolve(psVideoFpsCustom.Text));

        bool pSync = false;
        pSlider.ValueChanged += (_, _) =>
        {
            if (pSync)
            {
                return;
            }

            int pAt = Math.Clamp((int)Math.Round(pSlider.Value), 0, psVideoFpsScale.Length - 1);
            pSync = true;
            psVideoFpsCustom.Text = psVideoFpsScale[pAt].Value;
            psVideoFpsCustom.CaretIndex = psVideoFpsCustom.Text.Length;
            pSync = false;
        };
        psVideoFpsCustom.TextChanged += (_, _) =>
        {
            if (pSync)
            {
                return;
            }

            pSync = true;
            pSlider.Value = PSVideoFpsResolve(psVideoFpsCustom.Text);
            pSync = false;
        };

        return PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.CustomFps"),
            PSFieldRowBuild(pSlider, psVideoFpsCustom));
    }

    private void PSVideoFpsUpdate()
    {
        if (psVideoFpsRow is not null)
        {
            psVideoFpsRow.Visibility = psVideoFpsSource.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private string PSVideoFpsRead()
    {
        if (psVideoFpsSource.IsChecked == true)
        {
            return "Same as source";
        }

        string pValue = psVideoFpsCustom.Text.Trim();
        return pValue.Length == 0 ? "Same as source" : pValue;
    }

    private void PSVideoExtraBuild(LCapabilityCodec pCodec)
    {
        foreach (LCapabilityExtra pExtra in pCodec.LCapabilityExtraList)
        {
            string pSelected = lsExportSpecificEdit.LPresetVideo.LPresetExtras.TryGetValue(pExtra.CapabilityExtraOption, out string? pStored)
                               && pExtra.CapabilityExtraValues.Any(pChoice => string.Equals(pChoice.CapabilityChoiceValue, pStored, StringComparison.Ordinal))
                ? pStored
                : pExtra.CapabilityExtraDefault;

            ComboBox pCombo = PSComboBuild(pSelected, PSEncoderChoicesRead(pExtra.CapabilityExtraValues));
            psVideoExtraCombos[pExtra.CapabilityExtraOption] = pCombo;
            psVideoRowsPanel.Children.Add(PSFieldBuild(pExtra.CapabilityExtraLabel, pCombo));
        }
    }
}
