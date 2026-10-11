# TRosterCards.cs
Hash: `905b1449981d9e36`

## `public sealed class TRosterCards`

Checks how the roster deportment turns schedule records into cards, files and rows without any control.
Each test opens a roster over a temporary schedule and a station.
The Schedule collection keeps it from running beside tests that share the schedule.

## `public void Rebuild_GroupsRecordsIntoCardsFilesAndRows()`

Records of one batch share a card, and each record becomes a file with its own row.
The ordered id list follows the schedule order across cards.

## `public void Rebuild_MarksCompletedBatchesDone_AndCollapsesThemWhenAsked()`

A fully completed batch reads as done.
Collapsing done cards is a stored option that reports whether it changed.
Selecting a card changes its key but keeps the done body.

## `public void CollapseToggle_ReplacesOnlyThatCard()`

Toggling one card must leave the entries of the other cards untouched.
Only the last entry frame changes, because the frame depends on the card being last and open.

## `public void Rebuild_KeepsUnchangedCardEntries()`

A rebuild after a schedule change reuses the entry objects of cards that did not change.
The veneer relies on that identity to avoid redrawing them.

## `public void Lineage_TitleAndStageFollowDerivedOutputs()`

A derived output joins the lineage of its origin within the batch.
The ratio text shows a dash while the output size is unknown.

## `public void ElapsedTick_RefreshesTheDetailOnlyWhileVisibleAndRunning()`

The elapsed clock redraws the detail only when a job is running and the detail is visible.
