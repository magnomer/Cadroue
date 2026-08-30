using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PControlBar;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private readonly Dictionary<Guid, Guid> pRosterRowBatch = new();
    private readonly List<(Border Border, Guid Batch, bool Stage)> pRosterFileShades = new();

    private static Border PRosterFileBuild(PRosterLineageEntry pLineage, bool pStage) => new()
    {
        Background = pStage ? PRosterTheme.PRosterStageBrush : Brushes.Transparent,
        Padding = new Thickness(12, 5, 12, 3),
        Child = new TextBlock
        {
            Text = PLineageTitleRead(pLineage),
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTheme.PRosterTitleBrush,
            TextTrimming = TextTrimming.CharacterEllipsis
        }
    };

    private Brush PRosterShadeRead(Guid pRowId) =>
        pRosterRowBatch.TryGetValue(pRowId, out Guid pBatch) && pBatch == pRosterCardId
            ? Brushes.Transparent
            : pRosterStageIds.Contains(pRowId)
                ? PRosterTheme.PRosterStageBrush
                : Brushes.Transparent;

    private void PRosterShadeApply()
    {
        foreach ((Border pRow, Guid pBatch, bool pStage) in pRosterFileShades)
        {
            pRow.Background = pBatch == pRosterCardId
                ? Brushes.Transparent
                : pStage
                    ? PRosterTheme.PRosterStageBrush
                    : Brushes.Transparent;
        }
    }

    private Border PRosterRowBuild(LWorkItem pWorkItem)
    {
        var pGrid = PRosterColumnsCreate();
        pGrid.MinHeight = 29;

        TextBlock pStepCell = PRosterStepAdd(pGrid, pWorkItem);
        PRosterCellAdd(pGrid, 1, PRosterPriorityFormat(pWorkItem.LWorkPriority), PRosterTheme.PRosterMutedBrush);
        TextBlock pDurationCell = PRosterCellAdd(pGrid, 2, PRosterSpanFormat(pWorkItem.LWorkDuration), PRosterTheme.PRosterMutedBrush);

        TextBlock pProgressCell = PRosterCellAdd(pGrid, 3, PRosterProgressFormat(pWorkItem), PRosterTheme.PRosterMutedBrush);
        TextBlock pPercentCell = PRosterCellAdd(pGrid, 4, PRosterPlaceFormat(pWorkItem), PRosterTheme.PRosterMutedBrush);
        TextBlock pStateCell = PRosterCellAdd(pGrid, 5, PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent),
            PRosterTheme.PRosterStateRead(pWorkItem.LWorkStateCurrent));
        TextBlock pOwnerCell = PRosterCellAdd(pGrid, 6, PRosterOwnerFormat(pWorkItem), PRosterTheme.PRosterMutedBrush);

        pRosterRowCells[pWorkItem.LWorkId] = new PRosterRowCell
        {
            PRosterCellStep = pStepCell,
            PRosterCellDuration = pDurationCell,
            PRosterCellProgress = pProgressCell,
            PRosterCellPercent = pPercentCell,
            PRosterCellState = pStateCell,
            PRosterCellOwner = pOwnerCell
        };

        var pRow = new Border
        {
            Padding = new Thickness(12, 0, 12, 0),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            Child = pGrid,
            Tag = pWorkItem,
            ContextMenu = PMenu.PMenuContextCreate()
        };

        Guid pRowId = pWorkItem.LWorkId;
        pRow.PreviewMouseLeftButtonDown += (_, _) => PRosterStepSelect(pWorkItem);
        pRow.MouseEnter += (_, _) => PRosterHoverApply(pRowId, true);
        pRow.MouseLeave += (_, _) => PRosterHoverApply(pRowId, false);
        pRow.ContextMenuOpening += (_, pArgs) => PRosterMenuOpen(pRow, pArgs);

        pRosterStepRows[pRowId] = pRow;
        pRow.Background = pRosterSelectedIds.Contains(pRowId)
            ? PRosterTheme.PRosterSelectBrush
            : PRosterShadeRead(pRowId);
        return pRow;
    }

    private TextBlock PRosterStepAdd(Grid pGrid, LWorkItem pWorkItem)
    {
        bool pLast = !pRosterRowPlaces.TryGetValue(pWorkItem.LWorkId, out PRosterRowPlace? pPlace) || pPlace.PRosterPlaceLast;

        var pStepGrid = new Grid();
        pStepGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pStepGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        UIElement pConnector = PRosterConnectorBuild(pLast, pRosterStageIds.Contains(pWorkItem.LWorkId));
        Grid.SetColumn(pConnector, 0);
        pStepGrid.Children.Add(pConnector);

        var pStepCell = new TextBlock
        {
            Text = PRosterStepRead(pWorkItem),
            FontSize = PRosterTheme.PRosterRowSize,
            Foreground = PRosterTheme.PRosterTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(pStepCell, 1);
        pStepGrid.Children.Add(pStepCell);

        Grid.SetColumn(pStepGrid, 0);
        pGrid.Children.Add(pStepGrid);
        return pStepCell;
    }

    private static UIElement PRosterConnectorBuild(bool pLast, bool pStage)
    {
        Brush pSpineBrush = pStage ? PRosterTheme.PRosterMutedBrush : PRosterTheme.PRosterTrunkBrush;
        var pGrid = new Grid { Width = 18, UseLayoutRounding = true };
        pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var pSpineUpper = PRosterSpineBuild(pSpineBrush);
        Grid.SetRow(pSpineUpper, 0);
        pGrid.Children.Add(pSpineUpper);

        if (!pLast)
        {
            var pSpineLower = PRosterSpineBuild(pSpineBrush);
            Grid.SetRow(pSpineLower, 1);
            pGrid.Children.Add(pSpineLower);
        }

        var pNode = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = pStage ? PRosterTheme.PRosterTextBrush : Brushes.White,
            BorderBrush = pStage ? PRosterTheme.PRosterTextBrush : PRosterTheme.PRosterAccentBrush,
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

    private static TextBlock PRosterCellAdd(Grid pGrid, int pColumn, string pText, Brush pBrush)
    {
        var pCell = new TextBlock
        {
            Text = pText,
            FontSize = PRosterTheme.PRosterRowSize,
            Foreground = pBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(pCell, pColumn);
        pGrid.Children.Add(pCell);
        return pCell;
    }

    private void PRosterRowUpdate(LWorkItem pWorkItem)
    {
        if (!pRosterRowCells.TryGetValue(pWorkItem.LWorkId, out PRosterRowCell? pCell))
        {
            return;
        }

        pCell.PRosterCellStep.Text = PRosterStepRead(pWorkItem);
        pCell.PRosterCellDuration.Text = PRosterSpanFormat(pWorkItem.LWorkDuration);
        pCell.PRosterCellProgress.Text = PRosterProgressFormat(pWorkItem);
        pCell.PRosterCellPercent.Text = PRosterPlaceFormat(pWorkItem);
        pCell.PRosterCellState.Text = PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent);
        pCell.PRosterCellState.Foreground = PRosterTheme.PRosterStateRead(pWorkItem.LWorkStateCurrent);
        pCell.PRosterCellOwner.Text = PRosterOwnerFormat(pWorkItem);
    }
}
