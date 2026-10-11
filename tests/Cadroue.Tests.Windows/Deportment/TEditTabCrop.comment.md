# TEditTabCrop.cs
Hash: `613cdcea62681d46`

## `public sealed class TEditTabCrop`

Checks the Edit tab deportment where it hands crop state between the inspector, viewer and overlay.
The tab tests build it over real siblings and a seeded preset store.
The Preset collection keeps it from running beside tests that share the preset store.
Librarian seams are attached for a test and always cleared afterwards.

## `public void MediaChange_SidecarPlan_ReachesViewerRotateAndRect(int rotation, LRotateKind expected, double width, double height)`

A saved sidecar plan must reach the viewer when its media opens.
Quarter turns swap the source width and height the inspector sees.
The crop rectangle is expressed in the rotated frame.

## `public void MediaChange_SkipSaved_PushesNeutralViewer()`

A plan saved with skip must leave the viewer without rotation or crop.
The processing list shows skip as active.

## `public void CropHandle_RatioAndPersistent_ReachOverlayOwner()`

Inspector edits to ratio, persistence and apply are forwarded to the overlay owner.
Turning the fixed ratio off clears the overlay ratio.

## `public void LockHandle_Locked_CancelsNeutralAndDisarmsTool()`

Locking the tab must cancel a running neutral pick.
The crop tool returns once the lock is released.

## `public void RotateCropResolve_MapsRotationAndFlips()`

Only quarter turns map to a rotation.
Any other angle resolves to no rotation.
Both flips carry through unchanged.
