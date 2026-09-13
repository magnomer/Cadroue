# LBridge.cs

## Inline notes

### `lBridgeKeyframes.Select(lTime => new LKeyframeEntry(lTime, lTime)).ToArray(),`

Callers that only have presentation timestamps describe a decode-order stream with no reordering.
Packet-based callers must supply real DTS.

### `? lBridgeMatchedOrigin.LKeyframePresentationTime`

UI and sidecar boundaries are millisecond-based.
Seeking to that rounded value can land just after the packet and, with -copypriorss 0, discard the complete first GOP.
Use the precise packet PTS that established the keyframe match.

### `bool lBridgeCopyToEnd = lBridgeOpenEnd`

When the requested end reaches the source end, the region from the copy start through EOF is whole GOPs.
That region is fully copyable: the final GOP ends at a natural boundary and needs no re-encoded tail.
Copy straight through to the end.
Only a genuine mid-stream end requires a tail bridge.
Reaching the end alone makes a lone-keyframe interval copyable, not a whole re-encode.

### `TimeSpan? lBridgeDecodeEnd = lBridgeCopyToEnd ? null : lBridgeLastWithin?.LKeyframeDecodeTime`

A copy that runs to EOF has no following keyframe to stop before, so it needs no decode-time cutoff.

### `lBridgeDecodeEnd = null`

DTS is an optional precision hint for stopping before the tail GOP.
A missing or malformed hint must not erase a presentation-time middle.

### `TimeSpan lBridgeFrame = lBridgeFramerate > 0`

A cut that stops within one frame of the source end reaches it.
The requested and source ends arrive rounded to milliseconds.
A trim of a genuine frame or more stays a real mid-stream end.
