# LBridgeLeading.cs

## `public static bool LBridgeLeadingNormalize(byte[] lBridgeBytes)`

A copied middle that follows a head bridge begins mid-stream on the source's first interior keyframe.
When that keyframe is an open-GOP CRA it carries RASL leading pictures referencing the discarded pre-cut GOP.
A decoder drops them at a true stream start but not after a concatenated head.
There it fails to build the reference picture set.
Marking only that first CRA as a BLA (broken-link access) sets NoRaslOutputFlag.
The decoder then discards those leading pictures while every interior CRA keeps its own.
The leading pictures fall inside the head bridge's re-encoded range, so nothing user-visible is lost.
The RASL leading-picture NALs stay physically present in the copied middle.
So the marker must be BLA_W_LP (16), not BLA_N_LP (18).
A BLA_N_LP picture shall carry no associated leading pictures.
Relabelling to it leaves a non-conformant stream that lenient decoders tolerate but strict external players reject.

## Inline notes

### `if (lBridgeType != LBridgeLeadingCra)`

The first coded slice is the copy-start keyframe, and only it matters.
