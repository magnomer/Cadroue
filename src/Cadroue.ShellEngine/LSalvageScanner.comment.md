# LSalvageScanner.cs
Hash: `9101d7e33f86d61f`

## `private const double LSalvageGapSeconds = 1.0`

Two decodable packets separated by more than this many seconds straddle a dead region.
They are not treated as one continuous span.

## `internal static async Task<IReadOnlyList<LSalvageSpan>> LSalvageScanRun(`

Any probe failure other than cancellation yields no spans, so salvage is skipped rather than failing the job.

## Inline notes

### `return await LSalvageWholeResolve(lSalvageSource, lSalvageToken).ConfigureAwait(false)`

The packet probe found no usable span (no video stream, or a container the demuxer could not walk).
Fall back to the whole measured duration so the run still copies every readable byte rather than salvaging nothing.

### `if (lSalvageFields[3].Contains('C', StringComparison.OrdinalIgnoreCase))`

The demuxer marks a torn packet corrupt.
End the current span at it and resume a fresh span only once clean packets return.
