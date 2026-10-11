# TEncodeMode.cs
Hash: `866772d496f130d4`

## `public sealed class TEncodeMode`

Locks the three video modes (Copy / Smart / Encode) and the legacy Auto normalization through the production stage builder.
Also locks rate-control mode arguments, lossless conflict suppression, two-pass staging, and VP8 identity.

## Inline notes

### `IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeResolve(work, 12, 28)`

Interval is [10, 30], and interior keyframes at 12 and 28 align with neither bound.

### `Assert.Equal(8, stages.Count)`

Head, middle and tail are each followed by their MPEG-TS join piece.
The separate audio stage and the final join make eight.
