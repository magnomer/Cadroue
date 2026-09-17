using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;

using static Cadroue.UIVeneer.PSCasement.PSField;
using static Cadroue.UIVeneer.PSCasement.PSFader;
using static Cadroue.UIVeneer.PSCasement.PSEntry;
using static Cadroue.UIVeneer.PSCasement.PSNotice;

namespace Cadroue.UIVeneer.PPanel;

internal sealed partial class PSEncoder
{
    private void PSVideoResolutionBuild(Panel pHost)
    {
        psVideoResolutionValue = new TextBlock
        {
            Foreground = PSFieldText,
            VerticalAlignment = VerticalAlignment.Center
        };
        psVideoResolutionSlider = PSFaderCreate(0, LSEncoder.LSEncoderTiers.Count - 1, 0);
        psVideoWidthBox = PSEntryBuild(string.Empty, 110);
        psVideoHeightBox = PSEntryBuild(string.Empty, 110);
        psVideoWidthSlider = PSFaderCreate(0, LSEncoder.LSEncoderDimensionMost, 0);
        psVideoHeightSlider = PSFaderCreate(0, LSEncoder.LSEncoderDimensionMost, 0);
        psVideoWidthLabel = PSFieldLabelBuild(string.Empty);
        psVideoHeightLabel = PSFieldLabelBuild(string.Empty);

        pHost.Children.Add(PSFieldBuild(
            LLocalization.LLocalizationTextRead("Encoder.Video.Field.Size"),
            PSFaderRowBuild(psVideoResolutionSlider, psVideoResolutionValue)));
        pHost.Children.Add(
            PSVideoDimensionBuild(
                psVideoWidthLabel,
                PSFaderRowBuild(psVideoWidthSlider, psVideoWidthBox)));
        pHost.Children.Add(
            PSVideoDimensionBuild(
                psVideoHeightLabel,
                PSFaderRowBuild(psVideoHeightSlider, psVideoHeightBox)));
        psVideoSizeNotice = PSNoticeBuild(LLocalization.LLocalizationTextRead("Encoder.Video.Notice.SizeSource"));
        pHost.Children.Add(psVideoSizeNotice);

        psVideoResolutionSlider.ValueChanged += (_, _) =>
            lsEncoder.LSEncoderTierSelect((int)Math.Round(psVideoResolutionSlider.Value));
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
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
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

        lsEncoder.LSEncoderSizeSet(
            PSVideoDimensionRead(psVideoWidthBox),
            PSVideoDimensionRead(psVideoHeightBox));
    }

    private static int PSVideoDimensionRead(TextBox pBox) =>
        int.TryParse(pBox.Text.Trim(), out int pValue) && pValue > 0 ? pValue : 0;

    private static string PSVideoDimensionFormat(int pValue, bool pSource) =>
        pSource
            ? LLocalization.LLocalizationTextRead("Encoder.Sample.Source")
            : pValue > 0 ? pValue.ToString(CultureInfo.InvariantCulture) : string.Empty;

    private void PSVideoSizeApply()
    {
        int pTier = lsEncoder.LSEncoderSizeTier;
        bool pSource = pTier == 0;
        bool pCustom = pTier < 0;

        if (psVideoWidthBox is not null)
        {
            psVideoWidthBox.Text = PSVideoDimensionFormat(lsEncoder.LSEncoderWidth, pSource);
            psVideoWidthBox.Foreground = pSource ? PSFieldMuted : PSFieldText;
        }

        if (psVideoHeightBox is not null)
        {
            psVideoHeightBox.Text = PSVideoDimensionFormat(lsEncoder.LSEncoderHeight, pSource);
            psVideoHeightBox.Foreground = pSource ? PSFieldMuted : PSFieldText;
        }

        if (psVideoResolutionValue is not null)
        {
            psVideoResolutionValue.Text = pCustom
                ? LLocalization.LLocalizationTextRead("Encoder.Value.Custom")
                : pSource
                    ? LLocalization.LLocalizationTextRead("Encoder.Sample.Source")
                    : LSEncoder.LSEncoderTiers[pTier].LSEncoderTierLabel;
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
            psVideoWidthSlider.Value = Math.Min(lsEncoder.LSEncoderWidth, LSEncoder.LSEncoderDimensionMost);
        }

        if (psVideoHeightSlider is not null)
        {
            psVideoHeightSlider.Value = Math.Min(lsEncoder.LSEncoderHeight, LSEncoder.LSEncoderDimensionMost);
        }

        PSVideoKnobApply();
    }

    private void PSVideoKnobApply()
    {
        PSVideoThumbApply(psVideoResolutionSlider, lsEncoder.LSEncoderSizeTier >= 0);
        bool pDimension = lsEncoder.LSEncoderSizeTier != 0;
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
        psVideoWidthLabel.Text = LLocalization.LLocalizationTextRead(
            pReactive ? "Encoder.Video.Field.AxisX" : "Encoder.Video.Field.Width");
        psVideoHeightLabel.Text = LLocalization.LLocalizationTextRead(
            pReactive ? "Encoder.Video.Field.AxisY" : "Encoder.Video.Field.Height");
    }
}
