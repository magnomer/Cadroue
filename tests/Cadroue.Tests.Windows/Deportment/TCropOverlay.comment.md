# TCropOverlay.cs
Hash: `a1bf9d6236f96464`

## `public sealed class TCropOverlay`

Checks the crop overlay deportment without any control.
Each test loads a 1920 by 1080 source into a viewer and sizes the overlay area.
Box, handle and shade reads are in overlay pixels while the viewer's crop stays in source pixels.

## `public void VideoRead_FitsRotatedSourceInsideArea(LRotateKind rotate, double areaWidth, double areaHeight, double x, double y, double width, double height)`

The video rectangle is the displayed picture fitted and centered in the area.
A quarter turn swaps the source's width and height before fitting.

## `public void RectSet_PlacesBoxHandlesAndShade_InOverlayPixels()`

One rectangle set places the box, all eight handles and the shade together.
The viewer keeps the crop in source pixels and the overlay is told once.

## `public void RectSet_Rotated_BoxFollowsDisplayTransform()`

A crop is stored against the unrotated source.
The box shown follows the rotation so it lands on the displayed picture.

## `public void RectSet_OutsideSource_ClampsToFullFrame()`

A rectangle larger than the source is clamped to the source before it is stored or shown.

## `public void RectSet_Null_HidesBoxAndClearsPreview()`

A null rectangle removes the crop everywhere.
The box, handles and shade hide and the viewer's preview holds no crop.

## `public void SizeHandle_Resize_RestoresBoxFromPreview()`

A resize of the overlay area redraws the box from the stored source crop.
The box scales with the area and the crop value is untouched.

## `public void ActiveAndLock_GateEditableAndCursor()`

Only the crop tool shows the cross cursor.
Deactivating crop hides the box and releases the tool.
A locked crop stays visible but loses its handles and its editing.
