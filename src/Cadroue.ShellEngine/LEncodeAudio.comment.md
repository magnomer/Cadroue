# LEncodeAudio.cs

## `internal static void LEncodeMuxAppend(StringBuilder lArguments, LEncoding lOutput, bool lAllTracks)`

The mux stage of the staged audio pipeline.
Its first output audio track is the processed intermediate and always carries the configured encoder settings.
Any carried-through source track is copied, so encoder options never reach it.
