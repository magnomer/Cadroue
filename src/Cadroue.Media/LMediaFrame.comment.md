# LMediaFrame.cs

## `public static LMediaFrame? LMediaFrameRead(`

Decode a single RGBA frame from the stored source at the given position, without subtitles, overlays, or any preview correction.
The stream's own display matrix is applied.
So the output carries display-oriented pixels in the same space as the dimensions `LMediaInfo` publishes and every preview backend shows.
