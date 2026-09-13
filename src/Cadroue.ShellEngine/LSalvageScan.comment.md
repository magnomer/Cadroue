# LSalvageScan.cs

## `private const double LSalvageGapSeconds = 1.0`

Two decodable packets separated by more than this many seconds straddle a dead region.
They are not treated as one continuous span.

## Inline notes

### `return LSalvageWholeResolve(lSalvageSource, lSalvageToken)`

The packet probe found no usable span (no video stream, or a container the demuxer could not walk).
Fall back to the whole measured duration so the run still copies every readable byte rather than salvaging nothing.

### `if (lSalvageFields[3].Contains('C', StringComparison.OrdinalIgnoreCase))`

The demuxer marks a torn packet corrupt.
End the current span at it and resume a fresh span only once clean packets return.
