# TBridgeAudio.cs

## `public sealed class TBridgeAudio`

Locks the audio side of a bridged plan.
That is the single continuous audio stage and the track selection it carries.
It also locks how the final mux maps video and audio exactly once.

## Inline notes

### `string[] videoLabels = { "Encoding head bridge", "Copying middle", "Encoding tail bridge" }`

Video is split into head/middle/tail, and none of them touch audio.

### `IReadOnlyList<string> audioTokens = TEncodeToken.TEncodeTokenRead(stages[^2].LEncodeStageArguments)`

Audio is one continuous stream-copy region cut over the whole requested interval.
It retains source-relative packet timestamps so delayed tracks stay delayed.

### `Assert.Equal("0:a:0", TEncodeToken.TEncodeOptionRead(audioTokens, "-map"))`

Still one continuous audio region over the requested interval, not per-region pieces.

### `Assert.Equal(7, stages.Count)`

head, middle, tail (each with its join piece), join — no audio stage.
