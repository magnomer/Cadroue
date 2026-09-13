# LPreview.cs

## Inline notes

### `LColor lColor = lPreviewState.LColor`

The whole colour pipeline runs through this one lavfi graph.
So the live preview applies the adjustments in the exact order and form the export filter graph (`LEncodeVideo`) uses.
That order is white balance, exposure, then the batched eq for brightness/contrast/gamma/saturation.
