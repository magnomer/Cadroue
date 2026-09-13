# LSidecarFileRecord.cs

## `public List<LFlawKind> LSidecarScannedKinds { get; set; } = new()`

The defect kinds this record authoritatively covers.
A completed scan assesses every kind.
So an absent defect for a listed kind means Clean, a positive verdict rather than the absence of one.
A record whose scan never finished (or a legacy record predating this field) leaves this empty.
Readers then treat it as undiagnosed and rescan rather than reporting a false Clean.
