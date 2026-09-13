# LSidecarCacheStore.cs

## Inline notes

### `LSidecarScannedKinds = Enum.GetValues<LFlawKind>().ToList()`

A diagnosis record is only ever written from a scan that ran to completion.
A completed scan assesses every kind.
Recording the full kind set makes each clean kind a positive Clean verdict rather than a mere absence.

### `if (lSidecarDiagnosis.LSidecarScannedKinds.Count == 0)`

A record that carries no scanned-kind set never completed a diagnosis (or predates the field).
Its empty dossier list would otherwise read as a false Clean for every kind.
Treat it as undiagnosed so the caller rescans instead of trusting it.
