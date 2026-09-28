# TAuditChain.cs

## `public sealed class TAuditChain`

Keeps every ring on the chain: which ring a ring may name.
A ring reaches the one ring inside it, carries the data of any deeper ring and names nothing outward.
Above the cut, a UI ring names only its neighbour, data included.
A pair is counted in names, so a file already crossing cannot add crossings unseen.
The same chain is audited by `auditstructure.ps1` from its own configuration, and neither reads the other.

## `private const string TAuditChainAudit = "AUDITCHAIN";`

The audit name every report line opens with.

## `private static readonly string[] TAuditChainHeld`

The kinds a ceiling is written for.

## `private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditChainHits`

The tree hits, the surface hits and the seal hits, bound once and shared by every fact.

## `public void AuditChain_Rings_ReachOneRing()`

No ring reaches more than one ring or itself, and every ring reached is declared.

## `public void AuditChain_Reach_MatchesRingTable()`

Every ring's reach equals its project references, and every walked ring is a project.
A ring dropped from the reach table would stop being walked, so the table is tied to the projects.

## `public void AuditChain_Cut_MatchesShellRoots()`

The cut holds exactly the UI rings the driver and surface audits walk.

## `public void AuditChain_Sources_ReachNoDeeperRing()`

No pair holds more names reaching behaviour past the neighbour than its ceiling.
A method called on a deeper record is behaviour, even though the record is data.

## `public void AuditChain_Folders_HoldNoBannedWord()`

No bound source under a folder of `TAuditChainBanned` names a word banned there.
Opening a vault session belongs to a clerk, so the shell engine may not name it.

## `public void AuditChain_Sources_NameNoOuterRing()`

No pair holds more names from an outer ring than its ceiling.

## `public void AuditChain_Sources_CrossNoCut()`

No pair holds more names a UI ring takes from below the cut than its ceiling.
A `using` of a namespace below the cut counts as a name.

## `public void AuditChain_SurfaceSignatures_NameNoDeeperType()`

No pair holds more surface signature types from below Conduct than its ceiling.
Methods, operators, conversions, properties, events, fields and nested public types are all read.

## `public void AuditChain_SealedSurface_NamesNoEngine()`

No pair holds more sealed member types from below the neighbour than its ceiling.
The ceilings fall as each job seals a controller, until none is left to count.

## `public void AuditChain_Sources_HoldSurface()`

Where a surface is written for a pair, no neighbour name outside it is reached.

## `public void AuditChain_Rings_HoldFloor()`

Every ring with a floor holds at least that many source files.
A ring emptied by mistake would otherwise pass every reach fact vacuously.

## `public void AuditChain_Rings_DeclareNoStray()`

No ring declares a type whose name matches a stray pattern written for it.

## `public void AuditChain_Ceiling_MatchesHits()`

No ceiling sits above its count, so a shed name lowers its ceiling.

## `public void AuditChain_Waiver_MatchesSource()`

Every waiver row still matches a hit, so a fixed break deletes its row.
