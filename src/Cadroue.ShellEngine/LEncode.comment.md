# LEncode.cs

## `internal const string LEncodeIntermediate = "pcm_s32le"`

The working format between the extract, analyze and process stages.
32-bit signed PCM carries every integer sample depth a source can hold.
So filtering does not quantise the material down before it reaches the final encoder.

## `private static bool LEncodeCopyCheck(LWorkItem lWorkItem, LEncoding lOutput)`

Whether the whole-file command re-encodes nothing.
Video is stream-copied (mode Copy and no forced re-encode) and audio is copied or excluded.
Such a run is surfaced as "Copying", not "Encoding".

## `public static IReadOnlyList<LEncodeStage> LEncodeFixBuild(`

One Fix repair pass: an optional source-to-output copy, the precedence-ordered repair stages, and the closing validation.
The copy runs on the first pass only, since later recompose passes repair the output in place.
The repair stages cover the given correctable dossiers.
The dossier set is supplied so a recompose pass rebuilds over only what a fresh output scan found still warranted.

## `private static IReadOnlyList<LEncodeStage> LEncodeWholeBuild(LWorkItem lWorkItem, LEncoding lOutput)`

The audio-tab work has nothing to filter (no active chain, or audio excluded outright).
So it is one ordinary whole-file command honouring the Audio output contract.
Exclude writes no audio, Copy copies, Encode encodes.

## `internal static void LEncodeMuxerAppend(StringBuilder lArguments, LWorkItem lWorkItem)`

Every command that writes the final output states its muxer explicitly.
So the container the user chose is the one FFmpeg writes, and the output suffix never decides the format.

## Inline notes

### `if (lFixAction.LRemedyDossier.LDossierRepair == LFlawFfvone.LFlawReport)`

A report-only dossier (FFV1 integrity) is detection-only: no ffmpeg stage can correct a slice-CRC mismatch.
It is copied unchanged and surfaced as Unresolved at validation, never re-encoded here.

### `lArguments.Append(" -copypriorss 0")`

Stream-copy seeking otherwise retains packets before the requested boundary.
At a keyframe cut that exposes the preceding section in the output even though the user selected an exact interval.

### `lArguments.Append(" -ss 0")`

Fast input seeking can expose preroll packets from copied companion streams.
Discard them at the output boundary when video is decoded.

### `string lRecoverEncoder`

Last-resort coded recovery: decode the damaged principal video and re-encode it in its own source codec family.
That keeps the output close to the original.
Healthy companion streams are copied, never re-encoded.
The demuxer-side discard/genpts flags travel through `LDossierRepairInput`.

### `if (!lProcessed)`

Nothing is filtered, so the source stream is muxed straight through.
It keeps whatever the Audio settings ask for, Copy included.
No intermediate is written, so no precision is spent on a stream that is not processed.

### `lMux.Append(" -map 0:a? -map -0:a:0?")`

The processed track replaces the one extraction explicitly took (0:a:0).
Every other source track is carried through untouched.
