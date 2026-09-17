using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private UIElement PCropRatioBuild()
    {
        var pRatioRoot = new StackPanel
        {
            Margin = new Thickness(0, 14, 0, 0)
        };
        var pPresetPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pPresetPanel.Children.Add(PInspectorLabelBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Ratio")));
        pPresetPanel.Children.Add(pInspectorRatioPreset);

        pInspectorCustomPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(PInspectorLabelWidth, 6, 0, 0)
        };
        pInspectorCustomPanel.Children.Add(pInspectorRatioWidth);
        pInspectorCustomPanel.Children.Add(new TextBlock
        {
            Text = "×",
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(7, 0, 7, 0)
        });
        pInspectorCustomPanel.Children.Add(pInspectorRatioHeight);
        pRatioRoot.Children.Add(pPresetPanel);
        pRatioRoot.Children.Add(pInspectorCustomPanel);
        return pRatioRoot;
    }

    private ComboBox PInspectorRatioBuild()
    {
        var pPresetCombo = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pPresetCombo);
        pPresetCombo.Items.Add(new LLocalizationChoice("Custom", "Inspector.Crop.RatioCustom"));
        pPresetCombo.Items.Add("16:9");
        pPresetCombo.Items.Add("9:16");
        pPresetCombo.Items.Add("4:3");
        pPresetCombo.Items.Add("3:4");
        pPresetCombo.Items.Add("1:1");
        pPresetCombo.Items.Add("21:9");
        pPresetCombo.SelectedIndex = 0;
        pPresetCombo.SelectionChanged += (_, _) => PInspectorRatioHandle();
        return pPresetCombo;
    }
}
