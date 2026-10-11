# TBridgeCodec.cs
Hash: `b3ee384f93678ad9`

## `public sealed class TBridgeCodec`

Locks the source-codec match behind a boundary re-encode.
That is which encoder each source codec maps to and when an unmapped codec refuses the plan.
It also locks when the leading splice normalization is inserted.

## `public void HevcHeadlessPlan_HasNoLeadingNormalizeStage()`

No head bridge: the copied middle is first.
So a decoder discards its leading pictures at the stream start and no neutralization is required.

## `public void UnsupportedCodecWithBridges_ProducesNoStages()`

A boundary re-encode is required (head + tail) but no encoder maps to mpeg2video.
Smart encoding fails outright rather than mismatching the copied middle.

## `public void UnsupportedCodecWholeCopyable_StillCopiesWithoutAnEncoder()`

No head/tail: the whole video is stream-copied, so the unmapped codec never matters.
