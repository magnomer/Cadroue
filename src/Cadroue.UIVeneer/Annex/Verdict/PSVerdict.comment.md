# PSVerdict.cs
Hash: `1446bb9725f06092`

## `internal static void PSVerdictShow(Window pOwner, QSVerdict pVerdict)`

Opens the modeless encoder verification results window.
An open results window is closed first, so only the latest verification is ever shown.

## `private PSVerdict(Window pOwner, QSVerdict pVerdict)`

The window is owned by the owner's own owner when there is one.
That keeps it alive after the export dialog that opened it closes.

## `private UIElement PSVerdictTableBuild()`

Draws one row per trial-encoded encoder from the verdict records.
With no records it shows the not-run notice instead of an empty table.

## `private static Grid PSVerdictGridBuild()`

Header and rows share these fixed column widths, so cells line up without a shared size scope.

## `private UIElement PSVerdictRowBuild(QSVerdictRow pEntry)`

QSVerdict decides whether a row has detail worth showing.
Only those rows get the Details chip, which folds the trial message open and closed.

## `private void PSVerdictCloseHandle(object? pSender, EventArgs pEvent)`

Clears the single-instance slot only if it still points at this window.
A replaced window closing late therefore never clears its successor.
