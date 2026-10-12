# PWindow.xaml.cs
Hash: `32353b610cc09443`

## `public partial class PWindow : Window`

The main window shell.
It wires the strip, rail, toolbar, console and deck together and answers `QWindow` notices with control calls.
Partial peers hold the drop, shortcut and resize handlers.

## `public PWindow()`

Order matters.
Notices are attached before the deck and before the tabs start, so no tab add is missed.
Size is set before the first tab, and the position is restored after the options apply.
The relay starts once the tabs and their selection exist, so a handoff has a tab to land in.

## `public static PStrip? PWindowStripRead() =>`

Reaches the strip of the first main window from code that holds no window.
Returns null when no main window is open.

## `private void PWindowNoticesAttach()`

Subscribes every `QWindow`, `QSash` and ask notice in one place.
The close handler undoes the ask subscription and the peers' handlers.

## `private bool PWindowDispatch(Func<bool> pBody) => Dispatcher.Invoke(pBody);`

The seam that lets the deportment run a body on the UI thread without referencing the Dispatcher.

## `private void PWindowAskHandle(QAsk lAsk, Action<bool> lAnswer) => PSAlert.PSAlertConfirm(this, lAsk, lAnswer);`

The only place a destructive question becomes a modal.
The answer returns to the deportment through the callback.

## `private void PWindowTabHandle(QStripTab? lStripTab) =>`

Hands the deportment the selected tab and the viewer of its workspace.
A tab without a workspace passes a null viewer.

## `private void PWindowAddHandle(QWindowTab lTab)`

Creates the tab with its restored preset and layout, then applies its saved name.

## `private void PWindowMediaDefer(string pPath) =>`

Runs at Loaded priority.
So a launch with a media path opens it after the first layout pass.

## `private void PWindowTabAttach(QStripTab lTab, double pFlowHeight)`

Applies the command state to the newly selected workspace and follows its width.
The window width is resolved at once, so it fits the new tab.

## `private void PWindowTabDetach(QStripTab lTab)`

Stops following the width before the command state is reset.

## `private void PWindowLayoutApply(bool pVertical)`

Detaches the rail and the scene cluster first.
The vertical or horizontal notice that follows puts them back in the new place.

## `private void PWindowWidthApply()`

The deportment resolves the minimum and the width, this only assigns them.

## `private void PWindowExitHandle(object? sender, System.ComponentModel.CancelEventArgs eventArgs)`

A refused close confirmation cancels the window close.

## `private void PWindowCloseHandle(object? sender, EventArgs eventArgs)`

Saves the frame, closes the deportment, releases the window's subscriptions and closes each workspace.
The restore bounds are saved beside the live ones, so a maximized window reopens at its normal size.
