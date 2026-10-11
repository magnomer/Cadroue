# LSalvageExtract.cs
Hash: `b968717446e99603`

## `internal static async Task<IReadOnlyList<string>> LSalvageExtractRun(`

Extracts each planned salvage span beside the output and returns only the files actually delivered.
A span that fails is skipped, so one bad span never costs the others.

## `private static string LSalvageTempResolve(string lSalvageOutputPath)`

Each span is written under a free `.cadsalvage` name first.
A failed extraction thus never overwrites or half-writes the final output path.

## `private static async Task LSalvageTempClear(string lSalvageTemp)`

Deletion retries briefly because the just-exited ffmpeg child may still hold the file.
A temporary that survives every attempt is recorded rather than thrown.

## Inline notes

### `if (lSalvageResult.LEmployerExit == 0`

Fail safe: keep only a span that extracted cleanly and re-probes as real media.
Anything else is deleted so no partial or corrupt file is left behind.

### `return "-hide_banner -nostdin -y -err_detect ignore_err"`

Careful stream copy of one decodable span, keeping the source container and stream layout like the Fix copy stage.
Error tolerance lets the demuxer read past the surrounding damage.
Input seeking avoids decoding the broken file.
