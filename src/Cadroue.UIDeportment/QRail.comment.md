# QRail.cs
Hash: `157f23aecfa880af`

## `public enum QRailRelayOutcome`

Tells the veneer what became of a tab dropped outside the rail.
Skipped means no relay was sent, and Busy means a running worklist kept the tab.
Kept means the other window did not take it.
Copied means the tab stays because its worklist started during the relay.

## `public sealed class QRail`

Owns the tab drag, the rename gestures and the relay run of the tab rail.
It reorders and renames through the strip, so the veneer holds no ghost or element field.

## `public static bool QRailNameCheck(bool lVisible, double lX, double lY, double lWidth, double lHeight)`

A hidden name box never counts as hit.
Coordinates are relative to the box, so a point outside its size is a miss.

## `public bool QRailPressHandle(`

A double click on the name starts the rename and arms no drag.
Any other press selects the tab and arms a drag that is not yet active.
Returns whether the veneer should capture the pointer.

## `public bool QRailDragResolve(double lX, double lY, double lMinimumX, double lMinimumY)`

The drag becomes active only after the pointer leaves the threshold box once.
It reports true on that first crossing alone.

## `public void QRailDragMove(double lPointer, IReadOnlyList<double> lCenters)`

Moves nothing before the drag is active.
After that the tab follows the pointer by the tab centers.

## `public QStripTab? QRailReleaseResolve()`

Returns the tab only when a real drag happened.
A plain click releases to null, and either way the drag state is cleared.

## `public bool QRailKeyHandle(QStripTab? lTab, bool lEnter, bool lEscape, string lText)`

Enter commits the typed name and Escape abandons it.
Returns whether the key was consumed.

## `public void QRailLeaveHandle(QStripTab? lTab, string? lText)`

Losing focus commits an edit in progress.
A tab that is not being edited is left alone.

## `public void QRailOutsideHandle(QStripTab? lTab, bool lInside, string? lText)`

A press outside the name box commits the rename, a press inside does not.

## `public void QRailCloseHandle(QStripTab? lTab)`

Clears any drag first, so a close during a press cannot leave a stale drag tab.
The tab closes only when the confirmation passes.

## `public static int QRailIndexResolve(double lPointer, IReadOnlyList<double> lCenters)`

The target is the count of centers the pointer has passed.
It is clamped to the last index, and an empty list gives 0.

## `public static bool QRailInsideCheck(bool lMinimized, double lX, double lY, double lWidth, double lHeight)`

A minimized rail has no inside, so a drop on it always counts as outside.

## `public async Task<QRailRelayOutcome> QRailRelayRun(`

Relays a dropped tab to another window and closes it here only when the other window took it.
A busy worklist, a pending relay or a failed payload write keep the tab.
Once the payload is written, the pending flag is cleared whatever the dispatch answers.
A worklist that starts working during the relay turns the move into a copy.
