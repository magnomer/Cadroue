# TEncodingBoundary.cs
Hash: `06243e15ed4715bf`

## `private string TEncodingSourceCreate()`

Builds a red-then-green source whose only keyframe precedes the cut.
Open GOPs and B-frames mean the frames just after the cut reference red pictures before it.
So any mode that leaks pre-cut presentation shows red as the first visible frame.

## `private string TEncodingRefreshCreate()`

Joins three single-colour open-GOP parts, so each part starts with an independent refresh.
Smart then has a usable keyframe inside the interval and must keep the copied middle.

## Inline notes

### `Assert.InRange(TEncodingDurationRead(output), TEncodingCutEnd - TEncodingCutOrigin, TEncodingCutEnd - TEncodingCutOrigin + 0.35)`

Pure packet copy cannot manufacture an independent decoder refresh at an open-GOP boundary.
Its preroll/reorder allowance is intentionally tested separately from Smart, which must produce an exact clean cut.

### `LEncodeStage copy = Assert.Single(stages)`

The whole source is copyable end to end.
That is one stream copy, no tail bridge, no concat, and no re-encode that could reject the source profile/pixel format.
