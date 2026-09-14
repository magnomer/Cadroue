# TEncodeMode.cs

## `public sealed class TEncodeMode`

Locks the three video modes (Copy / Smart / Encode) and the legacy Auto normalization through the production stage builder.
Also locks rate-control mode arguments, lossless conflict suppression, two-pass staging, and VP8 identity.

## Inline notes

### `IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeResolve(work, 12, 28)`

Interval is [10, 30], and interior keyframes at 12 and 28 align with neither bound.

### `Assert.Equal(8, stages.Count)`

head, middle, tail — each followed by its MPEG-TS join piece — plus the join.
