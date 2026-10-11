# TEditTabRows.cs
Hash: `eb19040d5fc0fe4e`


## `public sealed class TEditTabRows`

Protects the processing rows of the Edit tab and the video plan read from their order.
It also covers the enabled flag and its notice on the processing Deportment.

## `public void Rows_EightSteps_CropFirstCurveLast()`

The row table fixes the default step order and the asset and label naming every row follows.

## `public void VideoRead_FollowsProcessingOrder_SkipsCrop()`

Crop is not a color kind, so it never becomes a video step.
The Edit tab turns the ordered flag off, so its steps keep the fixed order and cannot be dragged.

## `public void VideoRead_MpvIncapable_DropsMpvOnlyKinds()`

Without the mpv engine the kinds that only mpv can render must not enter the plan.
The plain color kinds stay available.

## `public void ColorUpdate_ActiveSteps_MarkTheirRows()`

A row is marked only when its own inspector step is active.
Crop is never marked here because it is not a color step.

## `public void EnabledSet_Notice_KeptWhileDisabled_ClearedWhenEnabled()`

A repeated disable with the same notice raises no second change.
Enabling clears the notice and raises once more.
