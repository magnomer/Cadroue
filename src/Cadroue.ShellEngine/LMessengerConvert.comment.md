# LMessengerConvert.cs
Hash: `3a4a72da8526a0b5`

## `public static partial class LMessenger`

Turns a Convert tab request into work items and routes them into the schedule.

## `public static async Task<int> LMessengerConvertDescribe(`

Without an export encoding nothing is queued and the count is zero.
Durations are resolved asynchronously, then the items are created and routed.
Each source keeps the batch it arrived with.
Sources that arrived without one share a single batch minted for this request.
Any failure is recorded through LTraceEngine and reported as zero queued.
