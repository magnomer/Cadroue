# TBridgeAlignment.cs

## `public sealed class TBridgeAlignment`

Locks where audio lands relative to video after a bridged cut.
Delayed tracks keep their cut-relative offsets and ordinary tracks start together.
Audio lying outside the cut leaves the video mux intact.
