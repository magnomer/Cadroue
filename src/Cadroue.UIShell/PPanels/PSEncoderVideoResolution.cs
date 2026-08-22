using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private static readonly (string Label, int Width, int Height)[] psVideoSizeStops =
    [
        ("Source", 0, 0),
        ("480p", 854, 480),
        ("720p", 1280, 720),
        ("1080p", 1920, 1080),
        ("1440p", 2560, 1440),
        ("4K", 3840, 2160),
        ("8K", 7680, 4320)
    ];

    private void PSVideoResolutionBuild(Panel pHost)
    {
        psVideoResolutionValue = new TextBlock { Foreground = PSFieldText, VerticalAlignment = VerticalAlignment.Center };
        psVideoResolutionSlider = PSFieldSliderCreate(0, psVideoSizeStops.Length - 1, 0);
        psVideoWidthBox = PSEntryBuild(string.Empty, 110);
        psVideoHeightBox = PSEntryBuild(string.Empty, 110);
        psVideoWidthLabel = PSFieldLabelBuild(string.Empty);
        psVideoHeightLabel = PSFieldLabelBuild(string.Empty);

        pHost.Children.Add(PSFieldBuild(
            LLocalization.LLocalizationTextRead("Encoder.Video.Field.Size"),
            PSFieldRowBuild(psVideoResolutionSlider, psVideoResolutionValue)));
        pHost.Children.Add(PSVideoDimensionRow(psVideoWidthLabel, psVideoWidthBox));
        pHost.Children.Add(PSVideoDimensionRow(psVideoHeightLabel, psVideoHeightBox));

        psVideoResolutionSlider.ValueChanged += (_, _) => PSVideoStopSelect();
        psVideoResolutionSlider.Loaded += (_, _) => PSVideoKnobApply();
        psVideoWidthBox.TextChanged += (_, _) => PSVideoDimensionChange();
        psVideoHeightBox.TextChanged += (_, _) => PSVideoDimensionChange();

        PSVideoSizeSeed();
        PSVideoReactiveApply();
    }

    private static UIElement PSVideoDimensionRow(TextBlock pLabel, TextBox pBox)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(pLabel);
        pBox.MinHeight = PSFieldControlHeight;
        Grid.SetColumn(pBox, 1);
        pGrid.Children.Add(pBox);
        return pGrid;
    }

    private void PSVideoSizeSeed()
    {
        string[] pParts = lsExportSpecificEdit.LPresetVideo.LPresetSize.Split(
            ['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pParts.Length == 2
            && int.TryParse(pParts[0], out int pWidth) && pWidth > 0
            && int.TryParse(pParts[1], out int pHeight) && pHeight > 0)
        {
            psVideoSizeBusy = true;
            psVideoWidthBox!.Text = pWidth.ToString(CultureInfo.InvariantCulture);
            psVideoHeightBox!.Text = pHeight.ToString(CultureInfo.InvariantCulture);
            psVideoSizeBusy = false;
            PSVideoDimensionChange();
        }
        else
        {
            PSVideoStateApply(0);
        }
    }

    private void PSVideoStopSelect()
    {
        if (psVideoSizeBusy || psVideoResolutionSlider is null)
        {
            return;
        }

        int pStop = Math.Clamp((int)Math.Round(psVideoResolutionSlider.Value), 0, psVideoSizeStops.Length - 1);

        psVideoSizeBusy = true;
        if (pStop == 0)
        {
            psVideoWidthBox!.Text = string.Empty;
            psVideoHeightBox!.Text = string.Empty;
        }
        else
        {
            psVideoWidthBox!.Text = psVideoSizeStops[pStop].Width.ToString(CultureInfo.InvariantCulture);
            psVideoHeightBox!.Text = psVideoSizeStops[pStop].Height.ToString(CultureInfo.InvariantCulture);
        }
        psVideoSizeBusy = false;

        PSVideoStateApply(pStop);
    }

    private void PSVideoDimensionChange()
    {
        if (psVideoSizeBusy || psVideoWidthBox is null || psVideoHeightBox is null)
        {
            return;
        }

        bool pWidthOk = int.TryParse(psVideoWidthBox.Text.Trim(), out int pWidth) && pWidth > 0;
        bool pHeightOk = int.TryParse(psVideoHeightBox.Text.Trim(), out int pHeight) && pHeight > 0;

        int pStop;
        if (!pWidthOk && !pHeightOk)
        {
            pStop = 0;
        }
        else if (pWidthOk && pHeightOk)
        {
            pStop = PSVideoStopMatch(pWidth, pHeight);
        }
        else
        {
            pStop = -1;
        }

        if (pStop >= 0 && psVideoResolutionSlider is not null)
        {
            psVideoSizeBusy = true;
            psVideoResolutionSlider.Value = pStop;
            psVideoSizeBusy = false;
        }

        PSVideoStateApply(pStop);
    }

    private static int PSVideoStopMatch(int pWidth, int pHeight)
    {
        for (int pAt = 1; pAt < psVideoSizeStops.Length; pAt++)
        {
            if (psVideoSizeStops[pAt].Width == pWidth && psVideoSizeStops[pAt].Height == pHeight)
            {
                return pAt;
            }
        }

        return -1;
    }

    private void PSVideoStateApply(int pStop)
    {
        psVideoSizeStop = pStop;
        bool pSource = pStop == 0;
        bool pCustom = pStop < 0;

        if (psVideoResolutionValue is not null)
        {
            if (pCustom)
            {
                psVideoResolutionValue.Text = LLocalization.LLocalizationTextRead("Encoder.Value.Custom");
            }
            else if (pSource)
            {
                psVideoResolutionValue.Text = LLocalization.LLocalizationTextRead("Encoder.Location.Source");
            }
            else
            {
                psVideoResolutionValue.Text = psVideoSizeStops[pStop].Label;
            }
        }

        Brush pForeground = pSource ? PSFieldMuted : PSFieldText;
        if (psVideoWidthBox is not null)
        {
            psVideoWidthBox.Foreground = pForeground;
        }

        if (psVideoHeightBox is not null)
        {
            psVideoHeightBox.Foreground = pForeground;
        }

        PSVideoKnobApply();
    }

    private void PSVideoKnobApply()
    {
        if (psVideoResolutionSlider is null)
        {
            return;
        }

        psVideoResolutionSlider.ApplyTemplate();
        if (psVideoResolutionSlider.Template?.FindName("pSliderThumb", psVideoResolutionSlider) is FrameworkElement pThumb)
        {
            pThumb.Visibility = psVideoSizeStop < 0 ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void PSVideoReactiveApply()
    {
        if (psVideoWidthLabel is null || psVideoHeightLabel is null)
        {
            return;
        }

        bool pReactive = psVideoReactiveBox.IsChecked == true;
        psVideoWidthLabel.Text = LLocalization.LLocalizationTextRead(pReactive ? "Encoder.Video.Field.AxisX" : "Encoder.Video.Field.Width");
        psVideoHeightLabel.Text = LLocalization.LLocalizationTextRead(pReactive ? "Encoder.Video.Field.AxisY" : "Encoder.Video.Field.Height");
    }

    private string PSVideoSizeRead()
    {
        if (psVideoSizeStop == 0 || psVideoWidthBox is null || psVideoHeightBox is null)
        {
            return "Same as source";
        }

        if (int.TryParse(psVideoWidthBox.Text.Trim(), out int pWidth) && pWidth > 0
            && int.TryParse(psVideoHeightBox.Text.Trim(), out int pHeight) && pHeight > 0)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{pWidth} × {pHeight}");
        }

        return "Same as source";
    }
}
