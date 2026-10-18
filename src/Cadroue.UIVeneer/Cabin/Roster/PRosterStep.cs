using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PRosterStep
{
    public static Grid PRosterStepBuild(QRosterRow lRow)
    {
        var pStepGrid = new Grid();
        pStepGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pStepGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid pConnector = PRosterConnectorBuild(lRow.QRosterRowLast, lRow.QRosterRowStage);
        Grid.SetColumn(pConnector, 0);
        pStepGrid.Children.Add(pConnector);

        var pStepCell = new TextBlock
        {
            Text = lRow.QRosterRowStep,
            FontSize = PRosterTheme.PRosterRowSize,
            Foreground = PRosterTheme.PRosterTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(pStepCell, 1);
        pStepGrid.Children.Add(pStepCell);
        Grid.SetColumn(pStepGrid, 0);
        return pStepGrid;
    }

    private static Grid PRosterConnectorBuild(bool pLast, bool pStage)
    {
        Brush pSpineBrush = PRosterTheme.PRosterSpineBrushes[pStage];
        var pGrid = new Grid { Width = 18, UseLayoutRounding = true };
        pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Border pSpineUpper = PRosterSpineBuild(pSpineBrush);
        Grid.SetRow(pSpineUpper, 0);
        pGrid.Children.Add(pSpineUpper);

        Border pSpineLower = PRosterSpineBuild(pSpineBrush);
        pSpineLower.Visibility = PLook.PLookVisible[!pLast];
        Grid.SetRow(pSpineLower, 1);
        pGrid.Children.Add(pSpineLower);

        var pNode = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = PRosterTheme.PRosterNodeFills[pStage],
            BorderBrush = PRosterTheme.PRosterNodeLines[pStage],
            BorderThickness = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };
        Grid.SetRowSpan(pNode, 2);
        pGrid.Children.Add(pNode);
        return pGrid;
    }

    private static Border PRosterSpineBuild(Brush pSpineBrush) => new()
    {
        Width = 2,
        Background = pSpineBrush,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Stretch,
        Margin = new Thickness(7, 0, 0, 0)
    };
}
