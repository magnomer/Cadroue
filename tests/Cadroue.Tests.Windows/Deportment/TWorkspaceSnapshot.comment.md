# TWorkspaceSnapshot.cs
Hash: `3942ef1c4fb9f0cd`

## `public sealed class TWorkspaceSnapshot`

Checks the deportment workspace without any control.
It covers the facts derived from the tab key, the history snapshots and the media clear.
The Preset collection keeps it from running beside tests that share the preset store.

## `public void Create_DerivesFactsFromKey()`

The key alone decides the section strip, the audio-only flag and the source presence.
A workspace not yet attached has no flow, no list and no work in flight.

## `public void PresetChange_RecordsHistory_UndoAndRedoRoundTrip()`

Each preset change is one history entry.
Undo and redo walk those entries and refuse at both ends.

## `public void Undo_WhileApplying_DoesNotRecordTheApply()`

Applying a snapshot must not record a new entry.
Otherwise redo would be lost the moment undo ran.

## `public void Snapshot_CarriesSegmentSections()`

A snapshot holds the sections and the selected index of the attached segment.

## `public void PathHandle_BlankPath_AsksMediaClose()`

A null or blank path means the source is gone, so the media closes.
A real path does not.

## `public void MediaClear_RemovesStaleDocketPaths_AndAsksClose()`

A clear that removes paths asks the media to close and returns true.
A second clear finds nothing to remove and returns false.
