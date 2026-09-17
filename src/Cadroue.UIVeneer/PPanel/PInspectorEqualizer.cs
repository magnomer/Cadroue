using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private const string pEqualizerAddIcon = "/PAsset/PPanel/PFunnelAdd.svg";

    private CheckBox pEqualizerApplyBox = null!;
    private CheckBox pEqualizerPersistent = null!;
    private ComboBox pEqualizerPreset = null!;
    private StackPanel pEqualizerStack = null!;
    private StackPanel pEqualizerRowPanel = null!;
    private StackPanel pEqualizerBody = null!;

    private StackPanel PEqualizerBodyBuild()
    {
        pEqualizerApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Equalizer.ApplyTooltip"));
        PInspectorSwitchAttach(pEqualizerApplyBox, LEqualizer.LEqualizerActiveSet);

        pEqualizerPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Equalizer.PersistentTooltip"));
        PInspectorSwitchAttach(pEqualizerPersistent, LEqualizer.LEqualizerPersistentSet);

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
        pEqualizerPreset.SelectionChanged += (_, _) =>
        {
            if (PInspectorPresetRead(pEqualizerPreset, LEqualizer.LEqualizerToken, LEqualizer.LEqualizerMatchRead())
                is { } pToken)
            {
                LEqualizer.LEqualizerPresetSelect(pToken);
            }
        };

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
        pAddButton.Click += (_, _) => LEqualizer.LEqualizerBandAdd();

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
        return pEqualizerBody;
    }

    private void PEqualizerUpdate()
    {
        PInspectorSwitchUpdate(pEqualizerApplyBox, LEqualizer.LEqualizerActive, false);
        PInspectorSwitchUpdate(pEqualizerPersistent, LEqualizer.LEqualizerPersistent, true);
        PEqualizerRowsUpdate();
        PInspectorPresetUpdate(
            pEqualizerPreset, LEqualizer.LEqualizerMatchRead(), LEqualizer.LEqualizerToken, PEqualizerKeyRead);
        PInspectorSectionUpdate(pEqualizerStack, LEqualizer.LEqualizerActive);
    }
}
