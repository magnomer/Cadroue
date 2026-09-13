# TNeutralPoint.cs

## `private static (double, double) TNeutralFinalResolve(`

Independent forward model of the mpv display pipeline: hflip, vflip, transpose(rotate), producing the pixel in final (post-transpose) space.

## Inline notes

### `var display = new TNeutralRect(12, 7, 300, 180)`

A letterboxed display rect that is not flush with the overlay origin.

### `foreach ((int sx, int sy) in TNeutralPixelRead())`

Only pixels that fall inside the shown crop region are recoverable.

### `return (fx + 0.5, fy + 0.5)`

Pixel centre so the resolver's floor recovers the exact pixel.
