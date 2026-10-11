# TRosterDetailRows.cs
Hash: `fd43daae44a917cd`

## `public sealed class TRosterDetailRows`

Checks the one-job detail plan of the roster without any control.
It covers the detail kind, the failure, meter, bar and record rows and the display formats.
The Schedule collection keeps it from running beside tests that share the schedule.

## `public void Kind_FollowsCardThenSelectionThenNothing()`

The detail pane shows nothing until something is selected.
Selecting a job shows the job detail and selecting a card title shows the card detail.

## `public void Pending_HasNoFailureNoMeterNoBar_AndSevenRecordRows()`

A job that has not run has nothing to measure and nothing to blame.
Its record rows are all plain and an unknown value shows the placeholder.

## `public void Running_ShowsMeterAndBarOnceBothSizesAreKnown()`

A size comparison needs both the source and the output size.
Until the output size is known the bar and the meter stay out.

## `public void Done_WithLargerOutput_MarksBarOver()`

An output larger than its source is marked over and the bar splits at the source size.
An equal size has its own key.
A missing or zero size yields no bar.

## `public void Failed_PutsTheReasonFirst_AndKeepsItOutOfTheMessageRow()`

A failed job shows its reason as a separate failure row.
The message record row appears only for a job that is not failed.

## `public void Encoding_ListsEncoderRowsOnlyWhenReencoding()`

Copy and exclude mean the stream is not re-encoded.
Only an encode shows encoder settings.

## `public void Formats_RenderSizesClocksStampsAndContainers()`

The formats are the single source of every size, clock and stamp the roster shows.
A missing size does not render as zero.
