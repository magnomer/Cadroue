# LEncodeVideoPixel.cs

## Inline notes

### `"h264_qsv" => "nv12",`

Delivery formats stay within their broadly decoded 4:2:0 profiles.
Preserve received high bit depth only where the codec and encoder have a mainstream 10-bit profile.
Never recover properties from an earlier lineage source.

### `"prores" or "prores_aw" or "prores_ks" => lAlpha ? "yuva444p10le" : "yuv422p10le",`

Professional intermediates use their native baseline rather than inheriting an arbitrary decoder layout.
Preserve alpha when the received source has it.

### `"ffv1" or "jpeg2000" or "libopenjpeg" => string.Empty,`

FFV1 and JPEG 2000 intentionally retain FFmpeg's received-format negotiation: their purpose is archival fidelity and their supported layouts are extensive.
