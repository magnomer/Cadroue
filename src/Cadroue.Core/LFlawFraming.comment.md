# LFlawFraming.cs

## Inline notes

### `bool lFlawAnnexb = lFlawContainer.Contains("mpegts", StringComparison.Ordinal)`

A container that is Annex-B native (MPEG-TS and raw elementary streams) must carry start-code framing.
There the framing fault is fixed with the mp4toannexb filter for the codec.
Any other container stores length-prefixed units, and the fix is to extract the in-band parameter sets to extradata.
