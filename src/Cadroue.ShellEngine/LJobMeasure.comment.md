# LJobMeasure.cs

## `private IReadOnlyList<long> LJobMergeRead()`

Fallback per-input byte sizes for a merge whose sources were not measured at add time.

## `private LWorkMedia? LJobOutputResolve(LWorkMedia? pOutputMedia, string pOutputPath, LWorkMedia? pSourceMedia)`

Output figures are stored on the item when the job finishes.
The keyframe-interval packet scan runs here only when the video was re-encoded.
A stream copy leaves the interval unchanged, so it is inherited from the source.
Output loudness is not decoded here.
A copied audio stream inherits the source loudness synchronously (no extra decode).
A re-encoded stream is measured off the runner loop by `LSubsidiary` after the job commits (see `LJobRun`).
This keeps a simple split, which copies both streams, from re-reading each finished output between jobs.
That full read would stall the runner loop on a spinning disk.
The source is never measured here.
Its figures come from the record made when the file was added to the worklist.
