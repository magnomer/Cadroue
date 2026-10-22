# Preset

These drivers build and run the Export preset list.
The Veneer templates only declare structure.
The driver that owns a handler also writes what its medium shows.
So a face is never split between two files.

## QExportPreset.cs

The driver builds the list from its template, refills it and detaches the header and row drivers it creates.
The list is a template and not a subclass, so it stays an exact `ScrollViewer`, which the tests assert.
Rebuild detaches the old drivers first.
It reads the rows before the entries, then clears the panel.
Fold visibility is set before a row joins the panel.
Detach runs once per driver.
The list is cleared after the detach, so a repeated detach finds nothing to do.
Selecting a preset first reveals its collapsed group, which rebuilds, and then selects.
Tests `Rebuild_DetachesStaleHeaderActions` and `Unload_DetachesHeaderAndRowHandlers` pin detach.

## QExportList.cs

The class holds the list policy and nothing about the medium.
Native presets group by their own group name, and user presets share one fold keyed `$User`.
Native groups start collapsed by default, and the user group starts open.
Tests `Read_PreservesOrderTransitionsAndFallbacks`, `Toggle_RebuildsAfterRestoredPersistenceFailure` and `Show_RevealsHiddenGroupBeforeSelection` pin it.

## QExportRows.cs and QExportFace.cs

Rows turn the Conduct row state into a face.
The face carries the display name, the flags and the notice key, not a rendered string.
The working copy is captured when the rows are read, while the selection and editing stay live.
Test `RowsRead_CapturesWorkingWhileSelectionAndEditingRemainLive` pins it.

## QExportHeader.cs

The header driver sets the `Header` tag first, because the drag code counts headers by that tag.
The label, fold icon, tooltip and click handler follow.
Detach is idempotent.

## QExportRow.cs

Build renders the whole row face from the template.
The incompatible marker is display text only.
The preset keeps its real name, so renaming, matching and persistence never see it.
An editing row swaps the name box in for the face and hands it to the name driver.
A press on a button or a native row clears the drag origin.
So neither starts a drag.
A double click on a user preset starts the rename.
A click selects, after it commits any open rename.
Test `RowFace_NativeModifiedUnsupportedAndEditing_PreservesPassiveSurfaceAndRoutes` pins the face and the routes.

## QExportName.cs

The driver owns one rename session at a time.
Every handler checks that its text box is still the session's box, so a stale handler does nothing.
Attach detaches the old session first.
Sync detaches it when the selection has moved off the edited name.
Commit, cancel, focus loss, Return, Escape and a click outside end the session.
Tests `Editor_ReturnCommits_EscapeCancels_AndOutsideClickCommits`, `Editor_FocusLossCommits_AndInvalidNameEndsWithoutRename` and `RebuildAndUnload_DetachStaleEditorHandlers` pin it.

## QExportDrag.cs

One gesture has one decision owner, and it is this class.
It owns the transient state, the panel attachment, the target policy and the exact lifetime of the release.
The persistent writer is the injected mover.

The mover is called only when the visual position changes.
So a same-position drag writes nothing and raises no store change.
The mover runs before the visual reorder, and the row moves only if the mover accepted.
Release clears the drag first, then releases the capture, and selects only when the press never became a drag.
A click commits any open rename before it selects.
Clearing after a real drag syncs once, which catches up the repaint that sync skipped.
Test `Drag_ThresholdMoveAndRelease_ReordersOnceAndClearsSurfaceState` pins one reorder, the cleared state and the lost-capture cleanup.

## Stamp

Commit: `b84e5458de50`
- `QExportDrag.cs` `ba08e2c85630d2af`
- `QExportFace.cs` `7202572e66455744`
- `QExportHeader.cs` `7f85143668095de0`
- `QExportList.cs` `6eed7fcc2156306e`
- `QExportName.cs` `d25bd91ba522d4d4`
- `QExportPreset.cs` `196f8ace28c6668c`
- `QExportRow.cs` `3a3f8fd1688df317`
- `QExportRows.cs` `91c071cdf3dad9fb`
