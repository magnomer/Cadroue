using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private CheckBox pVolumeApplyBox = null!;
    private CheckBox pInspectorVolumePersistent = null!;
    private Slider pInspectorVolumeSlider = null!;
    private TextBox pInspectorVolumeValue = null!;
    private StackPanel pInspectorVolumeStack = null!;
    private TextBlock pInspectorVolumeWarn = null!;
    private StackPanel pInspectorVolumeBody = null!;

    private StackPanel PVolumeBodyBuild()
    {
        pVolumeApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Volume.ApplyTooltip"));
        PInspectorSwitchAttach(pVolumeApplyBox, LVolume.LVolumeActiveSet);

        pInspectorVolumePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Volume.PersistentTooltip"));
        PInspectorSwitchAttach(pInspectorVolumePersistent, LVolume.LVolumePersistentSet);

        pInspectorVolumeSlider = new Slider
        {
            Minimum = LWorkAudio.LWorkGainLeast,
            Maximum = LWorkAudio.LWorkGainMost,
            Value = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pInspectorVolumeSlider);
        PSlider.PSliderResetApply(pInspectorVolumeSlider, static () => 0);

        pInspectorVolumeValue = PInspectorDecimalBuild();
        pInspectorVolumeValue.Text = "0";
        PInspectorValueAttach(
            pInspectorVolumeSlider,
            pInspectorVolumeValue,
            LWorkAudio.LWorkGainLeast,
            LWorkAudio.LWorkGainMost,
            () => LVolume.LVolumeGain,
            LVolume.LVolumeGainSet);

        var pGainRow = new Grid();
        pGainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TextBlock pGainLabel = PInspectorLabelBuild(LLocalization.LLocalizationTextRead("Inspector.Volume.Gain"));
        var pGainUnit = new TextBlock
        {
            Text = "dB",
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0)
        };

        Grid.SetColumn(pGainLabel, 0);
        Grid.SetColumn(pInspectorVolumeSlider, 1);
        Grid.SetColumn(pGainUnit, 2);
        Grid.SetColumn(pInspectorVolumeValue, 3);
        pGainRow.Children.Add(pGainLabel);
        pGainRow.Children.Add(pInspectorVolumeSlider);
        pGainRow.Children.Add(pGainUnit);
        pGainRow.Children.Add(pInspectorVolumeValue);

        pInspectorVolumeWarn = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Volume.ClipNotice"),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorWarnBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(PInspectorLabelWidth, 6, 0, 0),
            Visibility = Visibility.Collapsed
        };

        pInspectorVolumeStack = new StackPanel();
        pInspectorVolumeStack.Children.Add(pGainRow);
        pInspectorVolumeStack.Children.Add(pInspectorVolumeWarn);

        pInspectorVolumeBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pInspectorVolumeBody.Children.Add(pVolumeApplyBox);
        pInspectorVolumeBody.Children.Add(PInspectorSeparatorBuild());
        pInspectorVolumeBody.Children.Add(pInspectorVolumeStack);
        return pInspectorVolumeBody;
    }

    private void PVolumeUpdate()
    {
        PInspectorSwitchUpdate(pVolumeApplyBox, LVolume.LVolumeStep.LWorkStepActive, false);
        PInspectorSwitchUpdate(pInspectorVolumePersistent, LVolume.LVolumePersistent, true);
        PInspectorValueUpdate(pInspectorVolumeSlider, pInspectorVolumeValue, LVolume.LVolumeGain, "0.#");
        PInspectorSectionUpdate(pInspectorVolumeStack, LVolume.LVolumeStep.LWorkStepActive);
        pInspectorVolumeWarn.Visibility = LVolume.LVolumeGain > 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
