# LEncodeBridge.cs
Hash: `9b0780ac3be7d7c0`

## `public static async Task<LScoutAudioInterval?> LEncodeIntervalRead(`

Measures where audio packets sit only when a Smart plan re-encodes a boundary and audio is kept.
A whole-copy or non-Smart plan needs no offset, so it returns null without probing.

## `public static bool LEncodeSmartCheck(LWorkItem lWorkItem)`

True when the item must be fully encoded instead of Smart-cut.
That holds outside Split work, outside Smart mode, under a forced video re-encode, or with a changed frame rate.

## `internal static IReadOnlyList<LEncodeStage> LEncodeWholeBuild(`

The Smart fallback encode of the requested interval.
It carries the source track timescale, so its output joins neighbouring Smart sections on one time base.

## `public static IReadOnlyList<LEncodeStage> LEncodeSmartBuild(`

A boundary re-encode without a matching source-codec encoder yields no stages, so nothing is produced.
Audio is cut as its own piece and joined with the measured offset.
Unmeasured audio keeps a zero offset, and audio with no packets in the interval is dropped.

## Inline notes

### `return LEncodeWholeBuild(lWorkItem, lBridgeSource)`

STRICT SMART CONTRACT: full encoding is allowed only when the item demands it or planning found no copyable middle.
Once a middle exists, later uncertainty must never silently replace Smart with a full re-encode.

### `return new[] { LEncodeDirectBuild(lWorkItem, lBridgePlan.LBridgeMiddle, lAudioActive, lBridgeSource) }`

A keyframe-to-keyframe Smart interval is ordinary stream copy.
Keep the streams in one input timeline.
Splitting them through independent MKV intermediates can preserve different timestamp origins.
Those only become visible when the result is decoded by a later Edit/Convert operation.

### `if (lBridgePlan.LBridgeHead is not null && lLeadingCodec is "hevc" or "h265")`

The copied HEVC middle follows the head, so its open-GOP first keyframe must be neutralized before the join.
`LBridgeLeadingNormalize` does that rewrite.
A head-less plan starts on the middle, where a decoder discards leading pictures itself.
The splice edits the ISO-BMFF middle in place.
So it must run before that middle is remuxed into its join piece.

### `string lPiecePath = Path.ChangeExtension(lPartPath, ".ts")`

The concat demuxer carries only the first segment's parameter sets.
The ISO-BMFF pieces store SPS/PPS out-of-band in their sample-description box.
A copied middle's parameter sets can differ from the re-encoded head (weighted prediction, QP range, VUI).
It is then decoded against the head's sets and every slice desyncs.
Remuxing each piece to MPEG-TS emits its parameter sets in-band per packet.
So each segment stays self-describing across the join while the concat demuxer still stitches the piece timelines in order.

### `string lExtension = lIntermediateExtension ?? ".mov"`

Bridge pieces default to an ISO-BMFF container.
It preserves the copied middle's source timestamps and its mdat carries plain length-prefixed NAL units.
So the leading-keyframe neutralization is a direct byte rewrite.
