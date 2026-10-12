# PRosterRow.cs
Hash: `08ad24a72659d3a8`

## `internal static class PRosterRow`

Builds one job row of the roster table, with its columns, step connector and context menu.

## `public static Grid PRosterColumnsCreate()`

Defines the seven columns shared by every row and the table header.
The auto columns use shared size groups, so all rows align on the widest cell.

## `public static Border PRosterRowBuild(QRoster lRoster, QRosterRow lRow, ContextMenu pMenu)`

The state cell takes its brush from the row key, and every other cell is muted.
A press selects the job with the current shift and control state.
The shared menu is refilled for this row each time it opens.

## `private static void PRosterMenuApply(`

Replaces the menu items from the Deportment's plan.
An empty plan cancels the opening, so no empty menu appears.

## `private static Grid PRosterConnectorBuild(bool pLast, bool pStage)`

Draws a vertical spine through a node so consecutive rows read as one chain.
The last row hides the lower half, so the chain ends at its node.
The stage flag selects the node and spine colors.

## `public static void PRosterCellAdd(Grid pGrid, int pColumn, string pText, Brush pBrush)`

Adds one trimmed text cell to a column, with the same margin the step cell keeps.
