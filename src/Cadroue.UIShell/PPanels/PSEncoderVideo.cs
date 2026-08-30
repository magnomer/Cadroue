using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

using static Cadroue.UIShell.PSShared.PSField;
using static Cadroue.UIShell.PSShared.PSCombo;
using static Cadroue.UIShell.PSShared.PSNotice;
using static Cadroue.UIShell.PSShared.PSEntry;
using static Cadroue.UIShell.PSShared.PSFader;
using static Cadroue.UIShell.PSShared.PSInline;
using static Cadroue.UIShell.PSShared.PSPlate;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private UIElement PSVideoPlateBuild()
    {
        var pPanel = new StackPanel();
        var pVerify = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Button.Verify"), 84, new Thickness(8, 0, 0, 0));
        var pLog = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Button.Result"), 64, new Thickness(6, 0, 0, 0));
        pLog.IsEnabled = psCodecResults.Count > 0;
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

            pLog.IsEnabled = psCodecResults.Count > 0;
        };
        pLog.Click += (_, _) => PSVerdict.PSVerdictShow(this, LLocalization.LLocalizationTextRead("Encoder.Verification.VideoTitle"), psCodecResults);
        psVideoEncoderCombo.SelectionChanged += (_, _) => PSVideoChangeHandle();
        psVideoRateCombo.SelectionChanged += (_, _) => PSVideoRowsRebuild();

        psVideoEncodePanel.Children.Add(PSFieldButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.Encoder"), psVideoEncoderCombo, pVerify, pLog, pProgress));
        psVideoEncodePanel.Children.Add(psVideoEncoderNotice);
        psVideoEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.RateControl"), psVideoRateCombo));
        psVideoEncodePanel.Children.Add(psVideoRowsPanel);
        PSVideoResolutionBuild(psVideoEncodePanel);
        psVideoReactiveBox.Checked += (_, _) => PSVideoReactiveApply();
        psVideoReactiveBox.Unchecked += (_, _) => PSVideoReactiveApply();
        psVideoEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.Reactive"), psVideoReactiveBox));
        PSVideoFpsBuild(psVideoEncodePanel);
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

        return psEncoderSmart ? "Encoder.Video.Notice.Smart" : "Encoder.Video.Notice.SmartFull";
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

        if (!string.IsNullOrWhiteSpace(pCodec.CapabilityNotice))
        {
            psVideoRowsPanel.Children.Add(PSNoticeBuild(pCodec.CapabilityNotice));
        }

        PSVideoSpeedBuild(pCodec, pModeStored);
        PSVideoExtraBuild(pCodec);
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
                ? PSFaderBitrateBuild(pMinimum, pMaximum, pText, psVideoQualityBox)
                : PSFaderQualityBuild(pMinimum, pMaximum, pQuality.LCapabilityQualityStep, pText, psVideoQualityBox, pQuality.CapabilityQualityHigherBetter);
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

        Slider pSlider = PSFaderCreate(0, pLast, pLast - pIndex);
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

        psVideoRowsPanel.Children.Add(PSFieldBuild(pSpeed.CapabilitySpeedLabel, PSFaderRowBuild(pSlider, pText)));
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

    // Index 0 is the Source sentinel; the remaining entries are the ordered rate scale.
    private static readonly (double Rate, string Value)[] psVideoFpsScale = PSVideoScaleCreate();

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

        var pSorted = pList.OrderBy(pEntry => pEntry.Rate).ToList();
        pSorted.Insert(0, (0, string.Empty));
        return pSorted.ToArray();
    }

    private static bool PSVideoSourceCheck(string pFps)
    {
        string pTrim = pFps.Trim();
        return pTrim.Length == 0
            || string.Equals(pTrim, "Same as source", StringComparison.Ordinal)
            || string.Equals(pTrim, LLocalization.LLocalizationTextRead("Encoder.Sample.Source"), StringComparison.Ordinal);
    }

    private static int PSVideoFpsResolve(string pText)
    {
        if (PSVideoSourceCheck(pText)
            || !double.TryParse(pText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double pRate)
            || !double.IsFinite(pRate) || pRate <= 0)
        {
            return 0;
        }

        int pBest = 1;
        double pBestDiff = double.MaxValue;
        for (int pAt = 1; pAt < psVideoFpsScale.Length; pAt++)
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

    // The editable field is authoritative and accepts any FFmpeg rate expression; moving the
    // slider overwrites it with the picked scale value, and slider index 0 means source, which
    // reveals the explanatory notice below the row.
    private void PSVideoFpsBuild(Panel pHost)
    {
        psVideoFpsNotice = PSNoticeBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Notice.FpsSource"));

        if (PSVideoSourceCheck(psVideoFpsCustom.Text))
        {
            PSVideoFpsApply();
        }
        else
        {
            psVideoFpsNotice.Visibility = Visibility.Collapsed;
        }

        Slider pSlider = PSFaderCreate(0, psVideoFpsScale.Length - 1, PSVideoFpsResolve(psVideoFpsCustom.Text));

        bool pSync = false;
        pSlider.ValueChanged += (_, _) =>
        {
            if (pSync)
            {
                return;
            }

            int pAt = Math.Clamp((int)Math.Round(pSlider.Value), 0, psVideoFpsScale.Length - 1);
            pSync = true;
            if (pAt == 0)
            {
                PSVideoFpsApply();
            }
            else
            {
                psVideoFpsCustom.Text = psVideoFpsScale[pAt].Value;
                psVideoFpsCustom.Foreground = PSFieldText;
                psVideoFpsNotice.Visibility = Visibility.Collapsed;
            }
            psVideoFpsCustom.CaretIndex = psVideoFpsCustom.Text.Length;
            pSync = false;
        };
        psVideoFpsCustom.TextChanged += (_, _) =>
        {
            if (pSync)
            {
                return;
            }

            bool pSource = PSVideoSourceCheck(psVideoFpsCustom.Text);
            pSync = true;
            pSlider.Value = PSVideoFpsResolve(psVideoFpsCustom.Text);
            psVideoFpsCustom.Foreground = pSource ? PSFieldMuted : PSFieldText;
            psVideoFpsNotice.Visibility = pSource ? Visibility.Visible : Visibility.Collapsed;
            pSync = false;
        };

        pHost.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.FPS"),
            PSFaderRowBuild(pSlider, psVideoFpsCustom)));
        pHost.Children.Add(psVideoFpsNotice);
    }

    private void PSVideoFpsApply()
    {
        psVideoFpsCustom.Text = LLocalization.LLocalizationTextRead("Encoder.Sample.Source");
        psVideoFpsCustom.Foreground = PSFieldMuted;
        if (psVideoFpsNotice is not null)
        {
            psVideoFpsNotice.Visibility = Visibility.Visible;
        }
    }

    private string PSVideoFpsRead()
    {
        string pValue = psVideoFpsCustom.Text.Trim();
        return PSVideoSourceCheck(pValue) ? "Same as source" : pValue;
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
