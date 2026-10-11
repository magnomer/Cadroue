# LEncodeVideoGeometry.cs
Hash: `aebb25e805a37b35`

## `private static LWorkCrop LEncodeGeometryNormalize(LWorkCrop lCrop, LWorkMedia? lMedia)`

The crop runs after rotation, so its edges are clamped against the rotated frame.
A source without video drops the crop entirely.
Without a probe the stored edges pass through unchecked.

## `private static string LEncodeScaleResolve(string lSize, bool lReactive)`

A reactive size follows the source orientation, putting the long edge on the wider axis.

## Inline notes

### `if (lCrop.LWorkFlipHorizontal)`

Flyleaf applies its flip flags in source space, before Rotation.
Keep FFmpeg's sequential filter graph in that same order so combined transforms match preview.
