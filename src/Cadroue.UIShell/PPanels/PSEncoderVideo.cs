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
        pLog.Click += (_, _) => PSVerdict.PSVerdictShow(this, LLocalization.LLocalizationTextRead("Encoder.Verification.LogTitle"), psCodecResults);
        psVideoEncoderCombo.SelectionChanged += (_, _) => PSVideoChangeHandle();
        psVideoRateCombo.SelectionChanged += (_, _) => PSVideoRowsRebuild();

        psVideoEncodePanel.Children.Add(PSFieldButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.Encoder"), psVideoEncoderCombo, pVerify, pLog));
        psVideoEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.RateControl"), psVideoRateCombo));
        psVideoEncodePanel.Children.Add(psVideoRowsPanel);
        psVideoEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.Size"), psVideoSizeCombo));
        psVideoReactiveBox.Checked += (_, _) => PSVideoSizeUpdate();
        psVideoReactiveBox.Unchecked += (_, _) => PSVideoSizeUpdate();
        psVideoSizeCombo.SelectionChanged += (_, _) => PSVideoCustomUpdate();

        psVideoCustomRow = PSVideoCustomBuild();
        psVideoEncodePanel.Children.Add(psVideoCustomRow);
        psVideoEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.Reactive"), psVideoReactiveBox));
        psVideoEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.FPS"), psVideoFpsCombo));
        psVideoFpsCustomRow = PSFieldCustomBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.CustomFps"), psVideoFpsCustom);
        psVideoEncodePanel.Children.Add(psVideoFpsCustomRow);
        psVideoFpsCombo.SelectionChanged += (_, _) => PSFieldCustomToggle(psVideoFpsCombo, psVideoFpsCustomRow);
        PSFieldCustomToggle(psVideoFpsCombo, psVideoFpsCustomRow);
        PSVideoCustomUpdate();
        psVideoEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.PixelFormat"), psVideoPixelCombo));

        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.Stream"), psVideoStreamCombo));
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.Mode"), psVideoModeCombo));
        pPanel.Children.Add(psVideoEncodePanel);
        pPanel.Children.Add(psVideoNotice);

        psVideoStreamCombo.SelectionChanged += (_, _) => PSVideoScopeUpdate();
        psVideoModeCombo.SelectionChanged += (_, _) => PSVideoScopeUpdate();

        PSVideoRowsRebuild();
        PSVideoScopeUpdate();
        return PSPlateBuild(pPanel);
    }

    private static readonly string[] psVideoSizeItems =
        ["Same as source", "3840 × 2160", "2560 × 1440", "1920 × 1080", "1280 × 720", "854 × 480", "Custom"];

    private static readonly string[] psVideoReactiveItems =
        ["Same as source", "2160p", "1440p", "1080p", "720p", "480p", "Custom"];

    private static LLocalizationChoice[] PSVideoChoicesRead(bool pReactive)
    {
        string[] pTokens = pReactive ? psVideoReactiveItems : psVideoSizeItems;
        return pTokens.Select(pToken => pToken switch
        {
            "Same as source" => new LLocalizationChoice(pToken, "Encoder.Location.Source"),
            "Custom" => new LLocalizationChoice(pToken, "Encoder.Value.Custom"),
            _ => new LLocalizationChoice(pToken)
        }).ToArray();
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

    private string PSVideoSizeRead(string pLabel)
    {
        if (string.Equals(pLabel, "Custom", StringComparison.Ordinal))
        {
            return PSVideoCustomRead();
        }

        int pIndex = Array.IndexOf(psVideoReactiveItems, pLabel);
        return pIndex < 0 ? pLabel : psVideoSizeItems[pIndex];
    }

    private static string PSVideoLabelRead(string pSize, bool pReactive)
    {
        int pIndex = Array.IndexOf(psVideoSizeItems, pSize);
        if (pIndex < 0)
        {
            return "Custom";
        }

        return pReactive ? psVideoReactiveItems[pIndex] : psVideoSizeItems[pIndex];
    }

    private string PSVideoCustomRead()
    {
        if (int.TryParse(psVideoCustomWidth.Text.Trim(), out int pWidth) && pWidth > 0
            && int.TryParse(psVideoCustomHeight.Text.Trim(), out int pHeight) && pHeight > 0)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{pWidth} × {pHeight}");
        }

        return "Same as source";
    }

    private UIElement PSVideoCustomBuild()
    {
        psVideoCustomWidth.MinHeight = PSFieldControlHeight;
        psVideoCustomHeight.MinHeight = PSFieldControlHeight;

        var pPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        pPanel.Children.Add(psVideoCustomWidth);

        TextBlock pSeparator = PSFieldLabelBuild("×");
        pSeparator.Margin = new Thickness(8, 0, 8, 0);
        pPanel.Children.Add(pSeparator);
        pPanel.Children.Add(psVideoCustomHeight);

        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSFieldLabelBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Field.CustomSize")));
        Grid.SetColumn(pPanel, 1);
        pGrid.Children.Add(pPanel);
        return pGrid;
    }

    private void PSVideoCustomUpdate()
    {
        if (psVideoCustomRow is null)
        {
            return;
        }

        psVideoCustomRow.Visibility = string.Equals(PSComboTextRead(psVideoSizeCombo), "Custom", StringComparison.Ordinal)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void PSVideoSizeUpdate()
    {
        bool pReactive = psVideoReactiveBox.IsChecked == true;
        string pSize = PSVideoSizeRead(PSComboTextRead(psVideoSizeCombo));

        LLocalizationChoice[] pChoices = PSVideoChoicesRead(pReactive);
        string pSelected = PSVideoLabelRead(pSize, pReactive);
        psVideoSizeCombo.ItemsSource = pChoices;
        psVideoSizeCombo.SelectedItem = pChoices.FirstOrDefault(
            pChoice => string.Equals(pChoice.LLocalizationChoiceToken, pSelected, StringComparison.Ordinal));
        PSVideoCustomUpdate();
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
            ? LLocalization.LLocalizationTextRead("Encoder.Video.Notice.Excluded")
            : LLocalization.LLocalizationTextRead("Encoder.Video.Notice.Copied");
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
        LCapabilityMode pMode = pCodec.LCapabilityModeFind(PSComboTextRead(psVideoRateCombo));
        bool pModeStored = string.Equals(pMode.CapabilityModeLabel, lsExportSpecificEdit.LPresetRateControl, StringComparison.Ordinal);

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

        string pStored = lsExportSpecificEdit.LPresetVideoQuality;
        string pText = pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pQuality.CapabilityQualityDefault;

        psVideoQualityBox = PSEntryBuild(pText, 120);
        if (pQuality.CapabilityQualityMinimum is double pMinimum && pQuality.CapabilityQualityMaximum is double pMaximum)
        {
            UIElement pSliderRow = pQuality.LCapabilityQualityBitrate
                ? PSFieldBitrateBuild(pMinimum, pMaximum, pText, psVideoQualityBox)
                : PSFieldSliderBuild(pMinimum, pMaximum, pQuality.LCapabilityQualityStep, pText, psVideoQualityBox);
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

    private void PSVideoSpeedBuild(LCapabilityCodec pCodec, bool pModeStored)
    {
        if (pCodec.CapabilitySpeed is not LCapabilitySpeed pSpeed)
        {
            return;
        }

        string pStored = lsExportSpecificEdit.LPresetSpeedPreset;
        string pSelected = pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pSpeed.CapabilitySpeedDefault;

        psVideoSpeedCombo = PSComboBuild(pSelected, PSEncoderChoicesRead(pSpeed.CapabilitySpeedValues));
        psVideoRowsPanel.Children.Add(PSFieldBuild(pSpeed.CapabilitySpeedLabel, psVideoSpeedCombo));
    }

    private void PSVideoExtraBuild(LCapabilityCodec pCodec)
    {
        foreach (LCapabilityExtra pExtra in pCodec.LCapabilityExtraList)
        {
            string pSelected = lsExportSpecificEdit.LPresetVideoExtras.TryGetValue(pExtra.CapabilityExtraOption, out string? pStored)
                               && pExtra.CapabilityExtraValues.Any(pChoice => string.Equals(pChoice.CapabilityChoiceValue, pStored, StringComparison.Ordinal))
                ? pStored
                : pExtra.CapabilityExtraDefault;

            ComboBox pCombo = PSComboBuild(pSelected, PSEncoderChoicesRead(pExtra.CapabilityExtraValues));
            psVideoExtraCombos[pExtra.CapabilityExtraOption] = pCombo;
            psVideoRowsPanel.Children.Add(PSFieldBuild(pExtra.CapabilityExtraLabel, pCombo));
        }
    }
}
