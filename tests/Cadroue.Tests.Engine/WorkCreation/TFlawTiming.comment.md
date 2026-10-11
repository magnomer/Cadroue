# TFlawTiming.cs
Hash: `3b1a9c00793a4ee0`

## `public void ReorderedPresentation_IsNotATimingDefect()`

B-frame reorder: PTS differs from DTS but DTS stays monotonic, which is legal.

## `public void MissingPresentation_RegeneratesWithGenpts()`

A stream that presents some timestamps yet drops others has a reconstructable gap.
The genpts flag fills it from decode order.

## `public void UniformlyAbsentPresentation_IsContainerConvention()`

Every packet lacks a PTS (AVI without reordering).
Presentation order equals decode order, a container convention rather than a defect to regenerate.

## `public void StrayPresentationAmongAbsent_IsContainerConvention()`

One packet out of many carries a PTS while the rest do not.
Presentation timing is not the stream's norm, so the lone stamp is a container artifact, not a fillable gap.

## `public void WraparoundDecode_IsNotATimingDefect()`

MPEG-TS 33-bit wraparound: DTS falls back from near 2^33 to zero, which is legal.

## `public void PerStreamOrdering_IgnoresCrossStreamInterleave()`

Two streams interleaved: each stream's own DTS is monotonic though the report alternates between them.
No defect.
