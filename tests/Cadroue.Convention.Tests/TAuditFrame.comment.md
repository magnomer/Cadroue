# TAuditFrame.cs

## `public sealed class TAuditFrame`

Keeps every pure ring inside its frame: what a ring may name outside the project.
A pure ring names only the framework namespaces the frame lists and touches no ambient member.
The clock, the environment, the disk and the console arrive through a port or not at all.

## `private const string TAuditFrameAudit = "AUDITFRAME";`

The audit name every report line opens with.

## `private static readonly string[] TAuditFrameHeld`

The kinds a ceiling is written for.

## `private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditFrameHits`

The hits, bound once and shared by every fact.

## `public void AuditFrame_PureSources_StayInFrame()`

No pair holds more files naming a namespace outside the frame than its ceiling.

## `public void AuditFrame_PureSources_TouchNoAmbient()`

No pair holds more files touching an ambient member than its ceiling.

## `public void AuditFrame_Ceiling_MatchesHits()`

No ceiling sits above its count, so a shed file lowers its ceiling.

## `public void AuditFrame_Waiver_MatchesSource()`

Every waiver row still matches a hit, so a fixed break deletes its row.
