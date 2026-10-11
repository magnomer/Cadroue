# PLogWindow.cs
Hash: `8b6ff7bfebbc1606`

## `public static void PLogWindowShow(Window pOwner)`

Keeps one log window per program, so a second request restores and activates the open one.

## `private void PLogOwnerHandle(object? sender, EventArgs e) => Close();`

The log window closes with its owner, so it never outlives the main window.

## `private void PLogTextCopy(string pText)`

A busy clipboard throws, so the failure goes back to `LLog` to record and announce.

## `private void PLogErrorShow(string pMessage)`

Shows a log window failure as a warning over the window.
The failure is already recorded before the notice reaches this handler.
`LLog` records a copy failure, and the trace reader or `LUsher` records the rest.
