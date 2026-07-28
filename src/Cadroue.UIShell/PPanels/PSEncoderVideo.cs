using System.Globalization;
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
        psVideoReactiveBox.Checked += (_, _) => PSVideoSizeUpdate();
        psVideoReactiveBox.Unchecked += (_, _) => PSVideoSizeUpdate();
        psVideoSizeCombo.SelectionChanged += (_, _) => PSVideoCustomUpdate();

        psVideoCustomRow = PSVideoCustomBuild();
        psVideoEncodePanel.Children.Add(psVideoCustomRow);
        psVideoEncodePanel.Children.Add(PSFieldBuild("Reactive", psVideoReactiveBox));
        psVideoEncodePanel.Children.Add(PSFieldBuild("FPS", psVideoFpsCombo));
        PSVideoCustomUpdate();
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

    private static readonly string[] psVideoSizeItems =
        ["Same as source", "3840 × 2160", "2560 × 1440", "1920 × 1080", "1280 × 720", "854 × 480", "Custom"];

    private static readonly string[] psVideoReactiveItems =
        ["Same as source", "2160p", "1440p", "1080p", "720p", "480p", "Custom"];

    private static CheckBox PSVideoReactiveBuild(bool pReactive) => new()
    {
        Content = "Match the output orientation to the clip",
        IsChecked = pReactive,
        VerticalAlignment = VerticalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center
    };

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
        psVideoCustomWidth.MinHeight = PSSheetControlHeight;
        psVideoCustomHeight.MinHeight = PSSheetControlHeight;

        var pPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        pPanel.Children.Add(psVideoCustomWidth);

        TextBlock pSeparator = PSSheetLabelBuild("×");
        pSeparator.Margin = new Thickness(8, 0, 8, 0);
        pPanel.Children.Add(pSeparator);
        pPanel.Children.Add(psVideoCustomHeight);

        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSSheetLabelBuild("Custom size"));
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

        psVideoSizeCombo.ItemsSource = pReactive ? psVideoReactiveItems : psVideoSizeItems;
        psVideoSizeCombo.SelectedItem = PSVideoLabelRead(pSize, pReactive);
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
