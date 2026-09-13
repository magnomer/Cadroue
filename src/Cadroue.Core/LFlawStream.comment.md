# LFlawStream.cs

## `private const long LFlawWrapGuard = 8_000_000_000L`

MPEG-TS 33-bit 90 kHz timestamps wrap at 2^33.
A decode-timestamp step that falls back from above this guard is a legal wraparound, not a defect.

## Inline notes

### `bool lFlawAnnexb = lFlawContainer.Contains("mpegts", StringComparison.Ordinal)`

An Annex-B-native container (MPEG-TS, raw elementary stream) must carry start-code framing, so length-prefixed units there are converted with mp4toannexb.
Any other container (ISO-BMFF, Matroska) stores length-prefixed units with out-of-band parameter sets.
So the framing fault is normalized by lifting the in-band parameter sets to extradata for the muxer to reframe.

### `string lFlawFilter = lFlawExtradata ? "dump_extra" : "extract_extradata"`

No out-of-band extradata: derive it from the valid in-band long-term headers, changing only container-side configuration and leaving the packets exact.
Extradata present but inconsistent with the samples: reinsert the stored configuration into the packets, an in-place coded-carriage change.
Never synthesize an unknown parameter set — that would cause silent misdecode.

### `if (lFlawPts is null && lFlawDts is not null)`

A packet with neither timestamp is not reconstructable from timing alone.
It is a decode-recovery case, so it does not raise a timeline defect here.

### `bool lFlawMissingPts = lFlawMissingPtsCount.Any(lFlawEntry`

A stream that carries presentation timestamps on most packets yet drops them on a minority has a reconstructable gap.
That gap is worth regenerating (B-frame reorder).
A stream with no PTS, or only a stray one among packets that overwhelmingly lack it, follows a container convention.
AVI stores presentation order as decode order.
Its presentation timing is not a defect to rebuild.

### `var lFlawFlags = new List<string>()`

genpts regenerates presentation timestamps from decode order.
igndts drops the unreliable decode timestamps so the muxer re-derives them from the authoritative presentation timing.
Both are demuxer flags placed before -i.
Packets stay exact and no start offset is normalized to zero.
