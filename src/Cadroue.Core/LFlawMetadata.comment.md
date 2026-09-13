# LFlawMetadata.cs

## Inline notes

### `(double lFlawLow, double lFlawHigh) = LFlawSpanResolve(lFlawStreams)`

Two tracks whose own declared timelines disagree by more than a rounding margin is a container-metadata defect.
That holds even when the format duration matches the longest track.
The shorter essence has been stretched by an inflated per-track timescale or sample delta, not by real content.
