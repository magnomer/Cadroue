# QSplitTab.cs

## `public sealed class QSplitTab`

The class owns the Split tab behavior on top of the inspector, viewer, list and flow.
The veneer only constructs controls and subscribes to this class.
The detector state, the work descriptions and the sweep each live in a sibling it creates.
The siblings are `QSplitTabDetector`, `QSplitTabWork` and `QSplitTabSweep`.

## `public QSplitTab(`

The constructor turns the section band on and builds the section before it subscribes.
So building the section raises none of the subscribed events.
It subscribes the detector gate, blank, persistent, neutral-sample, run, stop and busy notices, and the list and viewer notices.

## `public void QSplitClose()`

Close first closes the sensor sweep surface and cancels a running sweep.
It then removes every subscription made in the constructor.
The detach also clears the gate's running flag, so a closed tab never leaves the sweep row busy.
Test `SweepButton_AfterClose_DoesNotStart` pins that the button starts nothing after close.
Test `Close_DetachesPathLockAndDropHandlers` pins the detached path, lock and drop handlers.

## `public void QSplitStart()`

Initial editability follows the lock state of the current file.

## `public void QSplitLockHandle(bool lLocked)`

A locked file cannot have its sections edited.
Test `LockHandle_LockedDisablesSectionEditing` pins it.

## `public LSceneTabEntry QSplitLayoutRead(LSceneTabEntry lLayout)`

The layout takes the detector bundle and the persistent switch.
Test `LayoutRead_CarriesEveryDetectorAndPersistent` pins both.

## `public void QSplitLayoutApply(LSceneTabEntry? lLayout)`

A missing record minimizes the processing list and the inspector.
Otherwise the saved detectors and the persistent switch are restored, then the list's active marks are refreshed.
Test `LayoutApply_Null_MinimizesProcessingAndInspector` pins the missing record.
Test `LayoutApply_RoundTripsIntoSecondTab` pins the restore.

## `private void QSplitBusyHandle(bool lBusy)`

The sweep reports busy and the tab mirrors it into the gate's running state.
So the sweep driver never touches the sensor surface.

## `private void QSplitOpenHandle(string lPath)`

After the viewer opens a path, its saved split state loads only when the persistent switch is off.
A persistent switch keeps the detector values across files.
Test `PathHandle_NotPersistent_LoadsSidecar` and test `PathHandle_Persistent_KeepsState` pin both cases.

## `public void QSplitPickHandle(bool lArmed)`

The blank picker arms the grey target of the viewer's neutral tool.

## `public void QSplitSampleHandle(LNeutralSample lSample)`

The sampled colour goes to the blank detector.

## `public void QSplitChangeHandle()`

Every detector edit refreshes the processing list and saves the state.
Test `SensorChange_UpdatesProcessingActive` and test `DetectorGateChange_UpdatesProcessingActive` pin the refresh.

## `public void QSplitPersistentHandle(bool lPersistent)`

The state is saved first.
Turning persistence on then starts a sweep on the current file without a click.

## `private static QProcessingRow QSplitRowCreate(LDetectorKind lKind, string lIcon, string lLabel)`

The row key is the detector's name from the detector plan.
Test `Rows_OneStepPerKind_KeyedBySensorName` pins one row per kind.
