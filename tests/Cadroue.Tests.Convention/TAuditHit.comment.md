# TAuditHit.cs
Hash: `695336cb3b584873`

## `internal sealed record TAuditHit`

One name a source names, with the ring of the file and the verdict a walker gave it.
`TAuditHitKind` is the check the hit counts under and `TAuditHitTarget` the ring, namespace or row named.
`TAuditHitName` is the type name, the namespace or the eavesdropping row the hit stands on.

## `public string TAuditPairRead()`

The `kind:ring>target` key the hit counts under.

## `public string TAuditExemptRead(IReadOnlyList<string> exempt)`

The exempt row clearing the hit, or an empty string.
A row is a path, a colon and the name the hit stands on.
A path ending in `/*` covers every file under it.
