# LFlawTransport.cs

## Inline notes

### `IReadOnlyDictionary<string, string>? lFlawFormat = LFlaw.LFlawSectionRead(lFlawProbeReport, "FORMAT").FirstOrDefault()`

Transport-layer repair is meaningful only for MPEG-TS/M2TS carriage.
Any other container is NotApplicable and produces no dossier.
