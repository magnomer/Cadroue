# TMergeTabGroups.cs
Hash: `5f588837f921584d`

## `public sealed class TMergeTabGroups`

Checks the Merge tab deportment groups and layout without any control.
Each test builds the tab over a real docket and a seeded preset store.
The Preset collection keeps it from running beside tests that share the preset store.
Sample paths include a numbered series of two files and one unrelated file.

## `public void LayoutRoundTrip_CarriesAutoStrictAndMode()`

The automatic and strict switches survive a layout read and a tab built from it.

## `public void DocketChange_AutoOn_GroupsSeries_AutoOffLeavesGroupsAlone()`

With automatic grouping off, adding paths creates no groups.
Once it is on, the next docket change groups the numbered series.

## `public void GroupsRead_SkipsLockedAndEmpty_FiltersByCohort()`

A group made of locked delivered paths is not offered for a run.
A cohort filter drops groups outside that cohort.

## `public void Run_NoPreset_RaisesMissing_CohortRunReturnsZero()`

A missing preset raises the missing notice for a plain run.
A cohort run relays nothing.
