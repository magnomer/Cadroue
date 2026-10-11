# LJobMeasure.cs
Hash: `3b172dd94325365e`

## `private IReadOnlyList<long> LJobMergeRead()`

Fallback per-input byte sizes for a merge whose sources were not measured at add time.

## `private async Task<LWorkMedia?> LJobOutputResolve(`

Output figures are stored on the item when the job finishes.
The keyframe-interval packet scan runs here only when the video was re-encoded.
A stream copy leaves the interval unchanged, so it is inherited from the source.
Output loudness is not decoded here.
Audio left unprocessed inherits the source loudness without an extra decode.
Processed audio is measured off the runner loop by `LSubsidiary` after the job commits (see `LJobRun`).
This keeps a simple split, which copies both streams, from re-reading each finished output between jobs.
That full read would stall the runner loop on a spinning disk.
The source is never measured here.
Its figures come from the record made when the file was added to the worklist.
