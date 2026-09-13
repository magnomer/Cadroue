# TBridgeKeyframe.cs

## `public sealed class TBridgeKeyframe`

Locks which plan the interior keyframes of an interval select.
That is a hybrid bridged plan when usable keyframes exist, and a whole-interval encode when none do.

## Inline notes

### `IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeResolve(work, 12, 28)`

Interval is [10, 30], and interior keyframes at 12 and 28 align with neither bound.

### `LEncodeStage stage = Assert.Single(TEncodeCommand.TBridgeResolve(work))`

No usable interior keyframe within [10, 30]: whole-interval fallback.
