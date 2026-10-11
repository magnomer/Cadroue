# TInspectorCropOrientation.cs
Hash: `42575dcd2c640232`

## `public sealed class TInspectorCropOrientation`

Checks the crop section's rotate and flip state.

## `public void RotateSelect_Index_MapsToDegrees(int index, int rotation)`

Each combo index maps to a quarter turn from 0 to 270 degrees.
The index is kept so the combo reads back the same.

## `public void RotateSelect_Negative_IsIgnored()`

A negative index means no selection, so the current rotation stays.

## `public void FlipSet_EachAxisIndependent()`

Horizontal and vertical flips are separate states.
Setting one axis never changes the other.

## `public void RotateSelect_WithEdges_RemapsEdgesLikeGeometry()`

Existing edges are remapped by the crop geometry orientation resolve.
The box therefore stays on the same picture area after a rotation.

## `public void OrientationSet_SameValues_RaisesNothing()`

Selecting the current rotation or flip raises no state notice.
