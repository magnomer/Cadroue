# QSplitTab.cs
Hash: `b6c67457c6335aac`

## `public sealed class QSplitTab`

Owns the Split tab behavior on top of the inspector, viewer, list and flow.
The veneer only constructs controls and subscribes to this class.

## `public static readonly IReadOnlyList<QProcessingRow> QSplitRows =`

The processing list rows in detector order, one per detector kind.
Their keys are the sensor names, so the list and the inspector agree on step identity.

## `public QSplitTab(`

Turns the section band on and subscribes sensor, blank and neutral-sample notices.
It also creates the sweep sibling.

## `public event Action? QSplitPresetMissing;`

Raised when a run is asked while no export preset is valid, so the veneer can tell the user.

## `public void QSplitClose()`

Cancels a running sweep and removes every subscription made in the constructor.

## `public void QSplitStart() => qSplitFlow.QFlowEdit.QFlowEditSet(!QSplitLockedCheck());`

Initial editability follows the lock state of the current file.

## `public LSceneTabEntry QSplitLayoutRead(LSceneTabEntry lLayout)`

Writes the detector bundle and the persistent switch into the scene record.

## `public void QSplitLayoutApply(LSceneTabEntry? lLayout)`

A missing record minimizes the processing list and the inspector.
Otherwise the saved detectors and the persistent switch are restored.

## `public void QSplitRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)`

Describes work for the current file only, with the sections of the flow.
An unlocked selection and a valid preset are required.

## `public void QSplitAllRun(Guid lRelayTarget, Guid lSourceTab)`

Queues every unlocked entry at normal priority, each with the sections saved beside that file.
A file without saved sections queues nothing.

## `public void QSplitItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)`

Like the all run, but limited to unlocked entries whose path was handed in.
Paths compare without regard to case.

## `public void QSplitPathHandle(string? lPath)`

Ignores a blank path and a path the viewer already shows or is loading.
Detector state is reloaded from the sidecar only when the persistent switch is off.

## `public void QSplitLockHandle(bool lLocked) => qSplitFlow.QFlowEdit.QFlowEditSet(!lLocked);`

A locked file cannot have its sections edited.

## `public void QSplitPickHandle(bool lArmed) =>`

The blank picker samples the grey target of the viewer's neutral tool.

## `public void QSplitSampleHandle(LNeutralSample lSample) =>`

Hands the sampled color to the blank section.

## `public void QSplitChangeHandle()`

Every detector edit refreshes the processing list and saves the state.

## `public void QSplitPersistentHandle(bool lPersistent)`

Turning persistence on starts a detection on the current file right away, without a click.

## `public void QSplitActiveUpdate()`

Mirrors each detector's enabled flag into the processing list row of the same key.

## `public void QSplitStateSave()`

Skips while a bulk apply suspends saving, without a source, or when the file is locked.
An inactive record is written only if a sidecar record already exists, so no empty file appears.

## `public void QSplitStateLoad(string lPath)`

Restores the sidecar record of the file when one exists, and leaves the current state otherwise.

## `public LDetectorBundle QSplitStateRead()`

Collects the six detectors, the three mode switches and the preset tokens as the inspector holds them.
A detector without a preset token is stored as an empty token.

## `private void QSplitStateApply(LDetectorBundle lSet)`

Applies under a suspended save, so the change notices of the setters write nothing back.
Absent optional parts of the bundle leave the current value unchanged.

## `private bool QSplitPresetCheck()`

Raises the missing notice when the check fails.

## `private LDocketEntry? QSplitSelectedRead() =>`

The current entry only if it exists and is unlocked.

## `private static QProcessingRow QSplitRowCreate(LDetectorKind lKind, string lIcon, string lLabel) =>`

Builds a row from the sensor name, the panel icon path and the step label key.
