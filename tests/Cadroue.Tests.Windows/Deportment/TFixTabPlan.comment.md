# TFixTabPlan.cs
Hash: `d3c453de5545e6e8`

## `public sealed class TFixTabPlan`

Checks the Fix tab deportment's plan handling without any control.
It covers the row table, the layout round trip, the sidecar save and restore and the missing preset notice.
Most tests build a tab over a clinic, a viewer, a docket and a processing list.
The Preset collection keeps it from running beside tests that share the preset store.

## `public void Rows_OneStepPerKindPlusSalvage_InFixOrder()`

The processing list has one row per defect kind and a final salvage row.
Every defect kind appears exactly once.

## `public void LayoutRead_NothingPersistent_LeavesInspectorNull()`

A plan with no persistent step has nothing worth keeping across restarts.
The scene entry then carries no inspector record.

## `public void LayoutRoundTrip_CarriesPersistentStepAndSalvage()`

A persistent step and the salvage choice survive the trip through a scene entry.
The restored tab marks exactly those steps active in its processing list.

## `public void LayoutApply_Null_MinimizesClinic()`

A scene without a Fix record leaves the clinic minimized.

## `public void StateSave_ActivePlan_WritesOnce_SuspendedOrLockedWritesNothing()`

The plan is written beside the current source.
A suspended save scope and a locked source both block the write.

## `public void StateSave_InactivePlanWithoutSidecar_WritesNothing()`

A plan that does nothing creates no sidecar file.

## `public void PlanRestore_PersistentWinsOverSaved_UpdatesActive()`

A persistent step the user set outranks the plan saved in the sidecar.
The processing list follows the winning plan.

## `public void ItemsHandle_PersistentStep_FansOutToEveryAddedPath()`

A persistent step is saved beside every path added to the list, not only the current one.

## `public void PathHandle_BlankOrSameSource_DoesNothing()`

A blank path or the source already loaded changes nothing.
The clinic is never handed a source.

## `public void Run_NoPreset_RaisesMissing()`

Both run entry points refuse to enqueue without a preset and raise the missing notice instead.
