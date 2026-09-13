# TFlawCoded.cs

## Inline notes

### `Assert.Null(TInterface.TFlawCodedResolve("[h264 @ 0x1] Invalid NAL unit size (-1 > 123)."))`

A framing/config fault is repaired without decode.
It is not decode damage and must not be escalated to the last-resort re-encode item.
