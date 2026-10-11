# LEncodeBridgeStage.cs
Hash: `b054abc3429f1be0`

## `internal static LEncodeStage LEncodeConcatBuild(`

Joins the bridge pieces by stream copy, adding the separately cut audio when there is one.
A positive audio offset delays the audio input, so it lines up with the first copied video frame.

## `private static string LEncodeTimescaleRead(`

Only an MOV-family target with a source time base of 1/N gets an explicit track timescale.
Any other target or time base reads empty, so FFmpeg keeps its own choice.

## `internal static void LEncodeBridgeClear(Guid lWorkId)`

Removes the concat list a job wrote, and leaves a locked or denied file in place without failing.

## Inline notes

### `TimeSpan lDecodeDuration = lDecodeEnd - lCopyOrigin`

A copied GOP must stop before the following keyframe's DTS.
Stopping at its PTS also copies that keyframe and its reordered frames into the encoded tail.
That produces duplicate preroll and an inflated timeline.

### `lArguments.Append(" -copypriorss 0 -c:v copy -an")`

The keyframe timestamps retain probe precision, so packets before the selected presentation boundary belong to the preceding GOP.
Keeping them can lengthen container timelines by a complete GOP.

### `return $"-video_track_timescale {lDenominator.ToString(CultureInfo.InvariantCulture)}"`

Smart may join independently muxed video pieces.
Without an explicit MOV/MP4 track timescale, the final remux can choose a different unit.
A neighboring Smart section that took the full-encode or direct-copy route may carry that other unit.
Such files are individually valid but concat later interprets their packet timestamps using one time base.
That shortens or lengthens video and corrupts the joined audio/video presentation timeline.
