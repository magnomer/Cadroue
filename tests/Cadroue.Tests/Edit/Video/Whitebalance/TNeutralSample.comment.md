# TNeutralSample.cs

## Inline notes

### `TNeutralPixelSet(pixels, 16, 12, 255, 0, 255, 255)`

A handful of blown/black single pixels inside the region must not move the result.

### `byte[] pixels = TNeutralFrameCreate(0, 255, 0, 0)`

Whole frame is transparent garbage, and only the neutral opaque pixels are sampled.

### `byte[] pixels = TNeutralFrameCreate(170, 160, 150, 255)`

Mild cast so no channel needs more than the 2x lift cap, giving full neutralization.

### `Assert.Equal(1, sample.LNeutralRedGain, 3)`

White target = brightest channel (red here): red stays put, deficient channels are only lifted, never pushed down.

### `byte[] pixels = TNeutralFrameCreate(240, 240, 240, 255)`

A near-white neutral pick: lenient picker must not blow it up, gains ~1.
