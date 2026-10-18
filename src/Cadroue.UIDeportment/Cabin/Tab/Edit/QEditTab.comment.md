# QEditTab.cs
Hash: `998f07c5e8b3d08a`

## `public sealed class QEditTab`

Owns the Edit tab's Deportment and wires the inspector, viewer, crop and processing panel together.
Its siblings hold colour, the stored plan, the crop tool gate and the processing rows.

## `public QEditTab(CUsher lUsher, CInspector lInspector, CEdit lEdit, LPresetSelection lPreset, QViewer lViewer, QList lList)`

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

Hands `CEdit.CEditRun` the preset owner's answers, the current path and every docket item, then acts on the verdict.
Only a Ran verdict enqueues the selected file with the current plan; the dispatch stays here until the plan can reach a gate.

## `public void QEditItemsRun(IReadOnlyList<string>? lPaths, Guid lRelayTarget, Guid lSourceTab)`

Hands `CEdit.CEditAllRun` the preset owner's answers and every docket item, which picks and dispatches the run.
No paths runs every unlocked item; the Add All action subscribes with no paths.

## `public void QEditPathHandle(string? lPath)`

Opens a newly selected file in the viewer unless it is already loaded or pending.
Any neutral pick is cancelled first, even when the selection clears.

## `private void QEditCropRestore()`

Applies the carried or sidecar plan to a newly opened media file.
The carried plan wins, and only a winning carried plan is saved back to the sidecar.

## `private bool QEditVerdictApply(CEditVerdict lVerdict)`

Raises the notice of a refused preset, missing or incompatible, so the Veneer shows the matching message.
Answers true only for a run that went.
