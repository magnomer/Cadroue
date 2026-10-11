# LEncodeVideo.cs
Hash: `f021a64de973b1f4`


## `internal static partial class LEncodeVideo`

Appends the video part of an ffmpeg command line for one output.
It chooses between stream copy and a real encoder, then adds the filters.

## `internal static void LEncodeVideoAppend(`

Stream copy is used only when the mode is Copy and no crop or video step needs re-encoding.
Every other case falls through to the encoder path.
Copy also zeroes negative timestamps so cut points start cleanly.

## `internal static void LEncodeEncoderAppend(`

An unnamed encoder emits no codec, so ffmpeg picks its default and only filters are added.
Extras that are empty, `none`, or conflict with the chosen rate-control mode are skipped.
The speed option is emitted only when the encoder declares one and a preset is set.
The pass arguments are emitted only for a positive pass number.

## `private static void LEncodeModeAppend(StringBuilder lArguments, LCapabilityMode lMode, string lQualityValue)`

A blank quality falls back to the encoder's declared default.
The mode argument substitutes the quality token, so one template serves every value.

## `internal static int LEncodePassRead(LWorkItem lWorkItem, LEncoding lOutput)`

Stream copy and an unnamed encoder always need exactly one pass.
Otherwise the pass count comes from the selected rate-control mode.

## `internal static string LEncodePassFormat(LWorkItem lWorkItem)`

Two-pass statistics live in a per-item folder under the depot pass folder, so concurrent items never collide.
That folder comes through LDepotEngine, so the encoder never names the Infrastructure depot.
The folder is created here because ffmpeg does not create it.
The returned value is a log-file prefix, not an existing file.

## `internal static bool LEncodeVideoCheck(LWorkItem lWorkItem, LEncoding lOutput) =>`

Reports whether the video stream must be re-encoded rather than copied.
A crop, a video adjustment, or a real resize forces re-encoding.

## `private static bool LEncodeSizeCheck(LEncodingVideo lVideo) =>`

A size only counts in Encode mode, because Copy cannot resize.
