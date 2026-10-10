# TAuditFault.cs
Hash: `b6f2ce6aeb4ab086`

## `public sealed class TAuditFault`

Counts the catch clauses below Conduct that swallow a fault, so the count can only fall.
A swallowed fault is only recorded or turned into null and never reaches the user.
Each ring has its own ceiling, and an exempt row counts against none.
The same count is audited by `AuditFault.ps1` from its own configuration, and neither reads the other.

## `private const string TAuditFaultAudit = "AUDITFAULT";`

The audit name every report line opens with.

## `private static readonly Lazy<IReadOnlyList<TViolation>> TAuditFaultHits = new(TAuditFaultWalker.TAuditFaultScan);`

The hits, bound once and shared by every fact.

## `public void AuditFault_Catches_HoldWithinCeiling()`

No ring holds more swallowing catch clauses than its ceiling.
Each ring above its ceiling lists every hit it holds.

## `public void AuditFault_Ceiling_MatchesHits()`

A ceiling above its count is stale and fails, so a fixed catch is locked in.

## `public void AuditFault_Exempt_MatchesSource()`

An exempt row that matches no hit fails and must be deleted.

## `private static string TAuditExemptRead(TViolation hit)`

The exempt row naming the file and enclosing method of the hit, or empty when none does.

## `private static List<TViolation> TAuditTallyRead(string ring)`

The hits of one ring that no exempt row covers.

## `private static List<string> TAuditAboveRead()`

One line per ring above its ceiling, followed by its hits.

## `private static List<string> TAuditStaleRead()`

One line per ring whose ceiling sits above its count.
