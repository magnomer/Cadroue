# LEncodeAudio.cs
Hash: `db328e6c2e3acb70`

## `internal static bool LEncodeExcludeCheck(LEncoding lOutput)`

Audio can be dropped from two settings, the stream choice or the mode, and either one suffices.

## `internal static void LEncodeMuxAppend(StringBuilder lArguments, LEncoding lOutput, bool lAllTracks)`

The mux stage of the staged audio pipeline.
Its first output audio track is the processed intermediate and always carries the configured encoder settings.
Any carried-through source track is copied, so encoder options never reach it.

## `private static void LEncodeSettingsAppend(`

Options come from the encoder's capability shape, so an encoder never receives a flag it lacks.
A `Custom` quality emits nothing, leaving the encoder default in force.
