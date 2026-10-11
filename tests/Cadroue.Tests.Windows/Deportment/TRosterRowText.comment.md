# TRosterRowText.cs
Hash: `8b1229e175b85f1a`

## `public sealed class TRosterRowText`

Checks the text the roster deportment formats for a job row without any control.
Items come from a throwaway schedule.
The Schedule collection keeps it from running beside tests that share the schedule.

## `public void Key_NamesEveryState(LWorkState state, string expected)`

Every work state has a stable key that brushes are looked up by.
Every state also has a non-empty display text.

## `public void Progress_ShowsAPercentOnlyWhileRunningOrStarted()`

A pending job shows a dash, not a percentage.
A running or already started job shows a percentage.

## `public void Owner_DistinguishesNoneThisTabOtherTabAndOtherWindow()`

An unowned job shows a dash.
An owner in this tab reads differently from one in another tab.
An owner in another process reads differently again.

## `public void Priority_Span_AndPhase_Format()`

A finished job shows its state text and ignores the phase.
A running job shows its phase.

## `public void Shade_PrefersSelectionThenStageOutsideASelectedCard(bool selected, bool cardSelected, bool stage, string expected)`

A selected row always wins.
Stage shading applies only outside a selected card.

## `public void Row_CarriesEveryCellFromTheItem()`

The row record is built from the item and its lineage entry alone.
No cell is left for the veneer to compute.
