# TInterfaceViewer.cs
Hash: `3579f097032b563d`

## `internal static class TInterfaceViewer`

The viewer area of the test interface.
Tests reach the viewer, its media, playback, source, renderer and neutral siblings only through these forwards.
It is a separate class, not a partial part, so its size stays visible.
The cargo, media-info and preview builders live here, and flow and tab tests use them too.
Most forwards add no logic.
