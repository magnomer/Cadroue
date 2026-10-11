# TViewfinderTicks.cs
Hash: `8a17ca5b00046579`

## `public sealed class TViewfinderTicks`

Checks the viewfinder frame records for ticks and the cursor chip, without any control.
Each test builds a flow over a media of known length.
Positions are pixels in a strip of the given width and height.

## `public void Ticks_PickTheStepFromTheZoom_AndLabelEachOne()`

The tick step follows the visible range.
A zoomed range gets finer ticks and a long media gets coarser ones.
Each tick carries its own label and span.

## `public void Frame_PlacesTheCursorChip_AndDropsItOutsideTheRange()`

The cursor appears only while the playhead is inside the working range.
The chip is centred on the cursor line with a fixed padding around the measured text.
A strip too short for the rail leaves fewer under shapes.
