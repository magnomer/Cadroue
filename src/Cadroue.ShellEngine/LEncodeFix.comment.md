# LEncodeFix.cs
Hash: `d7e599d08ce7fe6e`

## `public static IReadOnlyList<LEncodeStage> LEncodeFixBuild(`

One Fix pass becomes an ordered stage list: optional copy, repairs, then validation.
The copy stage exists only when the caller asks for it.
Repair stages follow the remedy plan order, so precedence is decided in one place.
Validation always closes the list and reads the output path.

## `internal static string LEncodeRecoverBuild(LWorkItem lWorkItem)`

Last-resort recovery re-encodes video only, so the output stays close to the source.
The encoder follows the source codec family and falls back to libx264.
Every other stream is copied, never re-encoded.
Fast start is added only for containers that support it.

## `internal static string LEncodeRemedyBuild(`

Non-recovery categories never re-encode, since they only rewrap or filter the stream.
Transport repair forces an MPEG-TS container regardless of the output extension.
The bitstream argument comes from the dossier and is trusted as given.

## `internal static string LEncodeRepairBuild(string lInputPath, string lInput, string lRemedy, string lOutputPath)`

Input options must precede the input path, so they are placed before it.
The remedy text is inserted verbatim and expected to be already formed.

## Inline notes

### `if (lFixAction.LRemedyDossier.LDossierRepair == LFlawFfvone.LFlawReport)`

A report-only dossier is detection-only, because no ffmpeg stage can correct it.
It produces no repair stage.
