# TInspectorCropEdge.cs
Hash: `113de5e50dfa0ef9`

## `public sealed class TInspectorCropEdge`

Checks the inspector crop section for edge text commits, ratio-fixed behaviour and resets.
Each test starts from a 1920 by 1080 source, so edge values are in source pixels.

## `public void EdgeCommit_Text_WritesClampedEdges()`

A commit stores a valid number as the edge.
A negative or non-numeric entry leaves the edge at zero.
The resolution text follows the remaining size.

## `public void EdgeFormat_EchoesEquivalentText()`

The echo shows the text the stored edge would produce.
Text that would store the same value is kept as typed.
Empty text stays empty only for an edge that holds zero.

## `public void EdgeCommit_FixedRatio_FollowsRatioAcrossAxis()`

With a fixed ratio preset, editing one edge moves another so the box keeps its ratio.
The ratio-fixed flag is raised by the preset.

## `public void EdgeReset_ClearsEdges_KeepsRatioOff()`

Reset returns every edge to zero and leaves the ratio unfixed.

## `public void CropSet_DrawnRect_BecomesEdges_NullClears()`

A rectangle drawn in the viewer converts to edges measured from each side.
A null rectangle clears the crop.

## `public void CropSet_FixedRatio_SnapsDrawnRect()`

With a fixed ratio, a drawn rectangle is snapped to that ratio before it is stored.

## `public void CropReset_PersistentSkips_OtherwiseClears()`

A persistent crop survives a media reset along with its armed tool.
Without persistence the reset clears the edges and disarms the tool.
