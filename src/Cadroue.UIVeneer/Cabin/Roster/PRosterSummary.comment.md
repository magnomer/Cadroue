# PRosterSummary.cs
Hash: `c56ad29212592c24`

## `public static void PRosterSummaryAdd(StackPanel pPanel, QSummary lSummary)`

Draws the batch overview shown when a queue card title is selected.
It adds one element per record that QSummary resolved and decides nothing itself.
The root is a shared size scope so source and output columns align across the comparison grid.

## `private static UIElement PRosterCountsBuild(QSummary lSummary)`

Places the source and output file counts above their path lists, left and right like the comparison columns.
