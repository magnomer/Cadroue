# TBridgeContainer.cs

## `public sealed class TBridgeContainer`

Locks what a bridged cut does to awkward container geometry.
That covers B-frame reordering, sub-millisecond and rounded keyframe times, and non-zero start timelines.
It also covers odd frame rates and source time bases that later merges depend on.

## Inline notes

### `LWorkItem work = TBridgeFixture.TBridgeWorkCreate(source, 2.044, 18.393, "Include", true)`

The actual packet boundaries are 2.043708s and 18.393375s.
UI and sidecar times are millisecond-based, so both ends arrive rounded.

### `Assert.InRange(TBridgeMetric.TBridgeFormatRead(work.LWorkOutputPath), 16.45, 16.60)`

Simultaneous stream copy retains codec preroll just like ordinary Copy.
The later decode must preserve it instead of silently dropping audio.
