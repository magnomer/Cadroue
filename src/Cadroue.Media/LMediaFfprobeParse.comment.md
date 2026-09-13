# LMediaFfprobeParse.cs

## `private static int LMediaRotationResolve(JsonElement videoStream)`

Report the stream's display orientation in degrees, from the display matrix side data or the legacy rotate tag.
FFmpeg, mpv and Flyleaf all present media already turned by this angle, so every dimension Cadroue publishes is display-oriented.
