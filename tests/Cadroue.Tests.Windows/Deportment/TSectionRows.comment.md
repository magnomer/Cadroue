# TSectionRows.cs
Hash: `e83046d38cb350e6`

## `public sealed class TSectionRows`

Checks the section deportment that turns the flow's pieces into rows and decides every row gesture.
Each test builds a flow with a source, a section from 0:10 to 0:30 and a second section named beta.
No control is involved, so the rebuild and select callbacks stand in for the veneer.

## `public void Rows_ComeFromPieces_WithNumbersTimesAffixesAndSelection()`

A row carries everything the veneer draws, so the veneer needs no other source.
That covers number, times, span text, affixes and the selected flag.

## `public void SameList_AppliesSelectionOnly_ChangedListRebuilds()`

A selection change on an identical list must not rebuild the rows.
Rebuilding would drop an open editor or a drag in progress.
Only a changed list triggers a rebuild.

## `public void Rename_StartsEditing_CommitAppliesTrimmedAffixes_CancelRestores()`

Only a double click starts a rename.
Commit trims the name and both affixes.
Escape drops the edit and leaves the stored name alone.
A blur carrying a stale serial must not commit again.

## `public void Blur_CommitsOnlyTheLiveEditor_WhenFocusLeftTheBoxes()`

Focus moving between the editor boxes is not a blur.
An outdated serial is also ignored.
Only a current blur with focus outside the boxes commits.

## `public void Drag_MovesAfterThreshold_ReordersThroughTheFlow_ReleaseRebuilds()`

A press only arms the drag, and the move must cross the threshold with the button held.
The reorder goes through the flow at once, but rows rebuild only on release.
A second release finds no drag.

## `public void Release_WithoutMove_SelectsByModifiers_ToggleAndSeekAskTheFlow()`

A release without a move is a click.
The modifier decides between adding to the selection and removing from it.
Hide toggles and seek are requests the flow performs.
Losing capture ends the drag.

## `public void EditableOff_DisablesCancelsAndClearsTheDrag()`

When the flow stops being editable the rows are disabled and any open editor is cancelled.
Renaming and dragging are refused afterwards.
