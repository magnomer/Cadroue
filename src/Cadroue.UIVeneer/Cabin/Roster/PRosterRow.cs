using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PRosterRow
{
    public static Grid PRosterColumnsCreate()
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 90 });
        pGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto, MinWidth = 58, SharedSizeGroup = "RosterPriority" });
        pGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto, MinWidth = 60, SharedSizeGroup = "RosterLength" });
        pGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto, MinWidth = 62, SharedSizeGroup = "RosterProgress" });
        pGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto, MinWidth = 80, SharedSizeGroup = "RosterRatio" });
        pGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto, MinWidth = 68, SharedSizeGroup = "RosterState" });
        pGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto, MinWidth = 84, SharedSizeGroup = "RosterOwner" });
        return pGrid;
    }

    public static Border PRosterRowBuild(QRoster lRoster, QRosterRow lRow, ContextMenu pMenu)
    {
        Border pRow = PRosterBodyBuild(lRow, pMenu);
        PRosterRowAttach(pRow, lRoster, lRow, pMenu);
        return pRow;
    }

    private static Border PRosterBodyBuild(QRosterRow lRow, ContextMenu pMenu)
    {
        Grid pGrid = PRosterColumnsCreate();
        pGrid.MinHeight = 29;
        pGrid.Children.Add(PRosterStep.PRosterStepBuild(lRow));
        PRosterCellAdd(pGrid, 1, lRow.QRosterRowPriority, PRosterTheme.PRosterMutedBrush);
        PRosterCellAdd(pGrid, 2, lRow.QRosterRowLength, PRosterTheme.PRosterMutedBrush);
        PRosterCellAdd(pGrid, 3, lRow.QRosterRowProgress, PRosterTheme.PRosterMutedBrush);
        PRosterCellAdd(pGrid, 4, lRow.QRosterRowRatio, PRosterTheme.PRosterMutedBrush);
        PRosterCellAdd(pGrid, 5, lRow.QRosterRowState, PRosterTheme.PRosterStateBrushes[lRow.QRosterRowKey]);
        PRosterCellAdd(pGrid, 6, lRow.QRosterRowOwner, PRosterTheme.PRosterMutedBrush);

        return new Border
        {
            Padding = new Thickness(12, 0, 12, 0),
            Background = PRosterTheme.PRosterRowShades[lRow.QRosterRowShade],
            Cursor = Cursors.Hand,
            Child = pGrid,
            ContextMenu = pMenu
        };
    }

    private static void PRosterRowAttach(Border pRow, QRoster lRoster, QRosterRow lRow, ContextMenu pMenu)
    {
        pRow.PreviewMouseLeftButtonDown += (_, _) => lRow.QRosterRowSelect(
            lRoster,
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
        pRow.MouseEnter += (_, _) => PRosterShadeSet(pRow, PRosterTheme.PRosterRowHovers[lRow.QRosterRowShade]);
        pRow.MouseLeave += (_, _) => PRosterShadeSet(pRow, PRosterTheme.PRosterRowShades[lRow.QRosterRowShade]);
        pRow.ContextMenuOpening += (_, pArgs) => PRosterMenu.PRosterMenuApply(
            pMenu, lRow.QRosterRowOpen(lRoster, PWindow.PWindowStripRead()?.QStrip), pArgs);
    }

    private static void PRosterShadeSet(Border pRow, Brush pShade) => pRow.Background = pShade;

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
