# LMediaFrame.cs
Hash: `1cbba9c191f6e9d4`

## `public static async Task<LMediaFrame?> LMediaFrameRead(`

Decode a single RGBA frame from the stored source at the given position, without subtitles, overlays, or any preview correction.
The stream's own display matrix is applied.
So the output carries display-oriented pixels in the same space as the dimensions `LMediaInfo` publishes and every preview backend shows.
A failed decode or a short read yields null, so callers never receive a partial frame.
The decode runs at the background custody standing, so sampling never competes with queued work.
