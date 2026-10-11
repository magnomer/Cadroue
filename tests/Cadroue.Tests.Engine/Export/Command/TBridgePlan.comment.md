# TBridgePlan.cs
Hash: `df03bdad9d075168`

## `public sealed class TBridgePlan`

Locks the smart-plan stage composition.
That is which stages a bridged interval emits, the span timing each carries, and the container every temporary stage uses.

## `public void WholeCopyableVideoWithCopyAudio_UsesOrdinarySinglePassCopy()`

Both boundaries are keyframes.
Smart must use the same simultaneous stream copy timing as Copy instead of manufacturing separate Matroska timelines.

## Inline notes

### `Assert.Equal(8, stages.Count)`

head, middle, tail (each followed by its MPEG-TS join piece) + audio + join.

### `IReadOnlyList<LEncodeStage> matroskaStages = stages`

The head/middle/tail spans and the audio stage keep the requested Matroska container.
The per-piece MPEG-TS remux is a separate join requirement.

### `Assert.Equal(6, stages.Count)`

middle, middle piece, tail, tail piece, audio, join (no head).
