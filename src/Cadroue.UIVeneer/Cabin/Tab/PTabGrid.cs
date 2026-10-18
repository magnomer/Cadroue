using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PTabGrid
{
    public static Grid PTabFrameBuild()
    {
        var pGrid = new Grid
        {
            Margin = new Thickness(8, 0, 8, 0),
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        return pGrid;
    }

    public static Grid PTabBodyBuild() => new()
    {
        ClipToBounds = true,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    public static ColumnDefinition PTabGutterBuild(UIElement pPanel) => new() { Width = new GridLength(6) };

    public static void PTabPanelsAdd(
        Grid pPanelGrid,
        IReadOnlyList<UIElement> pPanels,
        List<ColumnDefinition> pColumnItems,
        List<ColumnDefinition> pSplitterColumns,
        IReadOnlyList<int> pPanelIndexes)
    {
        pPanelIndexes.ToList().ForEach(pIndex =>
        {
            pPanelGrid.ColumnDefinitions.Add(pColumnItems[pIndex]);
            pPanelGrid.ColumnDefinitions.Add(pSplitterColumns[pIndex]);
            Grid.SetColumn(pPanels[pIndex], QSurfaceGrid.QSurfaceColumnResolve(pIndex));
            pPanelGrid.Children.Add(pPanels[pIndex]);
        });
        pPanelGrid.ColumnDefinitions.Remove(pSplitterColumns.Last());
    }

    public static void PTabSplittersAdd(
        Grid pPanelGrid,
        List<UIElement> pSplitters,
        IReadOnlyList<int> pSplitterIndexes) =>
        pSplitterIndexes.ToList().ForEach(pIndex =>
        {
            Grid.SetColumn(pSplitters[pIndex], QSurfaceGrid.QSurfaceGutterResolve(pIndex));
            pPanelGrid.Children.Add(pSplitters[pIndex]);
        });

    public static void PTabBodyAdd(Grid pGrid, Grid pPanelGrid)
    {
        Grid.SetRow(pPanelGrid, 0);
        pGrid.Children.Add(pPanelGrid);
    }

    public static void PTabActionAdd(Grid pGrid, UIElement pCompass, PAction pAction, UIElement pFlow)
    {
        var pActionRowContent = new Grid { MinHeight = 72 };
        pActionRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pAction, 1);
        pActionRowContent.Children.Add(pCompass);
        pActionRowContent.Children.Add(pAction);

        var pActionRowBox = new Border
        {
            Margin = new Thickness(8, 8, 8, 8),
            MinHeight = 74,
            Padding = new Thickness(10, 0, 10, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xD7, 0xDF, 0xEA)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = pActionRowContent,
            SnapsToDevicePixels = true
        };
        Grid.SetRow(pActionRowBox, 1);
        Grid.SetRow(pFlow, 2);
        pGrid.Children.Add(pActionRowBox);
        pGrid.Children.Add(pFlow);
    }
}
