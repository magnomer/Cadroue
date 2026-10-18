using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal static class PInspectorField
{
    private static readonly IReadOnlyDictionary<bool, HorizontalAlignment> pInspectorFieldAligns =
        new Dictionary<bool, HorizontalAlignment>
        {
            [true] = HorizontalAlignment.Center,
            [false] = HorizontalAlignment.Left
        };

    internal static UIElement PInspectorFieldBuild(string pFieldLabel, UIElement pFieldContent)
    {
        var pFieldPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Height = PInspectorLook.PInspectorRowHeight,
            Margin = new Thickness(0, 0, 0, 8)
        };
        pFieldPanel.Children.Add(PInspectorLabelBuild(pFieldLabel));
        pFieldPanel.Children.Add(pFieldContent);
        return pFieldPanel;
    }

    internal static UIElement PInspectorFieldBuild(
        string pFieldLabel, FrameworkElement pFieldContent, bool pFieldCenter)
    {
        var pFieldGrid = new Grid
        {
            Height = PInspectorLook.PInspectorRowHeight,
            Margin = new Thickness(0, 0, 0, 8)
        };
        TextBlock pFieldTag = PInspectorLabelBuild(pFieldLabel);
        pFieldTag.HorizontalAlignment = HorizontalAlignment.Left;
        pFieldContent.HorizontalAlignment = pInspectorFieldAligns[pFieldCenter];
        pFieldGrid.Children.Add(pFieldTag);
        pFieldGrid.Children.Add(pFieldContent);
        return pFieldGrid;
    }

    internal static TextBlock PInspectorLabelBuild(string pFieldLabel) => new()
    {
        Text = pFieldLabel,
        Width = PInspectorLook.PInspectorLabelWidth,
        FontSize = 12,
        FontFamily = PInspectorLook.PInspectorFontFamily,
        Foreground = QPanel.QPanelTextRead(),
        VerticalAlignment = VerticalAlignment.Center
    };

    internal static UIElement PInspectorSeparatorBuild() => new Border
    {
        Height = 1,
        Background = QPanel.QPanelLineRead(),
        Margin = new Thickness(0, 12, 0, 12)
    };

    internal static StackPanel PToneBodyBuild(CheckBox pApply, StackPanel pStack)
    {
        var pBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);
        return pBody;
    }

    internal static TextBlock PInspectorUnitBuild(string pUnit) => new()
    {
        Text = pUnit,
        FontSize = 11,
        FontFamily = PInspectorLook.PInspectorFontFamily,
        Foreground = PInspectorLook.PInspectorMutedBrush,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(8, 0, 8, 0)
    };

    internal static Grid PInspectorSliderBuild(string pLabel, Slider pSlider, string pUnit, TextBox pValue) =>
        PInspectorSliderBuild(pLabel, pSlider, PInspectorUnitBuild(pUnit), pValue);

    internal static Grid PInspectorSliderBuild(string pLabel, Slider pSlider, TextBlock pUnit, TextBox pValue)
    {
        var pRow = new Grid
        {
            Height = PInspectorLook.PInspectorRowHeight,
            Margin = new Thickness(0, 0, 0, 8)
        };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TextBlock pLabelBlock = PInspectorLabelBuild(pLabel);
        pSlider.VerticalAlignment = VerticalAlignment.Center;
        pValue.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(pLabelBlock, 0);
        Grid.SetColumn(pSlider, 1);
        Grid.SetColumn(pUnit, 2);
        Grid.SetColumn(pValue, 3);
        pRow.Children.Add(pLabelBlock);
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pUnit);
        pRow.Children.Add(pValue);
        return pRow;
    }
}
