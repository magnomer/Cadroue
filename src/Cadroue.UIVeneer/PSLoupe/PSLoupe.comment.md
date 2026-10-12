# PSLoupe.cs
Hash: `b81840750b6d30ad`

## `internal static void PSLoupeShow(Window pOwner, QViewer lViewer)`

Opens the detached preview window and attaches its QSLoupe to the given viewer.
The window plays the same media apart from the docked viewer panel.

## `private PSLoupe(Window pOwner)`

Both engine hosts are built once and stay collapsed until QSLoupe names the active engine.
The float mode is restored before the bar is built, so the segments open on the stored choice.

## `private void PSLoupeFloatApply()`

The float mode decides ownership and topmost together.
A floating loupe drops its owner, so it no longer minimizes with the main window.

## `private void PSLoupeEngineCreate()`

Answers the QSLoupe engine notice by showing one host and wrapping it into a player seam.
The seam choice is a readonly table keyed by the mpv flag, never a branch.

## `private void PSLoupeLoadedHandle(object pSender, RoutedEventArgs pEvent)`

Placement is restored only after load, when the window can be clamped to its monitor.
The float mode is applied again on the shown window before QSLoupe starts playback.

## `private void PSLoupeCloseHandle(object? pSender, EventArgs pEvent)`

The viewer is detached last, after both engine hosts are disposed.
Each host dispose runs as its own stage, so one failure is traced and the rest still run.
