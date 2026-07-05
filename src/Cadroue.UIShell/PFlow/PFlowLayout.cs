using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    private static Grid PReelGridBuild(FrameworkElement reelBody, TextBlock labelLeft, TextBlock labelRight)
    {
        const double labelWidth = 64;
        labelLeft.HorizontalAlignment = HorizontalAlignment.Right;
        labelLeft.VerticalAlignment = VerticalAlignment.Center;
        labelLeft.Margin = new Thickness(0, 0, 6, 0);
        labelRight.HorizontalAlignment = HorizontalAlignment.Left;
        labelRight.VerticalAlignment = VerticalAlignment.Center;
        labelRight.Margin = new Thickness(6, 0, 0, 0);
        Grid reelGrid = new();
        reelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelWidth) });
        reelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        reelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelWidth) });
        Grid.SetColumn(labelLeft, 0);
        Grid.SetColumn(reelBody, 1);
        Grid.SetColumn(labelRight, 2);
        reelGrid.Children.Add(labelLeft);
        reelGrid.Children.Add(reelBody);
        reelGrid.Children.Add(labelRight);
        return reelGrid;
    }

    private static TextBlock PReelLabelBuild() => new() { FontSize = 12, FontFamily = new FontFamily("Consolas"), Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)) };
    private static Border PDividerBuild() => new() { Height = 8, Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3)), Cursor = Cursors.SizeNS, Child = new Border { Height = 1, VerticalAlignment = VerticalAlignment.Center, Background = new SolidColorBrush(Color.FromRgb(0xD1, 0xD1, 0xD1)) } };
}
