# TFlawTiming.cs

## Inline notes

### `Assert.Null(TInterface.TFlawTimingResolve(`

B-frame reorder: PTS differs from DTS but DTS stays monotonic, which is legal.

### `LDossier? dossier = TInterface.TFlawTimingResolve(`

A stream that presents some timestamps yet drops others has a reconstructable gap.
genpts fills it from decode order.

### `Assert.Null(TInterface.TFlawTimingResolve(`

Every packet lacks a PTS (AVI without reordering).
Presentation order equals decode order, a container convention rather than a defect to regenerate.

### `Assert.Null(TInterface.TFlawTimingResolve(`

One packet out of many carries a PTS while the rest do not.
Presentation timing is not the stream's norm, so the lone stamp is a container artifact, not a fillable gap.

### `Assert.Null(TInterface.TFlawTimingResolve(`

MPEG-TS 33-bit wraparound: DTS falls back from near 2^33 to zero, which is legal.

### `Assert.Null(TInterface.TFlawTimingResolve(`

Two streams interleaved: each stream's own DTS is monotonic though the report alternates between them.
No defect.
