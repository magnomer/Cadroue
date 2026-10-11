# LMediaFfprobeParse.cs
Hash: `2340450676967b33`

## `internal static LMediaInfo LMediaFfprobeParse(string json)`

Only the first video stream and the first audio stream are read.
A quarter-turn rotation swaps width and height, so published dimensions match what players show.

## `private static int LMediaRotationResolve(JsonElement videoStream)`

Report the stream's display orientation in degrees, from the display matrix side data or the legacy rotate tag.
FFmpeg, mpv and Flyleaf all present media already turned by this angle, so every dimension Cadroue publishes is display-oriented.

## `private static double LMediaFpsResolve(JsonElement videoStream)`

The real frame rate is preferred, and the average rate stands in when it is missing or undefined.
