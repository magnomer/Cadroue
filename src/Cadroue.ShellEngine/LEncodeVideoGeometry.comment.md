# LEncodeVideoGeometry.cs

## Inline notes

### `if (lCrop.LWorkFlipHorizontal)`

Flyleaf applies its flip flags in source space, before Rotation.
Keep FFmpeg's sequential filter graph in that same order so combined transforms match preview.
