# TRosterSelection.cs
Hash: `17d615b16926f7df`

## `public sealed class TRosterSelection`

Checks the roster selection and collapse state without any control.
The Schedule collection keeps it from running beside tests that share the schedule.

## `public void StepSelect_RangeAndToggle()`

A range extends from the anchor, and the anchor stays current.
A toggle removes a step from the selection and still makes it current.

## `public void CardSelect_ClearsSteps_AndStepClearsCard()`

A card and a step selection exclude each other.
Selecting one clears the other and repaints the card key.

## `public void Rebuild_DropsMissingSelectionAndCollapse()`

A rebuild forgets selection and collapse state for jobs and batches that left the schedule.

## `public void Close_IsIdempotent_AndStopsTheDetailRefresh()`

After close, the detail run and the elapsed tick raise nothing.
A second close is harmless.
