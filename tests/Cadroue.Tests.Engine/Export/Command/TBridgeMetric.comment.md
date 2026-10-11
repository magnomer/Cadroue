# TBridgeMetric.cs
Hash: `36a5b11d003af9e0`

## `internal static class TBridgeMetric`

Reads what a produced file actually contains: packet timestamps, stream and format durations, frame counts, time bases, decode errors.
It also owns the process runner every FFmpeg and FFprobe call in these tests goes through.
Its asserts only guard a tool exit or a parse, never a bridged expectation.
