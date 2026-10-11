# PRail.xaml.cs
Hash: `79c2b8946f4581cb`

## `public partial class PRail : UserControl`

The tab bar control.
It draws the strip in one of two orientations and forwards gestures to `LRail` as plain numbers.
Every branch is a readonly table lookup, keyed by the vertical flag, a key, a flag or the outcome.

## `public const double PRailWidth = 180;`

The width of the vertical rail, which the window reserves as its left column.

## `public void PRailApply(bool pVertical)`

Sets the orientation on the strip first, because the item templates read it.
Then every layout property is swapped from the tables.

## `private void PRailLoadHandle(object sender, RoutedEventArgs e) =>`

The outside-press handler lives on the window, not the rail.
A press anywhere in the window must commit a rename.

## `private void PTabPressHandle(object sender, MouseButtonEventArgs e)`

Capture is taken only when `LRail` arms a drag.
A double click on the name starts the rename and takes no capture.

## `private static bool PTabHitCheck(FrameworkElement pTabElement, MouseButtonEventArgs e)`

Finds the name text through the item template, because the tab element does not own it.
The hit test is relative to that text.

## `private void PTabMoveRun(MouseEventArgs e)`

The ghost shows once the drag threshold is crossed, and then follows every move.
The tab centers are read fresh each time, because items shift as the tab reorders.

## `private void PTabGhostShow(FrameworkElement pTabElement)`

The dragged tab is dimmed in place.
The ghost keeps the grab offset, so it does not jump under the pointer.

## `private double PTabCenterRead(LStripTab lStripTab)`

Reads the center along the strip axis from the item container.
The measurement is relative to the items control, the space of the pointer reads.

## `private void PTabReleaseHandle(object sender, MouseButtonEventArgs e)`

The screen point is read before the drag is cleared.
A drag that ended outside the window is then offered to the relay.

## `private async void PTabRelayRun(LStripTab? lStripTab, Point pDevicePoint)`

The window decides inside or outside in its own space.
The relay receives the point in device pixels for the other window and in DIPs for the payload.
The outcome table then picks the notice.

## `private static void PTabKeptShow(Window pWindow) =>`

Warns that the other window did not take the tab.
The tab stays.

## `private static void PTabBusyShow(Window pWindow) =>`

Announces that a tab with running work cannot leave.

## `private static void PTabVisibleHandle(object sender, DependencyPropertyChangedEventArgs e)`

The rename box fills itself only when it becomes visible.
Hiding does nothing.

## `private static void PTabNamePrepare(TextBox pNameBox) =>`

Runs at Input priority, after the box has been laid out.
Focus before that would be lost.

## `private void PTabOutsideHandle(object sender, MouseButtonEventArgs e)`

Passes the focused name box and whether the press hit inside it.
`LRail` commits only when the press was outside.

## `private void PTabLeaveHandle(object sender, RoutedEventArgs e) =>`

Losing focus commits the typed name.

## `private static void PTabDragClear(FrameworkElement? pTabElement)`

Restores opacity, removes the ghost and releases the mouse.
Safe to call with no tab.

## `private void PTabMenuHandle(object sender, RoutedEventArgs e)`

Builds the add menu on each click from the current layout keys.

## `private void PTabCloseHandle(object sender, MouseButtonEventArgs e)`

Clears a drag in progress before closing, so the closed tab leaves no ghost.

## `protected override void OnLostMouseCapture(MouseEventArgs e)`

Any lost capture ends the drag in both the deportment and the veneer.
