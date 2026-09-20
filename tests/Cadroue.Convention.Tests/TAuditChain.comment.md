# TAuditChain.cs

## `public sealed class TAuditChain`

Keeps every ring on the chain: which ring a ring may name.
A ring reaches the one ring inside it, carries the data of any deeper ring and names nothing outward.
The same chain is audited by `auditstructure.ps1` from its own configuration, and neither reads the other.

## `private const string TAuditChainAudit = "AUDITCHAIN";`

The audit name every report line opens with.

## `private static readonly string[] TAuditChainHeld`

The kinds a ceiling is written for.

## `private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditChainHits`

The hits, bound once and shared by every fact.

## `public void AuditChain_Rings_ReachOneRing()`

No ring reaches more than one ring, and every ring reached is declared.

## `public void AuditChain_Sources_ReachNoDeeperRing()`

No pair holds more files reaching behaviour past the neighbour than its ceiling.

## `public void AuditChain_Sources_NameNoOuterRing()`

No pair holds more files naming an outer ring than its ceiling.

## `public void AuditChain_Sources_HoldSurface()`

Where a surface is written for a pair, no neighbour name outside it is reached.

## `public void AuditChain_Rings_HoldFloor()`

Every ring with a floor holds at least that many source files.
A ring emptied by mistake would otherwise pass every reach fact vacuously.

## `public void AuditChain_Rings_DeclareNoStray()`

No ring declares a type whose name matches a stray pattern written for it.

## `public void AuditChain_Ceiling_MatchesHits()`

No ceiling sits above its count, so a shed file lowers its ceiling.

## `public void AuditChain_Waiver_MatchesSource()`

Every waiver row still matches a hit, so a fixed break deletes its row.
