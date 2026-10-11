# TFixTabCheckup.cs
Hash: `92d5c937cdb1065b`

## `public sealed class TFixTabCheckup`

Checks how the Fix tab deportment relays diagnosis results to its clinic without any control.
The tab lists one media path and its clinic examines that path.
The Preset collection keeps it from running beside tests that share the preset store.

## `public void CheckupHandle_ListedPath_ReachesClinic_StrangerIgnored()`

A checkup outcome is shared by every Fix tab.
Only an outcome for a path in this tab's list may reach its clinic.

## `public void ProgressHandle_ListedPath_ReachesClinic_StrangerIgnored()`

Scan progress follows the same rule as outcomes.
Progress for a path outside the list leaves the clinic untouched.

## `public void ClearHandle_RemovesResultsForRemovedPaths()`

A path leaving the list drops its stored result.
The clinic then no longer shows a stale outcome for it.

## `public void DiagnosisRun_NoSelection_StartsNothing()`

A run needs a selected item.
Without one the scanner is never called.

## `public void DiagnosisRun_SelectedUnlockedItem_ScansOnce()`

The selected item is scanned on a background hop.
The test waits on the scanner call because the run returns before the scan.
