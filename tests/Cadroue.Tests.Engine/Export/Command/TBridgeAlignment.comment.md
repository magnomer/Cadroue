# TBridgeAlignment.cs
Hash: `add4160cb03c272a`

## `public sealed class TEncodeCommandCollection;`

Every encode-command test points the process-wide depot root at its own temporary folder.
So they all run alone in the `EncodeCommand` collection.
The definition sits in this file because the collection moved out of the shared interface project.

## `public sealed class TBridgeAlignment`

Locks where audio lands relative to video after a bridged cut.
Delayed tracks keep their cut-relative offsets and ordinary tracks start together.
Audio lying outside the cut leaves the video mux intact.
