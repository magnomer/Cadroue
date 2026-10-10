# TAuditPurity.cs
Hash: `9acbad32aaa34359`

## `public sealed class TAuditPurity`

Keeps every pure ring inside its frame, which limits what a ring may name outside the project.
A pure ring names only the framework namespaces the frame lists and touches no eavesdropping member.
The clock, the environment, the disk and the console arrive through a port or not at all.
The same purity is audited by `AuditStructure.ps1` from its own configuration, and neither reads the other.

## `private const string TAuditPurityAudit = "AUDITPURITY";`

The audit name every report line opens with.

## `private static readonly string[] TAuditPurityHeld`

The kinds a ceiling is written for.

## `private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditPurityHits`

The hits, bound once and shared by every fact.

## `public void AuditPurity_PureSources_HoldNoForaging()`

No pair holds more `Foraging` hits, naming a namespace outside the frame, than its ceiling.

## `public void AuditPurity_PureSources_HoldNoEavesdropping()`

No pair holds more `Eavesdropping` hits, touching an eavesdropping member, than its ceiling.

## `public void AuditPurity_Ceiling_MatchesHits()`

No ceiling sits above its count, so a shed hit lowers its ceiling.

## `public void AuditPurity_Exempt_MatchesSource()`

Every exempt row still matches a hit, so a fixed break deletes its row.
