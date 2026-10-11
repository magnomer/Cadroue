# TSplitTabSweep.cs
Hash: `343c09eb2f907645`

## `public sealed class TSplitTabSweep`

Checks the sweep owner of the split tab without any control.
The Preset collection keeps it from running beside tests that share the preset store.
A sweep starts only with a selected file, a spool and an enabled kind, and each refusal is silent.

## `public void StepsRead_EnabledKindsInCatalogOrder()`

The step order follows the detector catalog and not the order in which the kinds were enabled.

## `public async Task SweepStart_NoSelection_DoesNotRun()`

Without a selected file the start is refused before any busy notice or running flag appears.

## `public void SweepCancel_Idle_IsNoop()`

Cancelling or closing a tab that never swept must not throw.

## `public void LockHandle_LockedDisablesSectionEditing()`

A locked file cannot have its sections edited, and unlocking restores editing.
