# TSplitTabSidecar.cs
Hash: `84b4931d9ffd1fce`

## `public sealed class TSplitTabSidecar`

Checks how the Split tab saves and loads its detector record through the sidecar librarian.
Each test attaches a fake librarian that serves a stored record and captures every write.
The librarian is detached in a finally block so no test leaks it into another.
The Preset collection keeps it from running beside tests that share the preset store.

## `public void StateSave_ActiveDetector_WritesRecordThroughLibrarian()`

An enabled detector produces one record for the open source path.
The record carries all six detector entries.

## `public void StateSave_Suspended_WritesNothing()`

Inside the inspector save-suspend scope the tab must not write.
Bulk applies would otherwise persist half-restored state.

## `public void StateSave_LockedPath_WritesNothing()`

A path locked in the docket belongs to queued work.
Its sidecar must not be rewritten.

## `public void StateSave_InactiveAndAbsent_WritesNothing()`

With no detector enabled and no stored record there is nothing worth creating.

## `public void StateSave_InactiveButStored_OverwritesRecord()`

An existing record must not outlive the user turning every detector off.
The overwrite stores the record as inactive.

## `public void StateLoad_AppliesSensorAndBlankInsideSuspendScope()`

Loading restores the sensor steps and the blank step from the stored record.
The record holds no still entry, so the still mode reads discard.
Applying them must not write back, and the suspend scope is closed afterwards.

## `public void StateLoad_NothingStored_LeavesStateAlone()`

A missing record changes no inspector value and writes nothing.

## `public void PathHandle_NotPersistent_LoadsSidecar()`

A new source path pulls its own sidecar into the inspector.

## `public void PathHandle_Persistent_KeepsState()`

With the persistent switch on, the inspector keeps its current values.
The new path's sidecar is ignored.
