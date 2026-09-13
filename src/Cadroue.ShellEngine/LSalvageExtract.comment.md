# LSalvageExtract.cs

## Inline notes

### `if (lSalvageResult.LEmployerExit == 0`

Fail safe: keep only a span that extracted cleanly and re-probes as real media.
Anything else is deleted so no partial or corrupt file is left behind.

### `return "-hide_banner -nostdin -y -err_detect ignore_err"`

Careful stream copy of one decodable span, keeping the source container and stream layout like the Fix copy stage.
Error tolerance lets the demuxer read past the surrounding damage.
Input seeking avoids decoding the broken file.
