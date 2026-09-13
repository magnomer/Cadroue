# LFlawCoded.cs

## Inline notes

### `string lFlawEvidence = LFlawDamageRead(lFlawDecodeError)`

The diagnostic decode is a software decode.
A single hardware-decoder failure over a clean software decode is not proof of corruption and never reaches this evidence.
Only genuine decode damage that survives container, framing, timing and codec-config diagnosis is a coded defect.
