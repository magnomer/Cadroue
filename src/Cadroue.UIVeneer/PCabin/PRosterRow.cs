using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PPorch;

namespace Cadroue.UIVeneer.PCabin;

internal static class PRosterRow
{
    public static Grid PRosterColumnsCreate()
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

    public static Border PRosterRowBuild(LRoster lRoster, LRosterRow lRow)
    {
        Grid pGrid = PRosterColumnsCreate();
        pGrid.MinHeight = 29;
        pGrid.Children.Add(PRosterStepBuild(lRow));
        PRosterCellAdd(pGrid, 1, lRow.LRosterRowPriority, PRosterTheme.PRosterMutedBrush);
        PRosterCellAdd(pGrid, 2, lRow.LRosterRowLength, PRosterTheme.PRosterMutedBrush);
        PRosterCellAdd(pGrid, 3, lRow.LRosterRowProgress, PRosterTheme.PRosterMutedBrush);
        PRosterCellAdd(pGrid, 4, lRow.LRosterRowRatio, PRosterTheme.PRosterMutedBrush);
        PRosterCellAdd(pGrid, 5, lRow.LRosterRowState, PRosterTheme.PRosterStateBrushes[lRow.LRosterRowKey]);
        PRosterCellAdd(pGrid, 6, lRow.LRosterRowOwner, PRosterTheme.PRosterMutedBrush);

        ContextMenu pMenu = PMenu.PMenuContextCreate();
        var pRow = new Border
        {
            Padding = new Thickness(12, 0, 12, 0),
            Background = PRosterTheme.PRosterRowShades[lRow.LRosterRowShade],
            Cursor = Cursors.Hand,
            Child = pGrid,
            ContextMenu = pMenu
        };
        pRow.PreviewMouseLeftButtonDown += (_, _) => lRoster.LRosterStepSelect(
            lRow.LRosterRowId,
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
        pRow.MouseEnter += (_, _) => pRow.Background = PRosterTheme.PRosterRowHovers[lRow.LRosterRowShade];
        pRow.MouseLeave += (_, _) => pRow.Background = PRosterTheme.PRosterRowShades[lRow.LRosterRowShade];
        pRow.ContextMenuOpening += (_, pArgs) => PRosterMenuApply(
            pMenu, lRoster.LRosterMenu.LRosterMenuRead(lRow.LRosterRowId, PWindow.PWindowStripRead()?.LStrip), pArgs);
        return pRow;
    }

    private static void PRosterMenuApply(
        ContextMenu pMenu,
        IReadOnlyList<LRosterItem> lItems,
        ContextMenuEventArgs pArgs)
    {
        pMenu.Items.Clear();
        lItems.ToList().ForEach(lItem => pMenu.Items.Add(PRosterItemBuild(lItem)));
        pArgs.Handled = pMenu.Items.IsEmpty;
    }

    private static MenuItem PRosterItemBuild(LRosterItem lItem)
    {
        MenuItem pItem = PMenu.PMenuItemCreate(lItem.LRosterItemText, PTabIcon.PTabIconFind(lItem.LRosterItemIcon));
        pItem.IsEnabled = lItem.LRosterItemEnabled;
        pItem.Click += (_, _) => lItem.LRosterItemAction();
        return pItem;
    }

    private static Grid PRosterStepBuild(LRosterRow lRow)
    {
        var pStepGrid = new Grid();
        pStepGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pStepGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid pConnector = PRosterConnectorBuild(lRow.LRosterRowLast, lRow.LRosterRowStage);
        Grid.SetColumn(pConnector, 0);
        pStepGrid.Children.Add(pConnector);

        var pStepCell = new TextBlock
        {
            Text = lRow.LRosterRowStep,
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

    public static void PRosterCellAdd(Grid pGrid, int pColumn, string pText, Brush pBrush)
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
    }
}
