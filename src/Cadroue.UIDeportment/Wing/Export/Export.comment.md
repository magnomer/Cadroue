# Export

This folder holds the Export panel's state owner and its drivers.
The Veneer panel is passive, so everything that reacts, repaints or changes hands lives here.
Conduct decides what an action means, and this folder decides when and in what order it runs.
The list and its rows live in the `Preset` folder.

## QExport.cs

The class owns the projection of the preset store and the selection for one panel.
It is the one place that wires Conduct policy to the real preset store.
It builds each driver once and hands it out as a property.

Store changes and selection changes both arrive here and end in one repaint event.
Sync is skipped while a name is edited or a drag is active.
So a rebuild never destroys the editor or the dragged row.
The drag clear syncs once when a real drag ends, and the rename flow syncs after commit or cancel.
Test `Drag_DefersSync_UntilCleared` pins the deferral.

The failure-raise overloads run an operation with a request flag set.
Change events it causes then do not repaint mid-operation.
The flag is cleared in a finally block.
One repaint follows only on success, and the failure callback runs on failure.
Test `PresetAdd_PublishesOneRepaintOrFailure` pins one repaint.
Tests `PresetDeleteFailure_ReportsSelectionWithoutRepaint` and `ModificationApplyFailure_ReportsSelectionWithoutRepaint` pin the failure path.

Attach subscribes to the store and the selection, then syncs once.
Detach ends the name editor and unsubscribes both.
The panel calls them from Loaded and Unloaded, so a closed panel stays out of the static store event.

The summary digest is read from the working copy at call time.
Test `Projection_FollowsOwnerWithoutSync` pins that it follows the owner.

## QExportCommands.cs

The class owns the Export surface attachments.
It attaches the command buttons, the title, the scroll bar and the preset body to the surface.
A caller may inject a handler for any command, and the default is used otherwise.
Icons, tooltips and dialogs are set up here, so the passive template never opens a window.
The repaint event is marshalled through the surface dispatcher.
The list attaches and the first update runs when the surface is attached, before Loaded.
Loaded then attaches the owner and updates again.
Unloaded detaches the list before the owner.

## QExportDigest.cs and QExportSummary.cs

The digest is an immutable snapshot of four lines.
The summary driver is the sole writer of the summary medium.
It attaches once after the surface initialises and keeps its four text references.
The panel updates it after the list rebuild, so the summary shows the state the list shows.

## QExportTransfer.cs

The driver turns a Conduct transfer result into exactly one of three callbacks.
A cancelled dialog calls none.
Success, write failure and refusal stay distinct, and the dialogs stay in the commands class.
Test `TransferResults_RefusalAndSuccess_DispatchExactlyOneOutcome` pins it.

## Stamp

Commit: `b84e5458de50`
- `QExport.cs` `ac2b5c243910ee8b`
- `QExportCommands.cs` `d8d877e87c1ba8eb`
- `QExportDigest.cs` `daeee07d925415c4`
- `QExportSummary.cs` `dc73e24351178a26`
- `QExportTransfer.cs` `c877c18ed1cd5078`
