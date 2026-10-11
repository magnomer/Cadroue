# TNeutralWheel.cs
Hash: `c8adb6c3d8fb1037`

## Inline notes

### `LNeutralSample sample = TNeutral.TNeutralColorResolve(x, y)`

A wheel pick reconstructs a gray.
Placing that gray back on the wheel must land on the same disc coordinate.
Value is irrelevant to the cast direction.

### `LNeutralSample sample = TNeutral.TNeutralColorResolve(3, 4)`

Coordinates outside the unit disc still yield a valid, bounded sample.
