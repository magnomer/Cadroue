# Options

Each driver here attaches to a passive surface from Veneer and owns the handlers it adds.
Conduct decides, the driver projects, and Veneer holds only controls.
The Veneer window detaches every driver on close, so no handler outlives the window.

## QSOptions.cs

`QSOptions` composes the page drivers around one `CSOptions` and one draft.
Nothing reaches the live preference before apply.
Test `Draft_EditsStayPending_UntilApply` pins that.

Apply reads the General surface first, so pending General edits reach the gate before it saves.
Test `Apply_CommitsOnce_ThroughPreference` pins one save.
Test `Apply_CopiesSnapshotAfterPendingEdits` pins the General read.

The engine read passes the current mpv availability to the gate.
The sidecar folder read builds a throwaway gate with no usher, since it needs only the location request.

## QSGeneral.cs

The driver holds the General snapshot.
The startup and orientation choices reach the gate at once.
The selection, the checkboxes and the language reach it only when the snapshot is read.
Detach reads the controls first, so edits survive a reattach.
The startup radio shows the tab picker only for the default tab, and the picker keeps its selection when hidden.
Test `StartupMode_TogglesPickerWithoutSelectionLoss` pins that.
Test `Attach_ProjectsDraftAndLanguages` pins the first projection.

## Stamp

Commit: `b84e5458de50`
- `QSGeneral.cs` `fcf62cbe1bd2c7b9`
- `QSOptions.cs` `c2d66221921be15b`
