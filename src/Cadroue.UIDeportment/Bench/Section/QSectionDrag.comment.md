# QSectionDrag.cs

## `public sealed class QSectionDrag`

The class hands each press, move, release and lost capture to the drag gate and relays the gate's tells.
It keeps only the ghost's grab point.
Test `Drag_MovesAfterThreshold_ReordersThroughTheFlow_ReleaseRebuilds` pins the flow from press to rebuild.

## `public bool QSectionPressHandle(int lIndex, int lClickCount, double lX, double lY, double lGrabX, double lGrabY)`

The grab point is kept on every press.
Only a press the gate readies for a drag is ever dragged, so a stale grab point is never used.

## `public bool QSectionReleaseHandle(bool lShift, bool lControl)`

A release without a move selects by modifiers.
Test `Release_WithoutMove_SelectsByModifiers_ToggleAndSeekAskTheFlow` pins it.

## `public void QSectionLostHandle()`

A lost capture resets the gate's drag.
