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

        psVideoResolutionSlider.ValueChanged += (_, _) =>
            lsEncoder.QSEncoderTierSelect((int)Math.Round(psVideoResolutionSlider.Value));
        psVideoResolutionSlider.Loaded += (_, _) => PSVideoKnobApply();
        psVideoWidthSlider.Loaded += (_, _) => PSVideoKnobApply();
        psVideoHeightSlider.Loaded += (_, _) => PSVideoKnobApply();
        psVideoWidthSlider.ValueChanged += (_, _) => PSVideoDimensionChange(psVideoWidthSlider, psVideoWidthBox);
        psVideoHeightSlider.ValueChanged += (_, _) => PSVideoDimensionChange(psVideoHeightSlider, psVideoHeightBox);
        psVideoWidthBox.TextChanged += (_, _) => PSVideoDimensionChange();
        psVideoHeightBox.TextChanged += (_, _) => PSVideoDimensionChange();

        PSVideoSizeApply();
        PSVideoReactiveApply();
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

        int pValue = (int)Math.Round(pSlider.Value);
        string pText = pValue > 0 ? pValue.ToString(CultureInfo.InvariantCulture) : string.Empty;
        if (!string.Equals(pBox.Text.Trim(), pText, StringComparison.Ordinal))
        {
            pBox.Text = pText;
        }
    }

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
        bool pSource = pTier == 0;
        bool pCustom = pTier < 0;

        if (psVideoWidthBox is not null)
        {
            psVideoWidthBox.Text = PSVideoDimensionFormat(lsEncoder.QSEncoderWidth, pSource);
            psVideoWidthBox.Foreground = pSource ? PSField.PSFieldMuted : PSField.PSFieldText;
        }

        if (psVideoHeightBox is not null)
        {
            psVideoHeightBox.Text = PSVideoDimensionFormat(lsEncoder.QSEncoderHeight, pSource);
            psVideoHeightBox.Foreground = pSource ? PSField.PSFieldMuted : PSField.PSFieldText;
        }

        if (psVideoResolutionValue is not null)
        {
            psVideoResolutionValue.Text = pCustom
                ? QLocalizationCatalog.QLocalizationTextRead("Encoder.Value.Custom")
                : pSource
                    ? QLocalizationCatalog.QLocalizationTextRead("Encoder.Sample.Source")
                    : QSEncoder.QSEncoderTiers[pTier].QSEncoderTierLabel;
        }

        if (psVideoSizeNotice is not null)
        {
            psVideoSizeNotice.Visibility = pSource ? Visibility.Visible : Visibility.Collapsed;
        }

        if (pTier >= 0 && psVideoResolutionSlider is not null)
        {
            psVideoResolutionSlider.Value = pTier;
        }

        if (psVideoWidthSlider is not null)
        {
            psVideoWidthSlider.Value = Math.Min(lsEncoder.QSEncoderWidth, QSEncoder.QSEncoderDimensionMost);
        }

        if (psVideoHeightSlider is not null)
        {
            psVideoHeightSlider.Value = Math.Min(lsEncoder.QSEncoderHeight, QSEncoder.QSEncoderDimensionMost);
        }

        PSVideoKnobApply();
    }

    private void PSVideoKnobApply()
    {
        PSVideoThumbApply(psVideoResolutionSlider, lsEncoder.QSEncoderSizeTier >= 0);
        bool pDimension = lsEncoder.QSEncoderSizeTier != 0;
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

        bool pReactive = psVideoReactiveBox.IsChecked == true;
        psVideoWidthLabel.Text = QLocalizationCatalog.QLocalizationTextRead(
            pReactive ? "Encoder.Video.Field.AxisX" : "Encoder.Video.Field.Width");
        psVideoHeightLabel.Text = QLocalizationCatalog.QLocalizationTextRead(
            pReactive ? "Encoder.Video.Field.AxisY" : "Encoder.Video.Field.Height");
    }
}
