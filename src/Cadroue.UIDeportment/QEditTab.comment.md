# QEditTab.cs
Hash: `0e9dc1be5983cc8f`

## `public sealed class QEditTab`

Owns the Edit tab's Deportment and wires the inspector, viewer, crop and processing panel together.
Its siblings hold colour, the stored plan, the crop tool gate and the processing rows.

## `public QEditTab(LPresetSelection lPreset, QInspector lInspector, QViewer lViewer, QList lList, LDocket lDocket, QProcessing lProcessing)`

Builds the siblings first, so every subscription below can reach them.
Each subscription made here is removed in `QEditClose`, in the same order.

## `public void QEditClose()`

Removes every subscription the constructor made, so a closed tab receives no notice.

## `public LSceneTabEntry QEditLayoutRead(LSceneTabEntry lLayout)`

Writes the carried plan into the scene record only while some row is persistent.

## `public void QEditLayoutApply(LSceneTabEntry? lLayout)`

Restores the persistent plan from a scene record under one plan-save suspend.
A missing layout starts the inspector minimized.

## `public void QEditRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)`

Enqueues the selected unlocked file with the current plan, once the preset is valid.

## `public void QEditPathHandle(string? lPath)`

Opens a newly selected file in the viewer unless it is already loaded or pending.
Any neutral pick is cancelled first, even when the selection clears.

## `private void QEditCropRestore()`

Applies the carried or sidecar plan to a newly opened media file.
The carried plan wins, and only a winning carried plan is saved back to the sidecar.

## `private bool QEditPresetCheck()`

Refuses an enqueue when the preset is missing or cannot encode Edit work.
Each refusal raises its own notice, so the Veneer shows the matching message.
