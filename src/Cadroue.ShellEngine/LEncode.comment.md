# LEncode.cs
Hash: `0899a6d11790f316`

## `internal const string LEncodeIntermediate = "pcm_s32le"`

The working format between the extract, analyze and process stages.
32-bit signed PCM carries every integer sample depth a source can hold.
So filtering does not quantise the material down before it reaches the final encoder.

## `private static bool LEncodeCopyCheck(LWorkItem lWorkItem, LEncoding lOutput)`

Whether the whole-file command re-encodes nothing.
Video is stream-copied (mode Copy and no forced re-encode) and audio is copied or excluded.
Such a run is surfaced as "Copying", not "Encoding".

## `private static string LEncodeMergeSave(LWorkItem lWorkItem)`

Writes the concat list of a merge into the depot merge folder, named by work id.
Paths take forward slashes and escaped single quotes, as the concat demuxer's quoting requires.

## `private static IReadOnlyList<LEncodeStage> LEncodeStepsBuild(LWorkItem lWorkItem)`

Audio-tab work with an active chain runs as extract, optional analysis, process and mux stages.
The intermediate WAV files live in the depot audio folder and are marked temporary.

## `private static IReadOnlyList<LEncodeStage> LEncodeWholeBuild(LWorkItem lWorkItem, LEncoding lOutput)`

The single whole-file command for every work kind except Fix and filtered audio.
Audio-tab work lands here when it has nothing to filter or excludes audio outright.
Its audio honours the output contract: Exclude writes no audio, Copy copies, Encode encodes.
A two-pass video mode puts the log-only first pass ahead of it.

## `private static LEncodeStage LEncodePassBuild(LWorkItem lWorkItem, string lPassArguments)`

The first pass of a two-pass mode writes no media, only the rate-control log.
Its stage path is the per-job log folder, marked temporary, so job cleanup removes the whole folder.
Both passes carry the same filters, so the log describes the frames the second pass will encode.

## `private static bool LEncodePassAppend(StringBuilder lArguments, int lPass)`

Pass 1 ends the command right after the video arguments: no audio, no muxer, a null output.
Any later pass falls through and writes the real output.

## `internal static void LEncodeMuxerAppend(StringBuilder lArguments, LWorkItem lWorkItem)`

Every command that writes the final output states its muxer explicitly.
So the container the user chose is the one FFmpeg writes, and the output suffix never decides the format.

## Inline notes

### `lArguments.Append(" -copypriorss 0")`

Stream-copy seeking otherwise retains packets before the requested boundary.
At a keyframe cut that exposes the preceding section in the output even though the user selected an exact interval.

### `lArguments.Append(" -ss 0")`

Fast input seeking can expose preroll packets from copied companion streams.
Discard them at the output boundary when video is decoded.

### `if (!lProcessed)`

Nothing is filtered, so the source stream is muxed straight through.
It keeps whatever the Audio settings ask for, Copy included.
No intermediate is written, so no precision is spent on a stream that is not processed.

### `lMux.Append(" -map 0:a? -map -0:a:0?")`

The processed track replaces the one extraction explicitly took (0:a:0).
Every other source track is carried through untouched.
