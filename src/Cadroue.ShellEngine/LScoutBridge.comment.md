# LScoutBridge.cs

## Inline notes

### `bool lScoutHevc = lScoutStream.LBridgeCodec.ToLowerInvariant() is "hevc" or "h265"`

Only splice boundaries must be independent.
Internal keyframes stay in one continuous copied stream and inspecting every one adds seconds of trace_headers work without changing decodability.
Reject unsafe leading candidates until a usable copy start is found.
When the requested end is not itself keyed, do the same backwards for the tail bridge start.

### `TimeSpan lScoutSeek = lScoutKeyframe > TimeSpan.FromSeconds(1)`

Seek before the target so a reordered key packet is not lost because its DTS precedes its PTS.
FFmpeg may also expose the preceding key packet.
Therefore inspect all packets in the short window and classify the last key packet, which is the requested boundary.
