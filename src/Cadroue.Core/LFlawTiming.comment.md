# LFlawTiming.cs

## `private const long LFlawWrapGuard = 8_000_000_000L`

MPEG-TS 33-bit 90 kHz timestamps wrap at 2^33.
A decode-timestamp step that falls back from above this guard is a legal wraparound, not a defect.

## Inline notes

### `if (lFlawPts is null && lFlawDts is not null)`

A packet with neither timestamp is not reconstructable from timing alone.
It is a decode-recovery case, so it does not raise a timeline defect here.

### `bool lFlawMissingPts = lFlawMissingPtsCount.Any(lFlawEntry`

A stream counts as missing presentation timestamps only when its packets that carry one outnumber those that lack one.
A stream with no presentation timestamps, or only a stray one, raises no finding.
Its gap is not treated as reconstructable.

### `var lFlawFlags = new List<string>()`

The genpts flag regenerates presentation timestamps from decode order.
The igndts flag drops the unreliable decode timestamps, so the muxer re-derives them from the presentation timing.
Both are input flags placed before the input.
