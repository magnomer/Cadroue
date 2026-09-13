# LRepertoire.cs

## `public static string LRepertoireMuxerResolve(string lContainer, string lOutputPath)`

The single authoritative output format identity: the muxer FFmpeg must be told to write.
A named container answers directly.
"Same as source" and any unnamed container fall back to the container family the output suffix belongs to.
So a suffix FFmpeg would otherwise read as a different format (m4v, f4v) still muxes as its family.

## `public static string LRepertoireAudioFind(string lCodecName)`

Accepts both an FFmpeg encoder token and an ffprobe codec_name.
So the settings dialog and the copied-stream check read the same family table.
