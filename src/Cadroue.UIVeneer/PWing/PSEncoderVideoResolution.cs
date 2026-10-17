using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private void PSVideoResolutionBuild(Panel pHost)
    {
        PSVideoSizeBuild(pHost);
        PSVideoSizeAttach();
        PSVideoSizeApply();
        PSVideoReactiveApply();
    }

    private void PSVideoSizeBuild(Panel pHost)
    {
        psVideoResolutionValue = new TextBlock
        {
            Foreground = PSField.PSFieldText,
            VerticalAlignment = VerticalAlignment.Center
        };
        psVideoResolutionSlider = PSFader.PSFaderCreate(0, QSEncoder.QSEncoderTiers.Count - 1, 0);
        psVideoWidthBox = PSEntry.PSEntryBuild(string.Empty, 110);
        psVideoHeightBox = PSEntry.PSEntryBuild(string.Empty, 110);
        psVideoWidthSlider = PSFader.PSFaderCreate(0, QSEncoder.QSEncoderDimensionMost, 0);
        psVideoHeightSlider = PSFader.PSFaderCreate(0, QSEncoder.QSEncoderDimensionMost, 0);
        psVideoWidthLabel = PSField.PSFieldLabelBuild(string.Empty);
        psVideoHeightLabel = PSField.PSFieldLabelBuild(string.Empty);

        pHost.Children.Add(PSField.PSFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.Size"),
            PSFader.PSFaderRowBuild(psVideoResolutionSlider, psVideoResolutionValue)));
        pHost.Children.Add(
            PSVideoDimensionBuild(
                psVideoWidthLabel,
                PSFader.PSFaderRowBuild(psVideoWidthSlider, psVideoWidthBox)));
        pHost.Children.Add(
            PSVideoDimensionBuild(
                psVideoHeightLabel,
                PSFader.PSFaderRowBuild(psVideoHeightSlider, psVideoHeightBox)));
        psVideoSizeNotice = PSNotice.PSNoticeBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Notice.SizeSource"));
        pHost.Children.Add(psVideoSizeNotice);
    }

    private void PSVideoSizeAttach()
    {
        psVideoResolutionSlider!.ValueChanged += (_, _) =>
            lsEncoder.QSEncoderTierSelect((int)Math.Round(psVideoResolutionSlider.Value));
        psVideoResolutionSlider.Loaded += (_, _) => PSVideoKnobApply();
        psVideoWidthSlider!.Loaded += (_, _) => PSVideoKnobApply();
        psVideoHeightSlider!.Loaded += (_, _) => PSVideoKnobApply();
        psVideoWidthSlider.ValueChanged += (_, _) => PSVideoDimensionChange(psVideoWidthSlider, psVideoWidthBox);
        psVideoHeightSlider.ValueChanged += (_, _) => PSVideoDimensionChange(psVideoHeightSlider, psVideoHeightBox);
        psVideoWidthBox!.TextChanged += (_, _) => PSVideoDimensionChange();
        psVideoHeightBox!.TextChanged += (_, _) => PSVideoDimensionChange();
    }

    private static UIElement PSVideoDimensionBuild(TextBlock pLabel, UIElement pContent)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSField.PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSField.PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(pLabel);
        Grid.SetColumn(pContent, 1);
        pGrid.Children.Add(pContent);
        return pGrid;
    }

    private static void PSVideoDimensionChange(Slider? pSlider, TextBox? pBox)
    {
        if (pSlider is null || pBox is null)
        {
            return;
        }

        string pText = PSVideoValueFormat(pSlider.Value);
        if (PSVideoTextCheck(pBox, pText))
        {
            PSVideoBoxSet(pBox, pText);
        }
    }

    private static string PSVideoValueFormat(double pRaw)
    {
        int pValue = (int)Math.Round(pRaw);
        return pValue > 0 ? pValue.ToString(CultureInfo.InvariantCulture) : string.Empty;
    }

    private static bool PSVideoTextCheck(TextBox pBox, string pText) =>
        !string.Equals(pBox.Text.Trim(), pText, StringComparison.Ordinal);

    private static void PSVideoBoxSet(TextBox pBox, string pText) => pBox.Text = pText;

    private void PSVideoDimensionChange()
    {
        if (psVideoWidthBox is null || psVideoHeightBox is null)
        {
            return;
        }

        lsEncoder.QSEncoderSizeSet(
            PSVideoDimensionRead(psVideoWidthBox),
            PSVideoDimensionRead(psVideoHeightBox));
    }

    private static int PSVideoDimensionRead(TextBox pBox) =>
        int.TryParse(pBox.Text.Trim(), out int pValue) && pValue > 0 ? pValue : 0;

    private static string PSVideoDimensionFormat(int pValue, bool pSource) =>
        pSource
            ? QLocalizationCatalog.QLocalizationTextRead("Encoder.Sample.Source")
            : pValue > 0 ? pValue.ToString(CultureInfo.InvariantCulture) : string.Empty;

    private void PSVideoSizeApply()
    {
        int pTier = lsEncoder.QSEncoderSizeTier;
        bool pSource = PSVideoNativeCheck(pTier);
        bool pCustom = PSVideoCustomCheck(pTier);
        PSVideoDimensionApply(psVideoWidthBox, () => lsEncoder.QSEncoderWidth, pSource);
        PSVideoDimensionApply(psVideoHeightBox, () => lsEncoder.QSEncoderHeight, pSource);
        PSVideoTierSet(pTier, pSource, pCustom);
        PSVideoHintSet(psVideoSizeNotice, pSource);
        PSVideoSlidersSet(pTier);
        PSVideoKnobApply();
    }

    private static bool PSVideoNativeCheck(int pTier) => pTier == 0;

    private static bool PSVideoCustomCheck(int pTier) => pTier < 0;

    private static void PSVideoDimensionApply(TextBox? pBox, Func<int> pValue, bool pSource)
    {
        if (pBox is null)
        {
            return;
        }

        PSVideoBoxSet(pBox, PSVideoDimensionFormat(pValue(), pSource));
        PSVideoInkApply(pBox, pSource);
    }

    private static string PSVideoTierFormat(int pTier, bool pSource, bool pCustom) =>
        pCustom
            ? QLocalizationCatalog.QLocalizationTextRead("Encoder.Value.Custom")
            : pSource
                ? QLocalizationCatalog.QLocalizationTextRead("Encoder.Sample.Source")
                : QSEncoder.QSEncoderTiers[pTier].QSEncoderTierLabel;

    private void PSVideoTierSet(int pTier, bool pSource, bool pCustom)
    {
        if (psVideoResolutionValue is not null)
        {
            psVideoResolutionValue.Text = PSVideoTierFormat(pTier, pSource, pCustom);
        }
    }

    private void PSVideoSlidersSet(int pTier)
    {
        if (pTier >= 0 && psVideoResolutionSlider is not null)
        {
            psVideoResolutionSlider.Value = pTier;
        }

        if (psVideoWidthSlider is not null)
        {
            psVideoWidthSlider.Value = PSVideoKnobClamp(lsEncoder.QSEncoderWidth);
        }

        if (psVideoHeightSlider is not null)
        {
            psVideoHeightSlider.Value = PSVideoKnobClamp(lsEncoder.QSEncoderHeight);
        }
    }

    private static int PSVideoKnobClamp(int pValue) => Math.Min(pValue, QSEncoder.QSEncoderDimensionMost);

    private void PSVideoKnobApply()
    {
        PSVideoThumbApply(psVideoResolutionSlider, !PSVideoCustomCheck(lsEncoder.QSEncoderSizeTier));
        bool pDimension = !PSVideoNativeCheck(lsEncoder.QSEncoderSizeTier);
        PSVideoThumbApply(psVideoWidthSlider, pDimension);
        PSVideoThumbApply(psVideoHeightSlider, pDimension);
    }

    private static void PSVideoThumbApply(Slider? pSlider, bool pVisible)
    {
        if (pSlider is null)
        {
            return;
        }

        pSlider.ApplyTemplate();
        if (pSlider.Template?.FindName("pSliderThumb", pSlider) is FrameworkElement pThumb)
        {
            pThumb.Visibility = pVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void PSVideoReactiveApply()
    {
        if (psVideoWidthLabel is null || psVideoHeightLabel is null)
        {
            return;
        }

        (string pWidthKey, string pHeightKey) = PSVideoAxisResolve();
        PSVideoAxisSet(psVideoWidthLabel, psVideoHeightLabel, pWidthKey, pHeightKey);
    }

    private (string, string) PSVideoAxisResolve()
    {
        bool pReactive = psVideoReactiveBox.IsChecked == true;
        return (
            pReactive ? "Encoder.Video.Field.AxisX" : "Encoder.Video.Field.Width",
            pReactive ? "Encoder.Video.Field.AxisY" : "Encoder.Video.Field.Height");
    }

    private static void PSVideoAxisSet(
        TextBlock pWidthLabel,
        TextBlock pHeightLabel,
        string pWidthKey,
        string pHeightKey)
    {
        pWidthLabel.Text = QLocalizationCatalog.QLocalizationTextRead(pWidthKey);
        pHeightLabel.Text = QLocalizationCatalog.QLocalizationTextRead(pHeightKey);
    }
}
