# TListSelection.cs
Hash: `dd3a2fd012299c84`

## `public sealed class TListSelection`

Checks the input list deportment selection, removal, drag and add rules without any control.
Each test builds a docket of fake paths and a list over it.
Selection semantics must stay pure functions of the docket order.

## `public void ControlClick_TogglesMembership_CurrentFallsBackToLastSelected()`

Removing the current path from the set must leave a valid current path.
The last remaining selected path takes over.

## `public void PressOnMultiSelection_KeepsSetUntilRelease_ThenCollapses()`

Pressing inside a multi selection must not collapse it yet.
Otherwise the user could never start dragging the whole set.

## `public void SelectAll_WithoutSelection_CurrentIsLast_KeepsCurrentOtherwise()`

Select all only picks a current path when none exists.
An existing current path survives the call.

## `public void RemoveCurrent_SelectsNextSurvivor_ThenPreviousAtEnd()`

After a removal the next surviving row becomes current.
At the end of the list the previous row takes over.
The removed paths are raised to the clear subscriber.

## `public void RemoveOther_KeepsCurrent_DropsAnchorWhenGone()`

Removing a path from outside must not disturb a current path that still exists.
The anchor is dropped only when its own path is gone, and no path notice is raised.

## `public void RemoveCurrentFromOutside_ClearsSelection()`

When the current path disappears from the docket the selection empties.
Subscribers hear one null path notice.

## `public void Remove_SkipsLockedSelection_ClearKeepsLocked()`

Locked rows are delivered work and must never be removed by the user.
Both remove and clear leave them in the docket.

## `public void Cards_GroupLockedByBatch_StateKeysFollowSelection()`

Locked rows form their own card apart from the plain rows.
The state key of each row combines its locked and selected facts.

## `public void Press_LockedRowSelectsWithoutCapture_DragNeedsThreshold()`

A locked row selects but never starts a drag.
An unlocked press starts a drag only once the pointer has moved past the threshold with the button held.
Release clears the drag path and leaves the pressed row selected.

## `public void KeyRun_ControlA_SelectsAll_OtherKeysIgnored()`

Only the control modified A key is consumed.
The return value tells the veneer whether the key was handled.

## `public async Task PathsAdd_KeepsExistingMediaOnly_SelectsFirstAdded()`

Paths that do not exist are dropped before the docket sees them.
A dialog add that is not confirmed adds nothing and raises no notice.
