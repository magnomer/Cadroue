# TAuditFaultSetting.cs
Hash: `2c4838d8b1815d59`

## `internal static class TAuditFaultSetting`

Hand-written and tracked.
The rings, the ceilings and the exempt rows of the fault audit live here.
`AuditFault.ps1` reads its own AuditFault.json and never writes this file.

## `public const int TAuditGeneration = 21;`

Numbers the revision of the fault-audit settings this file holds.

## `public static readonly string[] TAuditFaultRing`

The projects below Conduct whose catch clauses are walked, each under `src/`.
A fault swallowed here never reaches the user, since only Conduct and above can tell them.

## `public static readonly IReadOnlyDictionary<string, int> TAuditFaultCeiling`

The `Swallowing` hit count each ring may reach, exempt rows left out.
A count above its ceiling fails the fact.
A ceiling above the count is stale and fails too.
Lower a ceiling when a catch hands its fault out or is removed.
Never raise one to admit a new one.

## `public static readonly string[] TAuditFaultExempt`

Rows of `path:EnclosingMethod` whose catch clauses swallow by design and count against no ceiling.
`LWorkspacePostureRead` reads an unreadable posture as none.
`LWorkspaceFallbackSave` is a best-effort save that records its fault and goes on.
A row that matches no hit fails, so no row outlives its catch.
