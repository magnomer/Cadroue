# LScoutBridge.cs
Hash: `58f4742880f9af3e`

## `internal static async Task<IReadOnlyList<LKeyframeEntry>> LScoutBridgeRead(`

An intra-only codec can splice anywhere, so the range ends stand in for keyframes without a scan.
Any failure other than cancellation yields no keyframes rather than failing the job.

## `private static async Task<bool> LScoutBoundaryCheck(`

HEVC accepts a boundary unless the trace proves it dependent.
H.264 accepts one only when the trace proves it an IDR picture.

## `internal static async Task<bool?> LScoutRefreshRead(`

Codecs other than H.264 and HEVC are not inspected and count as independent.
Null means the trace could not answer, never that the boundary is dependent.

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
