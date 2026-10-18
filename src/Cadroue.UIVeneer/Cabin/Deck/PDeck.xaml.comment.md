# PDeck.xaml.cs
Hash: `b4d06854b84c56e1`

## `public partial class PDeck : UserControl`

Hosts the root of every open tab in one grid and shows only the selected one.
It decides nothing, the deportment says which tab is shown and whether the notice is visible.

## `public void PDeckAttach(PStrip pStripOwner)`

Called once, after the strip exists.
Roots enter the grid on tab add and leave it on tab close.
Hiding keeps a root in the grid, so a tab keeps its state while another is shown.

## `private void PDeckUnloadHandle(object sender, RoutedEventArgs e)`

Unsubscribes from the strip, which outlives this control.
Without it a closed deck would keep receiving tab notices.
It also closes the deportment, which subscribes the strip's selection itself.

## `private void PDeckAddHandle(QStripTab lStripTab)`

A new root starts collapsed.
The deportment reveals it when its tab is selected.
