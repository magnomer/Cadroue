# TAuditCensus.cs
Hash: `1927cbc5ad714acf`

## `public sealed class TAuditCensus`

Keeps the census of every ring.
It counts what a ring holds, not what it names.
A ring holds no word listed for its folder, enough source files and no squatting type.
The same census is audited by `AuditStructure.ps1` from its own configuration, and neither reads the other.

## `private const string TAuditCensusAudit = "AUDITCENSUS";`

The audit name every report line opens with.

## `public void AuditCensus_Folders_HoldNoSmuggling()`

No bound source under a folder of `TAuditSmugglingWord` names a word listed for it.
Opening a vault session belongs to a clerk, so the shell engine may not name it.

## `public void AuditCensus_Rings_HoldNoHollowing()`

Every ring with a floor holds at least that many source files.
A ring emptied by mistake would otherwise pass every border fact vacuously.

## `public void AuditCensus_Rings_HoldNoSquatting()`

No ring declares a type whose name matches a squatting pattern written for it.
