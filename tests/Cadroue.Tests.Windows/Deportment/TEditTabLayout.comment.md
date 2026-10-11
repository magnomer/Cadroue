# TEditTabLayout.cs
Hash: `84e42858059f7221`

## `public sealed class TEditTabLayout`

Checks the Edit tab deportment layout read and apply used for scene save and restore.
No control is built.
The Preset collection keeps it from running beside tests that share the preset store.

## `public void LayoutRead_NothingPersistent_LeavesInspectorAbsent()`

Edits that are not persistent never enter the scene record.
The record then holds no inspector part at all.

## `public void LayoutRead_PersistentFlags_RoundTripIntoSecondTab()`

Persistent crop, video kinds and skip must survive a read followed by an apply on a new tab.
Kinds that were not persistent stay unmarked after the restore.
The plan save hold is released afterwards.

## `public void LayoutApply_CropNotPersistent_LeavesCropAlone()`

A record whose crop is not persistent must not write crop edges into the tab.
Other persistent parts, such as skip, are still restored.

## `public void LayoutApply_Null_MinimizesInspector()`

A tab restored without a layout record starts with the inspector minimized.
