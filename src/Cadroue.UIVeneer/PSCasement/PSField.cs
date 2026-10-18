using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIVeneer;

internal static class PSField
{
    internal const double PSFieldFontSize = 12;
    internal const double PSFieldControlHeight = 32;
    internal const double PSFieldLabelWidth = 130;
    internal const double PSFieldChipHeight = 26;

    internal static readonly Brush PSFieldLine = PSFieldBrushCreate(0xD9, 0xDE, 0xE7);
    internal static readonly Brush PSFieldText = PSFieldBrushCreate(0x1D, 0x2A, 0x3D);
    internal static readonly Brush PSFieldMuted = PSFieldBrushCreate(0x62, 0x6F, 0x83);
    internal static readonly Brush PSFieldAccent = PSFieldBrushCreate(0x4C, 0x86, 0xF7);
    internal static readonly Brush PSFieldInactive = PSFieldBrushCreate(0xF0, 0xF3, 0xFA);
    internal static readonly Brush PSFieldSoft = PSFieldBrushCreate(0xF7, 0xF9, 0xFC);

    private static Brush PSFieldBrushCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }

    internal static TextBlock PSFieldLabelBuild(string pText) => new()
    {
        Text = pText,
        Foreground = PSFieldMuted,
        VerticalAlignment = VerticalAlignment.Center
    };

    internal static UIElement PSFieldBuild(string pLabel, Control pControl)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSFieldLabelBuild(pLabel));
        pControl.MinHeight = PSFieldControlHeight;
        Grid.SetColumn(pControl, 1);
        pGrid.Children.Add(pControl);
        return pGrid;
    }

    internal static UIElement PSFieldBuild(string pLabel, UIElement pContent)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSFieldLabelBuild(pLabel));
        Grid.SetColumn(pContent, 1);
        pGrid.Children.Add(pContent);
        return pGrid;
    }

    internal static UIElement PSFieldButtonBuild(string pLabel, Control pControl, params UIElement[] pTrailing)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSFieldLabelBuild(pLabel));

        var pPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        pControl.MinHeight = PSFieldControlHeight;
        pPanel.Children.Add(pControl);
        foreach (UIElement pElement in pTrailing)
        {
            pPanel.Children.Add(pElement);
        }

        Grid.SetColumn(pPanel, 1);
        pGrid.Children.Add(pPanel);
        return pGrid;
    }
}
