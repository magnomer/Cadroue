# TEncodingBoundary.cs

## Inline notes

### `Assert.InRange(TEncodingDurationRead(output), TEncodingCutEnd - TEncodingCutOrigin, TEncodingCutEnd - TEncodingCutOrigin + 0.35)`

Pure packet copy cannot manufacture an independent decoder refresh at an open-GOP boundary.
Its preroll/reorder allowance is intentionally tested separately from Smart, which must produce an exact clean cut.

### `LEncodeStage copy = Assert.Single(stages)`

The whole source is copyable end to end.
That is one stream copy, no tail bridge, no concat, and no re-encode that could reject the source profile/pixel format.
