using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PRoster
{
    private static Grid PRosterHeaderBuild()
    {
        var pGrid = PRosterColumnsCreate();
        PRosterHeadAdd(pGrid, 0, LLocalization.LLocalizationTextRead("Roster.Queue.Step"));
        PRosterHeadAdd(pGrid, 1, LLocalization.LLocalizationTextRead("Roster.Queue.Priority"));
        PRosterHeadAdd(pGrid, 2, LLocalization.LLocalizationTextRead("Roster.Queue.Length"));
        PRosterHeadAdd(pGrid, 3, LLocalization.LLocalizationTextRead("Roster.Queue.Progress"));
        PRosterHeadAdd(pGrid, 4, LLocalization.LLocalizationTextRead("Roster.Queue.Percentage"));
        PRosterHeadAdd(pGrid, 5, LLocalization.LLocalizationTextRead("Roster.Queue.State"));
        PRosterHeadAdd(pGrid, 6, LLocalization.LLocalizationTextRead("Roster.Queue.Owner"));
        return pGrid;
    }

    private static void PRosterHeadAdd(Grid pGrid, int pColumn, string pText)
    {
        var pCell = new TextBlock
        {
            Text = pText,
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTheme.PRosterMutedBrush
        };
        Grid.SetColumn(pCell, pColumn);
        pGrid.Children.Add(pCell);
    }

    private static Grid PRosterColumnsCreate()
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 90 });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 58 });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 60 });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 62 });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 80 });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 68 });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 84 });
        return pGrid;
    }
}
