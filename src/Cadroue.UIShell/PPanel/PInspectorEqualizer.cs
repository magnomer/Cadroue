using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PAsset;
using Cadroue.UIShell.PHouse;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private const string pEqualizerAddIcon = "/PAsset/PPanel/PFunnelAdd.svg";

    private CheckBox pEqualizerApplyBox = null!;
    private CheckBox pEqualizerPersistent = null!;
    private ComboBox pEqualizerPreset = null!;
    private StackPanel pEqualizerStack = null!;
    private StackPanel pEqualizerRowPanel = null!;
    private StackPanel pEqualizerBody = null!;

    public LWorkAudioStep PEqualizerStepRead()
    {
        var pBands = new List<LWorkBand>();
        foreach (PInspectorBand pRow in pEqualizerRows)
        {
            double pFrequency = PInspectorDecimalRead(
                pRow.PInspectorBandFrequency, LContourCatalog.LContourFrequencyDefault);
            double pGain = PInspectorDecimalRead(pRow.PInspectorBandValue, 0);
            pBands.Add(new LWorkBand(pFrequency, pGain));
        }

        return LWorkAudioStep.LWorkEqualizerCreate(pEqualizerApplyBox.IsChecked == true, pBands);
    }

    private void PEqualizerActiveSet(LWorkEqualizerStep pStep)
    {
        pEqualizerApplyBox.IsChecked = pStep.LWorkStepActive;
        pEqualizerRows.Clear();
        pEqualizerRowPanel.Children.Clear();
        foreach (LWorkBand pBand in pStep.LWorkEqualizerBands)
        {
            PEqualizerRowAdd(pBand.LWorkBandFrequency, pBand.LWorkBandGain, false);
        }

        PEqualizerPresetUpdate();
        PEqualizerApplyUpdate();
    }

    private StackPanel PEqualizerBodyBuild()
    {
        pEqualizerApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Equalizer.ApplyTooltip"));
        pEqualizerApplyBox.Checked += (_, _) => PEqualizerApplyUpdate();
        pEqualizerApplyBox.Unchecked += (_, _) => PEqualizerApplyUpdate();

        pEqualizerPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Equalizer.PersistentTooltip"));

        pEqualizerPreset = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pEqualizerPreset);
        foreach (string pToken in LContourCatalog.LContourTokensRead())
        {
            pEqualizerPreset.Items.Add(new LLocalizationChoice(pToken, PEqualizerKeyRead(pToken)));
        }

        pEqualizerPreset.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        pEqualizerPreset.SelectedIndex = 0;
        pEqualizerPreset.SelectionChanged += (_, _) => PEqualizerPresetApply();

        pEqualizerRowPanel = new StackPanel();

        Button pAddButton = new()
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pEqualizerAddIcon, pInspectorIconBrush),
                Stretch = Stretch.Uniform
            },
            Width = 28,
            Height = 26,
            HorizontalAlignment = HorizontalAlignment.Left,
            Style = PButton.PButtonPanelCreate(),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Equalizer.Add")
        };
        pAddButton.Click += (_, _) =>
        {
            PEqualizerRowAdd(LContourCatalog.LContourFrequencyDefault, 0, true);
            PEqualizerDeviationCheck();
        };

        pEqualizerStack = new StackPanel();
        pEqualizerStack.Children.Add(PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pEqualizerPreset));
        pEqualizerStack.Children.Add(pEqualizerRowPanel);
        pEqualizerStack.Children.Add(pAddButton);

        pEqualizerBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pEqualizerBody.Children.Add(pEqualizerApplyBox);
        pEqualizerBody.Children.Add(PInspectorSeparatorBuild());
        pEqualizerBody.Children.Add(pEqualizerStack);

        foreach (LWorkBand pBand in LWorkEqualizerStep.LWorkBandsCreate())
        {
            PEqualizerRowAdd(pBand.LWorkBandFrequency, pBand.LWorkBandGain, false);
        }

        PEqualizerPresetUpdate();
        PEqualizerApplyUpdate();
        return pEqualizerBody;
    }

    private void PEqualizerApplyUpdate()
    {
        bool pEqualizerActive = pEqualizerApplyBox.IsChecked == true;
        pEqualizerStack.IsEnabled = pEqualizerActive;
        pEqualizerStack.Opacity = pEqualizerActive ? 1 : 0.4;
        PInspectorActiveRaise();
    }
}
