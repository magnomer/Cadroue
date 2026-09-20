using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PWing;

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
}
